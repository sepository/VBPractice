Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
<TestCategory("Integration")>
Public Class ReportRepositoryTests
    Inherits DatabaseTestBase

    <TestMethod>
    Public Async Function GetOrderLinesAsync_期間内の明細だけが取得され_境界の日付も含まれる() As Task
        Dim customerId As Integer = Await AddCustomerAsync("山田 太郎")
        Dim productId As Integer = Await AddProductAsync("P001", "商品A", 100D)

        Await AddOrderAsync(customerId, New DateTime(2026, 9, 30), NewItem(productId, "期間の前日", 1, 100D))
        Await AddOrderAsync(customerId, New DateTime(2026, 10, 1), NewItem(productId, "開始日", 1, 100D))
        Await AddOrderAsync(customerId, New DateTime(2026, 10, 31), NewItem(productId, "終了日", 1, 100D))
        Await AddOrderAsync(customerId, New DateTime(2026, 11, 1), NewItem(productId, "期間の翌日", 1, 100D))

        Dim lines As List(Of OrderLineRow) = Await New ReportRepository(ConnectionString).GetOrderLinesAsync(
            New DateTime(2026, 10, 1), New DateTime(2026, 10, 31))

        Assert.HasCount(2, lines)
        Assert.AreEqual("開始日", lines(0).ProductName)
        Assert.AreEqual("終了日", lines(1).ProductName)
    End Function

    <TestMethod>
    Public Async Function GetOrderLinesAsync_顧客名と商品コードと金額が入る() As Task
        Dim customerId As Integer = Await AddCustomerAsync("山田 太郎")
        Dim productId As Integer = Await AddProductAsync("P001", "商品A", 100D)
        Await AddOrderAsync(customerId, New DateTime(2026, 10, 1), NewItem(productId, "商品A", 3, 250D))

        Dim lines As List(Of OrderLineRow) = Await New ReportRepository(ConnectionString).GetOrderLinesAsync(
            New DateTime(2026, 10, 1), New DateTime(2026, 10, 1))

        Assert.HasCount(1, lines)
        Assert.AreEqual("山田 太郎", lines(0).CustomerName)
        Assert.AreEqual("P001", lines(0).ProductCode)
        Assert.AreEqual(750D, lines(0).Amount)
    End Function

    <TestMethod>
    Public Async Function GetOrderLinesAsync_複数の明細は注文日と注文ID順に並ぶ() As Task
        Dim customerId As Integer = Await AddCustomerAsync("山田 太郎")
        Dim productId As Integer = Await AddProductAsync("P001", "商品A", 100D)
        Await AddOrderAsync(customerId, New DateTime(2026, 10, 5), NewItem(productId, "後の注文", 1, 100D))
        Await AddOrderAsync(customerId, New DateTime(2026, 10, 1), NewItem(productId, "先の注文", 1, 100D))

        Dim lines As List(Of OrderLineRow) = Await New ReportRepository(ConnectionString).GetOrderLinesAsync(
            New DateTime(2026, 10, 1), New DateTime(2026, 10, 31))

        Assert.AreEqual("先の注文", lines(0).ProductName)
        Assert.AreEqual("後の注文", lines(1).ProductName)
    End Function

    <TestMethod>
    Public Async Function GetOrderLinesAsync_該当する注文がなければ空のリスト() As Task
        Dim lines As List(Of OrderLineRow) = Await New ReportRepository(ConnectionString).GetOrderLinesAsync(
            New DateTime(2026, 10, 1), New DateTime(2026, 10, 31))

        Assert.HasCount(0, lines)
    End Function

End Class
