Imports Microsoft.Data.SqlClient

Public Class OrderForm

    Private ReadOnly _repo As New OrderRepository(AppConfig.ConnectionString)

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
    Private Sub OrderForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Text = $"注文管理 - {_customerName}"
        lblCustomer.Text = $"{_customerName}(ID:{_customerId})"
        SetupGrid()
        LoadOrders()
        ClearForm()
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
                         .Width = 50
            })
            .Columns.Add(New DataGridViewTextBoxColumn With {
                         .DataPropertyName = "ProductName",
                         .HeaderText = "商品名",
                         .Width = 180
            })
            .Columns.Add(New DataGridViewTextBoxColumn With {
                         .DataPropertyName = "Quantity",
                         .HeaderText = "数量",
                         .Width = 60,
                         .DefaultCellStyle = rightAligned
            })
            .Columns.Add(New DataGridViewTextBoxColumn With {
                         .DataPropertyName = "UnitPrice",
                         .HeaderText = "単価",
                         .Width = 90,
                         .DefaultCellStyle = rightAligned
            })
            .Columns.Add(New DataGridViewTextBoxColumn With {
                         .DataPropertyName = "Amount",
                         .HeaderText = "金額",
                         .Width = 100,
                         .DefaultCellStyle = rightAligned
            })
            .ReadOnly = True
            .AllowUserToAddRows = False
            .MultiSelect = False
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect
        End With
    End Sub

    ''' <summary>
    ''' 注文情報の取得
    ''' </summary>
    Private Sub LoadOrders()
        Try
            Dim orders As List(Of CustomerOrder) = _repo.GetByCustomerId(_customerId)
            dgvOrders.DataSource = orders

            Dim total As Decimal = orders.Sum(Function(o) o.Amount)
            lblTotal.Text = $"{total:N0}円"
        Catch ex As SqlException
            MessageUtils.ShowError("注文データの取得に失敗しました。", ex)
        End Try
    End Sub

    ''' <summary>
    ''' 選択した行の情報を表示
    ''' </summary>
    Private Sub dgvOrders_SelectionChanged(sender As Object, e As EventArgs) Handles dgvOrders.SelectionChanged
        If dgvOrders.CurrentRow Is Nothing Then Return

        Dim o As CustomerOrder = TryCast(dgvOrders.CurrentRow.DataBoundItem, CustomerOrder)
        If o Is Nothing Then Return

        _currentOrderId = o.OrderId
        dtpOrderDate.Value = o.OrderDate
        txtProductName.Text = o.ProductName
        numQuantity.Value = o.Quantity
        numUnitPrice.Value = o.UnitPrice
        txtNote.Text = o.Note
    End Sub

    ''' <summary>
    ''' 小計
    ''' </summary>
    Private Sub AmountInput_ValueChanged(sender As Object, e As EventArgs) _
        Handles numQuantity.ValueChanged, numUnitPrice.ValueChanged

        Dim amount As Decimal = numQuantity.Value * numUnitPrice.Value
        lblAmount.Text = $"{amount}円"
    End Sub

    ''' <summary>
    ''' 新規
    ''' </summary>
    Private Sub btnNew_Click(sender As Object, e As EventArgs) Handles btnNew.Click
        ClearForm()
        txtProductName.Focus()
    End Sub

    ''' <summary>
    ''' 保存
    ''' </summary>
    Private Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        If String.IsNullOrWhiteSpace(txtProductName.Text) Then
            MessageUtils.ShowWarning("商品名を入力してください。", "入力エラー")
            txtProductName.Focus()
            Return
        End If

        Dim o As New CustomerOrder With {
            .CustomerId = _customerId,
            .OrderDate = dtpOrderDate.Value.Date,
            .ProductName = txtProductName.Text,
            .Quantity = CInt(numQuantity.Value),
            .UnitPrice = numUnitPrice.Value,
            .Note = txtNote.Text
        }

        Try
            If _currentOrderId.HasValue Then
                o.OrderId = _currentOrderId.Value
                If Not _repo.Update(o) Then
                    MessageUtils.ShowWarning("対象の注文が見つかりませんでした。", "更新できません")
                End If
            Else
                _repo.Insert(o)
            End If

            LoadOrders()
            ClearForm()
        Catch ex As SqlException
            MessageUtils.ShowError("注文データの保存に失敗しました。", ex)
        End Try
    End Sub

    ''' <summary>
    ''' 削除
    ''' </summary>
    Private Sub btnDelete_Click(sender As Object, e As EventArgs) Handles btnDelete.Click
        If Not _currentOrderId.HasValue Then
            MessageUtils.ShowInformaion("削除する注文を一覧から選択してください。", "削除")
            Return
        End If

        Dim answer As DialogResult = MessageUtils.ShowYesNoDialog(
            $"{txtProductName.Text}の注文を削除します。よろしいですか？", "削除の確認")
        If answer <> DialogResult.Yes Then
            Return
        End If

        Try
            _repo.Delete(_currentOrderId.Value)
            LoadOrders()
            ClearForm()
        Catch ex As SqlException
            MessageUtils.ShowError("注文データの削除に失敗しました。", ex)
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
        txtProductName.Clear()
        numQuantity.Value = 1
        numUnitPrice.Value = 0
        txtNote.Clear()
        AmountInput_ValueChanged(Me, EventArgs.Empty)
    End Sub

End Class