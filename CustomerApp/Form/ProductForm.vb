Imports Microsoft.EntityFrameworkCore

Public Class ProductForm

    Private ReadOnly _repo As New ProductRepository(AppConfig.ConnectionString)

    ''' <summary>
    ''' 編集中の商品ID
    ''' Nothingのときは新規登録モード
    ''' </summary>
    Private _currentId As Integer? = Nothing

    ''' <summary>
    ''' DBアクセス中はTrue
    ''' 二重操作や処理中の画面終了を防ぐ。
    ''' </summary>
    Private _isBusy As Boolean = False

    ''' <summary>
    ''' 画面起動時の処理
    ''' </summary>
    Private Async Sub ProductForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Text = "商品マスタ"
        SetupGrid()
        SetBusy(True)
        Try
            Await LoadProductsAsync()
        Finally
            SetBusy(False)
        End Try
        ClearForm()
    End Sub

    ''' <summary>
    ''' 画面を閉じる際の処理
    ''' </summary>
    Private Sub ProductForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If _isBusy Then
            e.Cancel = True
        End If
    End Sub

    ''' <summary>
    ''' グリッドの初期化
    ''' </summary>
    Private Sub SetupGrid()
        With dgvProducts
            .AutoGenerateColumns = False
            .Columns.Clear()
            .Columns.Add(New DataGridViewTextBoxColumn With {
                         .DataPropertyName = "ProductCode",
                         .HeaderText = "商品コード",
                         .Width = 90})
            .Columns.Add(New DataGridViewTextBoxColumn With {
                         .DataPropertyName = "Name",
                         .HeaderText = "商品名",
                         .Width = 200})
            .Columns.Add(New DataGridViewTextBoxColumn With {
                         .DataPropertyName = "UnitPrice",
                         .HeaderText = "標準単価",
                         .Width = 100,
                         .DefaultCellStyle = New DataGridViewCellStyle With {
                            .Format = "N0", .Alignment = DataGridViewContentAlignment.MiddleRight}
                         })
            .Columns.Add(New DataGridViewCheckBoxColumn With {
                         .DataPropertyName = "IsActive",
                         .HeaderText = "販売中",
                         .Width = 60})
            .ReadOnly = True
            .AllowUserToAddRows = False
            .MultiSelect = False
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect
        End With
    End Sub

    ''' <summary>
    ''' DBアクセス状態の変更
    ''' </summary>
    Private Sub SetBusy(busy As Boolean)
        _isBusy = busy
        UseWaitCursor = busy

        For Each ctrl As Control In New Control() {
            dgvProducts, btnNew, btnSave, btnDelete, btnImport, btnClose}
            ctrl.Enabled = Not busy
        Next
    End Sub

    ''' <summary>
    ''' 商品データ取得
    ''' </summary>
    Private Async Function LoadProductsAsync() As Task
        Try
            dgvProducts.DataSource = Await _repo.GetAllAsync()
        Catch ex As Exception
            ShowError("商品データの取得に失敗しました。", ex)
        End Try
    End Function

    ''' <summary>
    ''' 一覧の行選択時の処理
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Private Sub dgvProducts_SelectionChanged(sender As Object, e As EventArgs) Handles dgvProducts.SelectionChanged
        If dgvProducts.CurrentRow Is Nothing Then
            Return
        End If

        Dim p As Product = TryCast(dgvProducts.CurrentRow.DataBoundItem, Product)
        If p Is Nothing Then
            Return
        End If

        _currentId = p.ProductId
        txtProductCode.Text = p.ProductCode
        txtName.Text = p.Name
        numUnitPrice.Value = p.UnitPrice
        chkIsActive.Checked = p.IsActive
    End Sub

    ''' <summary>
    ''' 新規
    ''' </summary>
    Private Sub btnNew_Click(sender As Object, e As EventArgs) Handles btnNew.Click
        ClearForm()
        txtProductCode.Focus()
    End Sub

    ''' <summary>
    ''' 画面の情報クリア
    ''' </summary>
    Private Sub ClearForm()
        dgvProducts.ClearSelection()
        _currentId = Nothing
        txtProductCode.Clear()
        txtName.Clear()
        numUnitPrice.Value = 0
        chkIsActive.Checked = True
    End Sub

    ''' <summary>
    ''' 保存
    ''' </summary>
    Private Async Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        If Not ValidateInput() Then
            Return
        End If

        Dim p As New Product With {
            .ProductCode = txtProductCode.Text,
            .Name = txtName.Text,
            .UnitPrice = numUnitPrice.Value,
            .IsActive = chkIsActive.Checked
        }
        Dim editingId As Integer? = _currentId

        SetBusy(True)
        Try
            If editingId.HasValue Then
                p.ProductId = editingId.Value
                If Not Await _repo.UpdateAsync(p) Then
                    ShowWarning("対象の商品が見つかりませんでした。他で削除された可能性があります。", "保存できません")
                End If
            Else
                Await _repo.InsertAsync(p)
            End If

            Await LoadProductsAsync()
            ClearForm()
        Catch ex As DbUpdateException When IsUniqueViolation(ex)
            ShowWarning("この商品コードは既に使われています。別のコードにしてください。", "保存できません")
        Finally
            SetBusy(False)
        End Try
    End Sub

    ''' <summary>
    ''' 入力チェック
    ''' </summary>
    Private Function ValidateInput() As Boolean
        Dim code As String = txtProductCode.Text.Trim()
        If code.Length = 0 Then
            ShowWarning("商品コードを入力してください。", "入力エラー")
            txtProductCode.Focus()
            Return False
        End If

        If code.Length > 20 Then
            ShowWarning("商品コードは20文字以内で入力してください。", "入力エラー")
            txtProductCode.Focus()
            Return False
        End If

        Dim productName As String = txtName.Text.Trim()
        If productName.Length = 0 Then
            ShowWarning("商品名を入力してください。", "入力エラー")
            txtName.Focus()
            Return False
        End If

        If productName.Length > 100 Then
            ShowWarning("商品名は100文字以内で入力してください。", "入力エラー")
            txtName.Focus()
            Return False
        End If

        Return True
    End Function

    ''' <summary>
    ''' 削除
    ''' </summary>
    Private Async Sub btnDelete_Click(sender As Object, e As EventArgs) Handles btnDelete.Click
        If Not _currentId.HasValue Then
            ShowWarning("削除する商品を一覧から選択してください。", "削除")
            Return
        End If

        Dim answer As DialogResult = ShowYesNoDialog($"{txtName.Text}を削除します。よろしいですか？", "削除の確認")
        If answer <> DialogResult.Yes Then
            Return
        End If

        Dim targetId As Integer = _currentId.Value

        SetBusy(True)
        Try
            Await _repo.DeleteAsync(targetId)
            Await LoadProductsAsync()
            ClearForm()
        Catch ex As DbUpdateException When IsForeignKeyViolation(ex)
            AppLog.Warn($"商品を削除できませんでした(注文で使用中)。ProductId={targetId}")
            ShowWarning("この商品は注文で使われているため削除できません。" & vbCrLf &
                        "使えなくする場合は、販売中のチェックを外してください。", "削除できません")
        Catch ex As Exception
            ShowError("商品データの削除に失敗しました。", ex)
        Finally
            SetBusy(False)
        End Try
    End Sub

    ''' <summary>
    ''' CSV取込
    ''' </summary>
    Private Async Sub btnImport_Click(sender As Object, e As EventArgs) Handles btnImport.Click
        Dim imported As Boolean
        Using frm As New ProductImportForm()
            frm.ShowDialog(Me)
            imported = frm.Imported
        End Using

        If imported Then
            SetBusy(True)
            Try
                Await LoadProductsAsync()
                ClearForm()
            Finally
                SetBusy(False)
            End Try
        End If
    End Sub

    ''' <summary>
    ''' 画面を閉じる
    ''' </summary>
    Private Sub btnClose_Click(sender As Object, e As EventArgs) Handles btnClose.Click
        Close()
    End Sub

End Class