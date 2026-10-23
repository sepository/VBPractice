Imports System.Text

''' <summary>
''' 取り込みで見つかった問題
''' </summary>
Public Class ImportIssue

    Public ReadOnly Property LineNumber As Integer
    Public ReadOnly Property Message As String

    Public Sub New(lineNumber As Integer, message As String)
        Me.LineNumber = lineNumber
        Me.Message = message
    End Sub

End Class

''' <summary>
''' CSVを調べた結果
''' </summary>
Public Class CustomerImportPlan

    ''' <summary>
    ''' 取り込む顧客
    ''' </summary>
    Public ReadOnly Property Customers As New List(Of Customer)()
    ''' <summary>
    ''' エラー、1件でもあれば全体を取り込まない。
    ''' </summary>
    Public ReadOnly Property Errors As New List(Of ImportIssue)()
    ''' <summary>
    ''' 重複のため、取り込まない行
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property Skipped As New List(Of ImportIssue)()
    ''' <summary>
    ''' ヘッダーを除いたデータ行の数
    ''' </summary>
    ''' <returns></returns>
    Public Property DataRowCount As Integer
    ''' <summary>
    ''' 取り込み可能か。
    ''' エラー0件、取り込み行1件以上の場合
    ''' </summary>
    Public ReadOnly Property CanImport As Boolean
        Get
            Return Errors.Count = 0 AndAlso Customers.Count > 0
        End Get
    End Property

End Class

