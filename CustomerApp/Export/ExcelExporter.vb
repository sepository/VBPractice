Imports System.Globalization
Imports System.Threading
Imports ClosedXML.Excel

''' <summary>
''' 表をExcelファイルに書き出す。
''' </summary>
Public Module ExcelExporter

    ''' <summary>
    ''' Excelに書き出し
    ''' </summary>
    Public Sub Save(table As ExportTable, path As String, Optional cancellationToken As CancellationToken = Nothing)
        Using workbook As New XLWorkbook()
            Dim sheet As IXLWorksheet = workbook.Worksheets.Add(table.SheetName)

            Dim columnCount As Integer = table.Headers.Count
            Dim lastRow As Integer = table.Rows.Count + 1       ' +1 は見出し行

            ' ---- 見出し行 ----
            For col As Integer = 0 To columnCount - 1
                sheet.Cell(1, col + 1).Value = table.Headers(col)
            Next

            Dim headerRange As IXLRange = sheet.Range(1, 1, 1, columnCount)
            headerRange.Style.Font.Bold = True
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray

            ' ---- データ行 ----
            For r As Integer = 0 To table.Rows.Count - 1
                ' 同期の処理は、自分で中止されたかを確かめる
                cancellationToken.ThrowIfCancellationRequested()

                Dim values As Object() = table.Rows(r)
                For c As Integer = 0 To values.Length - 1
                    SetCellValue(sheet.Cell(r + 2, c + 1), values(c))
                Next
            Next

            ' ---- 列の表示形式 ----
            If table.Rows.Count > 0 Then
                For c As Integer = 0 To columnCount - 1
                    If c < table.ColumnFormats.Count AndAlso Not String.IsNullOrEmpty(table.ColumnFormats(c)) Then
                        sheet.Range(2, c + 1, lastRow, c + 1).Style.NumberFormat.Format = table.ColumnFormats(c)
                    End If
                Next
            End If

            ' ---- 見やすさの調整 ----
            sheet.SheetView.FreezeRows(1)
            sheet.Range(1, 1, lastRow, columnCount).SetAutoFilter()
            sheet.Columns().AdjustToContents()

            ' 保存は途中で止められないので、始める前に最後の確認
            cancellationToken.ThrowIfCancellationRequested()
            workbook.SaveAs(path)
        End Using
    End Sub

    ''' <summary>
    ''' 値の型に合わせて、セルに値をセットする。
    ''' </summary>
    Private Sub SetCellValue(cell As IXLCell, value As Object)
        If value Is Nothing OrElse value Is DBNull.Value Then
            Return
        End If

        If TypeOf value Is String Then
            cell.Value = DirectCast(value, String)
        ElseIf TypeOf value Is Integer Then
            cell.Value = DirectCast(value, Integer)
        ElseIf TypeOf value Is Long Then
            cell.Value = DirectCast(value, Long)
        ElseIf TypeOf value Is Decimal Then
            cell.Value = DirectCast(value, Decimal)
        ElseIf TypeOf value Is Double Then
            cell.Value = DirectCast(value, Double)
        ElseIf TypeOf value Is DateTime Then
            cell.Value = DirectCast(value, DateTime)
        ElseIf TypeOf value Is Boolean Then
            cell.Value = DirectCast(value, Boolean)
        Else
            cell.Value = Convert.ToString(value, CultureInfo.InvariantCulture)
        End If
    End Sub

End Module
