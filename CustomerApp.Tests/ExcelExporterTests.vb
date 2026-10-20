Imports System.IO
Imports ClosedXML.Excel
Imports DocumentFormat.OpenXml.Office.CustomUI
Imports DocumentFormat.OpenXml.Spreadsheet
Imports Microsoft.VisualStudio.TestTools.UnitTesting

<TestClass>
Public Class ExcelExporterTests

    Private ReadOnly _tempFiles As New List(Of String)()

    <TestCleanup>
    Public Sub Cleanup()
        For Each f As String In _tempFiles
            If File.Exists(f) Then
                File.Delete(f)
            End If
        Next
        _tempFiles.Clear()
    End Sub

    Private Function SaveToTemp(table As ExportTable) As String
        Dim path As String = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"customerapp-test-{Guid.NewGuid():N}.xlsx")
        _tempFiles.Add(path)

        ExcelExporter.Save(table, path)
        Return path
    End Function

    Private Shared Function NewCustomer(memo As String) As Customer
        Return New Customer With {
            .CustomerId = 7,
            .Name = "山田 太郎",
            .Memo = memo,
            .OrderCount = 2,
            .TotalAmount = 2800D
        }
    End Function

    <TestMethod>
    Public Sub Save_シート名と見出しとデータが書き込まれる()
        Dim table As ExportTable = ExportTableBuilder.FromCustomers(New List(Of Customer) From {NewCustomer("メモ")})
        Dim path As String = SaveToTemp(table)

        Using workbook As New XLWorkbook(path)
            Assert.HasCount(1, workbook.Worksheets)

            Dim sheet As IXLWorksheet = workbook.Worksheet("顧客一覧")
            Assert.AreEqual("顧客ID", sheet.Cell(1, 1).GetString())
            Assert.AreEqual("山田 太郎", sheet.Cell(2, 2).GetString())
            Assert.AreEqual("メモ", sheet.Cell(2, 7).GetString())
        End Using
    End Sub

    <TestMethod>
    Public Sub Save_数値は数値のまま保存され桁区切りの表示形式が付く()
        Dim table As ExportTable = ExportTableBuilder.FromCustomers(New List(Of Customer) From {NewCustomer("メモ")})
        Dim path As String = SaveToTemp(table)

        Using workbook As New XLWorkbook(path)
            Dim sheet As IXLWorksheet = workbook.Worksheet("顧客一覧")

            Assert.AreEqual(2800.0, sheet.Cell(2, 9).GetDouble(), 0.001)
            Assert.AreEqual("#,##0", sheet.Cell(2, 9).Style.NumberFormat.Format)
        End Using
    End Sub

    <TestMethod>
    Public Sub Save_日付は日付のまま保存される()
        Dim lines As New List(Of OrderLineRow) From {
            New OrderLineRow With {.OrderId = 1, .OrderDate = New DateTime(2026, 10, 1), .Quantity = 1, .UnitPrice = 100D}
        }
        Dim path As String = SaveToTemp(ExportTableBuilder.FromOrderLines(lines))

        Using workbook As New XLWorkbook(path)
            Dim sheet As IXLWorksheet = workbook.Worksheet("注文明細")

            Assert.AreEqual(New DateTime(2026, 10, 1), sheet.Cell(2, 2).GetDateTime())
        End Using
    End Sub

    <TestMethod>
    Public Sub Save_先頭が等号の文字列は数式にならず文字のまま保存される()
        ' セキュリティ上、重要なテスト。
        ' 人が入力した「=...」が、Excelの数式として動いてはいけない。
        Dim table As ExportTable = ExportTableBuilder.FromCustomers(New List(Of Customer) From {NewCustomer("=1+1")})
        Dim path As String = SaveToTemp(table)

        Using workbook As New XLWorkbook(path)
            Dim cell As IXLCell = workbook.Worksheet("顧客一覧").Cell(2, 7)

            Assert.IsFalse(cell.HasFormula, "数式として保存されている")
            Assert.AreEqual("=1+1", cell.GetString())
        End Using
    End Sub

    <TestMethod>
    Public Sub Save_NothingのセルはEmptyになる()
        Dim table As New ExportTable()
        table.Headers.AddRange(New String() {"A", "B"})
        table.Rows.Add(New Object() {"x", Nothing})
        Dim path As String = SaveToTemp(table)

        Using workbook As New XLWorkbook(path)
            Dim sheet As IXLWorksheet = workbook.Worksheet(1)
            Assert.AreEqual("x", sheet.Cell(2, 1).GetString())
            Assert.IsTrue(sheet.Cell(2, 2).IsEmpty())
        End Using
    End Sub

    <TestMethod>
    Public Sub Save_データが0行でも見出しは書き込まれる()
        Dim table As ExportTable = ExportTableBuilder.FromCustomers(New List(Of Customer)())
        Dim path As String = SaveToTemp(table)

        Using workbook As New XLWorkbook(path)
            Dim sheet As IXLWorksheet = workbook.Worksheet("顧客一覧")
            Assert.AreEqual("顧客ID", sheet.Cell(1, 1).GetString())
            Assert.IsTrue(sheet.Cell(2, 1).IsEmpty())
        End Using
    End Sub

End Class
