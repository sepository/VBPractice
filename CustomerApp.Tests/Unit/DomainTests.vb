Imports DocumentFormat.OpenXml.Drawing
Imports Microsoft.VisualStudio.TestTools.UnitTesting

''' <summary>
''' エンティティの計算プロパティと、DbHelperの小さな関数のテスト
''' </summary>
<TestClass>
Public Class DomainTests

    Private Shared Function NewItem(name As String, quantity As Integer, unitPirce As Decimal) As OrderItem
        Return New OrderItem With {.ProductName = name, .Quantity = quantity, .UnitPrice = unitPirce}
    End Function

    ' ---- OrderItem / CustomerOrder ----

    <TestMethod>
    Public Sub OrderItem_金額は数量かける単価()
        Dim item As OrderItem = NewItem("ペン", 3, 1200)

        Assert.AreEqual(3600, item.Amount)
    End Sub

    <TestMethod>
    Public Sub CustomerOrder_金額は明細の合計()
        Dim order As New CustomerOrder()
        order.Items.Add(NewItem("A", 2, 1000D))
        order.Items.Add(NewItem("B", 1, 500D))

        Assert.AreEqual(2500D, order.Amount)
        Assert.AreEqual(2, order.ItemCount)
    End Sub

    <TestMethod>
    Public Sub CustomerOrder_明細がなければ金額は0()
        Dim order As New CustomerOrder()

        Assert.AreEqual(0D, order.Amount)
        Assert.AreEqual(0, order.ItemCount)
    End Sub

    <TestMethod>
    Public Sub CustomerOrder_要約は明細の数で変わる()
        Dim order As New CustomerOrder()
        Assert.AreEqual("(明細なし)", order.Summary)

        order.Items.Add(NewItem("ノートPC", 1, 100D))
        Assert.AreEqual("ノートPC", order.Summary)

        order.Items.Add(NewItem("マウス", 1, 100D))
        order.Items.Add(NewItem("キーボード", 1, 100D))
        Assert.AreEqual("ノートPC ほか2件", order.Summary)
    End Sub

    <TestMethod>
    Public Sub Product_表示名は商品コードと商品名()
        Dim product As New Product With {.ProductCode = "P0001", .Name = "ノートPC"}

        Assert.AreEqual("P0001 ノートPC", product.DisplayName)
    End Sub

    ' ---- DbHelper ----

    <TestMethod>
    Public Sub NullIfBlank_空文字や空白だけならNothing()
        Assert.IsNull(DbHelper.NullIfBlank(Nothing))
        Assert.IsNull(DbHelper.NullIfBlank(""))
        Assert.IsNull(DbHelper.NullIfBlank("   "))
    End Sub

    <TestMethod>
    Public Sub NullIfBlank_前後の空白は取り除かれる()
        Assert.AreEqual("abc", DbHelper.NullIfBlank(" abc "))
    End Sub

    <TestMethod>
    Public Sub GetFullMessage_入れ子のメッセージがすべて凍結される()
        Dim ex As New Exception("外側", New Exception("中", New Exception("内側")))
        Dim message As String = DbHelper.GetFullMessage(ex)

        Assert.Contains("外側", message)
        Assert.Contains("中", message)
        Assert.Contains("内側", message)
        ' 外側 => 中 => 内側の順
        Assert.IsLessThan(message.IndexOf("中"), message.IndexOf("外側"))  ' 外側 < 中
        Assert.IsLessThan(message.IndexOf("内側"), message.IndexOf("中"))  ' 中 < 内側
    End Sub

    <TestMethod>
    Public Sub IsForeignKeyViolation_SQLエラーを含まない例外ならFalse()
        Assert.IsFalse(DbHelper.IsForeignKeyViolation(New Exception("関係ない例外")))
        Assert.IsFalse(DbHelper.IsForeignKeyViolation(Nothing))
    End Sub

    <TestMethod>
    Public Sub IsUniqueViolation_SQLエラーを含まない例外ならFalse()
        Assert.IsFalse(DbHelper.IsUniqueViolation(New Exception("関係ない例外")))
    End Sub

End Class
