Imports System.Data
Imports System.Data.Common
Imports System.IO
Imports Microsoft.Data.SqlClient
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Logging.Abstractions

Public Class CustomerRepository

    Private ReadOnly _connectionString As String

    Public Sub New(connectionString As String)
        _connectionString = connectionString
    End Sub

    ''' <summary>
    ''' 全件取得
    ''' 検索ワードがあれば、名前・フリガナ・メール・電話に部分一致するデータを抽出
    ''' </summary>
    ''' <param name="keyword">
    ''' 検索ワード
    ''' </param>
    ''' <returns></returns>
    Public Async Function GetAllAsync(Optional keyword As String = Nothing) As Task(Of List(Of Customer))
        Using db As New AppDbContext(_connectionString)
            Dim query As IQueryable(Of Customer) = db.Customers.AsNoTracking()

            ' WHERE
            If Not String.IsNullOrWhiteSpace(keyword) Then
                Dim kw As String = keyword.Trim()

                query = query.Where(Function(c) c.Name.Contains(kw) OrElse
                                                c.Kana.Contains(kw) OrElse
                                                c.Email.Contains(kw) OrElse
                                                c.Phone.Contains(kw))
            End If

            ' 顧客、顧客毎の注文数を取得
            Dim rows = Await query.OrderBy(Function(c) c.CustomerId).Select(
                Function(c) New With {
                    .Customer = c,
                    .OrderCount = c.Orders.Count()
                }).ToListAsync()

            ' 顧客毎の注文金額合計を取得
            Dim totals = Await db.OrderItems.AsNoTracking().
                GroupBy(Function(i) i.Order.CustomerId).
                Select(Function(g) New With {
                    .CustomerId = g.Key,
                    .Total = g.Sum(Function(i) i.Quantity * i.UnitPrice)
                }).ToListAsync()
            Dim totalByCutomer As Dictionary(Of Integer, Decimal) =
                totals.ToDictionary(Function(t) t.CustomerId, Function(t) t.Total)

            Dim result As New List(Of Customer)()
            For Each r In rows
                r.Customer.OrderCount = r.OrderCount

                Dim total As Decimal = 0D
                totalByCutomer.TryGetValue(r.Customer.CustomerId, total)
                r.Customer.TotalAmount = total

                result.Add(r.Customer)
            Next

            Return result
        End Using
    End Function

    ''' <summary>
    ''' 1件取得
    ''' </summary>
    ''' <param name="customerId">
    ''' 顧客識別番号
    ''' </param>
    ''' <returns>
    ''' 顧客情報1件、存在しなければNothing
    ''' </returns>
    Public Async Function GetByIdAsync(customerId As Integer) As Task(Of Customer)
        Using db As New AppDbContext(_connectionString)
            Return Await db.Customers.AsNoTracking().FirstOrDefaultAsync(Function(c) c.CustomerId = customerId)
        End Using
    End Function

    ''' <summary>
    ''' 新規登録
    ''' </summary>
    ''' <param name="customer">
    ''' 顧客情報
    ''' </param>
    ''' <returns>
    ''' 顧客識別番号
    ''' </returns>
    Public Async Function InsertAsync(customer As Customer) As Task(Of Integer)
        Using db As New AppDbContext(_connectionString)
            Dim entity As New Customer With {
                .Name = customer.Name.Trim(),
                .Kana = NullIfBlank(customer.Kana),
                .Email = NullIfBlank(customer.Email),
                .Phone = NullIfBlank(customer.Phone),
                .Address = NullIfBlank(customer.Address),
                .Memo = NullIfBlank(customer.Memo)
            }

            db.Customers.Add(entity)
            Await db.SaveChangesAsync()

            Return entity.CustomerId
        End Using
    End Function

    ''' <summary>
    ''' 更新
    ''' </summary>
    ''' <param name="customer">
    ''' 顧客情報
    ''' </param>
    ''' <returns></returns>
    Public Async Function UpdateAsync(customer As Customer) As Task(Of Boolean)
        Using db As New AppDbContext(_connectionString)
            Dim entity As Customer = db.Customers.FirstOrDefault(Function(c) c.CustomerId = customer.CustomerId)
            If entity Is Nothing Then
                Return False
            End If

            entity.Name = customer.Name.Trim()
            entity.Kana = NullIfBlank(customer.Kana)
            entity.Email = NullIfBlank(customer.Email)
            entity.Phone = NullIfBlank(customer.Phone)
            entity.Address = NullIfBlank(customer.Address)
            entity.Memo = NullIfBlank(customer.Memo)
            entity.UpdatedAt = DateTime.Now

            Await db.SaveChangesAsync()
            Return True
        End Using
    End Function

    ''' <summary>
    ''' 削除
    ''' </summary>
    ''' <param name="customerId">
    ''' 顧客識別番号
    ''' </param>
    ''' <returns></returns>
    Public Async Function DeleteAsync(customerId As Integer) As Task(Of Boolean)
        Using db As New AppDbContext(_connectionString)
            Dim entity As Customer = db.Customers.Find(customerId)
            If entity Is Nothing Then
                Return False
            End If

            db.Customers.Remove(entity)
            Await db.SaveChangesAsync()
            Return True
        End Using
    End Function

End Class
