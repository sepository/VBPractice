Imports System.IO
Imports System.Threading
Imports Microsoft.VisualStudio.TestTools.UnitTesting

''' <summary>
''' 検索等の中止の単体テスト
''' </summary>
<TestClass>
Public Class CancellationTests

    ''' <summary>
    ''' 最初から中止済のトークン
    ''' </summary>
    Private Shared ReadOnly CanceledToken As New CancellationToken(True)

    ' ---- DbHelper.IsCanceled ----

    <TestMethod>
    Public Sub IsCanceled_OperationCanceledExecptionは中止とみなす()
        Assert.IsTrue(DbHelper.IsCanceled(New OperationCanceledException(), CancellationToken.None))
    End Sub

    <TestMethod>
    Public Sub IsCanceled_TaskCanceledExceptionも中止とみなす()
        Assert.IsTrue(DbHelper.IsCanceled(New TaskCanceledException(), CancellationToken.None))
    End Sub

    <TestMethod>
    Public Sub IsCanceled_別の例外に包まれていても中止とみなす()
        Dim wrapped As New InvalidOperationException("外側", New OperationCanceledException())

        Assert.IsTrue(DbHelper.IsCanceled(wrapped, CancellationToken.None))
    End Sub

    <TestMethod>
    Public Sub IsCanceled_中止の要求が出ていれば_どんな例外でも中止とみなす()
        Assert.IsTrue(DbHelper.IsCanceled(New InvalidOperationException("別の形の例外"), CanceledToken))
    End Sub

    ' ---- エクスポートの中止 ----

    Private Shared Function NewTable() As ExportTable
        Dim customers As New List(Of Customer)()
        For i As Integer = 1 To 50
            customers.Add(New Customer With {.CustomerId = i, .Name = $"顧客{i}"})
        Next

        Return ExportTableBuilder.FromCustomers(customers)
    End Function

    Private Shared Function CatchException(action As Action) As Exception
        Try
            action()
        Catch ex As Exception
            Return ex
        End Try

        Return Nothing
    End Function

    <TestMethod>
    Public Sub CsvExporter_Save_中止済のトークンなら_OperationCanceledExceptionになる()
        Dim path As String = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"customerapp-cancel-{Guid.NewGuid():N}.csv")
        Try
            Dim table As ExportTable = NewTable()

            Dim ex As Exception = CatchException(Sub() CsvExporter.Save(table, path, CanceledToken))

            Assert.IsNotNull(ex, "例外が起きるはず")
            Assert.IsInstanceOfType(Of OperationCanceledException)(ex)
        Finally
            If File.Exists(path) Then
                File.Delete(path)
            End If
        End Try
    End Sub

    <TestMethod>
    Public Sub ExcelExporter_Save_中止済のトークンなら_OperationCanceledExceptionになる_ファイルは作られない()
        Dim path As String = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"customerapp-cancel-{Guid.NewGuid():N}.xlsx")
        Try
            Dim table As ExportTable = NewTable()

            Dim ex As Exception = CatchException(Sub() ExcelExporter.Save(table, path, CanceledToken))

            Assert.IsNotNull(ex, "例外が起きるはず")
            Assert.IsInstanceOfType(Of OperationCanceledException)(ex)
            Assert.IsFalse(File.Exists(path), "保存の前に止まるのでファイルは作られないはず")
            Assert.IsInstanceOfType(Of OperationCanceledException)(ex)
        Finally
            If File.Exists(path) Then
                File.Delete(path)
            End If
        End Try
    End Sub

    <TestMethod>
    Public Sub CsvExporter_Save_トークンを渡さなければ_これまでどおり書き出せる()
        Dim path As String = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"customerapp-cancel-{Guid.NewGuid():N}.csv")
        Try
            CsvExporter.Save(NewTable(), path)

            Assert.IsTrue(File.Exists(path))
            Assert.HasCount(51, File.ReadAllLines(path), "見出し行 + 50行")
        Finally
            If File.Exists(path) Then
                File.Delete(path)
            End If
        End Try
    End Sub

End Class
