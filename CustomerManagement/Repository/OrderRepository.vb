Imports Microsoft.EntityFrameworkCore

Public Class OrderRepository

    Private ReadOnly _connectionString As String

    Public Sub New(connectionString As String)
        _connectionString = connectionString
    End Sub

    ''' <summary>
    ''' 取得
    ''' </summary>
    ''' <param name="customerId">
    ''' 顧客ID
    ''' </param>
    ''' <returns>
    ''' 注文情報
    ''' </returns>
    Public Async Function GetByCustomerIdAsync(customerId As Integer) As Task(Of List(Of CustomerOrder))
        Using db As New AppDbContext(_connectionString)
            Return Await db.Orders.AsNoTracking().
                Where(Function(o) o.CustomerId = customerId).
                OrderByDescending(Function(o) o.OrderDate).
                ThenByDescending(Function(o) o.OrderId).
                ToListAsync()
        End Using
    End Function

    ''' <summary>
    ''' 新規
    ''' </summary>
    ''' <param name="order">
    ''' 注文情報
    ''' </param>
    ''' <returns>
    ''' 注文ID
    ''' </returns>
    Public Async Function InsertAsync(order As CustomerOrder) As Task(Of Integer)
        Using db As New AppDbContext(_connectionString)
            Dim entity As New CustomerOrder With {
                .CustomerId = order.CustomerId,
                .OrderDate = order.OrderDate.Date,
                .ProductName = order.ProductName.Trim(),
                .Quantity = order.Quantity,
                .UnitPrice = order.UnitPrice,
                .Note = NullIfBlank(order.Note)
            }

            db.Orders.Add(entity)
            Await db.SaveChangesAsync()

            Return entity.OrderId
        End Using
    End Function

    ''' <summary>
    ''' 更新
    ''' </summary>
    ''' <param name="order">
    ''' 注文情報
    ''' </param>
    ''' <returns>
    ''' 成功 => True
    ''' </returns>
    Public Async Function UpdateAsync(order As CustomerOrder) As Task(Of Boolean)
        Using db As New AppDbContext(_connectionString)
            Dim entity As CustomerOrder = db.Orders.FirstOrDefault(Function(o) o.OrderId = order.OrderId)
            If entity Is Nothing Then
                Return False
            End If

            entity.OrderDate = order.OrderDate.Date
            entity.ProductName = order.ProductName.Trim()
            entity.Quantity = order.Quantity
            entity.UnitPrice = order.UnitPrice
            entity.Note = NullIfBlank(order.Note)
            entity.UpdatedAt = DateTime.Now

            Await db.SaveChangesAsync()
            Return True
        End Using
    End Function

    ''' <summary>
    ''' 削除
    ''' </summary>
    ''' <param name="orderId">
    ''' 注文ID
    ''' </param>
    ''' <returns>
    ''' 成功 => True
    ''' </returns>
    Public Async Function DeleteAsync(orderId As Integer) As Task(Of Boolean)
        Using db As New AppDbContext(_connectionString)
            Dim entity As CustomerOrder = db.Orders.Find(orderId)
            If entity Is Nothing Then
                Return False
            End If

            db.Orders.Remove(entity)
            Await db.SaveChangesAsync()
            Return True
        End Using
    End Function

End Class
