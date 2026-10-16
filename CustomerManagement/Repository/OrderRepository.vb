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
    Public Function GetByCustomerId(customerId As Integer) As List(Of CustomerOrder)
        Using db As New AppDbContext(_connectionString)
            Return db.Orders.AsNoTracking().
                Where(Function(o) o.CustomerId = customerId).
                OrderByDescending(Function(o) o.OrderDate).
                ThenByDescending(Function(o) o.OrderId).
                ToList()
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
    Public Function Insert(order As CustomerOrder) As Integer
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
            db.SaveChanges()

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
    Public Function Update(order As CustomerOrder) As Boolean
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

            db.SaveChanges()
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
    Public Function Delete(orderId As Integer) As Boolean
        Using db As New AppDbContext(_connectionString)
            Dim entity As CustomerOrder = db.Orders.Find(orderId)
            If entity Is Nothing Then
                Return False
            End If

            db.Orders.Remove(entity)
            db.SaveChanges()
            Return True
        End Using
    End Function

End Class
