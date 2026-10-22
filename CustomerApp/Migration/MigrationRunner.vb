Imports System.Text
Imports Microsoft.Data.SqlClient

''' <summary>
''' マイグレーションスクリプトをデータベースに適用する。
''' </summary>
Public Class MigrationRunner

    Private Const LockResource As String = "CustomerApp.Migrations"
    Private Const LockTimeoutMilliseconds As Integer = 60000
    Private Const CommandTimeoutSeconds As Integer = 300

    Private ReadOnly _connectionString As String
    Private ReadOnly _scripts As List(Of MigrationScript)

    Public Sub New(connectionString As String, scripts As IEnumerable(Of MigrationScript))
        If String.IsNullOrWhiteSpace(connectionString) Then
            Throw New ArgumentException("接続文字列が空です。", NameOf(connectionString))
        End If

        _connectionString = connectionString
        _scripts = scripts.OrderBy(Function(s) s.Version).ToList()

        Dim duplicated = _scripts.GroupBy(Function(s) s.Version).FirstOrDefault(Function(g) g.Count() > 1)
        If duplicated IsNot Nothing Then
            Throw New MigrationException($"マイグレーションの番号が重複しています: V{duplicated.Key:000}")
        End If
    End Sub

    ' ---- データベースの作成 ----

    ''' <summary>
    ''' 接続文字列で指定されたデータベースが無ければ作成する。
    ''' </summary>
    Public Function EnsureDatabaseExists() As Boolean
        Dim builder As New SqlConnectionStringBuilder(_connectionString)
        Dim databaseName As String = builder.InitialCatalog

        If String.IsNullOrWhiteSpace(databaseName) Then
            Throw New MigrationException("接続文字列にデータベース名(Database=...)が指定されていません。")
        End If

        builder.InitialCatalog = "master"

        Using conn As New SqlConnection(builder.ConnectionString)
            conn.Open()

            Using exists As New SqlCommand("SELECT DB_ID(@name)", conn)
                exists.Parameters.Add("@name", SqlDbType.NVarChar, 128).Value = databaseName

                ' DB_IDは、データベースがないとNULL(DBNull)を返す
                If Not (exists.ExecuteScalar() Is DBNull.Value) Then
                    Return False
                End If
            End Using

            Using create As New SqlCommand("DECLARE @sql NVARCHAR(MAX) = N'CREATE DATABASE ' + QUOTENAME(@name); EXEC (@sql);", conn)
                create.Parameters.Add("@name", SqlDbType.NVarChar, 128).Value = databaseName
                create.CommandTimeout = CommandTimeoutSeconds
                create.ExecuteNonQuery()
            End Using
        End Using

        Return True
    End Function

    ' ---- 状態の確認 ----

    ''' <summary>
    ''' 未適用のスクリプトを番号順に返す。
    ''' 適用済のスクリプトの書き換えなど、異常があればMigrationExceptionをスロー。
    ''' </summary>
    Public Function GetPendingScripts() As List(Of MigrationScript)
        Using conn As SqlConnection = OpenConnection()
            ' 履歴テーブルがない = まだ1つも適用していない
            If Not HistoryTableExists(conn) Then
                Return _scripts.ToList()
            End If

            Return FindPending(ReadApplied(conn))
        End Using
    End Function

    ''' <summary>
    ''' 適用済の最大の番号。
    ''' なにも適用されていない場合は0
    ''' </summary>
    Public Function GetCurrentVersion() As Integer
        Using conn As SqlConnection = OpenConnection()
            If Not HistoryTableExists(conn) Then
                Return 0
            End If

            Using cmd As New SqlCommand("SELECT ISNULL(MAX(Version), 0) FROM dbo.SchemaVersions", conn)
                Return Convert.ToInt32(cmd.ExecuteScalar())
            End Using
        End Using
    End Function

    ' ---- 適用 ----

    ''' <summary>
    ''' 未適用のスクリプトを、番号順にすべて適用する。
    ''' 今回適用したスクリプトの一覧を返す。
    ''' </summary>
    Public Function ApplyPending() As List(Of MigrationScript)
        Dim appliedNow As New List(Of MigrationScript)

        Using conn As SqlConnection = OpenConnection()
            AcquireLock(conn)

            Try
                EnsureHistoryTable(conn)

                ' ロックを取った後で履歴を読む。
                For Each script As MigrationScript In FindPending(ReadApplied(conn))
                    ApplyOne(conn, script)
                    appliedNow.Add(script)
                Next
            Finally
                ReleaseLock(conn)
            End Try
        End Using

        Return appliedNow
    End Function

    ''' <summary>
    ''' 1つのスクリプトを、1つのトランザクションで適用し、履歴に記録する。
    ''' </summary>
    Private Sub ApplyOne(conn As SqlConnection, script As MigrationScript)
        Using tx As SqlTransaction = conn.BeginTransaction()
            Try
                ' スクリプトをGOで分けて、バッチごとに実行する。
                For Each batch As String In SqlBatchSplitter.Split(script.Sql)
                    Using cmd As New SqlCommand(batch, conn, tx)
                        cmd.CommandTimeout = CommandTimeoutSeconds
                        cmd.ExecuteNonQuery()
                    End Using
                Next

                ' 履歴の記録も同じトランザクションに入れる。
                Using cmd As New SqlCommand("INSERT INTO dbo.SchemaVersions (Version, ScriptName, CheckSum) VALUES (@version, @name, @checksum)", conn, tx)
                    cmd.Parameters.Add("@version", SqlDbType.Int).Value = script.Version
                    cmd.Parameters.Add("@name", SqlDbType.NVarChar, 200).Value = script.Name
                    cmd.Parameters.Add("@checksum", SqlDbType.Char, 64).Value = script.Checksum
                    cmd.ExecuteNonQuery()
                End Using

                tx.Commit()
            Catch ex As Exception
                Try
                    tx.Rollback()
                Catch
                    ' すでにロールバック済み(エラーでトランザクションが自動で取り消された)の場合は、何もしない
                End Try

                Throw New MigrationException($"マイグレーション{script.Name}の適用に失敗しました。このスクリプトの変更は全て取り消されました。", ex)
            End Try
        End Using
    End Sub

    ' ---- 履歴の読み取りと検証 ----

    Private Class AppliedRecord
        Public Property Version As Integer
        Public Property Name As String = String.Empty
        Public Property CheckSum As String = String.Empty
    End Class

    Private Shared Function ReadApplied(conn As SqlConnection) As Dictionary(Of Integer, AppliedRecord)
        Dim result As New Dictionary(Of Integer, AppliedRecord)

        Using cmd As New SqlCommand("SELECT Version, ScriptName, CheckSum FROM dbo.SchemaVersions", conn)
            Using reader As SqlDataReader = cmd.ExecuteReader()
                While reader.Read()
                    result(reader.GetInt32(0)) = New AppliedRecord With {
                        .Version = reader.GetInt32(0),
                        .Name = reader.GetString(1),
                        .CheckSum = reader.GetString(2)
                    }
                End While
            End Using
        End Using

        Return result
    End Function

    Private Function FindPending(applied As Dictionary(Of Integer, AppliedRecord)) As List(Of MigrationScript)
        ' ①適用済なのに、このアプリが知らないスクリプト = データベースが、アプリより新しい
        For Each record As AppliedRecord In applied.Values
            Dim recordVersion As Integer = record.Version

            If Not _scripts.Any(Function(s) s.Version = recordVersion) Then
                Throw New MigrationException($"データベースが、このアプリより新しいバージョンです。(適用済: {record.Name}){vbCrLf}" &
                                             $"アプリを最新版に更新してください。古いバージョンのアプリでは、このデータベースを使えません。")
            End If
        Next

        ' ②適用済のスクリプトが後から書き換えられていないか
        For Each script As MigrationScript In _scripts
            Dim record As AppliedRecord = Nothing

            If applied.TryGetValue(script.Version, record) AndAlso
               Not String.Equals(record.CheckSum, script.Checksum, StringComparison.OrdinalIgnoreCase) Then
                Throw New MigrationException($"適用済のマイグレーション{script.Name}の内容が、適用後に変更されています。{vbCrLf}" &
                                             $"適用済のスクリプトは書き換えられません。変更したいときは、新しい番号のスクリプトを追加してください。")
            End If
        Next

        ' ③履歴にないものが未適用
        Return _scripts.Where(Function(s) Not applied.ContainsKey(s.Version)).ToList()
    End Function

    ' ---- 履歴テーブル ----

    Private Shared Function HistoryTableExists(conn As SqlConnection) As Boolean
        Using cmd As New SqlCommand("SELECT CASE WHEN OBJECT_ID(N'dbo.SchemaVersions', N'U') IS NULL THEN 0 ELSE 1 END", conn)
            Return Convert.ToInt32(cmd.ExecuteScalar()) = 1
        End Using
    End Function

    ''' <summary>
    ''' 履歴テーブルがなければ作る。
    ''' </summary>
    Private Shared Sub EnsureHistoryTable(conn As SqlConnection)
        Dim sql As New StringBuilder()
        sql.Append("IF OBJECT_ID('dbo.SchemaVersions', N'U') IS NULL ")
        sql.Append("CREATE TABLE dbo.SchemaVersions (")
        sql.Append("    Version    INT           NOT NULL CONSTRAINT PK_SchemaVersions PRIMARY KEY, ")
        sql.Append("    ScriptName NVARCHAR(200) NOT NULL, ")
        sql.Append("    CheckSum   CHAR(64)      NOT NULL, ")
        sql.Append("    AppliedAt  DATETIME2     NOT NULL CONSTRAINT DF_SchemaVersions_AppliedAt DEFAULT SYSDATETIME(), ")
        sql.Append("    AppliedBy  NVARCHAR(128) NOT NULL CONSTRAINT DF_SchemaVersions_AppliedBy DEFAULT SUSER_SNAME() ")
        sql.Append(")")

        Using cmd As New SqlCommand(sql.ToString(), conn)
            cmd.ExecuteNonQuery()
        End Using
    End Sub

    ' ---- 接続・ロック ----

    Private Function OpenConnection() As SqlConnection
        Dim conn As New SqlConnection(_connectionString)
        conn.Open()
        Return conn
    End Function

    ''' <summary>
    ''' アプリケーションロックを取る。
    ''' 他のアプリがマイグレーション中なら、終わるまで待つ。
    ''' </summary>
    Private Shared Sub AcquireLock(conn As SqlConnection)
        Using cmd As New SqlCommand("sp_getapplock", conn)
            cmd.CommandType = CommandType.StoredProcedure
            cmd.Parameters.Add("@Resource", SqlDbType.NVarChar, 255).Value = LockResource
            cmd.Parameters.Add("@LockMode", SqlDbType.NVarChar, 32).Value = "Exclusive"
            cmd.Parameters.Add("@LockOwner", SqlDbType.NVarChar, 32).Value = "Session"
            cmd.Parameters.Add("@LockTimeout", SqlDbType.Int).Value = LockTimeoutMilliseconds

            Dim returnValue As SqlParameter = cmd.Parameters.Add("@Returnvalue", SqlDbType.Int)
            returnValue.Direction = ParameterDirection.ReturnValue

            cmd.ExecuteNonQuery()

            ' 0以上 = ロックを取れた。
            If Convert.ToInt32(returnValue.Value) < 0 Then
                Throw New MigrationException("他のアプリがマイグレーションを実行中のため、待ち時間内に開始できませんでした。しばらくしてから、もう一度起動してください。")
            End If
        End Using
    End Sub

    Private Shared Sub ReleaseLock(conn As SqlConnection)
        Try
            Using cmd As New SqlCommand("sp_releaseapplock", conn)
                cmd.CommandType = CommandType.StoredProcedure
                cmd.Parameters.Add("@Resource", SqlDbType.NVarChar, 255).Value = LockResource
                cmd.Parameters.Add("@LockOwner", SqlDbType.NVarChar, 32).Value = "Session"
                cmd.ExecuteNonQuery()
            End Using
        Catch
            ' 接続が切れている場合など。ロックは接続を閉じれば自動で解放されるので、ここで失敗しても問題ない
        End Try
    End Sub

End Class
