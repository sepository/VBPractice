Imports Microsoft.Data.SqlClient

Public Class MainForm

    Private ReadOnly _repo As New CustomerRepository(AppConfig.ConnectionString)

    Private _currentId As Integer? = Nothing

    ''' <summary>
    ''' 起動時
    ''' </summary>
    Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        SetupGrid()
        LoadCustomers()
        ClearForm()
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

    Private Sub LoadCustomers()
        Try
            dgvCustomers.DataSource = _repo.GetAll(txtSearch.Text)
        Catch ex As SqlException
            ShowError("顧客データの取得に失敗しました。", ex)
        End Try
    End Sub

    ''' <summary>
    ''' 検索
    ''' </summary>
    Private Sub btnSearch_Click(sender As Object, e As EventArgs) Handles btnSearch.Click
        LoadCustomers()
        ClearForm()
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
    Private Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        If Not ValidateInput() Then Return

        Dim c As New Customer With {
            .Name = txtName.Text,
            .Kana = txtKana.Text,
            .Email = txtEmail.Text,
            .Phone = txtPhone.Text,
            .Address = txtAddress.Text,
            .Memo = txtMemo.Text
        }

        Try
            If _currentId.HasValue Then
                c.CustomerId = _currentId.Value
                If Not _repo.Update(c) Then
                    MessageBox.Show("対象の顧客が見つかりませんでした。他で削除された可能性があります。", "更新できません", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                End If
            Else
                _repo.Insert(c)
            End If

            LoadCustomers()
            ClearForm()
        Catch ex As SqlException
            ShowError("顧客データの保存に失敗しました。", ex)
        End Try
    End Sub

    ''' <summary>
    ''' 入力チェック
    ''' </summary>
    ''' <returns></returns>
    Private Function ValidateInput() As Boolean
        If String.IsNullOrWhiteSpace(txtName.Text) Then
            MessageBox.Show("名前を入力してください。", "入力エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtName.Focus()
            Return False
        End If

        If txtName.Text.Trim().Length > 100 Then
            MessageBox.Show("名前は100文字以内で入力してください。", "入力エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtName.Focus()
            Return False
        End If

        Dim email As String = txtEmail.Text.Trim()
        If email.Length > 0 AndAlso Not email.Contains("@") Then
            MessageBox.Show("メールアドレスの形式が正しくありません。", "入力エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtEmail.Focus()
            Return False
        End If

        Return True
    End Function

    ''' <summary>
    ''' 削除
    ''' </summary>
    Private Sub btnDelete_Click(sender As Object, e As EventArgs) Handles btnDelete.Click
        If Not _currentId.HasValue Then
            MessageBox.Show("削除する顧客を一覧から選択してください。", "削除", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim answer = MessageBox.Show(
            $"{txtName.Text}を削除します。よろしいですか？",
            "削除の確認",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2)

        If answer <> DialogResult.Yes Then
            Return
        End If

        Try
            _repo.Delete(_currentId.Value)
            LoadCustomers
            ClearForm
        Catch ex As SqlException
            ShowError("顧客データの削除に失敗しました。", ex)
        End Try
    End Sub

    Private Sub btnOrders_Click(sender As Object, e As EventArgs) Handles btnOrders.Click
        If Not _currentId.HasValue Then
            MessageUtils.ShowInformaion("注文を管理する顧客を一覧から選択してください。", "注文管理")
            Return
        End If

        Dim customerId As Integer = _currentId

        Using frm As New OrderForm(customerId, txtName.Text)
            frm.ShowDialog(Me)
        End Using

        ' 注文管理編集後、再読み込みする
        LoadCustomers()
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
    ''' エラーメッセージ表示
    ''' </summary>
    ''' <param name="message"></param>
    ''' <param name="ex"></param>
    Private Sub ShowError(message As String, ex As Exception)
        MessageBox.Show($"{message}{vbCrLf}{vbCrLf}{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error)
    End Sub

End Class
