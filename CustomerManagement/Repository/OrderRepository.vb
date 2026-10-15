Imports Microsoft.Data.SqlClient

Public Class OrderRepository
    Inherits Repository

    Public Sub New(connectionString As String)
        MyBase.New(connectionString)
    End Sub

    ''' <summary>
    ''' 取得
    ''' </summary>
    ''' <param name="customerId">
    ''' 顧客ID
    ''' </param>
    ''' <returns>
    ''' 注文情報
    ''' </returns>
    Public Function GetByCustomerId(customerId As Integer) As List(Of CustomerOrder)
        Const sql As String =
            "SELECT OrderId, CustomerId, OrderDate, ProductName, Quantity, UnitPrice, Note, CreatedAt, UpdatedAt " &
            "FROM dbo.Orders " &
            "WHERE CustomerId = @CustomerId " &
            "ORDER BY OrderDate DESC, OrderId DESC"

        Dim result As New List(Of CustomerOrder)()

        Using conn As SqlConnection = CreateConnection()
            Using cmd As New SqlCommand(sql, conn)
                cmd.Parameters.Add("@CustomerId", SqlDbType.Int).Value = customerId

                conn.Open()
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        result.Add(MapOrder(reader))
                    End While
                End Using
            End Using
        End Using

        Return result
    End Function

    ''' <summary>
    ''' 新規
    ''' </summary>
    ''' <param name="order">
    ''' 注文情報
    ''' </param>
    ''' <returns>
    ''' 注文ID
    ''' </returns>
    Public Function Insert(order As CustomerOrder) As Integer
        Const sql As String =
            "INSERT INTO dbo.Orders (CustomerId, OrderDate, ProductName, Quantity, UnitPrice, Note )" &
            "OUTPUT INSERTED.OrderId " &
            "VALUES (@CustomerId, @OrderDate, @ProductName, @Quantity, @UnitPrice, @Note)"

        Using conn As SqlConnection = CreateConnection()
            Using cmd As New SqlCommand(sql, conn)
                AddOrderParameters(cmd, order)
                cmd.Parameters.Add("@CustomerId", SqlDbType.Int).Value = order.CustomerId

                conn.Open()
                Return Convert.ToInt32(cmd.ExecuteScalar())
            End Using
        End Using
    End Function

    ''' <summary>
    ''' 更新
    ''' </summary>
    ''' <param name="order">
    ''' 注文情報
    ''' </param>
    ''' <returns>
    ''' 成功 => True
    ''' </returns>
    Public Function Update(order As CustomerOrder) As Boolean
        Const sql As String =
            "UPDATE dbo.Orders " &
            "SET " &
            "OrderDate = @OrderDate, ProductName = @ProductName, Quantity = @Quantity, " &
            "UnitPrice = @UnitPrice, Note = @Note, UpdatedAt = SYSDATETIME() " &
            "WHERE OrderId = @OrderId"

        Using conn As SqlConnection = CreateConnection()
            Using cmd As New SqlCommand(sql, conn)
                AddOrderParameters(cmd, order)
                cmd.Parameters.Add("@OrderId", SqlDbType.Int).Value = order.OrderId

                conn.Open()
                Return cmd.ExecuteNonQuery() > 0
            End Using
        End Using
    End Function

    ''' <summary>
    ''' 削除
    ''' </summary>
    ''' <param name="orderId">
    ''' 注文ID
    ''' </param>
    ''' <returns>
    ''' 成功 => True
    ''' </returns>
    Public Function Delete(orderId As Integer) As Boolean
        Const sql As String = "DELETE FROM dbo.Orders WHERE OrderId = @OrderId"

        Using conn As SqlConnection = CreateConnection()
            Using cmd As New SqlCommand(sql, conn)
                cmd.Parameters.Add("@OrderId", SqlDbType.Int).Value = orderId

                conn.Open()
                Return cmd.ExecuteNonQuery() > 0
            End Using
        End Using
    End Function

    ''' <summary>
    ''' パラメータの一括セット
    ''' </summary>
    ''' <param name="cmd">
    ''' SQLコマンド
    ''' </param>
    ''' <param name="order">
    ''' 注文情報
    ''' </param>
    Private Shared Sub AddOrderParameters(cmd As SqlCommand, order As CustomerOrder)
        cmd.Parameters.Add("@OrderDate", SqlDbType.Date).Value = order.OrderDate.Date
        cmd.Parameters.Add("@ProductName", SqlDbType.NVarChar, 100).Value = order.ProductName.Trim()
        cmd.Parameters.Add("@Quantity", SqlDbType.Int).Value = order.Quantity
        Dim unitPrice As SqlParameter = cmd.Parameters.Add("@UnitPrice", SqlDbType.Decimal)
        unitPrice.Precision = 18
        unitPrice.Scale = 2
        unitPrice.Value = order.UnitPrice
        cmd.Parameters.Add("@Note", SqlDbType.NVarChar, 500).Value = ToDbValue(order.Note)
    End Sub

    Private Shared Function ToDbValue(value As String) As Object
        If String.IsNullOrWhiteSpace(value) Then
            Return DBNull.Value
        End If
        Return value.Trim()
    End Function

    Private Shared Function MapOrder(reader As SqlDataReader) As CustomerOrder
        Return New CustomerOrder With {
            .OrderId = reader.GetInt32(0),
            .CustomerId = reader.GetInt32(1),
            .OrderDate = reader.GetDateTime(2),
            .ProductName = reader.GetString(3),
            .Quantity = reader.GetInt32(4),
            .UnitPrice = reader.GetDecimal(5),
            .Note = If(reader.IsDBNull(6), String.Empty, reader.GetString(6)),
            .CreatedAt = reader.GetDateTime(7),
            .UpdatedAt = reader.GetDateTime(8)
        }
    End Function

End Class
