Imports System.IO
Imports System.Threading.Tasks
Imports CustomerApp
Imports Microsoft.Data.SqlClient
Imports Microsoft.VisualStudio.TestTools.UnitTesting

''' <summary>
''' バックアップ・復元(BackupService)の結合テスト(本物のテスト用DBを使う)。
''' </summary>
<TestClass>
<TestCategory("Integration")>
Public Class BackupRestoreTests
    Inherits DatabaseTestBase

    Private ReadOnly _createdFiles As New List(Of String)()

    Private Function NewService() As BackupService
        Return New BackupService(ConnectionString)
    End Function

    Private Function DatabaseName() As String
        Return New SqlConnectionStringBuilder(ConnectionString).InitialCatalog
    End Function

    Private Async Function NewBackupPathAsync(label As String) As Task(Of String)
        Dim info As BackupServerInfo = Await NewService().GetServerInfoAsync()

        If Not info.IsLocal OrElse String.IsNullOrWhiteSpace(info.DefaultBackupFolder) Then
            Assert.Inconclusive("SQL Server が、このPCにあり、既定のバックアップ先が分かる環境でだけ確認できます。")
        End If

        Dim fileName As String = BackupService.SuggestFileName(DatabaseName(), DateTime.Now, "_" & label & "_" & Guid.NewGuid().ToString("N"))
        Dim path As String = System.IO.Path.Combine(info.DefaultBackupFolder, fileName)
        _createdFiles.Add(path)
        Return path
    End Function

    <TestCleanup>
    Public Sub DeleteBackupFiles()
        For Each file As String In _createdFiles
            Try
                System.IO.File.Delete(file)
            Catch
                ' SQL Server のアカウントが作ったファイルは、消せないことがある。テストの失敗にはしない
            End Try
        Next
    End Sub

    Private Async Function CountCustomersAsync() As Task(Of Integer)
        Dim repo As New CustomerRepository(ConnectionString)
        Dim all As List(Of Customer) = Await repo.GetAllAsync()
        Return all.Count
    End Function

    <TestMethod>
    Public Async Function BackupAsync_バックアップを取ると_中身を読める() As Task
        Dim path As String = Await NewBackupPathAsync("basic")
        Await AddCustomerAsync("山田")

        Await NewService().BackupAsync(path)

        Dim info As BackupInfo = Await NewService().ReadInfoAsync(path)
        Assert.AreEqual(DatabaseName(), info.DatabaseName, ignoreCase:=True)
        Assert.IsLessThan(10.0, (DateTime.Now - info.FinishDate).TotalMinutes, "取ったばかりの日時のはず")
    End Function

    <TestMethod>
    Public Async Function RestoreAsync_バックアップ後に増えたデータは_取った時点に戻る() As Task
        Dim backupPath As String = Await NewBackupPathAsync("keep")
        Dim safetyPath As String = Await NewBackupPathAsync("safety")

        Await AddCustomerAsync("バックアップ時点の顧客")
        Await NewService().BackupAsync(backupPath)

        Await AddCustomerAsync("バックアップ後の顧客1")
        Await AddCustomerAsync("バックアップ後の顧客2")
        Assert.AreEqual(3, Await CountCustomersAsync())

        Await NewService().RestoreAsync(backupPath, safetyPath)

        Assert.AreEqual(1, Await CountCustomersAsync(), "バックアップを取った時点(1件)に戻るはず")

        ' 復元のあとも、通常どおり接続して、書き込める(SINGLE_USER のまま残っていない)
        Await AddCustomerAsync("復元後の顧客")
        Assert.AreEqual(2, Await CountCustomersAsync())

        ' 復元の前の状態(3件)が、退避用のバックアップとして残っている
        Dim safety As BackupInfo = Await NewService().ReadInfoAsync(safetyPath)
        Assert.AreEqual(DatabaseName(), safety.DatabaseName, ignoreCase:=True)
    End Function

    <TestMethod>
    Public Async Function RestoreAsync_存在しないファイルは_データを変えずに拒否される() As Task
        Dim missingPath As String = Await NewBackupPathAsync("missing")
        Dim safetyPath As String = Await NewBackupPathAsync("safety2")
        Await AddCustomerAsync("山田")

        Dim ex As Exception = Await CaptureAsync(Function() NewService().RestoreAsync(missingPath, safetyPath))

        Assert.IsNotNull(ex, "存在しないファイルからは復元できないはず")
        Assert.IsInstanceOfType(Of BackupException)(ex)
        Assert.AreEqual(1, Await CountCustomersAsync(), "データは、そのまま")
    End Function

    <TestMethod>
    Public Async Function BackupAsync_不正なパスは_SQLを実行せずに拒否される() As Task
        Dim ex As Exception = Await CaptureAsync(Function() NewService().BackupAsync("backup.txt"))

        Assert.IsNotNull(ex)
        Assert.IsInstanceOfType(Of BackupException)(ex)
    End Function

End Class