Imports System.Formats.Nrbf
Imports Microsoft.Data.SqlClient
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.Testing.Platform.OutputDevice
Imports Microsoft.VisualStudio.TestTools.UnitTesting

''' <summary>
''' EF Coreのモデル(AppDbContext)とマイグレーションで作ったデータベースが食い違ってないか
''' </summary>
<TestClass>
<TestCategory("Integration")>
Public Class SchemaConsistencyTests
    Inherits DatabaseTestBase

    Private Class DbColumn
        Public Property TableName As String = String.Empty
        Public Property ColumnName As String = String.Empty
        Public Property IsNullable As Boolean
        Public Property MaxLength As Integer?
    End Class

    Private Function ReadDbColumns() As List(Of DbColumn)
        Dim result As New List(Of DbColumn)()

        Using conn As New SqlConnection(ConnectionString)
            conn.Open()

            Dim sql As String =
                "SELECT TABLE_NAME, COLUMN_NAME, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH " &
                "FROM INFORMATION_SCHEMA.COLUMNS " &
                "WHERE TABLE_SCHEMA = N'dbo'"

            Using cmd As New SqlCommand(sql, conn)
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        Dim maxLength As Integer? = Nothing
                        If Not reader.IsDBNull(3) Then
                            maxLength = reader.GetInt32(3)
                        End If

                        result.Add(New DbColumn With {
                            .TableName = reader.GetString(0),
                            .ColumnName = reader.GetString(1),
                            .IsNullable = (reader.GetString(2) = "YES"),
                            .MaxLength = maxLength
                        })
                    End While
                End Using
            End Using
        End Using

        Return result
    End Function

    <TestMethod>
    Public Sub EFモデルはマイグレーションで作ったデータベースと食い違わない()
        Dim problems As New List(Of String)()
        Dim dbColumns As List(Of DbColumn) = ReadDbColumns()

        Using db As New AppDbContext(ConnectionString)
            For Each entityType As IEntityType In db.Model.GetEntityTypes()
                Dim tableName As String = entityType.GetTableName()
                If tableName Is Nothing Then
                    Continue For
                End If

                Dim tableColumns As List(Of DbColumn) = dbColumns.
                    Where(Function(c) String.Equals(c.TableName, tableName, StringComparison.OrdinalIgnoreCase)).
                    ToList()

                If tableColumns.Count = 0 Then
                    problems.Add($"[{tableName}] テーブルが、データベースにありません。")
                    Continue For
                End If

                ' モデルの列がデータベースにあり、性質が一致するか
                Dim mappedColumnNames As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

                For Each prop As IProperty In entityType.GetProperties()
                    Dim columnName As String = prop.GetColumnName()
                    mappedColumnNames.Add(columnName)

                    Dim dbColumn As DbColumn = tableColumns.FirstOrDefault(
                        Function(c) String.Equals(c.ColumnName, columnName, StringComparison.OrdinalIgnoreCase))

                    If dbColumn Is Nothing Then
                        problems.Add($"[{tableName}].{columnName} 列がデータベースにありません。(モデルにだけある)")
                        Continue For
                    End If

                    If prop.IsNullable <> dbColumn.IsNullable Then
                        problems.Add($"[{tableName}].{columnName} NULLの許可が違います。" &
                                     $"モデル={If(prop.IsNullable, "可", "不可")} " &
                                     $"DB={If(dbColumn.IsNullable, "可", "不可")}")
                    End If

                    If prop.ClrType = GetType(String) Then
                        Dim modelMax As Integer = If(prop.GetMaxLength(), -1)
                        Dim dbMax As Integer = If(prop.GetMaxLength, -1)

                        If modelMax <> dbMax Then
                            problems.Add($"{tableName}.{columnName} 文字列の最大長が違います。" &
                                         $"モデル={modelMax} DB={dbMax}")
                        End If
                    End If
                Next

                ' データベースにあって、モデルにない列
                For Each dbColumn As DbColumn In tableColumns
                    If Not mappedColumnNames.Contains(dbColumn.ColumnName) Then
                        problems.Add($"[{tableName}.{dbColumn.ColumnName}] 列がモデルにありません。(データベースにだけある)")
                    End If
                Next
            Next
        End Using

        Assert.IsEmpty(problems, "モデルとデータベースに食い違いがあります:" & vbCrLf & String.Join(vbCrLf, problems))
    End Sub

    ' ---- 本番と同じ制約があること ----

    <TestMethod>
    Public Async Function 商品の単価がマイナスだとCHECK制約で拒否される() As Task
        Dim ex As Exception = Await CaptureAsync(Function() AddProductAsync("P-NEG", "マイナス", -1D))

        Assert.IsNotNull(ex, "CHECK制約(CK_Products_UnitPrice)で拒否されるはず")
        ' CHECK制約違反も外部キー違反と同じエラー番号
        Assert.IsTrue(DbHelper.IsForeignKeyViolation(ex), DbHelper.GetFullMessage(ex))
    End Function

    <TestMethod>
    Public Async Function 注文明細の数量が0だとCHECK制約で拒否される() As Task
        Dim customerId As Integer = Await AddCustomerAsync("山田")
        Dim productId As Integer = Await AddProductAsync("P-OK", "商品", 100D)

        Dim ex As Exception = Await CaptureAsync(Function() AddOrderAsync(customerId, New DateTime(2026, 10, 1), NewItem(productId, "商品", 0, 100D)))

        Assert.IsNotNull(ex, "CHECK制約(CK_OrderItems_Quantity)で拒否されるはず")
        Assert.IsTrue(DbHelper.IsForeignKeyViolation(ex), DbHelper.GetFullMessage(ex))
    End Function

End Class
