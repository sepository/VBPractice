Imports Microsoft.Data.SqlClient

Public Class Repository

    Private ReadOnly _connectionString As String

    Public Sub New(connectionString As String)
        _connectionString = connectionString
    End Sub

    Public Function CreateConnection() As SqlConnection
        Return New SqlConnection(_connectionString)
    End Function

End Class
