<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class OrderForm
    Inherits System.Windows.Forms.Form

    'フォームがコンポーネントの一覧をクリーンアップするために dispose をオーバーライドします。
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Windows フォーム デザイナーで必要です。
    Private components As System.ComponentModel.IContainer

    'メモ: 以下のプロシージャは Windows フォーム デザイナーで必要です。
    'Windows フォーム デザイナーを使用して変更できます。  
    'コード エディターを使って変更しないでください。
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Label1 = New Label()
        lblCustomer = New Label()
        dgvOrders = New DataGridView()
        lblAmount = New Label()
        Label2 = New Label()
        Label3 = New Label()
        lblTotal = New Label()
        Label5 = New Label()
        dtpOrderDate = New DateTimePicker()
        Label6 = New Label()
        txtProductName = New TextBox()
        numQuantity = New NumericUpDown()
        Label7 = New Label()
        numUnitPrice = New NumericUpDown()
        Label8 = New Label()
        txtNote = New TextBox()
        Label9 = New Label()
        btnNew = New Button()
        btnSave = New Button()
        btnDelete = New Button()
        btnClose = New Button()
        CType(dgvOrders, ComponentModel.ISupportInitialize).BeginInit()
        CType(numQuantity, ComponentModel.ISupportInitialize).BeginInit()
        CType(numUnitPrice, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' Label1
        ' 
        Label1.AutoSize = True
        Label1.Location = New Point(12, 9)
        Label1.Name = "Label1"
        Label1.Size = New Size(43, 15)
        Label1.TabIndex = 0
        Label1.Text = "顧客名"
        ' 
        ' lblCustomer
        ' 
        lblCustomer.AutoSize = True
        lblCustomer.BackColor = SystemColors.ControlLightLight
        lblCustomer.Location = New Point(61, 9)
        lblCustomer.Name = "lblCustomer"
        lblCustomer.Size = New Size(76, 15)
        lblCustomer.TabIndex = 1
        lblCustomer.Text = "顧客名を表示"
        ' 
        ' dgvOrders
        ' 
        dgvOrders.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        dgvOrders.Location = New Point(12, 27)
        dgvOrders.Name = "dgvOrders"
        dgvOrders.Size = New Size(776, 150)
        dgvOrders.TabIndex = 2
        ' 
        ' lblAmount
        ' 
        lblAmount.BackColor = SystemColors.ControlLightLight
        lblAmount.Location = New Point(49, 180)
        lblAmount.Name = "lblAmount"
        lblAmount.Size = New Size(105, 15)
        lblAmount.TabIndex = 3
        lblAmount.Text = "99999999"
        lblAmount.TextAlign = ContentAlignment.TopRight
        ' 
        ' Label2
        ' 
        Label2.AutoSize = True
        Label2.Location = New Point(12, 180)
        Label2.Name = "Label2"
        Label2.Size = New Size(31, 15)
        Label2.TabIndex = 4
        Label2.Text = "小計"
        ' 
        ' Label3
        ' 
        Label3.AutoSize = True
        Label3.Location = New Point(12, 195)
        Label3.Name = "Label3"
        Label3.Size = New Size(31, 15)
        Label3.TabIndex = 5
        Label3.Text = "合計"
        ' 
        ' lblTotal
        ' 
        lblTotal.BackColor = SystemColors.ControlLightLight
        lblTotal.Location = New Point(49, 195)
        lblTotal.Name = "lblTotal"
        lblTotal.Size = New Size(105, 15)
        lblTotal.TabIndex = 6
        lblTotal.Text = "99999999"
        lblTotal.TextAlign = ContentAlignment.TopRight
        ' 
        ' Label5
        ' 
        Label5.AutoSize = True
        Label5.Location = New Point(12, 232)
        Label5.Name = "Label5"
        Label5.Size = New Size(43, 15)
        Label5.TabIndex = 7
        Label5.Text = "注文日"
        ' 
        ' dtpOrderDate
        ' 
        dtpOrderDate.Format = DateTimePickerFormat.Short
        dtpOrderDate.Location = New Point(61, 229)
        dtpOrderDate.Name = "dtpOrderDate"
        dtpOrderDate.Size = New Size(100, 23)
        dtpOrderDate.TabIndex = 8
        ' 
        ' Label6
        ' 
        Label6.AutoSize = True
        Label6.Location = New Point(12, 260)
        Label6.Name = "Label6"
        Label6.Size = New Size(43, 15)
        Label6.TabIndex = 9
        Label6.Text = "商品名"
        ' 
        ' txtProductName
        ' 
        txtProductName.Location = New Point(61, 257)
        txtProductName.Name = "txtProductName"
        txtProductName.Size = New Size(100, 23)
        txtProductName.TabIndex = 10
        ' 
        ' numQuantity
        ' 
        numQuantity.Location = New Point(61, 286)
        numQuantity.Maximum = New Decimal(New Integer() {9999, 0, 0, 0})
        numQuantity.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        numQuantity.Name = "numQuantity"
        numQuantity.Size = New Size(100, 23)
        numQuantity.TabIndex = 11
        numQuantity.Value = New Decimal(New Integer() {1, 0, 0, 0})
        ' 
        ' Label7
        ' 
        Label7.AutoSize = True
        Label7.Location = New Point(12, 288)
        Label7.Name = "Label7"
        Label7.Size = New Size(31, 15)
        Label7.TabIndex = 12
        Label7.Text = "数量"
        ' 
        ' numUnitPrice
        ' 
        numUnitPrice.Location = New Point(61, 315)
        numUnitPrice.Maximum = New Decimal(New Integer() {99999999, 0, 0, 0})
        numUnitPrice.Name = "numUnitPrice"
        numUnitPrice.Size = New Size(100, 23)
        numUnitPrice.TabIndex = 13
        ' 
        ' Label8
        ' 
        Label8.AutoSize = True
        Label8.Location = New Point(12, 317)
        Label8.Name = "Label8"
        Label8.Size = New Size(31, 15)
        Label8.TabIndex = 14
        Label8.Text = "単価"
        ' 
        ' txtNote
        ' 
        txtNote.Location = New Point(61, 344)
        txtNote.Multiline = True
        txtNote.Name = "txtNote"
        txtNote.Size = New Size(188, 52)
        txtNote.TabIndex = 15
        txtNote.Text = "あ" & vbCrLf & "あ" & vbCrLf & "あ"
        ' 
        ' Label9
        ' 
        Label9.AutoSize = True
        Label9.Location = New Point(12, 347)
        Label9.Name = "Label9"
        Label9.Size = New Size(31, 15)
        Label9.TabIndex = 16
        Label9.Text = "備考"
        ' 
        ' btnNew
        ' 
        btnNew.Location = New Point(12, 402)
        btnNew.Name = "btnNew"
        btnNew.Size = New Size(75, 23)
        btnNew.TabIndex = 17
        btnNew.Text = "新規"
        btnNew.UseVisualStyleBackColor = True
        ' 
        ' btnSave
        ' 
        btnSave.Location = New Point(93, 402)
        btnSave.Name = "btnSave"
        btnSave.Size = New Size(75, 23)
        btnSave.TabIndex = 18
        btnSave.Text = "更新"
        btnSave.UseVisualStyleBackColor = True
        ' 
        ' btnDelete
        ' 
        btnDelete.Location = New Point(174, 402)
        btnDelete.Name = "btnDelete"
        btnDelete.Size = New Size(75, 23)
        btnDelete.TabIndex = 19
        btnDelete.Text = "削除"
        btnDelete.UseVisualStyleBackColor = True
        ' 
        ' btnClose
        ' 
        btnClose.Location = New Point(713, 402)
        btnClose.Name = "btnClose"
        btnClose.Size = New Size(75, 23)
        btnClose.TabIndex = 20
        btnClose.Text = "閉じる"
        btnClose.UseVisualStyleBackColor = True
        ' 
        ' OrderForm
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(800, 433)
        Controls.Add(btnClose)
        Controls.Add(btnDelete)
        Controls.Add(btnSave)
        Controls.Add(btnNew)
        Controls.Add(Label9)
        Controls.Add(txtNote)
        Controls.Add(Label8)
        Controls.Add(numUnitPrice)
        Controls.Add(Label7)
        Controls.Add(numQuantity)
        Controls.Add(txtProductName)
        Controls.Add(Label6)
        Controls.Add(dtpOrderDate)
        Controls.Add(Label5)
        Controls.Add(lblTotal)
        Controls.Add(Label3)
        Controls.Add(Label2)
        Controls.Add(lblAmount)
        Controls.Add(dgvOrders)
        Controls.Add(lblCustomer)
        Controls.Add(Label1)
        Name = "OrderForm"
        Text = "OrderForm"
        CType(dgvOrders, ComponentModel.ISupportInitialize).EndInit()
        CType(numQuantity, ComponentModel.ISupportInitialize).EndInit()
        CType(numUnitPrice, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents Label1 As Label
    Friend WithEvents lblCustomer As Label
    Friend WithEvents dgvOrders As DataGridView
    Friend WithEvents lblAmount As Label
    Friend WithEvents Label2 As Label
    Friend WithEvents Label3 As Label
    Friend WithEvents lblTotal As Label
    Friend WithEvents Label5 As Label
    Friend WithEvents dtpOrderDate As DateTimePicker
    Friend WithEvents Label6 As Label
    Friend WithEvents txtProductName As TextBox
    Friend WithEvents numQuantity As NumericUpDown
    Friend WithEvents Label7 As Label
    Friend WithEvents numUnitPrice As NumericUpDown
    Friend WithEvents Label8 As Label
    Friend WithEvents txtNote As TextBox
    Friend WithEvents Label9 As Label
    Friend WithEvents btnNew As Button
    Friend WithEvents btnSave As Button
    Friend WithEvents btnDelete As Button
    Friend WithEvents btnClose As Button
End Class
