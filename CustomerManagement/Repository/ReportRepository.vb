Imports Microsoft.EntityFrameworkCore

''' <summary>
''' 帳票・エクスポート用の検索をまとめたクラス
''' </summary>
Public Class ReportRepository

    Private ReadOnly _connectionString As String

    Public Sub New(connectionString As String)
        _connectionString = connectionString
    End Sub

    ''' <summary>
    ''' 指定した期間内の注文明細を取得する。
    ''' </summary>
    Public Async Function GetOrderLinesAsync(fromDate As DateTime, toDate As DateTime) As Task(Of List(Of OrderLineRow))
        Dim fromDay As DateTime = fromDate.Date
        Dim toDay As DateTime = toDate.Date

        Using db As New AppDbContext(_connectionString)
            Return Await db.OrderItems.AsNoTracking().
                Where(Function(i) i.Order.OrderDate >= fromDay AndAlso i.Order.OrderDate <= toDay).
                OrderBy(Function(i) i.Order.OrderDate).
                ThenBy(Function(i) i.OrderId).
                ThenBy(Function(i) i.OrderItemId).
                Select(Function(i) New OrderLineRow With {
                    .OrderId = i.OrderId,
                    .OrderDate = i.Order.OrderDate,
                    .CustomerId = i.Order.CustomerId,
                    .CustomerName = i.Order.Customer.Name,
                    .ProductCode = i.Product.ProductCode,
                    .ProductName = i.ProductName,
                    .Quantity = i.Quantity,
                    .UnitPrice = i.UnitPrice
                }).
                ToListAsync()
        End Using
    End Function

End Class
