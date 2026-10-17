''' <summary>
''' 注文情報
''' </summary>
Public Class CustomerOrder

    Public Property OrderId As Integer
    Public Property CustomerId As Integer
    Public Property OrderDate As DateTime = DateTime.Today
    Public Property ProductName As String = String.Empty
    Public Property Quantity As Integer = 1
    Public Property UnitPrice As Decimal
    Public Property Note As String = String.Empty
    Public Property CreatedAt As DateTime
    Public Property UpdatedAt As DateTime

    ''' <summary>
    ''' 注文=>顧客のリレーション
    ''' </summary>
    Public Property Customer As Customer

    Public ReadOnly Property Amount As Decimal
        Get
            Return Quantity * UnitPrice
        End Get
    End Property

End Class
