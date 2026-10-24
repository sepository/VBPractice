Imports Microsoft.VisualStudio.TestTools.UnitTesting

''' <summary>
''' 商品のインポート
''' </summary>
<TestClass>
<TestCategory("Integration")>
Public Class ProductImportTests
    Inherits DatabaseTestBase

    Private Function NewRepository() As ProductRepository
        Return New ProductRepository(ConnectionString)
    End Function

    Private Shared Function NewProduct(code As String, name As String, price As Decimal, Optional isActive As Boolean = True) As Product
        Return New Product With {.ProductCode = code, .Name = name, .UnitPrice = price, .IsActive = isActive}
    End Function

    <TestMethod>
    Public Async Function ImportAsync_全件が登録される() As Task
        Dim repo As ProductRepository = NewRepository()

        Dim count As Integer = Await repo.ImportAsync(New Product() {
            NewProduct("A1", "りんご", 120D),
            NewProduct(" A2", " みかん", 80.5D, False)
        })

        Assert.AreEqual(2, count)

        Dim products As List(Of Product) = Await repo.GetAllAsync()
        Assert.HasCount(2, products)
        Assert.AreEqual("A2", products(1).ProductCode)
        Assert.AreEqual("みかん", products(1).Name)
        Assert.AreEqual(80.5D, products(1).UnitPrice)
        Assert.IsFalse(products(1).IsActive)
    End Function

    <TestMethod>
    Public Async Function ImportAsync_登録済のコードがあれば全件取り消される() As Task
        Dim repo As ProductRepository = NewRepository()
        Await AddProductAsync("A1", "既存", 100D)

        Dim ex As Exception = Await CaptureAsync(Function() repo.ImportAsync(New Product() {
            NewProduct("N1", "新規1", 1D),
            NewProduct("A1", "重複1", 2D),
            NewProduct("N2", "新規2", 3D)
        }))

        Assert.IsNotNull(ex)
        Assert.IsTrue(DbHelper.IsUniqueViolation(ex), DbHelper.GetFullMessage(ex))

        Dim products As List(Of Product) = Await repo.GetAllAsync()
        Assert.HasCount(1, products, "取込は全か無か。既存の1件だけが残るはず")
    End Function

    <TestMethod>
    Public Async Function ImportAsync_単価がマイナスだとCHECK制約で全体が取り消される() As Task
        Dim repo As ProductRepository = NewRepository()

        Dim ex As Exception = Await CaptureAsync(Function() repo.ImportAsync(New Product() {
            NewProduct("N1", "OK", 1D),
            NewProduct("N2", "NG", -1D)
        }))

        Assert.IsNotNull(ex)

        Dim products As List(Of Product) = Await repo.GetActiveAsync()
        Assert.IsEmpty(products)
    End Function

    <TestMethod>
    Public Async Function GetCodesAsync_登録済の商品コードを返す() As Task
        Await AddProductAsync("B1", "a", 1D)
        Await AddProductAsync("B2", "b", 1D, False)

        Dim codes As List(Of String) = Await NewRepository().GetCodesAsync()

        Assert.HasCount(2, codes, "販売停止の商品も含む")
        CollectionAssert.AreEquivalent(New String() {"B1", "B2"}, codes)
    End Function

    <TestMethod>
    Public Async Function 取込の流れ() As Task
        Dim repo As ProductRepository = NewRepository()
        Dim csv As String = String.Join(vbCrLf, "商品コード,商品名,単価,販売中", "C1,りんご,120,販売中", "C2,みかん,80,販売停止")

        Dim plan1 As ProductImportPlan = ProductCsvImporter.Analyze(csv, Await repo.GetCodesAsync())
        Assert.IsTrue(plan1.CanImport)
        Await repo.ImportAsync(plan1.Products)

        Dim plan2 As ProductImportPlan = ProductCsvImporter.Analyze(csv, Await repo.GetCodesAsync())
        Assert.IsEmpty(plan2.Errors)
        Assert.HasCount(2, plan2.Skipped)
        Assert.IsFalse(plan2.CanImport, "取り込む行がない")
    End Function

End Class
