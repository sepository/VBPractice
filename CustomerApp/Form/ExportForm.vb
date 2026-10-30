Imports System.IO
Imports System.Threading

Public Class ExportForm

    Private ReadOnly _customerRepo As New CustomerRepository(AppConfig.ConnectionString)
    Private ReadOnly _reportRepo As New ReportRepository(AppConfig.ConnectionString)
    Private ReadOnly _productRepo As New ProductRepository(AppConfig.ConnectionString)

    Private _isBusy As Boolean = False
    Private _exportCts As CancellationTokenSource = Nothing

    Private Sub ExportForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Text = "エクスポート"

        rdoCustomers.Checked = True
        rdoExcel.Checked = True

        dtpFrom.Value = New DateTime(DateTime.Today.Year, 1, 1)
        dtpTo.Value = DateTime.Today

        UpdatePeriodEnabled()
    End Sub

    Private Sub ExportForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If _isBusy Then
            e.Cancel = True
        End If
    End Sub

    Private Sub rdoTarget_CheckedChanged(sender As Object, e As EventArgs) _
        Handles rdoCustomers.CheckedChanged, rdoProducts.CheckedChanged, rdoOrders.CheckedChanged

        UpdatePeriodEnabled()
    End Sub

    Private Sub UpdatePeriodEnabled()
        dtpFrom.Enabled = rdoOrders.Checked
        dtpTo.Enabled = rdoOrders.Checked
    End Sub

    Private Sub SetBusy(busy As Boolean)
        _isBusy = busy
        UseWaitCursor = busy

        For Each ctrl As Control In New Control() {
            rdoCustomers, rdoOrders, dtpFrom, dtpTo, rdoExcel, rdoCsv, btnExport, btnClose}
            ctrl.Enabled = Not busy
        Next

        ' 中止ボタンだけは逆
        btnCancel.Enabled = busy

        If Not busy Then
            UpdatePeriodEnabled()
        End If
    End Sub

    Private Async Sub btnExport_Click(sender As Object, e As EventArgs) Handles btnExport.Click
        Dim exportOrders As Boolean = rdoOrders.Checked
        Dim exportProducts As Boolean = rdoProducts.Checked
        Dim useExcel As Boolean = rdoExcel.Checked
        Dim fromDate As DateTime = dtpFrom.Value.Date
        Dim toDate As DateTime = dtpTo.Value.Date

        If exportOrders AndAlso fromDate > toDate Then
            ShowWarning("期間の開始日が、終了日より後になっています。", "入力エラー")
            dtpFrom.Focus()
            Return
        End If

        Dim kind As String = If(exportOrders, "注文明細", If(exportProducts, "商品マスタ", "顧客一覧"))

        Dim path As String = AskSavePath(kind, useExcel)
        If path Is Nothing Then
            Return
        End If

        Dim format As String = If(useExcel, "Excel", "CSV")

        ' 書き出しは一時ファイルに行い、最後まで成功したら本来の名前に差し替える
        Dim tempPath As String = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(path),
            System.IO.Path.GetFileNameWithoutExtension(path) & ".tmp" &
            System.IO.Path.GetExtension(path))

        Using cts As New CancellationTokenSource()
            _exportCts = cts
            Dim token As CancellationToken = cts.Token

            SetBusy(True)
            Try
                ' データベースから取得
                Dim table As ExportTable
                If exportOrders Then
                    Dim lines As List(Of OrderLineRow) = Await _reportRepo.GetOrderLinesAsync(fromDate, toDate, token)
                    table = ExportTableBuilder.FromOrderLines(lines)
                ElseIf exportProducts Then
                    Dim products As List(Of Product) = Await _productRepo.GetAllAsync(token)
                    table = ExportTableBuilder.FromProducts(products)
                Else
                    Dim customers As List(Of Customer) = Await _customerRepo.GetAllAsync(Nothing, token)
                    table = ExportTableBuilder.FromCustomers(customers)
                End If

                ' ファイルに書き出す
                Await Task.Run(Sub()
                                   If useExcel Then
                                       ExcelExporter.Save(table, tempPath, token)
                                   Else
                                       CsvExporter.Save(table, tempPath, token)
                                   End If
                               End Sub)

                ' 一時ファイルを本来の名前に差し替える
                File.Move(tempPath, path, True)

                ' ログ
                AppLog.Info($"エクスポートしました。内容={kind}, 形式={format}, 件数={table.Rows.Count}")

                Dim answer As DialogResult = ShowYesNoDialog(
                $"{table.Rows.Count}件を書き出しました。{vbCrLf}{vbCrLf}保存先のフォルダを開きますか？", "エクスポート完了")
                If answer = DialogResult.Yes Then
                    Process.Start("explorer.exe", $"select, ""{path}""")
                End If

            Catch ex As Exception When DbHelper.IsCanceled(ex, token)
                AppLog.Info($"エクスポートを中止しました。内容={kind}, 形式={format}")
                ShowInformaion("エクスポートを中止しました。ファイルは作成されていません。", "中止")

            Catch ex As IOException
                AppLog.Warn("エクスポート先のファイルに書き込めませんでした。", ex)
                ShowWarning("ファイルに書き込めませんでした。" & vbCrLf & vbCrLf &
                        "同じ名前のファイルがExcelなどで開かれている場合は、閉じてからやり直してください。",
                        "書き出せません")

            Catch ex As UnauthorizedAccessException
                AppLog.Warn("エクスポート先への書き込み権限がありませんでした。", ex)
                ShowWarning("この場所には書き込めません。別のフォルダを指定してください。", "書き出せません")

            Finally
                _exportCts = Nothing
                TryDeleteFile(tempPath)
                SetBusy(False)
            End Try
        End Using
    End Sub

    ''' <summary>
    ''' 中止
    ''' </summary>
    Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        If _exportCts IsNot Nothing Then
            _exportCts.Cancel()
        End If
    End Sub

    ''' <summary>
    ''' ファイル削除
    ''' 削除に失敗しても続行する
    ''' </summary>
    Private Shared Sub TryDeleteFile(filePath As String)
        Try
            If File.Exists(filePath) Then
                File.Delete(filePath)
            End If
        Catch ex As IOException
            AppLog.Warn("一時ファイルを削除できませんでした。", ex)
        Catch ex As UnauthorizedAccessException
            AppLog.Warn("一時ファイルを削除できませんでした。", ex)
        End Try
    End Sub

    Private Function AskSavePath(baseName As String, useExcel As Boolean) As String
        Using dlg As New SaveFileDialog()
            dlg.Title = "エクスポート先の指定"
            dlg.OverwritePrompt = True
            dlg.AddExtension = True

            If useExcel Then
                dlg.Filter = "Excel ブック (*.xlsx)|*.xlsx"
                dlg.DefaultExt = "xlsx"
            Else
                dlg.Filter = "CSV (UTF-8) (*.csv)|*.csv"
                dlg.DefaultExt = "csv"
            End If

            dlg.FileName = $"{baseName}_{DateTime.Now:yyyyMMdd_HHmmss}"

            If dlg.ShowDialog(Me) = DialogResult.OK Then
                Return dlg.FileName
            End If
        End Using

        Return Nothing
    End Function

    Private Sub btnClose_Click(sender As Object, e As EventArgs) Handles btnClose.Click
        Close()
    End Sub

End Class