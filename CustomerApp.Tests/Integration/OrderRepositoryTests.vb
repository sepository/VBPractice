Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualStudio.TestTools.UnitTesting

''' <summary>
''' OrderRepositoryの結合テスト
''' </summary>
<TestClass>
<TestCategory("Integration")>
Public Class OrderRepositoryTests
    Inherits DatabaseTestBase

    Private Function NewRepository() As OrderRepository
        Return New OrderRepository(ConnectionString)
    End Function

    ''' <summary>
    ''' 商品名の最大長(100文字)を超える文字列。
    ''' </summary>
    Private Shared Function TooLongName() As String
        Return New String("あ"c, 101)
    End Function

    <TestMethod>
    Public Async Function InsertAsync_見出しと明細がまとめて保存される() As Task
        Dim customerId As Integer = Await AddCustomerAsync("山田 太郎")
        Dim productId As Integer = Await AddProductAsync("P1", "ペン", 100D)

        Await AddOrderAsync(customerId, New DateTime(2026, 10, 1),
                            NewItem(productId, "ペン", 2, 100D), NewItem(productId, "ペン(赤)", 1, 120D))
        Dim orders As List(Of CustomerOrder) = Await NewRepository().GetByCustomerIdAsync(customerId)

        Assert.HasCount(1, orders)
        Assert.HasCount(2, orders(0).Items)
        Assert.AreEqual(320D, orders(0).Amount)
    End Function

    <TestMethod>
    Public Async Function GetByCustomerIdAsync_新しい注文が並ぶ() As Task
        Dim customerId As Integer = Await AddCustomerAsync("山田 太郎")
        Dim productId As Integer = Await AddProductAsync("P1", "ペン", 100D)
        Await AddOrderAsync(customerId, New DateTime(2026, 10, 1), NewItem(productId, "古い注文", 1, 100D))
        Await AddOrderAsync(customerId, New DateTime(2026, 10, 15), NewItem(productId, "新しい注文", 1, 100D))
        Dim orders As List(Of CustomerOrder) = Await NewRepository().GetByCustomerIdAsync(customerId)

        Assert.HasCount(2, orders)
        Assert.AreEqual("新しい注文", orders(0).Items(0).ProductName)
        Assert.AreEqual("古い注文", orders(1).Items(0).ProductName)
    End Function

    <TestMethod>
    Public Async Function GetByCustomerIdAsync_他の顧客の注文は含まれない() As Task
        Dim customerA As Integer = Await AddCustomerAsync("顧客A")
        Dim customerB As Integer = Await AddCustomerAsync("顧客B")
        Dim productId As Integer = Await AddProductAsync("P1", "ペン", 100D)
        Await AddOrderAsync(customerA, New DateTime(2026, 10, 1), NewItem(productId, "A の注文", 1, 100D))
        Await AddOrderAsync(customerB, New DateTime(2026, 10, 1), NewItem(productId, "B の注文", 1, 100D))
        Dim orders As List(Of CustomerOrder) = Await NewRepository().GetByCustomerIdAsync(customerA)

        Assert.HasCount(1, orders)
        Assert.AreEqual("A の注文", orders(0).Items(0).ProductName)
    End Function

    <TestMethod>
    Public Async Function UpdateAsync_明細が入れ替わる() As Task
        Dim customerId As Integer = Await AddCustomerAsync("山田 太郎")
        Dim productId As Integer = Await AddProductAsync("P1", "ペン", 100D)
        Dim orderId As Integer = Await AddOrderAsync(customerId, New DateTime(2026, 10, 1),
                                                     NewItem(productId, "古い明細1", 1, 100D),
                                                     NewItem(productId, "古い明細2", 1, 100D))
        Dim repo As OrderRepository = NewRepository()

        Dim updated As New CustomerOrder With {
            .OrderId = orderId, .CustomerId = customerId, .OrderDate = New DateTime(2026, 10, 2), .Note = "更新後"
        }
        updated.Items.Add(NewItem(productId, "新しい明細", 5, 200D))
        Dim result As Boolean = Await repo.UpdateAsync(updated)

        Assert.IsTrue(result)
        Dim orders As List(Of CustomerOrder) = Await repo.GetByCustomerIdAsync(customerId)
        Assert.HasCount(1, orders)
        Assert.AreEqual("更新後", orders(0).Note)
        Assert.HasCount(1, orders(0).Items)
        Assert.AreEqual(1000D, orders(0).Amount)
    End Function

    <TestMethod>
    Public Async Function UpdateAsync_存在しない注文ならFalseを返す() As Task
        Dim customerId As Integer = Await AddCustomerAsync("山田 太郎")
        Dim productId As Integer = Await AddProductAsync("P1", "ペン", 100D)
        Dim order As New CustomerOrder With {.OrderId = 999999, .CustomerId = customerId}
        order.Items.Add(NewItem(productId, "x", 1, 100D))
        Dim result As Boolean = Await NewRepository().UpdateAsync(order)

        Assert.IsFalse(result)
    End Function

    ' ---- トランザクション ----

    <TestMethod>
    Public Async Function InsertAsync_明細の保存に失敗したら注文も保存されない() As Task
        Dim customerId As Integer = Await AddCustomerAsync("山田 太郎")
        Dim productId As Integer = Await AddProductAsync("P1", "ペン", 100D)
        Dim repo As OrderRepository = NewRepository()

        ' 1行目は正常、2行目は商品名が長すぎてDBに拒否される。
        Dim order As New CustomerOrder With {
            .CustomerId = customerId, .OrderDate = New DateTime(2026, 10, 1)
        }
        order.Items.Add(NewItem(productId, "正常な明細", 1, 100D))
        order.Items.Add(NewItem(productId, TooLongName(), 1, 100D))

        Dim ex As Exception = Await CaptureAsync(Function() repo.InsertAsync(order))
        Assert.IsNotNull(ex, "DBに拒否されて、例外が起きるはず")

        ' 暗黙のトランザクション : 見出しも、正常だった1行目も保存されていないはず。
        Dim orders As List(Of CustomerOrder) = Await repo.GetByCustomerIdAsync(customerId)
        Assert.HasCount(0, orders, "注文が残っている(ロールバックされていない)")

        Using db As New AppDbContext(ConnectionString)
            Assert.AreEqual(0, Await db.OrderItems.CountAsync(), "明細が残っている(ロールバックされていない)")
        End Using
    End Function

    <TestMethod>
    Public Async Function UpdateAsync_途中で失敗したら元の内容のまま残る() As Task
        Dim customerId As Integer = Await AddCustomerAsync("山田 太郎")
        Dim productId As Integer = Await AddProductAsync("P1", "ペン", 100D)
        Dim repo As OrderRepository = NewRepository()

        Dim original As New CustomerOrder With {
            .CustomerId = customerId, .OrderDate = New DateTime(2026, 10, 1), .Note = "元のメモ"
        }
        original.Items.Add(NewItem(productId, "元の明細1", 1, 100D))
        original.Items.Add(NewItem(productId, "元の明細2", 1, 100D))
        Dim orderId As Integer = Await repo.InsertAsync(original)

        ' 更新
        Dim updated As New CustomerOrder With {
            .OrderId = orderId, .CustomerId = customerId, .OrderDate = New DateTime(2026, 12, 31), .Note = "更新後のメモ"
        }
        updated.Items.Add(NewItem(productId, TooLongName(), 1, 100D))

        Dim ex As Exception = Await CaptureAsync(Function() repo.UpdateAsync(updated))
        Assert.IsNotNull(ex, "DBに拒否されて、例外が起きるはず")

        ' 明示的なトランザクション : SQLで直接実行した「古い明細の削除」も含めて、元に戻っているはず。
        Dim orders As List(Of CustomerOrder) = Await repo.GetByCustomerIdAsync(customerId)
        Assert.HasCount(1, orders)
        Assert.AreEqual("元のメモ", orders(0).Note, "見出しの更新が残っている")
        Assert.AreEqual(New DateTime(2026, 10, 1), orders(0).OrderDate, "注文日の更新が残っている")
        Assert.HasCount(2, orders(0).Items, "古い明細の削除が残っている(ロールバックされていない)")
    End Function

    <TestMethod>
    Public Async Function DeleteAsync_注文を削除すると明細も連鎖して削除される() As Task
        Dim customerId As Integer = Await AddCustomerAsync("山田 太郎")
        Dim productId As Integer = Await AddProductAsync("P1", "ペン", 100D)
        Dim orderId As Integer = Await AddOrderAsync(customerId, New DateTime(2026, 10, 1),
                                                     NewItem(productId, "明細1", 1, 100D), NewItem(productId, "明細2", 1, 100D))

        Dim result As Boolean = Await NewRepository().DeleteAsync(orderId)
        Assert.IsTrue(result)

        Using db As New AppDbContext(ConnectionString)
            Assert.AreEqual(0, Await db.Orders.CountAsync())
            Assert.AreEqual(0, Await db.OrderItems.CountAsync(), "明細が残っている(連鎖削除されていない)")
        End Using
    End Function

End Class
