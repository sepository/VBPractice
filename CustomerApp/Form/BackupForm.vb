Imports System.IO
Imports Microsoft.EntityFrameworkCore.ValueGeneration.Internal

Public Class BackupForm

    Private ReadOnly _service As New BackupService(AppConfig.ConnectionString)

    Private _info As BackupServerInfo = Nothing
    Private _isBusy As Boolean = False
    Private _restored As Boolean = False

    ''' <summary>
    ''' 復元が完了したか。
    ''' Trueなら呼び出し元は、アプリを閉じて再起動を案内する。
    ''' </summary>
    Public ReadOnly Property Restored As Boolean
        Get
            Return _restored
        End Get
    End Property

    ' ---- 起動時・終了時 ----

    Private Async Sub BackupForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Text = "バックアップ / 復元"

        lblServer.Text = $"サーバー: {_service.DataSource}" & vbCrLf & $"データベース: {_service.DatabaseName}"
        txtResult.Text =
            "【バックアップ】パスを確認して「バックアップを取る」を押します。取った直後に、ファイルを検証します。" & vbCrLf &
            "【復元】バックアップファイルのパスを入れて「復元する...」を押します。現在のデータは、置き換わります。" & vbCrLf &
            "  復元の前に、現在のデータベースを自動でバックアップします(復元を後悔したときの戻り先)。" & vbCrLf & vbCrLf &
            "※ 保存先は、SQL Server が動いている PC のフォルダです。SQL Server が書き込める場所にしてください。"

        SetBusy(True)
        Try
            _info = Await _service.GetServerInfoAsync()

            lblServer.Text &= If(_info.IsLocal, vbCrLf & "(SQL Server は、この PC で動いています)", vbCrLf & "(SQL Server は、別の PC です。パスはそちらの PC のパスです)")

            Dim folder As String = _info.DefaultBackupFolder
            If Not String.IsNullOrWhiteSpace(folder) Then
                txtPath.Text = Path.Combine(folder, BackupService.SuggestFileName(_service.DatabaseName, DateTime.Now))
            End If
        Catch ex As BackupException
            AppLog.Warn("サーバーの情報を取得できませんでした。", ex)
            AppendResult(ex.Message)
        Catch ex As Exception
            ShowError("サーバーの情報を取得できませんでした。", ex)
        Finally
            SetBusy(False)
        End Try
    End Sub

    Private Sub BackupForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If _isBusy Then
            e.Cancel = True
        End If
    End Sub

    Private Sub SetBusy(busy As Boolean)
        _isBusy = busy
        UseWaitCursor = busy

        txtPath.Enabled = Not busy
        btnBackup.Enabled = Not busy
        btnRestore.Enabled = Not busy
        btnClose.Enabled = Not busy
        btnBrowse.Enabled = (Not busy) AndAlso _info IsNot Nothing AndAlso _info.IsLocal
    End Sub

    Private Sub AppendResult(message As String)
        txtResult.Text = message
    End Sub

    ' ---- ファイルを選ぶ(SQL ServerがこのPCのときだけ) ----

    Private Sub btnBrowse_Click(sender As Object, e As EventArgs) Handles btnBrowse.Click
        Using dlg As New SaveFileDialog()
            dlg.Title = "バックアップファイルを選ぶ"
            dlg.Filter = "SQL Server バックアップ (*.bak)|*.bak"
            dlg.OverwritePrompt = False     ' 上書きの確認は、実行時に自分で行う(復元では、既存のファイルを選ぶため)
            dlg.CheckFileExists = False
            dlg.DefaultExt = "bak"

            If Not String.IsNullOrWhiteSpace(txtPath.Text) Then
                Try
                    dlg.InitialDirectory = Path.GetDirectoryName(txtPath.Text.Trim())
                    dlg.FileName = Path.GetFileName(txtPath.Text.Trim())
                Catch ex As ArgumentException
                    ' 入力途中のパスが不正でも、画面を落とさない
                End Try
            End If

            If dlg.ShowDialog(Me) = DialogResult.OK Then
                txtPath.Text = dlg.FileName
            End If
        End Using
    End Sub

    ' ---- バックアップ ----

    Private Async Sub btnBackup_Click(sender As Object, e As EventArgs) Handles btnBackup.Click
        Dim filePath As String = txtPath.Text.Trim()

        Dim problem As String = BackupService.ValidatePath(filePath)
        If problem IsNot Nothing Then
            ShowWarning(problem, "入力エラー")
            txtPath.Focus()
            Return
        End If

        If _info IsNot Nothing AndAlso _info.IsLocal AndAlso File.Exists(filePath) Then
            Dim overWrite As DialogResult = ShowYesNoDialog("同じ名前のファイルが既にあります。上書きしますか？", "上書きの確認")
            If overWrite <> DialogResult.Yes Then
                Return
            End If
        End If

        SetBusy(True)
        Dim timer As Stopwatch = Stopwatch.StartNew()
        Try
            AppendResult("バックアップを取っています...")
            Await _service.BackupAsync(filePath)

            timer.Stop()
            AppLog.Info($"バックアップを取りました。ファイル={Path.GetFileName(filePath)}, 所要={timer.Elapsed.TotalSeconds:N1}秒")

            Dim size As String = String.Empty
            If _info IsNot Nothing AndAlso _info.IsLocal AndAlso File.Exists(filePath) Then
                size = $"{vbCrLf}サイズ: {New FileInfo(filePath).Length / 1024.0 / 1024.0:N1}MB"
            End If

            AppendResult($"バックアップを取りました。(検証済み){vbCrLf}ファイル: {filePath}{size}{vbCrLf}所要時間: {timer.Elapsed.TotalSeconds:N1}秒")
            ShowInformaion("バックアップを取りました。(ファイルの検証も済んでいます)", "完了")
        Catch ex As BackupException
            AppLog.Warn("バックアップに失敗しました。", ex)
            AppendResult(ex.Message)
            ShowWarning(ex.Message, "バックアップできません。")
        Catch ex As Exception
            ShowError("バックアップに失敗しました。", ex)
        Finally
            SetBusy(False)
        End Try
    End Sub

    ' ---- 復元 ----

    Private Async Sub btnRestore_Click(sender As Object, e As EventArgs) Handles btnRestore.Click
        Dim filePath As String = txtPath.Text.Trim()

        Dim problem As String = BackupService.ValidatePath(filePath)
        If problem IsNot Nothing Then
            ShowWarning(problem, "入力エラー")
            txtPath.Focus()
            Return
        End If

        SetBusy(True)
        Try
            ' ファイルの中身を調べる
            AppendResult("バックアップファイルを確認しています...")
            Dim info As BackupInfo = Await _service.ReadInfoAsync(filePath)

            Dim sameName As Boolean = String.Equals(info.DatabaseName, _service.DatabaseName, StringComparison.OrdinalIgnoreCase)
            Dim safetyPath As String = BuildSafetyPath(filePath)

            Dim message As String =
                "このバックアップから復元します。" & vbCrLf & vbCrLf &
                $"  バックアップ元のデータベース: {info.DatabaseName}" & vbCrLf &
                $"  バックアップを取った日時: {info.FinishDate:yyyy/MM/dd HH:mm:ss}" & vbCrLf &
                $"  取ったサーバー: {info.ServerName}" & vbCrLf & vbCrLf &
                $"現在のデータベース「{_service.DatabaseName}」の内容はすべて置き換わります。" & vbCrLf &
                "復元の前に現在のデータを自動でバックアップします:" & vbCrLf & $"  {safetyPath}" & vbCrLf & vbCrLf

            If Not sameName Then
                message &= $"※ バックアップ元の名前({info.DatabaseName})が、現在のデータベース名と違います。" & vbCrLf &
                           "  別のデータベースのバックアップを選んでいないか、確認してください。" & vbCrLf & vbCrLf
            End If

            message &= "他の画面や他のPCのこのアプリが接続していると切断されます。続けますか？"

            Dim answer As DialogResult = ShowYesNoDialog(message, "復元の確認")
            If answer <> DialogResult.Yes Then
                AppendResult("復元を中止しました。")
                Return
            End If

            ' 復元
            AppendResult("復元しています。完了するまで画面を閉じないでください...")
            Dim timer As Stopwatch = Stopwatch.StartNew()
            Await _service.RestoreAsync(filePath, safetyPath)
            timer.Stop()

            _restored = True
            AppLog.Info($"データベースを復元しました。ファイル={System.IO.Path.GetFileName(filePath)}, 所要={timer.Elapsed.TotalSeconds:N1}秒")

            AppendResult($"復元しました。{vbCrLf}復元前のデータは、次のファイルに退避してあります:{vbCrLf}{safetyPath}{vbCrLf}{vbCrLf}アプリを再起動してください。")
            ShowInformaion("復元しました。" & vbCrLf & "アプリを一度終了します。もう一度起動してください。" & vbCrLf & vbCrLf &
                           "(復元前のデータは、自動バックアップとして残してあります)", "復元完了")
        Catch ex As BackupException
            AppLog.Warn("復元に失敗しました。", ex)
            AppendResult(ex.Message)
            ShowWarning(ex.Message, "復元できません。")
        Catch ex As Exception
            ShowError("復元に失敗しました。", ex)
        Finally
            SetBusy(False)
        End Try

        ' 復元後のデータベースは、構造のバージョンが、このアプリと違う可能性がある。
        ' 再起動すると、起動時のマイグレーションの確認が、改めて行われる。
        ' (処理中は閉じられないので、SetBusy(False) の後で閉じる)
        If _restored Then
            Close()
        End If
    End Sub

    ''' <summary>
    ''' 復元前の退避バックアップの保存先
    ''' </summary>
    Private Function BuildSafetyPath(restorePath As String) As String
        Dim folder As String = Nothing
        If _info IsNot Nothing Then
            folder = _info.DefaultBackupFolder
        End If

        If String.IsNullOrWhiteSpace(folder) Then
            folder = System.IO.Path.GetDirectoryName(restorePath)
        End If

        Return System.IO.Path.Combine(folder, BackupService.SuggestFileName(_service.DatabaseName, DateTime.Now, "_復元前"))
    End Function

    Private Sub btnClose_Click(sender As Object, e As EventArgs) Handles btnClose.Click
        Close()
    End Sub

End Class