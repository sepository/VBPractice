''' <summary>
''' データをエクスポート用の表に変換する。
''' </summary>
Public Module ExportTableBuilder

    ''' <summary>
    ''' 顧客一覧
    ''' </summary>
    Public Function FromCustomers(customers As IEnumerable(Of Customer)) As ExportTable
        Dim table As New ExportTable With {.SheetName = "顧客一覧"}

        ' 見出し
        table.Headers.AddRange(New String() {
            "顧客ID", "名前", "フリガナ", "メール", "電話", "住所", "メモ", "注文数", "累計金額"
        })

        ' 書式
        table.ColumnFormats.AddRange(New String() {
            Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, "#,##0", "#,##0"
        })

        For Each c As Customer In customers
            table.Rows.Add(New Object() {
                c.CustomerId, c.Name, c.Kana, c.Email, c.Phone, c.Address, c.Memo, c.OrderCount, c.TotalAmount
            })
        Next

        Return table
    End Function

    ''' <summary>
    ''' 注文明細
    ''' </summary>
    Public Function FromOrderLines(lines As IEnumerable(Of OrderLineRow)) As ExportTable
        Dim table As New ExportTable With {.SheetName = "注文明細"}

        ' 見出し
        table.Headers.AddRange(New String() {
            "注文ID", "注文日", "顧客ID", "顧客名", "商品コード", "商品名", "数量", "単価", "金額"
        })

        ' 書式
        table.ColumnFormats.AddRange(New String() {
            Nothing, "yyyy/mm/dd", Nothing, Nothing, Nothing, Nothing, "#,##0", "#,##0", "#,##0"
        })

        For Each line As OrderLineRow In lines
            table.Rows.Add(New Object() {
                line.OrderId, line.OrderDate, line.CustomerId, line.CustomerName,
                line.ProductCode, line.ProductName, line.Quantity, line.UnitPrice, line.Amount
            })
        Next

        Return table
    End Function

End Module
