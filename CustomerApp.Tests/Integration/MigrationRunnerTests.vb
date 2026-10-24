Imports System.Text
Imports Microsoft.Data.SqlClient
Imports Microsoft.EntityFrameworkCore.Migrations
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
<TestCategory("Integration")>
Public Class MigrationRunnerTests

    Private _databaseName As String
    Private _connectionString As String

    <TestInitialize>
    Public Sub Setup()
        Dim builder As New SqlConnectionStringBuilder(TestDatabase.ConnectionString)

        _databaseName = $"CustomerDb_Mig_{Guid.NewGuid:N}_Test"
        builder.InitialCatalog = _databaseName
        _connectionString = builder.ConnectionString
    End Sub

    <TestCleanup>
    Public Sub Cleanup()
        Try
            DropDatabase()
        Catch
            ' 削除に失敗しても、テストの結果は変えない。(残ったデータベースは、名前の接頭辞 CustomerDb_Mig_ で見分けられる)
        End Try
    End Sub

    ' ---- テスト用の道具 ----

    ''' <summary>
    ''' このテストのために作ったデータベースを削除する。
    ''' </summary>
    Private Sub DropDatabase()
        If Not (_databaseName.StartsWith("CustomerDb_Mig_", StringComparison.Ordinal) AndAlso
                _databaseName.EndsWith("_Test", StringComparison.Ordinal)) Then
            Return
        End If

        ' 接続プールに残った接続が、データベースを使用中にしないため。
        SqlConnection.ClearAllPools()

        Dim builder As New SqlConnectionStringBuilder(_connectionString) With {
            .InitialCatalog = "master"
        }

        Using conn As New SqlConnection(builder.ConnectionString)
            conn.Open()

            Dim sql As String =
                $"IF DB_ID(N'{_databaseName}') IS NOT NULL BEGIN " &
                $"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " &
                $"DROP DATABASE [{_databaseName}]; END"

            Using cmd As New SqlCommand(sql, conn)
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub

    Private Shared Function RealScripts() As List(Of MigrationScript)
        Return EmbeddedMigrations.Load()
    End Function

    Private Function NewRunner(scripts As IEnumerable(Of MigrationScript)) As MigrationRunner
        Return New MigrationRunner(_connectionString, scripts)
    End Function

    Private Sub ExecuteNonQuery(sql As String)
        Using conn As New SqlConnection(_connectionString)
            conn.Open()
            Using cmd As New SqlCommand(sql, conn)
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub

    Private Function ExecuteScalar(sql As String) As Object
        Using conn As New SqlConnection(_connectionString)
            conn.Open()
            Using cmd As New SqlCommand(sql, conn)
                Return cmd.ExecuteScalar()
            End Using
        End Using
    End Function

    Private Function TableExists(tableName As String) As Boolean
        Return Convert.ToInt32(ExecuteScalar($"SELECT CASE WHEN OBJECT_ID(N'dbo.{tableName}', N'U') IS NULL THEN 0 ELSE 1 END")) = 1
    End Function

    Private Function Count(tableName As String) As Integer
        Return Convert.ToInt32(ExecuteScalar($"SELECT COUNT(*) FROM dbo.{tableName}"))
    End Function

    ' ---- テスト ----

    <TestMethod>
    Public Sub ApplyPending_データベースが無くても作成して_全スクリプトを適用できる()
        Dim scripts As List(Of MigrationScript) = RealScripts()
        Dim runner As MigrationRunner = NewRunner(scripts)

        Dim created As Boolean = runner.EnsureDatabaseExists()
        Dim applied As List(Of MigrationScript) = runner.ApplyPending()

        Assert.IsTrue(created, "データベースが作成されるはず")
        Assert.HasCount(scripts.Count, applied)
        Assert.IsTrue(TableExists("Customers"))
        Assert.IsTrue(TableExists("Orders"))
        Assert.IsTrue(TableExists("OrderItems"))
        Assert.IsTrue(TableExists("Products"))
        Assert.IsTrue(TableExists("SchemaVersions"))
        Assert.AreEqual(scripts.Max(Function(s) s.Version), runner.GetCurrentVersion())
        Assert.AreEqual(scripts.Count, Count("SchemaVersions"), "適用しただけ、履歴に残るはず")
    End Sub

    <TestMethod>
    Public Sub EnsureDatabaseExists_既にあれば作成せず()
        Dim runner As MigrationRunner = NewRunner(RealScripts())
        runner.EnsureDatabaseExists()

        Assert.IsFalse(runner.EnsureDatabaseExists())
    End Sub

    <TestMethod>
    Public Sub ApplyPending_2回目は何も適用されない()
        Dim runner As MigrationRunner = NewRunner(RealScripts())
        runner.EnsureDatabaseExists()
        runner.ApplyPending()

        Dim second As List(Of MigrationScript) = runner.ApplyPending()

        Assert.IsEmpty(second)
        Assert.IsEmpty(runner.GetPendingScripts())
    End Sub

    <TestMethod>
    Public Sub ApplyPending_履歴がない既存のデータベースに再適用しても壊れない()
        ' 「このアプリの導入前に、手作業でテーブルを作ってあるデータベース」に、初めてマイグレーションを適用する場面。
        ' どのスクリプトも「無ければ作る」の形で書かれているので、既存のテーブルを壊さず、履歴だけが記録されるはず
        Dim scripts As List(Of MigrationScript) = RealScripts()
        Dim runner As MigrationRunner = NewRunner(scripts)
        runner.EnsureDatabaseExists()
        runner.ApplyPending()
        ExecuteNonQuery("INSERT INTO dbo.Customers (Name) VALUES (N'既存の顧客')")
        ExecuteNonQuery("DROP TABLE dbo.SchemaVersions")

        Dim applied As List(Of MigrationScript) = runner.ApplyPending()

        Assert.HasCount(scripts.Count, applied)
        Assert.AreEqual(1, Count("Customers"), "既存のデータが残っているはず")
        Assert.AreEqual(scripts.Count, Count("SchemaVersions"))
    End Sub

    <TestMethod>
    Public Sub ApplyPending_旧構造の注文データが明細と商品マスタに移行される()
        ' V001~V002だけを適用した「古い構造」のデータベースに、データを入れてから最新へ移行する。
        Dim oldScripts As List(Of MigrationScript) = RealScripts().Where(Function(s) s.Version <= 2).ToList()
        Dim oldRunner As MigrationRunner = NewRunner(oldScripts)
        oldRunner.EnsureDatabaseExists()
        oldRunner.ApplyPending()

        ' 旧構造のOrdersは、商品名・数量・単価を注文自身が持つ
        Dim sql As New StringBuilder()
        sql.Append("INSERT INTO dbo.Customers (Name) VALUES (N'山田');")
        sql.Append("INSERT INTO dbo.Orders (CustomerId, OrderDate, ProductName, Quantity, UnitPrice) ")
        sql.Append("SELECT CustomerId, '2026-10-01', N'ノートPC', 2, 1000 FROM dbo.Customers;")
        sql.Append("INSERT INTO dbo.Orders (CustomerId, OrderDate, ProductName, Quantity, UnitPrice) ")
        sql.Append("SELECT CustomerId, '2026-10-02', N'ノートPC', 1, 1200 FROM dbo.Customers;")
        sql.Append("INSERT INTO dbo.Orders (CustomerId, OrderDate, ProductName, Quantity, UnitPrice) ")
        sql.Append("SELECT CustomerId, '2026-10-03', N'マウス', 1, 3000 FROM dbo.Customers;")
        ExecuteNonQuery(sql.ToString())

        ' 最新へ移行
        Dim applied As List(Of MigrationScript) = NewRunner(RealScripts()).ApplyPending()

        Assert.IsGreaterThanOrEqualTo(2, applied.Count)
        Assert.AreEqual(3, Count("Orders"), "注文はそのまま残るはず")
        Assert.AreEqual(3, Count("OrderItems"), "注文ごとに明細が1行できるはず")
        Assert.AreEqual(2, Count("Products"), "商品名ごと(ノートPC・マウス)に商品が登録されるはず")
        Assert.AreEqual(3, Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM dbo.OrderItems WHERE ProductId IS NOT NULL")), "全ての明細が商品マスタに紐づくはず")
        Assert.AreEqual(6200D, Convert.ToDecimal(ExecuteScalar("SELECT SUM(Quantity * UnitPrice) FROM dbo.OrderItems")), "移行の前後で金額の合計は変わらないはず。(2×1000 + 1×1200 + 1×3000)")
        Assert.AreEqual(1, Convert.ToInt32(ExecuteScalar("SELECT CASE WHEN COL_LENGTH(N'dbo.Orders', N'ProductName') IS NULL THEN 1 ELSE 0 END")), "Ordersから商品名の列が削除されているはず")
    End Sub

    <TestMethod>
    Public Sub ApplyPending_途中で失敗したスクリプトは記録せず変更も残らない()
        Dim scripts As New List(Of MigrationScript) From {
            New MigrationScript(1, "V001__ok", "CREATE TABLE dbo.T1 (id INT)"),
            New MigrationScript(2, "V002__broken", "CREATE TABLE dbo.T2 (id INT);" & vbCrLf & vbCrLf & "GO" & vbCrLf & "SELECT 1/0")
        }
        Dim runner As MigrationRunner = NewRunner(scripts)
        runner.EnsureDatabaseExists()

        Dim caught As MigrationException = Nothing
        Try
            runner.ApplyPending()
        Catch ex As MigrationException
            caught = ex
        End Try

        Assert.IsNotNull(caught, "2つ目のスクリプトが失敗して例外になるはず")
        Assert.Contains("V002__broken", caught.Message, "どのスクリプトが失敗したか、メッセージで分かるはず")
        Assert.IsTrue(TableExists("T1"), "成功した1つ目は残るはず")
        Assert.IsFalse(TableExists("T2"), "失敗した2つ目は取り消されているはず")
        Assert.AreEqual(1, Count("SchemaVersions"), "履歴には成功した1件のみが残るはず")
    End Sub

    <TestMethod>
    Public Sub GetPendingScripts_適用済のスクリプトが書き換えられていたら例外になる()
        Dim original As New MigrationScript(1, "V001__t1", "CREATE TABLE dbo.T1 (id INT);")
        Dim runner As MigrationRunner = NewRunner({original})
        runner.EnsureDatabaseExists()
        runner.ApplyPending()

        ' 同じ番号・名前のまま、中身だけが変わっている
        Dim tampered As New MigrationScript(1, "V001__t1", "CREATE TABLE dbo.T1 (id INT, Extra INT);")
        Dim caught As MigrationException = Nothing
        Try
            NewRunner({tampered}).GetPendingScripts()
        Catch ex As MigrationException
            caught = ex
        End Try

        Assert.IsNotNull(caught, "書き換えが検出されて例外になるはず")
        Assert.Contains("V001__t1", caught.Message)
    End Sub

    <TestMethod>
    Public Sub GetPendingScripts_データベースがアプリより新しければ例外になる()
        Dim s1 As New MigrationScript(1, "V001__t1", "CREATE TABLE dbo.T1 (id INT);")
        Dim s2 As New MigrationScript(2, "V002__t2", "CREATE TABLE dbo.T2 (id INT);")
        Dim newerApp As MigrationRunner = NewRunner({s1, s2})
        newerApp.EnsureDatabaseExists()
        newerApp.ApplyPending()

        ' 古いバージョンのアプリ(V002がない)が、新しい構造のデータベースに接続した場面
        Dim caught As MigrationException = Nothing
        Try
            NewRunner({s1}).GetPendingScripts()
        Catch ex As MigrationException
            caught = ex
        End Try

        Assert.IsNotNull(caught, "データベースが新しいと検出されて例外になるはず")
        Assert.Contains("V002__t2", caught.Message)
    End Sub

    <TestMethod>
    Public Sub GetCurrentVersion_何も適用していなければ0()
        Dim runner As MigrationRunner = NewRunner(RealScripts())
        runner.EnsureDatabaseExists()

        Assert.AreEqual(0, runner.GetCurrentVersion())
        Assert.HasCount(RealScripts().Count, runner.GetPendingScripts())
    End Sub

    ' ---- V005(顧客のメールアドレスの一意インデックス) ----

    Private Function EmailIndexCount() As Integer
        Return Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM sys.indexes WHERE name = N'UX_Customers_Email' AND object_id = OBJECT_ID(N'dbo.Customers')"))
    End Function

    Private Sub PrepareDatabaseBeforeV005()
        Dim runner As MigrationRunner = NewRunner(RealScripts().Where(Function(s) s.Version <= 4).ToList())
        runner.EnsureDatabaseExists()
        runner.ApplyPending()
    End Sub

    <TestMethod>
    Public Sub ApplyPending_V005はメールアドレスが重複していたら分かりやすく失敗し何も変えない()
        PrepareDatabaseBeforeV005()
        ' V005より前は、一意インデックスがないので同じメールアドレスが登録できてしまっていた。
        ExecuteNonQuery("INSERT INTO dbo.Customers (Name, Email) VALUES (N'A', 'dup@example.com'), (N'B', N'dup@example.com')")

        Dim runner As MigrationRunner = NewRunner(RealScripts())
        Dim caught As MigrationException = Nothing
        Try
            runner.ApplyPending()
        Catch ex As MigrationException
            caught = ex
        End Try

        Assert.IsNotNull(caught, "重複が検出されて例外になるはず")
        Assert.Contains("V005", caught.Message, "どのスクリプトが失敗したか、メッセージで分かるはず")
        Assert.IsGreaterThanOrEqualTo(0, DbHelper.GetFullMessage(caught).IndexOf("重複", StringComparison.Ordinal), "原因(メールアドレスの重複)がメッセージで分かるはず")
        Assert.AreEqual(0, EmailIndexCount(), "インデックスは作られていないはず")
        Assert.AreEqual(4, runner.GetCurrentVersion(), "V005は記録されず、V004のままのはず")
        Assert.AreEqual(2, Count("Customers"), "既存の顧客は勝手に消されていないはず")
    End Sub

    <TestMethod>
    Public Sub ApplyPending_V005は重複を解消したあとなら適用できる()
        PrepareDatabaseBeforeV005()
        ExecuteNonQuery("INSERT INTO dbo.Customers (Name, Email) VALUES (N'A', N'dup@example.com'), (N'B', N'dup@example.com')")
        Dim runner As MigrationRunner = NewRunner(RealScripts())
        Try
            runner.ApplyPending()
        Catch ex As MigrationException
            ' 1回目は、重複で失敗する(上のテストで確認済み)
        End Try

        ExecuteNonQuery("DELETE FROM dbo.Customers WHERE Name = N'B'")
        Dim applied As List(Of MigrationScript) = runner.ApplyPending()

        Assert.HasCount(1, applied)
        Assert.AreEqual(1, EmailIndexCount())
        Assert.AreEqual(5, runner.GetCurrentVersion())
    End Sub

    <TestMethod>
    Public Sub ApplyPending_V005の一意インデックスは空のメールアドレスを何件でも許し重複は拒否する()
        Dim runner As MigrationRunner = NewRunner(RealScripts())
        runner.EnsureDatabaseExists()
        runner.ApplyPending()

        ' NULLと空文字は何件あってもよい
        ExecuteNonQuery("INSERT INTO dbo.Customers (Name, Email) VALUES (N'A', NULL), (N'B', NULL), (N'C', ''), (N'D', '')")
        Assert.AreEqual(4, Count("Customers"))

        ' 空でないメールアドレスの重複は拒否される
        ExecuteNonQuery("INSERT INTO dbo.Customers (Name, Email) VALUES (N'E', N'x@example.com')")
        Dim caught As SqlException = Nothing
        Try
            ExecuteNonQuery("INSERT INTO dbo.Customers (Name, Email) VALUES (N'F', N'x@example.com')")
        Catch ex As SqlException
            caught = ex
        End Try

        Assert.IsNotNull(caught, "重複は拒否されるはず")
        Assert.AreEqual(2601, caught.Number)
    End Sub

End Class
