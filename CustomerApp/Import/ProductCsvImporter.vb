Imports System.Globalization
Imports System.Text

''' <summary>
''' 商品のCSVを調べた結果
''' </summary>
Public Class ProductImportPlan

    Public ReadOnly Property Products As New List(Of Product)()
    Public ReadOnly Property Errors As New List(Of ImportIssue)()
    Public ReadOnly Property Skipped As New List(Of ImportIssue)()
    Public Property DataRowCount As Integer

    Public ReadOnly Property CanImport As Boolean
        Get
            Return Errors.Count = 0 AndAlso Products.Count > 0
        End Get
    End Property

End Class

''' <summary>
''' 商品のCSVを調べて、取込計画を作る。
''' </summary>
Public Module ProductCsvImporter

    Public Const MaxRows As Integer = 10000

    Public Const MaxCodeLength As Integer = 20
    Public Const MaxNameLength As Integer = 100
    Public Const MaxUnitPrice As Decimal = 99999999D

    Private Enum ProductColumn
        Code
        Name
        UnitPrice
        IsActive
    End Enum

    Private ReadOnly HeaderMap As New Dictionary(Of String, ProductColumn)(StringComparer.OrdinalIgnoreCase) From {
        {"商品コード", ProductColumn.Code}, {"コード", ProductColumn.Code},
        {"productcode", ProductColumn.Code}, {"code", ProductColumn.Code},
        {"商品名", ProductColumn.Name}, {"名前", ProductColumn.Name},
        {"productname", ProductColumn.Name}, {"name", ProductColumn.Name},
        {"単価", ProductColumn.UnitPrice}, {"標準単価", ProductColumn.UnitPrice},
        {"unitprice", ProductColumn.UnitPrice}, {"price", ProductColumn.UnitPrice},
        {"販売中", ProductColumn.IsActive}, {"isactive", ProductColumn.IsActive}, {"active", ProductColumn.IsActive}
    }

    Private ReadOnly TrueWords As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase) From {
        "販売中", "はい", "○", "〇", "有効", "1", "true", "yes", "y"
    }

    Private ReadOnly FalseWords As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase) From {
        "販売停止", "停止", "いいえ", "×", "無効", "0", "false", "no", "n"
    }

    Public Function Analyze(csvText As String, existingCodes As IEnumerable(Of String)) As ProductImportPlan
        Dim plan As New ProductImportPlan()

        Dim records As List(Of CsvRecord)
        Try
            records = CsvParser.Parse(csvText)
        Catch ex As CsvFormatException
            plan.Errors.Add(New ImportIssue(ex.LineNumber, ex.Message))
            Return plan
        End Try

        If records.Count = 0 Then
            plan.Errors.Add(New ImportIssue(1, "ファイルが空です。1行目にヘッダーが必要です。(例：商品コード, 商品名, 単価, 販売中)"))
            Return plan
        End If

        Dim header As CsvRecord = records(0)
        Dim columnIndex As Dictionary(Of ProductColumn, Integer) = MapHeader(header, plan)
        If plan.Errors.Count > 0 Then
            Return plan
        End If

        Dim dataRows As Integer = records.Count - 1
        If dataRows = 0 Then
            plan.Errors.Add(New ImportIssue(1, "データの行がありません。(ヘッダーだけのファイルです)"))
            Return plan
        End If

        If dataRows > MaxRows Then
            plan.Errors.Add(New ImportIssue(1, $"行数が多すぎます。(データ行: {dataRows:N0}行, 最大{MaxRows}行まで)"))
            Return plan
        End If

        ' 商品コード => 最初に現れた行番号。DBにある分は0
        Dim seenCodes As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
        If existingCodes IsNot Nothing Then
            For Each code As String In existingCodes
                If Not String.IsNullOrWhiteSpace(code) Then
                    seenCodes(code.Trim()) = 0
                End If
            Next
        End If

        For r As Integer = 1 To records.Count - 1
            Dim rec As CsvRecord = records(r)
            plan.DataRowCount += 1

            If rec.Fields.Length <> header.Fields.Length Then
                plan.Errors.Add(New ImportIssue(rec.LineNumber,
                    $"項目の数がヘッダーと合いません。(ヘッダー: {header.Fields.Length}個, この行: {rec.Fields.Length}個)"))
                Continue For
            End If

            Dim code As String = GetValue(rec, columnIndex, ProductColumn.Code)
            Dim productName As String = GetValue(rec, columnIndex, ProductColumn.Name)
            Dim priceText As String = GetValue(rec, columnIndex, ProductColumn.UnitPrice)
            Dim activeText As String = GetValue(rec, columnIndex, ProductColumn.IsActive)

            Dim errorsBefore As Integer = plan.Errors.Count

            If code.Length = 0 Then
                plan.Errors.Add(New ImportIssue(rec.LineNumber, "商品コードが空です。"))
            ElseIf code.Length > MaxCodeLength Then
                plan.Errors.Add(New ImportIssue(rec.LineNumber, $"商品コードが長すぎます。({code.Length}文字、最大{MaxCodeLength}文字)"))
            End If

            If productName.Length = 0 Then
                plan.Errors.Add(New ImportIssue(rec.LineNumber, "商品名が空です。"))
            ElseIf productName.Length > MaxNameLength Then
                plan.Errors.Add(New ImportIssue(rec.LineNumber, $"商品名が長すぎます。({productName.Length}文字、最大{MaxNameLength}文字)"))
            End If

            Dim price As Decimal = 0D
            Dim priceError As String = TryParsePrice(priceText, price)
            If priceError IsNot Nothing Then
                plan.Errors.Add(New ImportIssue(rec.LineNumber, priceError))
            End If

            Dim isActive As Boolean = True
            If activeText.Length > 0 Then
                If TrueWords.Contains(activeText) Then
                    isActive = True
                ElseIf FalseWords.Contains(activeText) Then
                    isActive = False
                Else
                    plan.Errors.Add(New ImportIssue(rec.LineNumber,
                        $"「販売中」の値が分かりません: {activeText} (「販売中」か「販売停止」、または 1 or 0 にしてください。"))
                End If
            End If

            ' エラーのある行は重複の判定をしない。
            If plan.Errors.Count > errorsBefore Then
                Continue For
            End If

            Dim firstLine As Integer
            If seenCodes.TryGetValue(code, firstLine) Then
                Dim reason As String =
                    If(firstLine = 0,
                        $"商品コード {code}は既に登録されています。",
                        $"商品コード {code}は{firstLine}行目と重複しています。")
                plan.Skipped.Add(New ImportIssue(rec.LineNumber, reason & "(取り込まない)"))
                Continue For
            End If

            seenCodes(code) = rec.LineNumber

            plan.Products.Add(New Product With {
                .ProductCode = code, .Name = productName, .UnitPrice = price, .IsActive = isActive
            })
        Next

        Return plan
    End Function

    Public Function BuildReport(plan As ProductImportPlan, encodingName As String, Optional maxLines As Integer = 50) As String
        Dim sb As New StringBuilder()

        If Not String.IsNullOrEmpty(encodingName) Then
            sb.AppendLine($"文字コード: {encodingName}")
        End If

        sb.AppendLine($"データ行: {plan.DataRowCount:N0}行")
        sb.AppendLine($"取り込める: {plan.Products.Count:N0}件")
        sb.AppendLine($"重複でスキップ: {plan.Skipped.Count:N0}件")
        sb.AppendLine($"エラー: {plan.Errors.Count:N0}件")

        If plan.Errors.Count > 0 Then
            sb.AppendLine()
            sb.AppendLine("■エラー(1件でもあると取り込めません。直してからもう一度選んでください)")
            AppendIssues(sb, plan.Errors, maxLines)
        End If

        If plan.Skipped.Count > 0 Then
            sb.AppendLine()
            sb.AppendLine("■ スキップ(商品コードの重複)")
            AppendIssues(sb, plan.Skipped, maxLines)
        End If

        Return sb.ToString()
    End Function

    ' ---- 内部の処理 ----

    Private Sub AppendIssues(sb As StringBuilder, issues As List(Of ImportIssue), maxLines As Integer)
        For Each issue As ImportIssue In issues.Take(maxLines)
            sb.AppendLine($"  {issue.LineNumber}行目: {issue.Message}")
        Next

        If issues.Count > maxLines Then
            sb.AppendLine($"  ...ほか {issues.Count - maxLines:N0}件")
        End If
    End Sub

    Private Function MapHeader(header As CsvRecord, plan As ProductImportPlan) As Dictionary(Of ProductColumn, Integer)
        Dim result As New Dictionary(Of ProductColumn, Integer)()

        For i As Integer = 0 To header.Fields.Length - 1
            Dim headerName As String = header.Fields(i).Trim()
            Dim target As ProductColumn

            If HeaderMap.TryGetValue(headerName, target) Then
                If result.ContainsKey(target) Then
                    plan.Errors.Add(New ImportIssue(header.LineNumber, $"ヘッダーに同じ項目が2つあります: {headerName}"))
                Else
                    result(target) = i
                End If
            End If
        Next

        If Not result.ContainsKey(ProductColumn.Code) Then
            plan.Errors.Add(New ImportIssue(header.LineNumber, "ヘッダーに「商品コード」の列がありません。"))
        End If
        If Not result.ContainsKey(ProductColumn.Name) Then
            plan.Errors.Add(New ImportIssue(header.LineNumber, "ヘッダーに「商品名」の列がありません。"))
        End If
        If Not result.ContainsKey(ProductColumn.UnitPrice) Then
            plan.Errors.Add(New ImportIssue(header.LineNumber, "ヘッダーに「単価」の列がありません。"))
        End If

        Return result
    End Function

    Private Function GetValue(rec As CsvRecord, columnIndex As Dictionary(Of ProductColumn, Integer), target As ProductColumn) As String
        Dim index As Integer
        If Not columnIndex.TryGetValue(target, index) Then
            Return String.Empty
        End If

        Return RemoveFormulaGuard(rec.Fields(index)).Trim()
    End Function

    Private Function RemoveFormulaGuard(value As String) As String
        If value.Length >= 2 AndAlso value(0) = "'"c AndAlso "=+-@".IndexOf(value(1)) Then
            Return value.Substring(1)
        End If

        Return value
    End Function

    ''' <summary>
    ''' 単価の文字列を数値にする。
    ''' </summary>
    ''' <returns>
    ''' エラーメッセージ
    ''' </returns>
    Private Function TryParsePrice(priceText As String, ByRef price As Decimal) As String
        price = 0D

        If priceText.Length = 0 Then
            Return "単価が空です。"
        End If

        ' ¥, 円, カンマ, 空白を取り除く
        Dim cleaned As String = priceText.
            Replace("¥", "").Replace(ChrW(&HFFE5), "").Replace("\", "").Replace("円", "").
            Replace(",", "").Replace(ChrW(&HFF0C), "").Replace(" ", "").Replace(ChrW(&H3000), "")

        Dim value As Decimal
        If Not Decimal.TryParse(cleaned, NumberStyles.AllowLeadingSign Or NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, value) Then
            Return $"単価を数値として読めません: {priceText}"
        End If

        If value < 0D Then
            Return $"単価がマイナスです: {priceText}"
        End If

        If value > MaxUnitPrice Then
            Return $"単価が大きすぎます: {priceText}(上限は{MaxUnitPrice:N0}"
        End If

        price = value
        Return Nothing
    End Function

End Module
