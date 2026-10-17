''' <summary>
''' 注文明細
''' </summary>
Public Class OrderItem

    Public Property OrderItemId As Integer
    Public Property OrderId As Integer
    Public Property ProductName As String = String.Empty
    Public Property Quantity As Integer = 0
    Public Property UnitPrice As Decimal

    ''' <summary>
    ''' 注文のナビゲーション
    ''' </summary>
    Public Property Order As CustomerOrder

    ''' <summary>
    ''' 金額（数量×単価）
    ''' </summary>
    Public ReadOnly Property Amount As Decimal
        Get
            Return Quantity * UnitPrice
        End Get
    End Property

End Class
