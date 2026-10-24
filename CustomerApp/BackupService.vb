Imports System.IO
Imports DocumentFormat.OpenXml.Vml.Spreadsheet
Imports Microsoft.Data.SqlClient

''' <summary>
''' バックアップ・復元で起きた例外
''' </summary>
Public Class BackupException
    Inherits Exception

    Public Sub New(message As String)
        MyBase.New(message)
    End Sub

    Public Sub New(message As String, innerException As Exception)
        MyBase.New(message, innerException)
    End Sub

End Class

''' <summary>
''' バックアップファイルの中身
''' </summary>
Public Class BackupInfo

    ''' <summary>
    ''' バックアップ元のデータベース名
    ''' </summary>
    Public Property DatabaseName As String = String.Empty

    ''' <summary>
    ''' バックアップを取ったサーバー名
    ''' </summary>
    Public Property ServerName As String = String.Empty

    ''' <summary>
    ''' バックアップが完了した日時
    ''' </summary>
    Public Property FinishDate As DateTime

    ''' <summary>
    ''' ファイルの中で復元に使うバックアップの位置
    ''' </summary>
    Public Property Position As Integer = 1

End Class

''' <summary>
''' サーバーの情報
''' </summary>
Public Class BackupServerInfo

    ''' <summary>
    ''' SQL Serverの既定のバックアップ先フォルダ、不明な場合はNothing
    ''' </summary>
    Public Property DefaultBackupFolder As String

    ''' <summary>
    ''' SQL Serverがこのアプリと同じPCで動いているか
    ''' </summary>
    Public Property IsLocal As Boolean

End Class

''' <summary>
''' データベースのバックアップと復元
''' </summary>
Public Class BackupService

    Private Const CommandTimeoutSeconds As Integer = 0

    Private ReadOnly _masterConnectionString As String
    Private ReadOnly _databaseName As String
    Private ReadOnly _dataSource As String

    Public Sub New(connectionString As String)
        Dim builder As New SqlConnectionStringBuilder(connectionString)

        If String.IsNullOrWhiteSpace(builder.InitialCatalog) Then
            Throw New ArgumentException("接続文字列にデータベース名が指定されていません。", NameOf(connectionString))
        End If

        _databaseName = builder.InitialCatalog
        _dataSource = builder.DataSource
        builder.InitialCatalog = "master"
        _masterConnectionString = builder.ConnectionString
    End Sub

    Public ReadOnly Property DatabaseName As String
        Get
            Return _databaseName
        End Get
    End Property

    Public ReadOnly Property DataSource As String
        Get
            Return _dataSource
        End Get
    End Property

    ' ---- 部品(テストしやすいよう Public Shared ----

    ''' <summary>
    ''' データベース名等を、[ ]で安全に囲む(] は ]] にする)。
    ''' 名前の位置にはパラメータが使えないため。
    ''' </summary>
    Public Shared Function QuoteName(name As String) As String
        Return $"[{name.Replace("]", "]]")}]"
    End Function

    ''' <summary>
    ''' バックアップのファイル名候補
    ''' 例: CustomerDb_20261001_180000.bak
    ''' </summary>
    Public Shared Function SuggestFileName(databaseName As String, [when] As DateTime, Optional suffix As String = "") As String
        Dim safe As String = databaseName
        For Each ch As Char In Path.GetInvalidFileNameChars()
            safe = safe.Replace(ch, "_"c)
        Next

        Return $"{safe}{suffix}_{[when]:yyyyMMdd_HHmmss}.bak"
    End Function

    ''' <summary>
    ''' パスの形式を調べる。
    ''' 問題があれば、メッセージを返す。
    ''' </summary>
    Public Shared Function ValidatePath(path As String) As String
        If String.IsNullOrWhiteSpace(path) Then
            Return "ファイルのパスを入力してください。"
        End If

        Dim trimmed As String = path.Trim()
        If trimmed.Length > 260 Then
            Return "パスが長すぎます。(260文字まで)"
        End If

        If trimmed.IndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0 OrElse
           trimmed.IndexOfAny({"*"c, "?"c, "<"c, ">"c, "|"c, """"c}) >= 0 Then
            Return "パスに使えない文字が含まれています。"
        End If

        If Not (System.IO.Path.IsPathRooted(trimmed)) Then
            Return "フォルダまで含めた完全なパスを入力してください。(例: C:\Backup\CustomerDb.bak)"
        End If

        If Not trimmed.EndsWith(".bak", StringComparison.OrdinalIgnoreCase) Then
            Return "ファイルの拡張子は.bakにしてください。"
        End If

        Return Nothing
    End Function

    Private Shared Sub EnsureValidPath(path As String)
        Dim problem As String = ValidatePath(path)
        If problem IsNot Nothing Then
            Throw New BackupException(problem)
        End If
    End Sub

    ''' <summary>
    ''' SQL Serverのエラーを利用者に伝わる説明に直す。
    ''' </summary>
    Private Shared Function Translate(ex As SqlException, action As String) As BackupException
        Dim reason As String

        Select Case ex.Number
            Case 3201, 3203
                reason = "バックアップファイルを開けません。ファイルの場所が正しいか、SQL Serverのサービスアカウントが" &
                         "そのフォルダを読み書きできるかを確認してください。(パスはSQL Serverが動いているPC上のものです)"
            Case 3013, 3241, 3242, 3243, 3183, 3282
                reason = "バックアップファイルが壊れているか、バックアップではないファイルです。"
            Case 3154
                reason = "このバックアップは別のデータベースのものです。"
            Case 3169, 3167
                reason = "このバックアップはお使いのSQL Serverより新しいバージョンで作られているため、復元できません。"
            Case 229, 262, 300, 15247
                reason = "この操作は行う権限がありません。" &
                         "バックアップにはdb_backupoperator、復元にはdbcreator(またはsysadmin)の権限が必要です。"
            Case 3101
                reason = "データベースが使用中で排他アクセスを取れませんでした。他のアプリや画面を閉じてからやり直してください。"
            Case Else
                reason = ex.Message
        End Select

        Return New BackupException($"{action}に失敗しました。{reason}", ex)
    End Function

    ' ---- サーバー情報 ----

    Public Async Function GetServerInfoAsync() As Task(Of BackupServerInfo)
        Try
            Using conn As New SqlConnection(_masterConnectionString)
                Await conn.OpenAsync()

                Using cmd As New SqlCommand(
                    "SELECT CONVERT(NVARCHAR(512), SERVERPROPERTY('InstanceDefaultBackupPath')), " &
                    "CONVERT(NVARCHAR(128), SERVERPROPERTY('MachineName'))", conn)

                    Using reader As SqlDataReader = Await cmd.ExecuteReaderAsync()
                        Dim info As New BackupServerInfo()

                        If Await reader.ReadAsync() Then
                            If Not reader.IsDBNull(0) Then
                                info.DefaultBackupFolder = reader.GetString(0)
                            End If

                            If Not reader.IsDBNull(1) Then
                                info.IsLocal = String.Equals(reader.GetString(1), Environment.MachineName, StringComparison.OrdinalIgnoreCase)
                            End If
                        End If

                        Return info
                    End Using
                End Using
            End Using
        Catch ex As SqlException
            Throw Translate(ex, "サーバーの情報の取得")
        End Try
    End Function

    ' ---- バックアップ ----

    ''' <summary>
    ''' バックアップを取り、すぐに検証する。
    ''' </summary>
    Public Async Function BackupAsync(path As String) As Task
        EnsureValidPath(path)
        path = path.Trim()

        Try
            Using conn As New SqlConnection(_masterConnectionString)
                Await conn.OpenAsync()
                Await BackupCoreAsync(conn, _databaseName, path)
            End Using

            Await VerifyAsync(path, 1)
        Catch ex As SqlException
            Throw Translate(ex, "バックアップ")
        End Try
    End Function

    Private Shared Async Function BackupCoreAsync(conn As SqlConnection, databaseName As String, path As String) As Task
        ' INIT      = ファイルの中身を置き換える。
        ' CHECKSUM  = 書き込み時にチェックサムを付ける。
        ' COPY_ONLY = 他のバックアップ運用に影響しない。
        Dim sql As String = $"BACKUP DATABASE {QuoteName(databaseName)} TO DISK = @path WITH INIT, CHECKSUM, COPY_ONLY"

        Using cmd As New SqlCommand(sql, conn)
            cmd.CommandTimeout = CommandTimeoutSeconds
            cmd.Parameters.Add("@path", SqlDbType.NVarChar, 260).Value = path
            Await cmd.ExecuteNonQueryAsync()
        End Using
    End Function

    ''' <summary>
    ''' ファイルが読めて壊れていないことを確認する。
    ''' </summary>
    Private Async Function VerifyAsync(path As String, position As Integer) As Task
        Using conn As New SqlConnection(_masterConnectionString)
            Await conn.OpenAsync()

            Using cmd As New SqlCommand("RESTORE VERIFYONLY FROM DISK = @path WITH FILE = @file, CHECKSUM", conn)
                cmd.CommandTimeout = CommandTimeoutSeconds
                cmd.Parameters.Add("@path", SqlDbType.NVarChar, 260).Value = path
                cmd.Parameters.Add("@file", SqlDbType.Int).Value = position
                Await cmd.ExecuteNonQueryAsync()
            End Using
        End Using
    End Function

    ' ---- バックアップファイルの検査 ----

    Public Async Function ReadInfoAsync(path As String) As Task(Of BackupInfo)
        EnsureValidPath(path)
        path = path.Trim()

        Try
            Using conn As New SqlConnection(_masterConnectionString)
                Await conn.OpenAsync()

                Using cmd As New SqlCommand("RESTORE HEADERONLY FROM DISK = @path", conn)
                    cmd.CommandTimeout = CommandTimeoutSeconds
                    cmd.Parameters.Add("@path", SqlDbType.NVarChar, 260).Value = path

                    Dim last As BackupInfo = Nothing
                    Dim lastType As Integer = 0

                    Using reader As SqlDataReader = Await cmd.ExecuteReaderAsync()
                        While Await reader.ReadAsync()
                            lastType = Convert.ToInt32(reader("BackupType"))
                            last = New BackupInfo With {
                                .DatabaseName = Convert.ToString(reader("DatabaseName")),
                                .ServerName = Convert.ToString(reader("ServerName")),
                                .FinishDate = Convert.ToDateTime(reader("BackupFinishDate")),
                                .Position = Convert.ToInt32(reader("Position"))
                            }
                        End While
                    End Using

                    If last Is Nothing Then
                        Throw New BackupException("バックアップファイルの中にバックアップが見つかりません。")
                    End If

                    ' BackupType 1 = データベースの完全バックアップ, 2 = ログ, 5 = 差分 など
                    If lastType <> 1 Then
                        Throw New BackupException("このファイルはデータベースの完全バックアップではありません。完全バックアップのファイルを選んでください。")
                    End If

                    Return last
                End Using
            End Using
        Catch ex As SqlException
            Throw Translate(ex, "バックアップファイルの確認")
        End Try
    End Function

    ' ---- 復元 ----

    ''' <summary>
    ''' バックアップから復元する。
    ''' </summary>
    Public Async Function RestoreAsync(path As String, safetyBackupPath As String) As Task
        EnsureValidPath(path)
        EnsureValidPath(safetyBackupPath)
        path = path.Trim()
        safetyBackupPath = safetyBackupPath.Trim()

        If String.Equals(path, safetyBackupPath, StringComparison.OrdinalIgnoreCase) Then
            Throw New BackupException("復元するファイルと退避用のバックアップの保存先が同じです。")
        End If

        ' 検査
        Dim info As BackupInfo = Await ReadInfoAsync(path)

        Try
            Await VerifyAsync(path, info.Position)

            Using conn As New SqlConnection(_masterConnectionString)
                Await conn.OpenAsync()

                Dim backupFiles As List(Of FileEntry) = Await ReadBackupFilesAsync(conn, path, info.Position)
                Dim currentFiles As List(Of FileEntry) = Await ReadCurrentFilesAsync(conn)
                Dim exists As Boolean = currentFiles.Count > 0 OrElse Await DatabaseExistsAsync(conn)

                Dim dataFile As FileEntry = Nothing
                Dim logFile As FileEntry = Nothing
                PickFiles(backupFiles, dataFile, logFile, "バックアップ")

                ' 置き場所を現在のデータベースと揃える。
                Dim moveData As String = Nothing
                Dim moveLog As String = Nothing
                If exists Then
                    Dim currentData As FileEntry = Nothing
                    Dim currentLog As FileEntry = Nothing
                    PickFiles(currentFiles, currentData, currentLog, "現在のデータベース")
                    moveData = currentData.PhysicalName
                    moveLog = currentLog.PhysicalName
                End If

                ' VBでは、Finallyの中でAwaitが使えないため、例外を一旦変数で受けて後から投げ直す。
                Dim failure As Exception = Nothing
                Try
                    If exists Then
                        ' 復元の前の退避バックアップ
                        Await BackupCoreAsync(conn, _databaseName, safetyBackupPath)
                        Await VerifyAsync(safetyBackupPath, 1)

                        ' 排他アクセス
                        SqlConnection.ClearAllPools()
                        Await ExecuteAsync(conn, $"ALTER DATABASE {QuoteName(_databaseName)} SET SINGLE_USER WITH ROLLBACK IMMEDIATE")
                    End If

                    ' 復元
                    Await RestoreCoreAsync(conn, path, info.Position, dataFile, logFile, moveData, moveLog)
                Catch ex As Exception
                    failure = ex
                End Try

                ' 後始末
                Try
                    Await ExecuteAsync(conn, $"IF DB_ID(@db) IS NOT NULL ALTER DATABASE {QuoteName(_databaseName)} SET MULTI_USER", _databaseName)
                Catch
                    ' 復元に失敗してデータベースが無い、などの場合
                End Try

                SqlConnection.ClearAllPools()

                If failure IsNot Nothing Then
                    ' スタックトレースを保ったまま投げ直す(外側の Catch で、SqlException なら利用者向けの説明に直される)
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw()
                End If
            End Using
        Catch ex As SqlException
            Throw Translate(ex, "復元")
        End Try
    End Function

    Private Class FileEntry
        Public LogicalName As String = String.Empty
        Public PhysicalName As String = String.Empty
        Public IsLog As Boolean
    End Class

    Private Shared Sub PickFiles(files As List(Of FileEntry), ByRef dataFile As FileEntry, ByRef logFile As FileEntry, label As String)
        Dim dataFiles As List(Of FileEntry) = files.Where(Function(f) Not f.IsLog).ToList()
        Dim logFiles As List(Of FileEntry) = files.Where(Function(f) f.IsLog).ToList()

        If dataFiles.Count <> 1 OrElse logFiles.Count <> 1 Then
            Throw New BackupException(
                $"{label}のファイル構成(データファイル{dataFiles.Count}個、ログファイル{logFiles.Count}個)には対応していません。" &
                 "データファイル1個とログファイル1個の構成だけ扱えます。")
        End If

        dataFile = dataFiles(0)
        logFile = logFiles(0)
    End Sub

    Private Shared Async Function ReadBackupFilesAsync(conn As SqlConnection, path As String, position As Integer) As Task(Of List(Of FileEntry))
        Dim result As New List(Of FileEntry)()

        Using cmd As New SqlCommand("RESTORE FILELISTONLY FROM DISK = @path WITH FILE = @file", conn)
            cmd.CommandTimeout = CommandTimeoutSeconds
            cmd.Parameters.Add("@path", SqlDbType.NVarChar, 260).Value = path
            cmd.Parameters.Add("@file", SqlDbType.Int).Value = position

            Using reader As SqlDataReader = Await cmd.ExecuteReaderAsync()
                While Await reader.ReadAsync()
                    ' Type: D = データ, L = ログ, F = フルテキスト, S = FILESTREAM など
                    Dim kind As String = Convert.ToString(reader("Type"))
                    If kind = "S" OrElse kind = "F" Then
                        Throw New BackupException("FILESTREAMやフルテキストのファイルを含むバックアップには対応していません。")
                    End If

                    result.Add(New FileEntry With {
                        .LogicalName = Convert.ToString(reader("LogicalName")),
                        .PhysicalName = Convert.ToString(reader("PhysicalName")),
                        .IsLog = (kind = "L")
                    })
                End While
            End Using
        End Using

        Return result
    End Function

    Private Async Function ReadCurrentFilesAsync(conn As SqlConnection) As Task(Of List(Of FileEntry))
        Dim result As New List(Of FileEntry)()

        ' type: 0 = 行データ, 1 = ログ
        Using cmd As New SqlCommand("SELECT name, physical_name, type FROM sys.master_files WHERE database_id = DB_ID(@db)", conn)
            cmd.Parameters.Add("@db", SqlDbType.NVarChar, 128).Value = _databaseName

            Using reader As SqlDataReader = Await cmd.ExecuteReaderAsync()
                While Await reader.ReadAsync()
                    result.Add(New FileEntry With {
                        .LogicalName = reader.GetString(0),
                        .PhysicalName = reader.GetString(1),
                        .IsLog = (Convert.ToInt32(reader.GetValue(2)) = 1)
                    })
                End While
            End Using
        End Using

        Return result
    End Function

    Private Async Function DatabaseExistsAsync(conn As SqlConnection) As Task(Of Boolean)
        Using cmd As New SqlCommand("SELECT DB_ID(@db)", conn)
            cmd.Parameters.Add("@db", SqlDbType.NVarChar, 128).Value = _databaseName
            Return Not (Await cmd.ExecuteScalarAsync() Is DBNull.Value)
        End Using
    End Function

    Private Async Function RestoreCoreAsync(conn As SqlConnection, path As String, position As Integer,
                                            backupData As FileEntry, backupLog As FileEntry,
                                            targetDataPath As String, targetLogPath As String) As Task
        Dim sql As String = $"RESTORE DATABASE {QuoteName(_databaseName)} FROM DISK = @path WITH FILE = @file, REPLACE"

        If targetDataPath IsNot Nothing Then
            sql &= ", MOVE @dn TO @dp, MOVE @ln TO @lp"
        End If

        Using cmd As New SqlCommand(sql, conn)
            cmd.CommandTimeout = CommandTimeoutSeconds
            cmd.Parameters.Add("@path", SqlDbType.NVarChar, 260).Value = path
            cmd.Parameters.Add("@file", SqlDbType.Int).Value = position

            If targetDataPath IsNot Nothing Then
                cmd.Parameters.Add("@dn", SqlDbType.NVarChar, 128).Value = backupData.LogicalName
                cmd.Parameters.Add("@dp", SqlDbType.NVarChar, 260).Value = targetDataPath
                cmd.Parameters.Add("@ln", SqlDbType.NVarChar, 128).Value = backupLog.LogicalName
                cmd.Parameters.Add("@lp", SqlDbType.NVarChar, 260).Value = targetLogPath
            End If

            Await cmd.ExecuteNonQueryAsync()
        End Using
    End Function

    Private Shared Async Function ExecuteAsync(conn As SqlConnection, sql As String, Optional databaseNameParameter As String = Nothing) As Task
        Using cmd As New SqlCommand(sql, conn)
            cmd.CommandTimeout = CommandTimeoutSeconds

            If databaseNameParameter IsNot Nothing Then
                cmd.Parameters.Add("@db", SqlDbType.NVarChar, 128).Value = databaseNameParameter
            End If

            Await cmd.ExecuteNonQueryAsync()
        End Using
    End Function

End Class