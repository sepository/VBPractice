Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class ExportTableBuilderTests

    Private Shared Function NewCustomer() As Customer
        Return New Customer With {
            .CustomerId = 7,
            .Name = "山田 太郎",
            .Kana = "ヤマダ タロウ",
            .Email = "yamada@example.com",
            .Phone = "090-0000-0001",
            .Address = "愛知県名古屋市",
            .Memo = "メモ",
            .OrderCount = 2,
            .TotalAmount = 2800D
        }
    End Function

    Private Shared Function NewLine() As OrderLineRow
        Return New OrderLineRow With {
            .OrderId = 3,
            .OrderDate = New Date(2026, 10, 1),
            .CustomerId = 7,
            .CustomerName = "山田 太郎",
            .ProductCode = "P0001",
            .ProductName = "ノートPC",
            .Quantity = 2,
            .UnitPrice = 1000D
        }
    End Function

    ' ---- 顧客一覧 ----

    <TestMethod>
    Public Sub FromCustomers_見出し_表示形式_行の列数が揃っている()
        Dim table As ExportTable = ExportTableBuilder.FromCustomers(New List(Of Customer) From {NewCustomer()})

        Assert.HasCount(table.ColumnFormats.Count, table.Headers)
        Assert.HasCount(table.Rows(0).Length, table.Headers)
    End Sub

    <TestMethod>
    Public Sub FromCustomers_値が正しい列に入る()
        Dim table As ExportTable = ExportTableBuilder.FromCustomers(New List(Of Customer) From {NewCustomer()})
        Dim row As Object() = table.Rows(0)

        Assert.AreEqual("顧客ID", table.Headers(0))
        Assert.AreEqual(7, row(0))
        Assert.AreEqual("山田 太郎", row(1))
        Assert.AreEqual("注文数", table.Headers(7))
        Assert.AreEqual(2, row(7))
        Assert.AreEqual("累計金額", table.Headers(8))
        Assert.AreEqual(2800D, row(8))
    End Sub

    <TestMethod>
    Public Sub FromCustomer_顧客の数だけ行ができる()
        Dim customers As New List(Of Customer) From {NewCustomer(), NewCustomer(), NewCustomer()}
        Dim table As ExportTable = ExportTableBuilder.FromCustomers(customers)

        Assert.HasCount(3, table.Rows)
    End Sub

    ' ---- 注文明細 ----

    <TestMethod>
    Public Sub FromOrderLines_見出し_表示形式_行の列数が揃っている()
        Dim table As ExportTable = ExportTableBuilder.FromOrderLines(New List(Of OrderLineRow) From {NewLine()})

        Assert.HasCount(table.ColumnFormats.Count, table.Headers)
        Assert.HasCount(table.Rows(0).Length, table.Headers)
    End Sub

    <TestMethod>
    Public Sub FromOrderLines_金額は数量かける単価()
        Dim table As ExportTable = ExportTableBuilder.FromOrderLines(New List(Of OrderLineRow) From {NewLine()})
        Dim row As Object() = table.Rows(0)

        Assert.AreEqual("金額", table.Headers(8))
        Assert.AreEqual(2000D, row(8))  ' 2 × 1000
    End Sub

    <TestMethod>
    Public Sub FromOrderLines_注文日の列は日付の表示形式が付く()
        Dim table As ExportTable = ExportTableBuilder.FromOrderLines(New List(Of OrderLineRow) From {NewLine()})

        Assert.AreEqual("注文日", table.Headers(1))
        Assert.AreEqual("yyyy/mm/dd", table.ColumnFormats(1))
    End Sub

    <TestMethod>
    Public Sub FromOrderLines_顧客名と商品コードが入る()
        Dim table As ExportTable = ExportTableBuilder.FromOrderLines(New List(Of OrderLineRow) From {NewLine()})
        Dim row As Object() = table.Rows(0)

        Assert.AreEqual("山田 太郎", row(3))
        Assert.AreEqual("P0001", row(4))
    End Sub

End Class
