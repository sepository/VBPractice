''' <summary>
''' マイグレーションスクリプト1本
''' </summary>
Public Class MigrationScript

    Public ReadOnly Property Version As Integer
    Public ReadOnly Property Name As String
    Public ReadOnly Property Sql As String
    Public ReadOnly Property Checksum As String

    Public Sub New(version As Integer, name As String, sql As String)
        If String.IsNullOrWhiteSpace(sql) Then
            Throw New ArgumentException($"マイグレーションスクリプトの中身が空です: {name}", NameOf(sql))
        End If

        Me.Version = version
        Me.Name = name
        Me.Sql = sql
        Me.Checksum = MigrationChecksum.Compute(sql)

    End Sub

    Public Overrides Function ToString() As String
        Return Name
    End Function

End Class

''' <summary>
''' マイグレーションの失敗
''' </summary>
Public Class MigrationException
    Inherits Exception

    Public Sub New(message As String)
        MyBase.New(message)
    End Sub

    Public Sub New(message As String, innerException As Exception)
        MyBase.New(message, innerException)
    End Sub

End Class