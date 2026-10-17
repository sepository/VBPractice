''' <summary>
''' 注文情報
''' </summary>
Public Class CustomerOrder

    Public Property OrderId As Integer
    Public Property CustomerId As Integer
    Public Property OrderDate As DateTime = DateTime.Today
    Public Property Note As String = String.Empty
    Public Property CreatedAt As DateTime
    Public Property UpdatedAt As DateTime

    ''' <summary>
    ''' 顧客のナビゲーション
    ''' </summary>
    Public Property Customer As Customer

    ''' <summary>
    ''' 注文明細のナビゲーション
    ''' </summary>
    Public Property Items As List(Of OrderItem) = New List(Of OrderItem)()

    ''' <summary>
    ''' 合計金額
    ''' </summary>
    Public ReadOnly Property Amount As Decimal
        Get
            Return Items.Sum(Function(i) i.Amount)
        End Get
    End Property

    ''' <summary>
    ''' 明細の行数
    ''' </summary>
    Public ReadOnly Property ItemCount As Integer
        Get
            Return Items.Count
        End Get
    End Property

    ''' <summary>
    ''' 一覧表示用の要約
    ''' </summary>
    Public ReadOnly Property Summary As String
        Get
            If Items.Count = 0 Then
                Return "(明細なし)"
            End If

            Dim firstName As String = Items(0).ProductName
            If Items.Count = 1 Then
                Return firstName
            End If

            Return $"{firstName} ほか{Items.Count - 1}件"
        End Get
    End Property

End Class
