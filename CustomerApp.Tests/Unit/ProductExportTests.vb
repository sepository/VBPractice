Imports System.Formats.Nrbf
Imports Microsoft.VisualStudio.TestTools.UnitTesting

''' <summary>
''' 商品マスタのエクスポートのテスト
''' </summary>
<TestClass>
Public Class ProductExportTests

    Private Shared Function Sample() As List(Of Product)
        Return New List(Of Product) From {
            New Product With {.ProductCode = "P0001", .Name = "りんご", .UnitPrice = 120D, .IsActive = True},
            New Product With {.ProductCode = "P0002", .Name = "みかん, 大(""特選"")", .UnitPrice = 1200.5D, .IsActive = False},
            New Product With {.ProductCode = "=SUM(A1)", .Name = "-危険な名前", .UnitPrice = 0D, .IsActive = True}
        }
    End Function

    <TestMethod>
    Public Sub FromProducts_見出しと正しい列に入る()
        Dim table As ExportTable = ExportTableBuilder.FromProducts(Sample())

        CollectionAssert.AreEqual(New String() {"商品コード", "商品名", "単価", "販売中"}, table.Headers)
        Assert.HasCount(4, table.ColumnFormats)
        Assert.HasCount(3, table.Rows)
        Assert.AreEqual("P0001", table.Rows(0)(0))
        Assert.AreEqual(120D, table.Rows(0)(2))
        Assert.AreEqual("販売中", table.Rows(0)(3))
        Assert.AreEqual("販売停止", table.Rows(1)(3))
    End Sub

    <TestMethod>
    Public Sub FromProducts_０件でも見出しだけの表が作れる()
        Dim table As ExportTable = ExportTableBuilder.FromProducts(New List(Of Product)())

        Assert.HasCount(4, table.Headers)
        Assert.IsEmpty(table.Rows)
    End Sub

    <TestMethod>
    Public Sub 往復_出力したCSVを取り込むと同じ商品になる()
        Dim path As String = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"products_{Guid.NewGuid():N}.csv")

        Try
            CsvExporter.Save(ExportTableBuilder.FromProducts(Sample()), path)

            Dim encodingName As String = Nothing
            Dim csvText As String = CsvFileReader.ReadFile(path, encodingName)
            Dim plan As ProductImportPlan = ProductCsvImporter.Analyze(csvText, New String() {})

            Assert.IsEmpty(plan.Errors, ProductCsvImporter.BuildReport(plan, encodingName))
            Assert.IsEmpty(plan.Skipped)
            Assert.HasCount(3, plan.Products)

            Dim expected As List(Of Product) = Sample()
            For i As Integer = 0 To expected.Count - 1
                Assert.AreEqual(expected(i).ProductCode, plan.Products(i).ProductCode, $"{i}件目のコード")
                Assert.AreEqual(expected(i).Name, plan.Products(i).Name, $"{i}件目の名前")
                Assert.AreEqual(expected(i).UnitPrice, plan.Products(i).UnitPrice, $"{i}件目の単価")
                Assert.AreEqual(expected(i).IsActive, plan.Products(i).IsActive, $"{i}件目の販売中")
            Next
        Finally
            If System.IO.File.Exists(path) Then
                System.IO.File.Delete(path)
            End If
        End Try
    End Sub

    <TestMethod>
    Public Sub 往復_出力したCSVをそのまま取り込み直すと全部スキップされる()
        Dim path As String = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"products_{Guid.NewGuid():N}.csv")

        Try
            CsvExporter.Save(ExportTableBuilder.FromProducts(Sample()), path)
            Dim encodingName As String = Nothing
            Dim csvText As String = CsvFileReader.ReadFile(path, encodingName)

            Dim plan As ProductImportPlan = ProductCsvImporter.Analyze(csvText, Sample().Select(Function(p) p.ProductCode).ToList())

            Assert.IsEmpty(plan.Errors)
            Assert.HasCount(3, plan.Skipped, "既存のコードは上書きしない")
            Assert.IsFalse(plan.CanImport)
        Finally
            If System.IO.File.Exists(path) Then
                System.IO.File.Delete(path)
            End If
        End Try
    End Sub

    ' ---- 数式ガード(')の切り分け用 ----

    <TestMethod>
    Public Sub 出力側_数式として実行される文字で始まる値には先頭に1つだけアポストロフィが付く()
        Dim path As String = System.IO.Path.Combine(System.IO.Path.GetTempPath, $"products_{Guid.NewGuid():N}.csv")

        Try
            CsvExporter.Save(ExportTableBuilder.FromProducts(Sample()), path)

            Dim lines As String() = System.IO.File.ReadAllLines(path)
            Assert.AreEqual("'=SUM(A1),'-危険な名前,0,販売中", lines(3).Replace(ChrW(&HFEFF), ""), "3行目(= で始まる商品)")
        Finally
            If System.IO.File.Exists(path) Then
                System.IO.File.Delete(path)
            End If
        End Try
    End Sub

    <TestMethod>
    Public Sub 取込側_先頭のアポストロフィは1つだけ取り除かれる()
        Dim plan As ProductImportPlan = ProductCsvImporter.Analyze(
            "商品コード,商品名,単価" & vbCrLf & "'=SUM(A1),'-危険な名前,0", New String() {})

        Assert.IsEmpty(plan.Errors)
        Assert.HasCount(1, plan.Products)
        Assert.AreEqual("=SUM(A1)", plan.Products(0).ProductCode)
        Assert.AreEqual("-危険な名前", plan.Products(0).Name)
    End Sub

End Class
