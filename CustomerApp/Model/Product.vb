Public Class Product

    Public Property ProductId As Integer
    Public Property ProductCode As String = String.Empty
    Public Property Name As String = String.Empty
    Public Property UnitPrice As Decimal
    Public Property IsActive As Boolean = True
    Public Property CreatedAt As DateTime
    Public Property UpdatedAt As DateTime

    ''' <summary>
    ''' ComboBox等の表示用
    ''' </summary>
    Public ReadOnly Property DisplayName As String
        Get
            Return $"{ProductCode} {Name}"
        End Get
    End Property

End Class
