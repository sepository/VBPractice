Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualStudio.TestTools.UnitTesting

''' <summary>
''' 顧客のメールアドレスの一意インデックスのテスト
''' </summary>
<TestClass>
<TestCategory("Integration")>
Public Class CustomerEmailUniqueTests
    Inherits DatabaseTestBase

    Private Function NewRepository() As CustomerRepository
        Return New CustomerRepository(ConnectionString)
    End Function

    Private Shared Function NewCustomer(name As String, Optional email As String = "") As Customer
        Return New Customer With {.Name = name, .Email = email}
    End Function

    Private Shared Sub AssertUniqueViolation(ex As Exception)
        Assert.IsNotNull(ex, "例外が起きるはず")
        Assert.IsTrue(DbHelper.IsUniqueViolation(ex), "一意制約違反として判定できるはず: " & DbHelper.GetFullMessage(ex))
    End Sub

    ' ---- 登録・更新 ----

    <TestMethod>
    Public Async Function InsertAsync_同じメールアドレスは登録できない() As Task
        Dim repo As CustomerRepository = NewRepository()
        Await repo.InsertAsync(NewCustomer("山田", "a@example.com"))

        Dim ex As Exception = Await CaptureAsync(Function() repo.InsertAsync(NewCustomer("佐藤", "a@example.com")))

        AssertUniqueViolation(ex)
        Assert.IsInstanceOfType(Of DbUpdateException)(ex)
    End Function

    <TestMethod>
    Public Async Function InsertAsync_大文字小文字が違うだけでも重複とみなされる() As Task
        Dim repo As CustomerRepository = NewRepository()
        Await repo.InsertAsync(NewCustomer("山田", "Taro@example.com"))

        Dim ex As Exception = Await CaptureAsync(Function() repo.InsertAsync(NewCustomer("佐藤", "taro@example.com")))

        AssertUniqueViolation(ex)
    End Function

    <TestMethod>
    Public Async Function InsertAsync_メールアドレスが空の顧客は何件でも登録できる() As Task
        Dim repo As CustomerRepository = NewRepository()

        Await repo.InsertAsync(NewCustomer("A"))
        Await repo.InsertAsync(NewCustomer("B", "  "))
        Await repo.InsertAsync(NewCustomer("C", Nothing))

        Dim customers As List(Of Customer) = Await repo.GetAllAsync()
        Assert.HasCount(3, customers)
    End Function

    <TestMethod>
    Public Async Function UpdateAsync_自分自身のメールアドレスのまま他の項目を更新できる() As Task
        Dim repo As CustomerRepository = NewRepository()
        Dim id As Integer = Await repo.InsertAsync(NewCustomer("山田", "a@example.com"))

        Dim updated As Boolean = Await repo.UpdateAsync(New Customer With {
            .CustomerId = id, .Name = "山田 改め", .Email = "a@example.com"
        })

        Assert.IsTrue(updated)
    End Function

    <TestMethod>
    Public Async Function DeleteAsync_削除した顧客のメールアドレスは再び使える() As Task
        Dim repo As CustomerRepository = NewRepository()
        Dim id As Integer = Await repo.InsertAsync(NewCustomer("山田", "a@example.com"))
        Await repo.DeleteAsync(id)

        Dim newId As Integer = Await repo.InsertAsync(NewCustomer("佐藤", "a@example.com"))

        Assert.IsGreaterThanOrEqualTo(0, newId)
    End Function

    ' ---- 一括取込 ----

    <TestMethod>
    Public Async Function ImportAsync_登録済と同じメールアドレスがあれば全体が取り消される() As Task
        Dim repo As CustomerRepository = NewRepository()
        Await repo.InsertAsync(NewCustomer("既存", "a@example.com"))

        Dim ex As Exception = Await CaptureAsync(Function() repo.ImportAsync(New Customer() {
            NewCustomer("新規1", "new1@example.com"),
            NewCustomer("重複1", "a@example.com"),
            NewCustomer("新規2", "new2@example.com")
        }))

        AssertUniqueViolation(ex)

        Dim customers As List(Of Customer) = Await repo.GetAllAsync()
        Assert.HasCount(1, customers, "取込は全か無か。既存の1件だけが残るはず")
    End Function

    <TestMethod>
    Public Async Function ImportAsync_同じ取込の中の重複も全体が取り消される() As Task
        Dim repo As CustomerRepository = NewRepository()

        Dim ex As Exception = Await CaptureAsync(Function() repo.ImportAsync(New Customer() {
            NewCustomer("A", "same@example.com"),
            NewCustomer("B", "same@example.com")
        }))

        AssertUniqueViolation(ex)

        Dim customers As List(Of Customer) = Await repo.GetAllAsync()
        Assert.IsEmpty(customers)
    End Function

End Class
