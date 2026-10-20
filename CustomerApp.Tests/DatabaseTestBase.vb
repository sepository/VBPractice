Imports System.Threading
Imports Microsoft.EntityFrameworkCore.Storage
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Public MustInherit Class DatabaseTestBase

    Protected ReadOnly Property ConnectionString As String
        Get
            Return TestDatabase.ConnectionString
        End Get
    End Property

    ''' <summary>
    ''' データベースは1つしかないので、DBを使うテストは、同時に1つしか動かしてはいけない。
    ''' </summary>
    Private Shared ReadOnly DatabaseGate As New SemaphoreSlim(1, 1)

    ''' <summary>
    ''' このテストが入口の権利を持っているか。
    ''' </summary>
    Private _holdsGate As Boolean = False

    ''' <summary>
    ''' 派生クラスの全テストの前に自動で実行される。
    ''' 初回だけ、データベースを作り直す。
    ''' データを空にする。
    ''' </summary>
    <TestInitialize>
    Public Sub PrepareDatabase()
        ' 他のDBテストが終わるまで待つ
        DatabaseGate.Wait()
        _holdsGate = True

        Try
            TestDatabase.EnsureReady()
            TestDatabase.ClearAllData()
        Catch
            ' 準備に失敗したら、他のテストが止まらないよう入口を開ける
            ReleaseGate()
            Throw
        End Try

    End Sub

    ''' <summary>
    ''' 派生クラスの全テストの後に自動で実行される。
    ''' </summary>
    <TestCleanup>
    Public Sub FinishDatabase()
        ReleaseGate()
    End Sub

    Private Sub ReleaseGate()
        If _holdsGate Then
            _holdsGate = False
            DatabaseGate.Release()
        End If
    End Sub

    ' ---- テストデータ生成用 ----

    Protected Async Function AddCustomerAsync(name As String) As Task(Of Integer)
        Dim repo As New CustomerRepository(ConnectionString)
        Return Await repo.InsertAsync(New Customer With {
            .Name = name
        })
    End Function

    Protected Async Function AddProductAsync(code As String, name As String, price As Decimal,
                                             Optional isActive As Boolean = True) As Task(Of Integer)
        Dim repo As New ProductRepository(ConnectionString)
        Return Await repo.InsertAsync(New Product With {
            .ProductCode = code,
            .Name = name,
            .UnitPrice = price,
            .IsActive = isActive
        })
    End Function

    Protected Shared Function NewItem(productId As Integer, name As String, quantity As Integer, unitPrice As Decimal) As OrderItem
        Return New OrderItem With {
            .ProductId = productId,
            .ProductName = name,
            .Quantity = quantity,
            .UnitPrice = unitPrice
        }
    End Function

    Protected Async Function AddOrderAsync(customerId As Integer, orderDate As DateTime, ParamArray items As OrderItem()) As Task(Of Integer)
        Dim order As New CustomerOrder With {
            .CustomerId = customerId,
            .OrderDate = orderDate.Date
        }
        order.Items.AddRange(items)

        Dim repo As New OrderRepository(ConnectionString)
        Return Await repo.InsertAsync(order)
    End Function

    ''' <summary>
    ''' 処理を実行し、例外が起きたらそれを返す(起きなければNothing)。
    ''' 「失敗すること」を確かめるテストで使う。
    ''' </summary>
    Protected Shared Async Function CaptureAsync(action As Func(Of Task)) As Task(Of Exception)
        Try
            Await action()
            Return Nothing
        Catch ex As Exception
            Return ex
        End Try
    End Function

End Class
