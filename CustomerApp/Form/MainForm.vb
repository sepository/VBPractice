Imports System.Threading
Imports Microsoft.EntityFrameworkCore

Public Class MainForm

    Private ReadOnly _repo As New CustomerRepository(AppConfig.ConnectionString)

    ''' <summary>
    ''' 顧客ID
    ''' Nothingの場合、新規登録モード
    ''' </summary>
    Private _currentId As Integer? = Nothing

    ''' <summary>
    ''' DBアクセス中、二重処理や画面終了を防ぐ。
    ''' </summary>
    Private _isBusy As Boolean = False

    ''' <summary>
    ''' 検索を中止するためのリモコン
    ''' </summary>
    Private _searchCts As CancellationTokenSource = Nothing

    ''' <summary>
    ''' 起動時
    ''' </summary>
    Private Async Sub MainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        SetupGrid()
        SetBusy(True)
        Try
            Await LoadCustomersAsync()
        Finally
            SetBusy(False)
        End Try
        ClearForm()
    End Sub

    ''' <summary>
    ''' 画面を閉じる際、DBアクセス中ならキャンセルさせる。
    ''' </summary>
    Private Sub MainForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If _isBusy Then
            e.Cancel = True
        End If
    End Sub

    Private Sub SetupGrid()
        With dgvCustomers
            .AutoGenerateColumns = False
            .Columns.Clear()
            .Columns.Add(New DataGridViewTextBoxColumn With {
                         .DataPropertyName = "CustomerId",
                         .HeaderText = "ID",
                         .Width = 50})
            .Columns.Add(New DataGridViewTextBoxColumn With {
                         .DataPropertyName = "Name",
                         .HeaderText = "名前",
                         .Width = 140})
            .Columns.Add(New DataGridViewTextBoxColumn With {
                         .DataPropertyName = "Kana",
                         .HeaderText = "フリガナ",
                         .Width = 140})
            .Columns.Add(New DataGridViewTextBoxColumn With {
                         .DataPropertyName = "Email",
                         .HeaderText = "メール",
                         .Width = 180})
            .Columns.Add(New DataGridViewTextBoxColumn With {
                         .DataPropertyName = "Phone",
                         .HeaderText = "電話",
                         .Width = 110})
            .Columns.Add(New DataGridViewTextBoxColumn With {
                         .DataPropertyName = "OrderCount",
                         .HeaderText = "注文数",
                         .Width = 60,
                         .DefaultCellStyle = New DataGridViewCellStyle With {
                            .Alignment = DataGridViewContentAlignment.MiddleRight}
                         })
            .Columns.Add(New DataGridViewTextBoxColumn With {
                         .DataPropertyName = "TotalAmount",
                         .HeaderText = "累計金額",
                         .Width = 100,
                         .DefaultCellStyle = New DataGridViewCellStyle With {
                            .Format = "N0", .Alignment = DataGridViewContentAlignment.MiddleRight}
                         })
            .ReadOnly = True
            .AllowUserToResizeRows = False
            .MultiSelect = False
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect
        End With
    End Sub

    ''' <summary>
    ''' DBアクセス中、操作ボタンを無効にし、待機カーソルにする。
    ''' </summary>
    Private Sub SetBusy(busy As Boolean)
        _isBusy = busy
        UseWaitCursor = busy

        For Each ctrl As Control In New Control() {
            txtSearch, btnSearch, dgvCustomers, btnNew, btnSave, btnDelete, btnOrders, btnImport}
            ctrl.Enabled = Not busy
        Next
    End Sub

    Private Async Function LoadCustomersAsync(Optional cancellationToken As CancellationToken = Nothing) As Task
        Try
            Dim customers As List(Of Customer) = Await _repo.GetAllAsync(txtSearch.Text, cancellationToken)
            dgvCustomers.DataSource = customers
        Catch ex As Exception When DbHelper.IsCanceled(ex, cancellationToken)
            AppLog.Info("顧客の検索を中止しました。")
        Catch ex As Exception
            ShowError("顧客データの取得に失敗しました。", ex)
        End Try
    End Function

    ''' <summary>
    ''' 検索
    ''' </summary>
    Private Async Sub btnSearch_Click(sender As Object, e As EventArgs) Handles btnSearch.Click

        Using cts As New CancellationTokenSource()
            _searchCts = cts

            SetBusy(True)
            btnCancelSearch.Enabled = True
            Try
                Await LoadCustomersAsync(cts.Token)
            Finally
                btnCancelSearch.Enabled = False
                _searchCts = Nothing
                SetBusy(False)
            End Try
        End Using

        ClearForm()
    End Sub

    ''' <summary>
    ''' 検索中止
    ''' </summary>
    Private Sub btnCancelSearch_Click(sender As Object, e As EventArgs) Handles btnCancelSearch.Click
        If _searchCts IsNot Nothing Then
            _searchCts.Cancel()
        End If
    End Sub

    ''' <summary>
    ''' 一覧で行を選んだら入力欄に表示
    ''' </summary>
    Private Sub dgvCustomers_SelectionChanged(sender As Object, e As EventArgs) Handles dgvCustomers.SelectionChanged
        If dgvCustomers.CurrentRow Is Nothing Then
            Return
        End If

        Dim c As Customer = TryCast(dgvCustomers.CurrentRow.DataBoundItem, Customer)

        If c Is Nothing Then
            Return
        End If

        _currentId = c.CustomerId
        txtName.Text = c.Name
        txtKana.Text = c.Kana
        txtEmail.Text = c.Email
        txtPhone.Text = c.Phone
        txtAddress.Text = c.Address
        txtMemo.Text = c.Memo
    End Sub

    ''' <summary>
    ''' 新規
    ''' </summary>
    Private Sub btnNew_Click(sender As Object, e As EventArgs) Handles btnNew.Click
        ClearForm()
        txtName.Focus()
    End Sub

    ''' <summary>
    ''' 画面の初期化
    ''' </summary>
    Private Sub ClearForm()
        dgvCustomers.ClearSelection()
        _currentId = Nothing
        txtName.Clear()
        txtKana.Clear()
        txtEmail.Clear()
        txtPhone.Clear()
        txtAddress.Clear()
        txtMemo.Clear()
    End Sub

    ''' <summary>
    ''' 保存
    ''' </summary>
    Private Async Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        If Not ValidateInput() Then Return

        Dim c As New Customer With {
            .Name = txtName.Text,
            .Kana = txtKana.Text,
            .Email = txtEmail.Text,
            .Phone = txtPhone.Text,
            .Address = txtAddress.Text,
            .Memo = txtMemo.Text
        }
        Dim editingId As Integer? = _currentId

        SetBusy(True)
        Try
            If editingId.HasValue Then
                c.CustomerId = editingId.Value
                If Not Await _repo.UpdateAsync(c) Then
                    ShowWarning("対象の顧客が見つかりませんでした。他で削除された可能性があります。", "更新できません")
                End If
            Else
                Await _repo.InsertAsync(c)
            End If

            Await LoadCustomersAsync()
            ClearForm()
        Catch ex As Exception
            ShowError("顧客データの保存に失敗しました。", ex)
        Finally
            SetBusy(False)
        End Try
    End Sub

    ''' <summary>
    ''' 入力チェック
    ''' </summary>
    ''' <returns></returns>
    Private Function ValidateInput() As Boolean
        If String.IsNullOrWhiteSpace(txtName.Text) Then
            ShowWarning("名前を入力してください。", "入力エラー")
            txtName.Focus()
            Return False
        End If

        If txtName.Text.Trim().Length > 100 Then
            ShowWarning("名前は100文字以内で入力してください。", "入力エラー")
            txtName.Focus()
            Return False
        End If

        Dim email As String = txtEmail.Text.Trim()
        If email.Length > 0 AndAlso Not email.Contains("@") Then
            ShowWarning("メールアドレスの形式が正しくありません。", "入力エラー")
            txtEmail.Focus()
            Return False
        End If

        Return True
    End Function

    ''' <summary>
    ''' 削除
    ''' </summary>
    Private Async Sub btnDelete_Click(sender As Object, e As EventArgs) Handles btnDelete.Click
        If Not _currentId.HasValue Then
            ShowInformaion("削除する顧客を一覧から選択してください。", "削除")
            Return
        End If

        Dim answer As DialogResult = ShowYesNoDialog(
            $"{txtName.Text}を削除します。よろしいですか？", "削除の確認")
        If answer <> DialogResult.Yes Then
            Return
        End If

        Dim targetId As Integer = _currentId

        SetBusy(True)
        Try
            Await _repo.DeleteAsync(targetId)
            Await LoadCustomersAsync()
            ClearForm()
        Catch ex As DbUpdateException When IsForeignKeyViolation(ex)
            AppLog.Warn($"顧客を削除できませんでした(注文が登録済み)。CustomerId={targetId}")
            ShowWarning($"この顧客には注文が登録されているため削除できません。{vbCrLf}さきに注文管理で注文を削除してください。", "削除できません。")
        Catch ex As Exception
            ShowError("顧客データの削除に失敗しました。", ex)
        Finally
            SetBusy(False)
        End Try
    End Sub

    Private Async Sub btnOrders_Click(sender As Object, e As EventArgs) Handles btnOrders.Click
        If Not _currentId.HasValue Then
            ShowInformaion("注文を管理する顧客を一覧から選択してください。", "注文管理")
            Return
        End If

        Dim customerId As Integer = _currentId

        Using frm As New OrderForm(customerId, txtName.Text)
            frm.ShowDialog(Me)
        End Using

        ' 注文管理編集後、再読み込みする
        SetBusy(True)
        Try
            Await LoadCustomersAsync()
        Finally
            SetBusy(False)
        End Try
        SelectRowById(customerId)
    End Sub

    ''' <summary>
    ''' 顧客IDの一致する行を選択
    ''' </summary>
    ''' <param name="customerId">
    ''' 顧客ID
    ''' </param>
    Private Sub SelectRowById(customerId As Integer)
        For Each row As DataGridViewRow In dgvCustomers.Rows
            Dim c As Customer = TryCast(row.DataBoundItem, Customer)
            If c IsNot Nothing AndAlso c.CustomerId = customerId Then
                dgvCustomers.CurrentCell = row.Cells(0)
                row.Selected = True
                Return
            End If
        Next
    End Sub

    ''' <summary>
    ''' 商品マスタ
    ''' </summary>
    Private Sub btnProducts_Click(sender As Object, e As EventArgs) Handles btnProducts.Click
        Using frm As New ProductForm()
            frm.ShowDialog(Me)
        End Using
    End Sub

    ''' <summary>
    ''' エクスポート
    ''' </summary>
    Private Sub btnExport_Click(sender As Object, e As EventArgs) Handles btnExport.Click
        Using frm As New ExportForm()
            frm.ShowDialog(Me)
        End Using
    End Sub

    ''' <summary>
    ''' CSV取込
    ''' </summary>
    Private Async Sub btnImport_Click(sender As Object, e As EventArgs) Handles btnImport.Click
        Dim imported As Boolean

        Using frm As New ImportForm()
            frm.ShowDialog(Me)
            imported = frm.Imported
        End Using

        ' 取り込んだ顧客を一覧に反映する
        If imported Then
            SetBusy(True)
            Try
                Await LoadCustomersAsync()
            Finally
                SetBusy(False)
            End Try

            ClearForm()
        End If
    End Sub

End Class
