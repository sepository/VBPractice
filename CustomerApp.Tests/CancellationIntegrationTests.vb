Imports System.Threading
Imports Microsoft.Data.SqlClient
Imports Microsoft.VisualStudio.TestTools.UnitTesting

''' <summary>
''' データベースの検索を中止できるかの結合テスト
''' </summary>
<TestClass>
<TestCategory("Integration")>
Public Class CancellationIntegrationTests
    Inherits DatabaseTestBase

    Private Shared ReadOnly CanceledToken As New CancellationToken(True)

    Private Function NewCustomerRepository() As CustomerRepository
        Return New CustomerRepository(ConnectionString)
    End Function

    <TestMethod>
    Public Async Function GetAllAsync_中止済のトークンならOperationCanceledExceptionになる() As Task
        Dim repo As CustomerRepository = NewCustomerRepository()

        Dim ex As Exception = Await CaptureAsync(Function() repo.GetAllAsync(Nothing, CanceledToken))

        Assert.IsNotNull(ex, "例外が起きるはず")
        Assert.IsInstanceOfType(Of OperationCanceledException)(ex)
    End Function

    <TestMethod>
    Public Async Function GetEmailsAsync_中止済のトークンならOperationCanceledExceptionになる() As Task
        Dim repo As CustomerRepository = NewCustomerRepository()

        Dim ex As Exception = Await CaptureAsync(Function() repo.GetEmailsAsync(CanceledToken))

        Assert.IsNotNull(ex, "例外が起きるはず")
        Assert.IsInstanceOfType(Of OperationCanceledException)(ex)
    End Function

    <TestMethod>
    Public Async Function GetOrderLinesAsync_中止済のトークンならOperationCanceledExceptionになる() As Task
        Dim repo As New ReportRepository(ConnectionString)

        Dim ex As Exception = Await CaptureAsync(Function() repo.GetOrderLinesAsync(New DateTime(2026, 1, 1), New DateTime(2026, 12, 31), CanceledToken))

        Assert.IsNotNull(ex, "例外が起きるはず")
        Assert.IsInstanceOfType(Of OperationCanceledException)(ex)
    End Function

    <TestMethod>
    Public Async Function GetAllAsync_トークンを渡さなければ_これまでどおり取得できる() As Task
        Await NewCustomerRepository().InsertAsync(New Customer With {.Name = "山田"})

        Dim customers As List(Of Customer) = Await NewCustomerRepository().GetAllAsync()

        Assert.HasCount(1, customers)
    End Function

    <TestMethod>
    Public Async Function GetAllAsync_ロックで待たされている検索を途中で中止できる() As Task
        Using lockConnection As New SqlConnection(ConnectionString)
            Await lockConnection.OpenAsync()

            Using transaction As SqlTransaction = lockConnection.BeginTransaction()
                ' 顧客テーブルに排他ロックをかけ、トランザクションを閉じないでおく
                Using lockCommand As New SqlCommand("SELECT COUNT(*) FROM dbo.Customers WITH (TABLOCKX, HOLDLOCK)", lockConnection, transaction)
                    Await lockCommand.ExecuteScalarAsync()
                End Using

                Using cts As New CancellationTokenSource()
                    cts.CancelAfter(TimeSpan.FromMicroseconds(500))
                    Dim token As CancellationToken = cts.Token
                    Dim repo As CustomerRepository = NewCustomerRepository()

                    Dim timer As Stopwatch = Stopwatch.StartNew()
                    Dim ex As Exception = Await CaptureAsync(Function() repo.GetAllAsync(Nothing, token))
                    timer.Stop()

                    Assert.IsNotNull(ex, "中止されて、例外になるはず")
                    Assert.IsTrue(DbHelper.IsCanceled(ex, token), "中止が原因の例外として判定できるはず:" & DbHelper.GetFullMessage(ex))
                    Assert.IsLessThan(15000L, timer.ElapsedMilliseconds, "コマンドのタイムアウト(30秒)を待たずに戻ってくるはず")
                End Using

                transaction.Rollback()
            End Using
        End Using
    End Function

End Class
