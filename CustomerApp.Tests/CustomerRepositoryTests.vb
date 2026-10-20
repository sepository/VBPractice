Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
<TestCategory("Integration")>
Public Class CustomerRepositoryTests
    Inherits DatabaseTestBase

    Private Function NewRepository() As CustomerRepository
        Return New CustomerRepository(ConnectionString)
    End Function

    <TestMethod>
    Public Async Function GetAllAsync_注文のない顧客は注文数0_累計金額0になる() As Task
        Dim id As Integer = Await AddCustomerAsync("山田 太郎")
        Dim customers As List(Of Customer) = Await NewRepository().GetAllAsync()

        Assert.HasCount(1, customers)
        Assert.AreEqual(id, customers(0).CustomerId)
        Assert.AreEqual(0, customers(0).OrderCount)
        Assert.AreEqual(0D, customers(0).TotalAmount)
    End Function

    <TestMethod>
    Public Async Function GetAllAsync_注文数と累計金額が顧客ごとに集計される() As Task
        Dim customerA As Integer = Await AddCustomerAsync("顧客A")
        Dim customerB As Integer = Await AddCustomerAsync("顧客B")
        Dim productId As Integer = Await AddProductAsync("P1", "商品", 100D)

        ' 顧客A : 注文1 2×1000 + 1×500 = 2500, 注文2 3×100 = 300 => 合計2800
        ' 顧客B : 注文なし
        Await AddOrderAsync(customerA, New DateTime(2026, 10, 1),
                            NewItem(productId, "商品", 2, 1000D), NewItem(productId, "商品", 1, 500D))
        Await AddOrderAsync(customerA, New DateTime(2026, 10, 1),
                            NewItem(productId, "商品", 3, 100D))

        Dim customers As List(Of Customer) = Await NewRepository().GetAllAsync()
        Dim a As Customer = customers.Single(Function(c) c.CustomerId = customerA)
        Dim b As Customer = customers.Single(Function(c) c.CustomerId = customerB)

        Assert.AreEqual(2, a.OrderCount)
        Assert.AreEqual(2800D, a.TotalAmount)
        Assert.AreEqual(0, b.OrderCount)
        Assert.AreEqual(0D, b.TotalAmount)
    End Function

    <TestMethod>
    Public Async Function GetAllAsync_キーワードで絞り込める() As Task
        Await AddCustomerAsync("山田 太郎")
        Await AddCustomerAsync("佐藤 花子")

        Dim customers As List(Of Customer) = Await NewRepository().GetAllAsync("山田")

        Assert.HasCount(1, customers)
        Assert.AreEqual("山田 太郎", customers(0).Name)
    End Function

    <TestMethod>
    Public Async Function InsertAsync_空欄の項目はNULLで保存される() As Task
        Dim repo As CustomerRepository = NewRepository()
        Dim id As Integer = Await repo.InsertAsync(New Customer With {
            .Name = "山田 太郎", .Kana = "", .Email = ""
        })
        Dim saved As Customer = Await repo.GetByIdAsync(id)

        Assert.IsNotNull(saved)
        Assert.IsNull(saved.Kana)
        Assert.IsNull(saved.Email)
    End Function

    <TestMethod>
    Public Async Function UpdateAsync_存在しないIDならFalseを返す() As Task
        Dim result As Boolean = Await NewRepository().UpdateAsync(New Customer With {
            .CustomerId = 999999, .Name = "x"
        })

        Assert.IsFalse(result)
    End Function

    <TestMethod>
    Public Async Function DeleteAsync_注文のない顧客は削除できる() As Task
        Dim id As Integer = Await AddCustomerAsync("山田 太郎")
        Dim repo As CustomerRepository = NewRepository()
        Dim result As Boolean = Await repo.DeleteAsync(id)

        Assert.IsTrue(result)
        Assert.IsNull(Await repo.GetByIdAsync(id))
    End Function

    <TestMethod>
    Public Async Function DeleteAsync_注文のある顧客は外部キー違反で削除できない() As Task
        Dim customerId As Integer = Await AddCustomerAsync("山田 太郎")
        Dim productId As Integer = Await AddProductAsync("P1", "商品", 100D)
        Await AddOrderAsync(customerId, New DateTime(2026, 10, 1), NewItem(productId, "商品", 1, 100D))
        Dim repo As CustomerRepository = NewRepository()
        Dim ex As Exception = Await CaptureAsync(Function() repo.DeleteAsync(customerId))

        Assert.IsNotNull(ex, "例外が起きるはず")
        Assert.IsTrue(DbHelper.IsForeignKeyViolation(ex), "外部キー違反(547)と判定されるはず")
        Assert.IsNotNull(Await repo.GetByIdAsync(customerId), "削除に失敗したので、顧客は残っているはず")
    End Function

End Class
