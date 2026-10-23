Imports System.IO

Public Class ImportForm

    ' 大きすぎるファイルは読み込まない。
    Private Const MaxFileBytes As Long = 10L * 1024 * 1024

    Private ReadOnly _repo As New CustomerRepository(AppConfig.ConnectionString)

    ' 調べた結果
    Private _plan As CustomerImportPlan = Nothing

    ' 取込が完了したか
    Private _imported As Boolean = False

    Public ReadOnly Property Imported As Boolean
        Get
            Return _imported
        End Get
    End Property

    Private _isBusy As Boolean = False

    ' ---- 起動時・終了時 ----

    Private Sub ImportForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Text = "CSV取込(顧客)"
        txtResult.Text =
            "顧客の CSV ファイルを取り込みます。" & vbCrLf & vbCrLf &
            "・1行目に、見出しが必要です(名前 / フリガナ / メール / 電話 / 住所 / メモ)。必須は「名前」だけです。" & vbCrLf &
            "・列の順番は自由です。このアプリの「顧客一覧」の CSV エクスポートは、そのまま取り込めます。" & vbCrLf &
            "・文字コードは、UTF-8 と Shift_JIS(Excel の CSV)を自動で判定します。" & vbCrLf &
            "・1行でもエラーがあると、1件も取り込みません(全部の行のエラーを、行番号つきで表示します)。" & vbCrLf &
            "・メールアドレスが、既に登録されているものや、ファイル内で重複しているものは、取り込まずに飛ばします。"
    End Sub

    Private Sub ImportForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If _isBusy Then
            e.Cancel = True
        End If
    End Sub

    Private Sub SetBusy(busy As Boolean)
        _isBusy = busy
        UseWaitCursor = busy
        btnBrowse.Enabled = Not busy
        btnClose.Enabled = Not busy
        btnImport.Enabled = (Not busy) AndAlso _plan IsNot Nothing AndAlso _plan.CanImport
    End Sub

    ' ---- ①ファイルを選んで調べる ----

    Private Async Sub btnBrowse_Click(sender As Object, e As EventArgs) Handles btnBrowse.Click
        Dim path As String = AskOpenPath()
        If path Is Nothing Then
            Return
        End If

        _plan = Nothing
        SetBusy(True)
        Try
            Dim info As New FileInfo(path)
            If info.Length > MaxFileBytes Then
                lblFile.Text = Nothing
                txtResult.Text = $"ファイルが大きすぎます({info.Length / 1024 / 1024:N1} MB)。{MaxFileBytes \ 1024 \ 1024} MB 以下にしてください。ファイルを分けて取り込んでください。"
                Return
            End If

            ' フォルダ名にはユーザー名が含まれることがあるので、ファイル名だけを表示する。
            lblFile.Text = info.Name

            ' 読み込みと解析は、時間がかかることがあるので、別スレッドで行い、画面を固めない。
            ' (Task.Run の中では、画面のコントロールを触らない)
            Dim encodingName As String = Nothing
            Dim csvText As String = Await Task.Run(Function() CsvFileReader.ReadFile(path, encodingName))

            ' 重複の判定に使う、登録済みのメールアドレス
            Dim existing As List(Of String) = Await _repo.GetEmailsAsync()

            Dim plan As CustomerImportPlan = Await Task.Run(Function() CustomerCsvImporter.Analyze(csvText, existing))

            _plan = plan
            txtResult.Text = CustomerCsvImporter.BuildReport(plan, encodingName)

            ' ログには、件数だけを書く(顧客の個人情報は、書かない)
            AppLog.Info($"CSVを調べました。データ行={plan.DataRowCount}, 取り込める={plan.Customers.Count}, スキップ={plan.Skipped.Count}, エラー={plan.Errors.Count}")
        Catch ex As IOException
            ' Excelで開いたままだと読めないことがある。
            AppLog.Warn("CSVファイルを読み込めませんでした。", ex)
            txtResult.Text = "ファイルを読み込めませんでした。他のアプリ(Excel など)で開いている場合は、閉じてからやり直してください。"
        Catch ex As UnauthorizedAccessException
            AppLog.Warn("CSV ファイルへのアクセス権限がありませんでした。", ex)
            txtResult.Text = "このファイルは読めません。別の場所に、コピーしてから選んでください。"
        Catch ex As Exception
            MessageUtils.ShowError("CSVの確認に失敗しました。", ex)
        Finally
            SetBusy(False)
        End Try
    End Sub

    Private Function AskOpenPath() As String
        Using dlg As New OpenFileDialog()
            dlg.Title = "取り込むCSVファイルを選ぶ"
            dlg.Filter = "CSV (*.csv)|*.csv|すべてのファイル (*.*)|*.*"
            dlg.CheckFileExists = True

            If dlg.ShowDialog(Me) = DialogResult.OK Then
                Return dlg.FileName
            End If
        End Using

        Return Nothing
    End Function

    ' ---- ②取り込む ----

    Private Async Sub btnImport_Click(sender As Object, e As EventArgs) Handles btnImport.Click
        Dim plan As CustomerImportPlan = _plan
        If plan Is Nothing OrElse Not plan.CanImport Then
            Return
        End If

        Dim answer As DialogResult = MessageUtils.ShowYesNoDialog(
            $"{plan.Customers.Count:N0}件を取り込みます。よろしいですか？{vbCrLf}{vbCrLf}" &
            "取込は取り消せません(取り込んだ顧客は、1件ずつ削除することになります)。",
            "取込の確認")
        If answer <> DialogResult.Yes Then
            Return
        End If

        ' 二重に押されて、同じデータを2回取り込まないよう、先に結果を消す
        _plan = Nothing
        SetBusy(True)
        Try
            Dim count As Integer = Await _repo.ImportAsync(plan.Customers)

            _imported = True
            AppLog.Info($"CSVから顧客を取り込みました。件数={count}")
            txtResult.Text = $"{count:N0}件を取り込みました。" & vbCrLf & vbCrLf & "続けて取り込む場合は、「ファイルを選ぶ」から、もう一度選んでください。"

            MessageUtils.ShowInformaion($"{count:N0}件を取り込みました。", "取込完了")
        Catch ex As Exception
            ' 途中で失敗しても、トランザクションにより1件も入っていない
            MessageUtils.ShowError("取込に失敗しました。1件も登録されていません。", ex)
            _plan = plan    ' 原因を直したら、もう一度「取り込む」を押せるように戻す
        Finally
            SetBusy(False)
        End Try
    End Sub

    Private Sub btnClose_Click(sender As Object, e As EventArgs) Handles btnClose.Click
        Close()
    End Sub

End Class