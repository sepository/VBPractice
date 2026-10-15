Imports System.Data
Imports System.Data.Common
Imports Microsoft.Data.SqlClient

Public Class CustomerRepository

    Private ReadOnly _connectionString As String

    Public Sub New(connectionString As String)
        _connectionString = connectionString
    End Sub

    ''' <summary>
    ''' 全件取得
    ''' 検索ワードがあれば、名前・フリガナ・メール・電話に部分一致するデータを抽出
    ''' </summary>
    ''' <param name="keyword">
    ''' 検索ワード
    ''' </param>
    ''' <returns></returns>
    Public Function GetAll(Optional keyword As String = Nothing) As List(Of Customer)
        Dim result As New List(Of Customer)()
        Dim hasKeyword As Boolean = Not String.IsNullOrEmpty(keyword)

        Dim sql As String = "SELECT c.CustomerId, c.Name, c.Kana, c.Email, c.Phone, c.Address, " &
                            "c.Memo, c.CreatedAt, c.UpdatedAt, " &
                            "COUNT(o.OrderId) AS OrderCount, " &
                            "ISNULL(SUM(o.Quantity * o.UnitPrice), 0) AS TotalAmount " &
                            "FROM dbo.Customers As c " &
                            "LEFT JOIN dbo.Orders AS o ON o.CustomerId = c.CustomerId "
        If hasKeyword Then
            sql &= "WHERE Name LIKE @kw OR Kana LIKE @kw OR Email LIKE @kw OR Phone LIKE @kw "
        End If
        sql &= "GROUP BY c.CustomerId, c.Name, c.Kana, c.Email, c.Phone, c.Address, c.Memo, c.CreatedAt, c.UpdatedAt "
        sql &= "ORDER BY c.CustomerId "

        Using conn As New SqlConnection(_connectionString)
            Using cmd As New SqlCommand(sql, conn)
                If hasKeyword Then
                    cmd.Parameters.Add("@kw", SqlDbType.NVarChar, 200).Value = "%" & keyword.Trim() & "%"
                End If

                conn.Open()
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        Dim customer As Customer = MapCustomer(reader)
                        customer.OrderCount = reader.GetInt32(9)
                        customer.TotalAmount = reader.GetDecimal(10)
                        result.Add(customer)
                    End While
                End Using
            End Using
        End Using

        Return result
    End Function

    ''' <summary>
    ''' 1件取得
    ''' </summary>
    ''' <param name="customerId">
    ''' 顧客識別番号
    ''' </param>
    ''' <returns>
    ''' 顧客情報1件、存在しなければNothing
    ''' </returns>
    Public Function GetById(customerId As Integer) As Customer
        Const sql As String =
            "SELECT CustomerId, Name, Kana, Email, Phone, Address, Memo, CreatedAt, UpdatedAt " &
            "FROM dbo.Customers WHERE CustomerId = @CustomerId"

        Using conn As New SqlConnection(_connectionString)
            Using cmd As New SqlCommand(sql, conn)
                cmd.Parameters.Add("@CustomerId", SqlDbType.Int).Value = customerId

                conn.Open()
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    If reader.Read() Then
                        Return MapCustomer(reader)
                    End If
                End Using
            End Using
        End Using

        Return Nothing
    End Function

    ''' <summary>
    ''' 新規登録
    ''' </summary>
    ''' <param name="customer">
    ''' 顧客情報
    ''' </param>
    ''' <returns>
    ''' 顧客識別番号
    ''' </returns>
    Public Function Insert(customer As Customer) As Integer
        Const sql As String =
            "INSERT INTO dbo.Customers (Name, Kana, Email, Phone, Address, Memo) " &
            "OUTPUT INSERTED.CustomerId " &
            "VALUES (@Name, @Kana, @Email, @Phone, @Address, @Memo)"

        Using conn As New SqlConnection(_connectionString)
            Using cmd As New SqlCommand(sql, conn)
                AddCustomerParameters(cmd, customer)

                conn.Open()
                Return Convert.ToInt32(cmd.ExecuteScalar())
            End Using
        End Using
    End Function

    ''' <summary>
    ''' 更新
    ''' </summary>
    ''' <param name="customer">
    ''' 顧客情報
    ''' </param>
    ''' <returns></returns>
    Public Function Update(customer As Customer) As Boolean
        Const sql As String =
            "UPDATE dbo.Customers " &
            "SET Name = @Name, Kana = @Kana, Email = @Email, Phone = @Phone, Address = @Address, Memo = @Memo, UpdatedAt = SYSDATETIME() " &
            "WHERE CustomerId = @CustomerId"

        Using conn As New SqlConnection(_connectionString)
            Using cmd As New SqlCommand(sql, conn)
                AddCustomerParameters(cmd, customer)
                cmd.Parameters.Add("@CustomerId", SqlDbType.Int).Value = customer.CustomerId

                conn.Open()
                Return cmd.ExecuteNonQuery() > 0
            End Using
        End Using
    End Function

    ''' <summary>
    ''' 削除
    ''' </summary>
    ''' <param name="customerId">
    ''' 顧客識別番号
    ''' </param>
    ''' <returns></returns>
    Public Function Delete(customerId As Integer) As Boolean
        Const sql As String =
            "DELETE FROM dbo.Customers WHERE CustomerId = @CustomerId"

        Using conn As New SqlConnection(_connectionString)
            Using cmd As New SqlCommand(sql, conn)
                cmd.Parameters.Add("@CustomerId", SqlDbType.Int).Value = customerId

                conn.Open()
                Return cmd.ExecuteNonQuery() > 0
            End Using
        End Using
    End Function

    Private Shared Function GetString(reader As SqlDataReader, ordinal As Integer) As String
        Return If(reader.IsDBNull(ordinal), String.Empty, reader.GetString(ordinal))
    End Function

    Private Shared Function MapCustomer(reader As SqlDataReader) As Customer
        Return New Customer With {
            .CustomerId = reader.GetInt32(0),
            .Name = reader.GetString(1),
            .Kana = GetString(reader, 2),
            .Email = GetString(reader, 3),
            .Phone = GetString(reader, 4),
            .Address = GetString(reader, 5),
            .Memo = GetString(reader, 6),
            .CreatedAt = reader.GetDateTime(7),
            .UpdatedAt = reader.GetDateTime(8)
        }
    End Function

    Private Shared Sub AddCustomerParameters(cmd As SqlCommand, cusomer As Customer)
        cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = cusomer.Name
        cmd.Parameters.Add("@Kana", SqlDbType.NVarChar, 100).Value = ToDbValue(cusomer.Kana)
        cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 254).Value = ToDbValue(cusomer.Email)
        cmd.Parameters.Add("@Phone", SqlDbType.NVarChar, 20).Value = ToDbValue(cusomer.Phone)
        cmd.Parameters.Add("@Address", SqlDbType.NVarChar, 200).Value = ToDbValue(cusomer.Address)
        cmd.Parameters.Add("@Memo", SqlDbType.NVarChar, 500).Value = ToDbValue(cusomer.Memo)
    End Sub

    Private Shared Function ToDbValue(value As String) As Object
        If String.IsNullOrWhiteSpace(value) Then
            Return DBNull.Value
        End If
        Return value.Trim()
    End Function

End Class