Public Module CustomerCsvImporter

    ''' <summary>
    ''' 一度に処理できる最大の行数
    ''' </summary>
    Public Const MaxRows As Integer = 10000

    ' 各項目の最大長
    Public Const MaxNameLength As Integer = 100
    Public Const MaxKanaLength As Integer = 100
    Public Const MaxEmailLength As Integer = 254
    Public Const MaxPhoneLength As Integer = 20
    Public Const MaxAddressLength As Integer = 200
    Public Const MaxMemoLength As Integer = 500

    Private Enum Column
        Name
        Kana
        Email
        Phone
        Address
        Memo
    End Enum

    ''' <summary>
    ''' ヘッダー名 => 列
    ''' 大文字・小文字は区別しない。
    ''' </summary>
    Private ReadOnly HeaderMap As New Dictionary(Of String, Column)(StringComparer.OrdinalIgnoreCase) From {
        {"名前", Column.Name}, {"name", Column.Name},
        {"フリガナ", Column.Kana}, {"kana", Column.Kana},
        {"メール", Column.Email}, {"email", Column.Email},
        {"電話", Column.Phone}, {"phone", Column.Phone},
        {"住所", Column.Address}, {"address", Column.Address},
        {"メモ", Column.Memo}, {"memo", Column.Memo}
    }

    ''' <summary>
    ''' CSVの文字列を調べる。
    ''' </summary>
    ''' <param name="csvText">
    ''' CSVの中身
    ''' </param>
    ''' <param name="existingEmails">
    ''' データベースに既に登録されているメールアドレス
    ''' </param>
    Public Function Analyze(csvText As String, existingEmails As IEnumerable(Of String)) As CustomerImportPlan
        Dim plan As New CustomerImportPlan()

        Dim records As List(Of CsvRecord)
        Try
            records = CsvParser.Parse(csvText)
        Catch ex As CsvFormatException
            plan.Errors.Add(New ImportIssue(ex.LineNumber, ex.Message))
            Return plan
        End Try

        If records.Count = 0 Then
            plan.Errors.Add(New ImportIssue(1, "ファイルが空です。1行目にヘッダー(名前, フリガナ, メール, ...)が必要です。"))
            Return plan
        End If

        ' ---- ヘッダー ----
        Dim header As CsvRecord = records(0)
        Dim columnIndex As Dictionary(Of Column, Integer) = MapHeader(header, plan)
        If plan.Errors.Count > 0 Then
            Return plan
        End If

        Dim dataRows As Integer = records.Count - 1
        If dataRows = 0 Then
            plan.Errors.Add(New ImportIssue(1, "データ行がありません。(ヘッダーだけのファイルです。)"))
            Return plan
        End If

        If dataRows > MaxRows Then
            plan.Errors.Add(New ImportIssue(1, $"行数が多すぎます。({dataRows:N0}行) 一度に取り込めるのは{MaxRows:N0}行までです。ファイルを分けてください。"))
            Return plan
        End If

        ' ---- 重複判定の準備 ----
        Dim seenEmails As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
        If existingEmails IsNot Nothing Then
            For Each email As String In existingEmails
                If Not String.IsNullOrWhiteSpace(email) Then
                    seenEmails(email.Trim()) = 0
                End If
            Next
        End If

        ' ---- データ行 ----
        For r As Integer = 1 To records.Count - 1
            Dim rec As CsvRecord = records(r)
            plan.DataRowCount += 1

            ' 項目数がヘッダーと違う行は、列がずれている可能性が高いのでエラーとする。
            If rec.Fields.Length <> header.Fields.Length Then
                plan.Errors.Add(New ImportIssue(rec.LineNumber,
                                                $"項目の数がヘッダーと合いません。" &
                                                $"(ヘッダー: {header.Fields.Length}個, この行: {rec.Fields.Length}個) カンマや引用符の数を確認してください。"))
                Continue For
            End If

            Dim name As String = GetValue(rec, columnIndex, Column.Name)
            Dim kana As String = GetValue(rec, columnIndex, Column.Kana)
            Dim email As String = GetValue(rec, columnIndex, Column.Email)
            Dim phone As String = GetValue(rec, columnIndex, Column.Phone)
            Dim address As String = GetValue(rec, columnIndex, Column.Address)
            Dim memo As String = GetValue(rec, columnIndex, Column.Memo)

            Dim errorsBefore As Integer = plan.Errors.Count

            If name.Length = 0 Then
                plan.Errors.Add(New ImportIssue(rec.LineNumber, "名前が空です。"))
            End If
            CheckLength(plan, rec, "名前", name, MaxNameLength)
            CheckLength(plan, rec, "フリガナ", kana, MaxKanaLength)
            CheckLength(plan, rec, "メール", email, MaxEmailLength)
            CheckLength(plan, rec, "電話", phone, MaxPhoneLength)
            CheckLength(plan, rec, "住所", address, MaxAddressLength)
            CheckLength(plan, rec, "メモ", memo, MaxMemoLength)

            If email.Length > 0 AndAlso Not IsPlausibleEmail(email) Then
                plan.Errors.Add(New ImportIssue(rec.LineNumber, $"メールアドレスの形式が正しくありません: {email}"))
            End If

            ' この行にエラーがあれば、重複の判定はしない。
            If plan.Errors.Count > errorsBefore Then
                Continue For
            End If

            ' ---- 重複 ----
            If email.Length > 0 Then
                Dim firstLine As Integer
                If seenEmails.TryGetValue(email, firstLine) Then
                    Dim reason As String = If(
                        firstLine = 0,
                        $"メールアドレス{email}は、既に登録されています。",
                        $"メールアドレス{email}は、{firstLine}行目と重複しています。"
                    )
                    plan.Skipped.Add(New ImportIssue(rec.LineNumber, reason & "(取り込まない)"))
                    Continue For
                End If

                seenEmails(email) = rec.LineNumber
            End If

            plan.Customers.Add(New Customer With {
                .Name = name, .Kana = kana, .Email = email, .Phone = phone, .Address = address, .Memo = memo
            })
        Next

        Return plan
    End Function

    ''' <summary>
    ''' 画面に表示する取込結果を作る。
    ''' </summary>
    Public Function BuildReport(plan As CustomerImportPlan, encodingName As String, Optional maxLines As Integer = 50) As String
        Dim sb As New StringBuilder()

        If Not String.IsNullOrEmpty(encodingName) Then
            sb.AppendLine($"文字コード: {encodingName}")
        End If

        sb.AppendLine($"データ行: {plan.DataRowCount:N0}行")
        sb.AppendLine($"取り込める: {plan.Customers.Count:N0}件")
        sb.AppendLine($"重複でスキップ: {plan.Skipped.Count:N0}件")
        sb.AppendLine($"エラー: {plan.Errors.Count:N0}件")

        If plan.Errors.Count > 0 Then
            sb.AppendLine()
            sb.AppendLine("■エラー(1件でもあると取り込めません。修正後、もう一度実行してください。)")
            AppendIssues(sb, plan.Errors, maxLines)
        End If

        If plan.Skipped.Count > 0 Then
            sb.AppendLine()
            sb.AppendLine("■スキップ(重複)")
            AppendIssues(sb, plan.Skipped, maxLines)
        End If

        Return sb.ToString()
    End Function

    ' ---- 内部処理 ----

    Private Sub AppendIssues(sb As StringBuilder, issues As List(Of ImportIssue), maxLines As Integer)
        For Each issue As ImportIssue In issues.Take(maxLines)
            sb.AppendLine($"  {issue.LineNumber}行目: {issue.Message}")
        Next

        If issues.Count > maxLines Then
            sb.AppendLine($"  ...ほか {issues.Count - maxLines:N0}件")
        End If
    End Sub

    ''' <summary>
    ''' ヘッダー行から、どの列が何の項目かを調べる。
    ''' </summary>
    Private Function MapHeader(header As CsvRecord, plan As CustomerImportPlan) As Dictionary(Of Column, Integer)
        Dim result As New Dictionary(Of Column, Integer)()

        For i As Integer = 0 To header.Fields.Length - 1
            Dim headerName As String = header.Fields(i).Trim()
            Dim target As Column

            ' 知らない見出しは無視する
            If HeaderMap.TryGetValue(headerName, target) Then
                If result.ContainsKey(target) Then
                    plan.Errors.Add(New ImportIssue(header.LineNumber, $"ヘッダーに同じ項目が2つあります。: {headerName}"))
                Else
                    result(target) = i
                End If
            End If
        Next

        If Not result.ContainsKey(Column.Name) Then
            plan.Errors.Add(New ImportIssue(header.LineNumber, "ヘッダーに「名前」の列がありません。"))
        End If

        Return result
    End Function

    ''' <summary>
    ''' 列の値を取り出す。
    ''' </summary>
    Private Function GetValue(rec As CsvRecord, columnIndex As Dictionary(Of Column, Integer), target As Column) As String
        Dim index As Integer
        If Not columnIndex.TryGetValue(target, index) Then
            Return String.Empty
        End If

        Return RemoveFormulaGuard(rec.Fields(index)).Trim()
    End Function

    ''' <summary>
    ''' 数式の除去
    ''' </summary>
    Private Function RemoveFormulaGuard(value As String) As String
        If value.Length >= 2 AndAlso value(0) = "'"c AndAlso "=+-@".IndexOf(value(1)) >= 0 Then
            Return value.Substring(1)
        End If

        Return value
    End Function

    ''' <summary>
    ''' 項目の最大長チェック
    ''' </summary>
    Private Sub CheckLength(plan As CustomerImportPlan, rec As CsvRecord, label As String, value As String, maxlength As Integer)
        If value.Length > maxlength Then
            plan.Errors.Add(New ImportIssue(rec.LineNumber, $"{label}が長すぎます。({value.Length}文字、上限は{maxlength}文字)"))
        End If
    End Sub

    ''' <summary>
    ''' メールアドレスの簡易的な整合性チェック
    ''' </summary>
    Private Function IsPlausibleEmail(email As String) As Boolean
        If email.Any(Function(ch) Char.IsWhiteSpace(ch)) Then
            Return False
        End If

        Dim at As Integer = email.IndexOf("@"c)
        Return at > 0 AndAlso at = email.LastIndexOf("@"c) AndAlso at < email.Length - 1
    End Function

End Module
