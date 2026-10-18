Public Class OrderLineRow

    Public Property OrderId As Integer
    Public Property OrderDate As DateTime
    Public Property CustomerId As Integer
    Public Property CustomerName As String
    Public Property ProductCode As String
    Public Property ProductName As String
    Public Property Quantity As Integer
    Public Property UnitPrice As Decimal

    Public ReadOnly Property Amount As Decimal
        Get
            Return Quantity * UnitPrice
        End Get
    End Property

End Class
