Imports Microsoft.VisualStudio.TestTools.UnitTesting

''' <summary>
''' ProductRepositoryの結合テスト
''' </summary>
<TestClass>
<TestCategory("Integration")>
Public Class ProductRepositoryTests
    Inherits DatabaseTestBase

    Private Function NewRepository() As ProductRepository
        Return New ProductRepository(ConnectionString)
    End Function

    <TestMethod>
    Public Async Function InsertAsync_同じ商品コードは一意制約違反になる() As Task
        Await AddProductAsync("P001", "商品A", 100D)

        Dim ex As Exception = Await CaptureAsync(Function() AddProductAsync("P001", "商品B", 200D))

        Assert.IsNotNull(ex, "例外が起きるはず")
        Assert.IsTrue(DbHelper.IsUniqueViolation(ex), "一意制約違反(2627 または 2601)と判定されるはず")
    End Function

    <TestMethod>
    Public Async Function GetActiveAsync_販売停止の商品は含まれない() As Task
        Await AddProductAsync("P001", "販売中", 100D, isActive:=True)
        Await AddProductAsync("P002", "販売停止", 100D, isActive:=False)

        Dim active As List(Of Product) = Await NewRepository().GetActiveAsync()
        Dim all As List(Of Product) = Await NewRepository().GetAllAsync()

        Assert.HasCount(1, active)
        Assert.AreEqual("販売中", active(0).Name)
        Assert.HasCount(2, all, "管理画面用のGetAllAsyncには、販売停止も含まれる")
    End Function

    <TestMethod>
    Public Async Function GetAllAsync_商品コード順に並ぶ() As Task
        Await AddProductAsync("P003", "C", 100D)
        Await AddProductAsync("P001", "A", 100D)
        Await AddProductAsync("P002", "B", 100D)

        Dim all As List(Of Product) = Await NewRepository().GetAllAsync()

        Assert.AreEqual("P001", all(0).ProductCode)
        Assert.AreEqual("P002", all(1).ProductCode)
        Assert.AreEqual("P003", all(2).ProductCode)
    End Function

    <TestMethod>
    Public Async Function DeleteAsync_注文で使われている商品は削除できない() As Task
        Dim customerId As Integer = Await AddCustomerAsync("山田 太郎")
        Dim productId As Integer = Await AddProductAsync("P001", "商品A", 100D)
        Await AddOrderAsync(customerId, New DateTime(2026, 10, 1), NewItem(productId, "商品A", 1, 100D))
        Dim repo As ProductRepository = NewRepository()

        Dim ex As Exception = Await CaptureAsync(Function() repo.DeleteAsync(productId))

        Assert.IsNotNull(ex, "例外が起きるはず")
        Assert.IsTrue(DbHelper.IsForeignKeyViolation(ex), "外部キー違反(547)と判定されるはず")
    End Function

    <TestMethod>
    Public Async Function DeleteAsync_使われていない商品は削除できる() As Task
        Dim productId As Integer = Await AddProductAsync("P001", "商品A", 100D)

        Dim result As Boolean = Await NewRepository().DeleteAsync(productId)

        Assert.IsTrue(result)
    End Function

    <TestMethod>
    Public Async Function UpdateAsync_商品の単価を変えても過去の注文明細の単価は変わらない() As Task
        ' スナップショットの確認 : 明細は、注文した時点の単価を自分で持っている
        Dim customerId As Integer = Await AddCustomerAsync("山田 太郎")
        Dim productId As Integer = Await AddProductAsync("P001", "商品A", 1000D)
        Await AddOrderAsync(customerId, New DateTime(2026, 10, 1), NewItem(productId, "商品A", 1, 1000D))

        Dim changed As Boolean = Await NewRepository().UpdateAsync(New Product With {
            .ProductId = productId, .ProductCode = "P001", .Name = "商品A(改)", .UnitPrice = 2000D, .IsActive = True
        })
        Dim orders As List(Of CustomerOrder) = Await New OrderRepository(ConnectionString).GetByCustomerIdAsync(customerId)

        Assert.IsTrue(changed)
        Assert.AreEqual(1000D, orders(0).Items(0).UnitPrice, "過去の注文の単価が、マスタの変更に引きずられている")
        Assert.AreEqual("商品A", orders(0).Items(0).ProductName, "過去の注文の商品名が、マスタの変更に引きずられている")
    End Function

End Class
