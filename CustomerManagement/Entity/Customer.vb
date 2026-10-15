''' <summary>
''' 顧客
''' Customersテーブルの1件毎のデータを表す
''' </summary>
Public Class Customer

    Public Property CustomerId As Integer
    Public Property Name As String = String.Empty
    Public Property Kana As String = String.Empty
    Public Property Email As String = String.Empty
    Public Property Phone As String = String.Empty
    Public Property Address As String = String.Empty
    Public Property Memo As String = String.Empty
    Public Property CreatedAt As DateTime
    Public Property UpdatedAt As DateTime

    ' 表示専用(JOIN)
    Public Property OrderCount As Integer
    Public Property TotalAmount As Decimal

End Class
