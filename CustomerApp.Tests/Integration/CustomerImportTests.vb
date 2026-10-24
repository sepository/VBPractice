Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
<TestCategory("Integration")>
Public Class CustomerImportTests
    Inherits DatabaseTestBase

    Private Function NewRepository() As CustomerRepository
        Return New CustomerRepository(ConnectionString)
    End Function

    Private Shared Function NewCustomer(name As String, Optional email As String = "") As Customer
        Return New Customer With {.Name = name, .Email = email}
    End Function

    <TestMethod>
    Public Async Function ImportAsync_ImportAsync_まとめて登録できる() As Task
        Dim count As Integer = Await NewRepository().ImportAsync(New Customer() {
            New Customer With {
                .Name = "山田 太郎", .Kana = "ヤマダ タロウ", .Email = "taro@example.com",
                .Phone = "03-0000-0000", .Address = "東京都", .Memo = "メモ"
            },
            NewCustomer("佐藤 花子")
        })

        Dim customers As List(Of Customer) = Await NewRepository().GetAllAsync()

        Assert.AreEqual(2, count)
        Assert.HasCount(2, customers)

        Dim taro As Customer = customers.Single(Function(c) c.Name = "山田 太郎")
        Assert.AreEqual("ヤマダ タロウ", taro.Kana)
        Assert.AreEqual("taro@example.com", taro.Email)
        Assert.AreEqual("東京都", taro.Address)
    End Function

    <TestMethod>
    Public Async Function ImportAsync_空の項目はNULLとして保存される() As Task
        Await NewRepository().ImportAsync(New Customer() {NewCustomer("山田")})

        Dim customers As List(Of Customer) = Await NewRepository().GetAllAsync()

        ' アプリの検索は NULL と空文字を区別しないが、登録済みの顧客(InsertAsync)と同じ状態であることを確かめる
        Dim viaInsert As Integer = Await NewRepository().InsertAsync(NewCustomer("佐藤"))
        Dim a As Customer = Await NewRepository().GetByIdAsync(customers(0).CustomerId)
        Dim b As Customer = Await NewRepository().GetByIdAsync(viaInsert)

        Assert.AreEqual(a.Email, b.Email)
        Assert.AreEqual(a.Memo, b.Memo)
    End Function

    <TestMethod>
    Public Async Function ImportAsync_途中で失敗したら1件も登録されない() As Task
        Dim tooLong As String = New String("あ"c, 101)

        Dim caught As Exception = Nothing
        Try
            Await NewRepository().ImportAsync(New Customer() {NewCustomer("正常な顧客"), NewCustomer(tooLong)})
        Catch ex As Exception
            caught = ex
        End Try

        Assert.IsNotNull(caught, "例外が起きるはず")
        Assert.IsInstanceOfType(Of DbUpdateException)(caught)

        Dim customers As List(Of Customer) = Await NewRepository().GetAllAsync()
        Assert.IsEmpty(customers, "正常な1件目も取り消されているはず")
    End Function

    <TestMethod>
    Public Async Function ImportAsync_空のリストなら何もしない() As Task
        Dim count As Integer = Await NewRepository().ImportAsync(New List(Of Customer)())

        Assert.AreEqual(0, count)
    End Function

    <TestMethod>
    Public Async Function GetEmailsAsync_メールアドレスのある顧客の分だけ返す() As Task
        Await NewRepository().InsertAsync(NewCustomer("A", "a@example.com"))
        Await NewRepository().InsertAsync(NewCustomer("B"))
        Await NewRepository().InsertAsync(NewCustomer("C", "c@example.com"))

        Dim emails As List(Of String) = Await NewRepository().GetEmailsAsync()

        Assert.HasCount(2, emails)
        CollectionAssert.AreEquivalent(New String() {"a@example.com", "c@example.com"}, emails)
    End Function

    Public Async Function 取り込みの流れ_既存と重複する行だけがスキップされる() As Task
        Await NewRepository().InsertAsync(NewCustomer("既存の山田", "yamada@example.com"))

        Dim csv As String =
            "名前,メール" & vbCrLf &
            "山田 太郎,Yamada@example.com" & vbCrLf &
            "佐藤 花子,sato@example.com"

        Dim existing As List(Of String) = Await NewRepository().GetEmailsAsync()
        Dim plan As CustomerImportPlan = CustomerCsvImporter.Analyze(csv, existing)
        Dim count As Integer = Await NewRepository().ImportAsync(plan.Customers)

        Dim customers As List(Of Customer) = Await NewRepository().GetAllAsync()

        Assert.AreEqual(1, count)
        Assert.HasCount(1, plan.Skipped)
        Assert.HasCount(2, customers)
        Assert.IsTrue(customers.Any(Function(c) c.Name = "佐藤 花子"))
        Assert.IsFalse(customers.Any(Function(c) c.Name = "山田 太郎"), "重複なので取り込まれていないはず")
    End Function

End Class
