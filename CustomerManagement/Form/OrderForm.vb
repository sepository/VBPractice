Imports System.ComponentModel

Public Class OrderForm

    Private ReadOnly _repo As New OrderRepository(AppConfig.ConnectionString)
    Private ReadOnly _productRepo As New ProductRepository(AppConfig.ConnectionString)

    ''' <summary>
    ''' 顧客ID
    ''' </summary>
    Private _customerId As Integer

    ''' <summary>
    ''' 顧客名
    ''' </summary>
    Private _customerName As String = String.Empty

    ''' <summary>
    ''' 注文ID
    ''' Nothingの時は新規登録モード
    ''' </summary>
    Private _currentOrderId As Integer? = Nothing

    ''' <summary>
    ''' 販売中の商品
    ''' </summary>
    Private _activeProducts As New List(Of Product)()

    ''' <summary>
    ''' 編集中の注文明細
    ''' </summary>
    Private ReadOnly _items As New BindingList(Of OrderItem)

    ''' <summary>
    ''' DBアクセス中、二重処理や画面終了を防ぐ。
    ''' </summary>
    Private _isBusy As Boolean = False

    ''' <summary>
    ''' コンストラクタ(呼び出さない)
    ''' </summary>
    Public Sub New()
        InitializeComponent()
    End Sub

    ''' <summary>
    ''' コンストラクタ
    ''' </summary>
    ''' <param name="customerId">
    ''' 顧客ID
    ''' </param>
    ''' <param name="customerName">
    ''' 顧客名
    ''' </param>
    Public Sub New(customerId As Integer, customerName As String)
        Me.New()
        _customerId = customerId
        _customerName = customerName
    End Sub

    ''' <summary>
    ''' 起動時の処理
    ''' </summary>
    Private Async Sub OrderForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Text = $"注文管理 - {_customerName}"
        lblCustomer.Text = $"{_customerName}(ID:{_customerId})"
        SetupGrid()
        SetupItemsGrid()
        SetBusy(True)
        Try
            Await LoadProductsAsync()
            Await LoadOrdersAsync()
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

    ''' <summary>
    ''' グリッドの初期化
    ''' </summary>
    Private Sub SetupGrid()
        Dim rightAligned As New DataGridViewCellStyle With {
            .Format = "N0",
            .Alignment = DataGridViewContentAlignment.MiddleRight
        }

        With dgvOrders
            .AutoGenerateColumns = False
            .Columns.Clear()
            .Columns.Add(New DataGridViewTextBoxColumn With {
                         .DataPropertyName = "OrderId",
                         .HeaderText = "ID",
                         .Width = 50})
            .Columns.Add(New DataGridViewTextBoxColumn With {
                         .DataPropertyName = "OrderDate",
                         .HeaderText = "注文日",
                         .Width = 100,
                         .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "yyyy/MM/dd"}})
            .Columns.Add(New DataGridViewTextBoxColumn With {
                         .DataPropertyName = "Summary",
                         .HeaderText = "内容",
                         .Width = 200})
            .Columns.Add(New DataGridViewTextBoxColumn With {
                         .DataPropertyName = "ItemCount",
                         .HeaderText = "明細数",
                         .Width = 60,
                         .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleRight}})
            .Columns.Add(New DataGridViewTextBoxColumn With {
                         .DataPropertyName = "Amount",
                         .HeaderText = "金額",
                         .Width = 100,
                         .DefaultCellStyle = rightAligned})
            .ReadOnly = True
            .AllowUserToAddRows = False
            .MultiSelect = False
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect
        End With
    End Sub

    ''' <summary>
    ''' 明細グリッドの初期化
    ''' </summary>
    Private Sub SetupItemsGrid()
        Dim rightAligned As New DataGridViewCellStyle With {
            .Format = "N0",
            .Alignment = DataGridViewContentAlignment.MiddleRight
        }

        With dgvItems
            .AutoGenerateColumns = False
            .Columns.Clear()
            .Columns.Add(New DataGridViewTextBoxColumn With {
                         .DataPropertyName = "ProductName",
                         .HeaderText = "商品名",
                         .Width = 200})
            .Columns.Add(New DataGridViewTextBoxColumn With {
                         .DataPropertyName = "Quantity",
                         .HeaderText = "数量",
                         .Width = 60,
                         .DefaultCellStyle = rightAligned})
            .Columns.Add(New DataGridViewTextBoxColumn With {
                         .DataPropertyName = "UnitPrice",
                         .HeaderText = "単価",
                         .Width = 90,
                         .DefaultCellStyle = rightAligned})
            .Columns.Add(New DataGridViewTextBoxColumn With {
                         .DataPropertyName = "Amount",
                         .HeaderText = "金額",
                         .Width = 100,
                         .DefaultCellStyle = rightAligned})
            .ReadOnly = True
            .AllowUserToAddRows = False
            .MultiSelect = False
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect
            .DataSource = _items
        End With
    End Sub

    ''' <summary>
    ''' DBアクセス中、操作ボタンを無効にし、待機カーソルにする。
    ''' </summary>
    Private Sub SetBusy(busy As Boolean)
        _isBusy = busy
        UseWaitCursor = busy

        For Each ctrl As Control In New Control() {
            dgvOrders, dgvItems, btnItemAdd, btnItemUpdate, btnItemRemove, btnNew, btnSave, btnDelete, btnClose}
            ctrl.Enabled = Not busy
        Next
    End Sub

    ''' <summary>
    ''' 販売中の商品取得
    ''' </summary>
    Private Async Function LoadProductsAsync() As Task
        Try
            _activeProducts = Await _productRepo.GetActiveAsync()

            cboProduct.DisplayMember = "DisplayName"
            cboProduct.ValueMember = "ProductId"
            cboProduct.DataSource = _activeProducts
            cboProduct.SelectedIndex = -1
        Catch ex As Exception
            ShowError("商品データの取得に失敗しました。", ex)
        End Try
    End Function

    ''' <summary>
    ''' 注文情報の取得
    ''' </summary>
    Private Async Function LoadOrdersAsync() As Task
        Try
            Dim orders As List(Of CustomerOrder) = Await _repo.GetByCustomerIdAsync(_customerId)
            dgvOrders.DataSource = orders

            Dim total As Decimal = orders.Sum(Function(o) o.Amount)
            lblTotal.Text = $"{total:N0}円"
        Catch ex As Exception
            ShowError("注文データの取得に失敗しました。", ex)
        End Try
    End Function

    ''' <summary>
    ''' 選択した行の情報を表示
    ''' </summary>
    Private Sub dgvOrders_SelectionChanged(sender As Object, e As EventArgs) Handles dgvOrders.SelectionChanged
        If dgvOrders.CurrentRow Is Nothing Then
            Return
        End If

        Dim o As CustomerOrder = TryCast(dgvOrders.CurrentRow.DataBoundItem, CustomerOrder)
        If o Is Nothing Then
            Return
        End If

        _currentOrderId = o.OrderId
        dtpOrderDate.Value = o.OrderDate
        txtNote.Text = o.Note

        _items.Clear()
        For Each i As OrderItem In o.Items
            _items.Add(New OrderItem With {
                       .ProductId = i.ProductId,
                       .ProductName = i.ProductName,
                       .Quantity = i.Quantity,
                       .UnitPrice = i.UnitPrice
            })
        Next

        ClearItemInput()
        UpdateOrderTotal()
    End Sub

    ''' <summary>
    ''' 明細の行選択
    ''' </summary>
    Private Sub dgvItems_SelectionChanged(sender As Object, e As EventArgs) Handles dgvItems.SelectionChanged
        If dgvItems.SelectedRows.Count = 0 Then
            Return
        End If

        Dim item As OrderItem = TryCast(dgvItems.SelectedRows(0).DataBoundItem, OrderItem)
        If item Is Nothing Then
            Return
        End If

        cboProduct.SelectedIndex = _activeProducts.FindIndex(Function(p) p.ProductId = item.ProductId)
        numQuantity.Value = item.Quantity
        numUnitPrice.Value = item.UnitPrice
    End Sub

    ''' <summary>
    ''' 商品選択時
    ''' </summary>
    Private Sub cboProduct_SelectionChangeCommitted(sender As Object, e As EventArgs) _
        Handles cboProduct.SelectionChangeCommitted

        Dim p As Product = TryCast(cboProduct.SelectedItem, Product)
        If p IsNot Nothing Then
            numUnitPrice.Value = p.UnitPrice
        End If
    End Sub

    ''' <summary>
    ''' 明細の金額
    ''' </summary>
    Private Sub AmountInput_ValueChanged(sender As Object, e As EventArgs) _
        Handles numQuantity.ValueChanged, numUnitPrice.ValueChanged

        Dim amount As Decimal = numQuantity.Value * numUnitPrice.Value
        lblItemTotal.Text = $"{amount:N0}円"
    End Sub

    ''' <summary>
    ''' 明細の追加
    ''' </summary>
    Private Sub btnItemAdd_Click(sender As Object, e As EventArgs) Handles btnItemAdd.Click
        If Not ValidateItemInput() Then
            Return
        End If

        Dim p As Product = DirectCast(cboProduct.SelectedItem, Product)

        ' 商品名と単価は、この時点の値を明細にコピーして持たせる(スナップショット)
        _items.Add(New OrderItem With {
            .ProductId = p.ProductId,
            .ProductName = p.Name,
            .Quantity = CInt(numQuantity.Value),
            .UnitPrice = numUnitPrice.Value
        })

        ClearItemInput()
        UpdateOrderTotal()
        cboProduct.Focus()
    End Sub

    ''' <summary>
    ''' 明細の更新
    ''' </summary>
    Private Sub btnItemUpdate_Click(sender As Object, e As EventArgs) Handles btnItemUpdate.Click
        If dgvItems.SelectedRows.Count = 0 Then
            ShowInformaion("更新する明細を一覧から選択してください。", "明細の更新")
            Return
        End If

        If Not ValidateItemInput() Then
            Return
        End If

        Dim p As Product = DirectCast(cboProduct.SelectedItem, Product)

        Dim index As Integer = dgvItems.SelectedRows(0).Index
        Dim item As OrderItem = _items(index)
        item.ProductId = p.ProductId
        item.ProductName = p.Name
        item.Quantity = CInt(numQuantity.Value)
        item.UnitPrice = numUnitPrice.Value

        ' 中身を書き換えただけでは画面に反映されないので、再描画する
        _items.ResetItem(index)

        ClearItemInput()
        UpdateOrderTotal()
    End Sub

    ''' <summary>
    ''' 明細の削除
    ''' </summary>
    Private Sub btnItemRemove_Click(sener As Object, e As EventArgs) Handles btnItemRemove.Click
        If dgvItems.SelectedRows.Count = 0 Then
            ShowInformaion("削除する明細を一覧から選択してください。", "明細の削除")
            Return
        End If

        _items.RemoveAt(dgvItems.SelectedRows(0).Index)

        ClearItemInput()
        UpdateOrderTotal()
    End Sub

    ''' <summary>
    ''' 明細の入力チェック
    ''' </summary>
    Private Function ValidateItemInput() As Boolean
        If cboProduct.SelectedItem Is Nothing Then
            ShowWarning("商品を選択してください。(販売停止の商品は選べません)", "入力エラー")
            cboProduct.Focus()
            Return False
        End If

        Return True
    End Function

    ''' <summary>
    ''' 明細のクリア
    ''' </summary>
    Private Sub ClearItemInput()
        cboProduct.SelectedIndex = -1
        numQuantity.Value = 1
        numUnitPrice.Value = 0
        dgvItems.ClearSelection()
        AmountInput_ValueChanged(Me, EventArgs.Empty)
    End Sub

    ''' <summary>
    ''' 注文の合計金額
    ''' </summary>
    Private Sub UpdateOrderTotal()
        Dim total As Decimal = _items.Sum(Function(i) i.Amount)
        lblOrderTotal.Text = $"{total:N0}円"
    End Sub

    ''' <summary>
    ''' 新規
    ''' </summary>
    Private Sub btnNew_Click(sender As Object, e As EventArgs) Handles btnNew.Click
        ClearForm()
        cboProduct.Focus()
    End Sub

    ''' <summary>
    ''' 保存
    ''' </summary>
    Private Async Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        If _items.Count = 0 Then
            ShowWarning("明細を1行以上追加してください。", "入力エラー")
            cboProduct.Focus()
            Return
        End If

        ' 入力値は、Awaitに入る前にオブジェクトに取り込んでおく
        Dim o As New CustomerOrder With {
            .CustomerId = _customerId,
            .OrderDate = dtpOrderDate.Value.Date,
            .Note = txtNote.Text
        }
        o.Items.AddRange(_items)
        Dim editingId As Integer? = _currentOrderId

        SetBusy(True)
        Try
            If editingId.HasValue Then
                o.OrderId = editingId.Value
                If Not Await _repo.UpdateAsync(o) Then
                    ShowWarning("対象の注文が見つかりませんでした。", "更新できません")
                End If
            Else
                Await _repo.InsertAsync(o)
            End If

            Await LoadOrdersAsync()
            ClearForm()
        Catch ex As Exception
            ShowError("注文データの保存に失敗しました。", ex)
        Finally
            SetBusy(False)
        End Try
    End Sub

    ''' <summary>
    ''' 削除
    ''' </summary>
    Private Async Sub btnDelete_Click(sender As Object, e As EventArgs) Handles btnDelete.Click
        If Not _currentOrderId.HasValue Then
            ShowInformaion("削除する注文を一覧から選択してください。", "削除")
            Return
        End If

        Dim answer As DialogResult = ShowYesNoDialog(
            $"{dtpOrderDate.Value:yyyy/MM/dd}の注文を削除します。{vbCrLf}" &
            $"明細({_items.Count}行)もすべて削除されます。よろしいですか？",
            "削除の確認")
        If answer <> DialogResult.Yes Then
            Return
        End If

        Dim targetId As Integer = _currentOrderId

        SetBusy(True)
        Try
            Await _repo.DeleteAsync(targetId)
            Await LoadOrdersAsync()
            ClearForm()
        Catch ex As Exception
            ShowError("注文データの削除に失敗しました。", ex)
        Finally
            SetBusy(False)
        End Try
    End Sub

    ''' <summary>
    ''' 画面を閉じる
    ''' </summary>
    Private Sub btnClose_Click(sender As Object, e As EventArgs) Handles btnClose.Click
        Close()
    End Sub

    ''' <summary>
    ''' 画面の入力情報クリア
    ''' </summary>
    Private Sub ClearForm()
        dgvOrders.ClearSelection()
        _currentOrderId = Nothing
        dtpOrderDate.Value = DateTime.Today
        txtNote.Clear()
        _items.Clear()
        ClearItemInput()
        UpdateOrderTotal()
    End Sub

End Class