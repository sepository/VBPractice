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
        lblOrderTotal = New Label()
        Label2 = New Label()
        Label3 = New Label()
        lblTotal = New Label()
        Label5 = New Label()
        dtpOrderDate = New DateTimePicker()
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
        GroupBox1 = New GroupBox()
        GroupBox2 = New GroupBox()
        GroupBox3 = New GroupBox()
        cboProduct = New ComboBox()
        Label4 = New Label()
        lblItemTotal = New Label()
        Label10 = New Label()
        btnItemRemove = New Button()
        btnItemUpdate = New Button()
        btnItemAdd = New Button()
        dgvItems = New DataGridView()
        CType(dgvOrders, ComponentModel.ISupportInitialize).BeginInit()
        CType(numQuantity, ComponentModel.ISupportInitialize).BeginInit()
        CType(numUnitPrice, ComponentModel.ISupportInitialize).BeginInit()
        GroupBox1.SuspendLayout()
        GroupBox2.SuspendLayout()
        GroupBox3.SuspendLayout()
        CType(dgvItems, ComponentModel.ISupportInitialize).BeginInit()
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
        dgvOrders.Location = New Point(6, 22)
        dgvOrders.Name = "dgvOrders"
        dgvOrders.Size = New Size(764, 150)
        dgvOrders.TabIndex = 2
        ' 
        ' lblOrderTotal
        ' 
        lblOrderTotal.BackColor = SystemColors.ControlLightLight
        lblOrderTotal.Location = New Point(58, 62)
        lblOrderTotal.Name = "lblOrderTotal"
        lblOrderTotal.Size = New Size(105, 15)
        lblOrderTotal.TabIndex = 3
        lblOrderTotal.Text = "99999999"
        lblOrderTotal.TextAlign = ContentAlignment.TopRight
        ' 
        ' Label2
        ' 
        Label2.AutoSize = True
        Label2.Location = New Point(9, 62)
        Label2.Name = "Label2"
        Label2.Size = New Size(43, 15)
        Label2.TabIndex = 4
        Label2.Text = "注文額"
        ' 
        ' Label3
        ' 
        Label3.AutoSize = True
        Label3.Location = New Point(6, 175)
        Label3.Name = "Label3"
        Label3.Size = New Size(31, 15)
        Label3.TabIndex = 5
        Label3.Text = "合計"
        ' 
        ' lblTotal
        ' 
        lblTotal.BackColor = SystemColors.ControlLightLight
        lblTotal.Location = New Point(43, 175)
        lblTotal.Name = "lblTotal"
        lblTotal.Size = New Size(105, 15)
        lblTotal.TabIndex = 6
        lblTotal.Text = "99999999"
        lblTotal.TextAlign = ContentAlignment.TopRight
        ' 
        ' Label5
        ' 
        Label5.AutoSize = True
        Label5.Location = New Point(9, 25)
        Label5.Name = "Label5"
        Label5.Size = New Size(43, 15)
        Label5.TabIndex = 7
        Label5.Text = "注文日"
        ' 
        ' dtpOrderDate
        ' 
        dtpOrderDate.Format = DateTimePickerFormat.Short
        dtpOrderDate.Location = New Point(58, 22)
        dtpOrderDate.Name = "dtpOrderDate"
        dtpOrderDate.Size = New Size(100, 23)
        dtpOrderDate.TabIndex = 8
        ' 
        ' numQuantity
        ' 
        numQuantity.Location = New Point(364, 220)
        numQuantity.Maximum = New Decimal(New Integer() {9999, 0, 0, 0})
        numQuantity.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        numQuantity.Name = "numQuantity"
        numQuantity.Size = New Size(100, 23)
        numQuantity.TabIndex = 11
        numQuantity.TextAlign = HorizontalAlignment.Right
        numQuantity.Value = New Decimal(New Integer() {1, 0, 0, 0})
        ' 
        ' Label7
        ' 
        Label7.AutoSize = True
        Label7.Location = New Point(327, 224)
        Label7.Name = "Label7"
        Label7.Size = New Size(31, 15)
        Label7.TabIndex = 12
        Label7.Text = "数量"
        ' 
        ' numUnitPrice
        ' 
        numUnitPrice.Location = New Point(507, 220)
        numUnitPrice.Maximum = New Decimal(New Integer() {99999999, 0, 0, 0})
        numUnitPrice.Name = "numUnitPrice"
        numUnitPrice.Size = New Size(100, 23)
        numUnitPrice.TabIndex = 13
        numUnitPrice.TextAlign = HorizontalAlignment.Right
        ' 
        ' Label8
        ' 
        Label8.AutoSize = True
        Label8.Location = New Point(470, 224)
        Label8.Name = "Label8"
        Label8.Size = New Size(31, 15)
        Label8.TabIndex = 14
        Label8.Text = "単価"
        ' 
        ' txtNote
        ' 
        txtNote.Location = New Point(201, 25)
        txtNote.Multiline = True
        txtNote.Name = "txtNote"
        txtNote.Size = New Size(188, 52)
        txtNote.TabIndex = 15
        txtNote.Text = "あ" & vbCrLf & "あ" & vbCrLf & "あ"
        ' 
        ' Label9
        ' 
        Label9.AutoSize = True
        Label9.Location = New Point(164, 25)
        Label9.Name = "Label9"
        Label9.Size = New Size(31, 15)
        Label9.TabIndex = 16
        Label9.Text = "備考"
        ' 
        ' btnNew
        ' 
        btnNew.Location = New Point(12, 579)
        btnNew.Name = "btnNew"
        btnNew.Size = New Size(75, 23)
        btnNew.TabIndex = 17
        btnNew.Text = "新規"
        btnNew.UseVisualStyleBackColor = True
        ' 
        ' btnSave
        ' 
        btnSave.Location = New Point(93, 579)
        btnSave.Name = "btnSave"
        btnSave.Size = New Size(75, 23)
        btnSave.TabIndex = 18
        btnSave.Text = "保存"
        btnSave.UseVisualStyleBackColor = True
        ' 
        ' btnDelete
        ' 
        btnDelete.Location = New Point(174, 579)
        btnDelete.Name = "btnDelete"
        btnDelete.Size = New Size(75, 23)
        btnDelete.TabIndex = 19
        btnDelete.Text = "削除"
        btnDelete.UseVisualStyleBackColor = True
        ' 
        ' btnClose
        ' 
        btnClose.Location = New Point(713, 579)
        btnClose.Name = "btnClose"
        btnClose.Size = New Size(75, 23)
        btnClose.TabIndex = 20
        btnClose.Text = "閉じる"
        btnClose.UseVisualStyleBackColor = True
        ' 
        ' GroupBox1
        ' 
        GroupBox1.Controls.Add(dgvOrders)
        GroupBox1.Controls.Add(Label3)
        GroupBox1.Controls.Add(lblTotal)
        GroupBox1.Location = New Point(12, 27)
        GroupBox1.Name = "GroupBox1"
        GroupBox1.Size = New Size(776, 200)
        GroupBox1.TabIndex = 21
        GroupBox1.TabStop = False
        GroupBox1.Text = "注文一覧"
        ' 
        ' GroupBox2
        ' 
        GroupBox2.Controls.Add(GroupBox3)
        GroupBox2.Controls.Add(Label5)
        GroupBox2.Controls.Add(dtpOrderDate)
        GroupBox2.Controls.Add(txtNote)
        GroupBox2.Controls.Add(Label9)
        GroupBox2.Controls.Add(lblOrderTotal)
        GroupBox2.Controls.Add(Label2)
        GroupBox2.Location = New Point(12, 233)
        GroupBox2.Name = "GroupBox2"
        GroupBox2.Size = New Size(776, 340)
        GroupBox2.TabIndex = 22
        GroupBox2.TabStop = False
        GroupBox2.Text = "注文情報"
        ' 
        ' GroupBox3
        ' 
        GroupBox3.Controls.Add(cboProduct)
        GroupBox3.Controls.Add(Label4)
        GroupBox3.Controls.Add(lblItemTotal)
        GroupBox3.Controls.Add(Label10)
        GroupBox3.Controls.Add(btnItemRemove)
        GroupBox3.Controls.Add(btnItemUpdate)
        GroupBox3.Controls.Add(Label7)
        GroupBox3.Controls.Add(Label8)
        GroupBox3.Controls.Add(numQuantity)
        GroupBox3.Controls.Add(btnItemAdd)
        GroupBox3.Controls.Add(numUnitPrice)
        GroupBox3.Controls.Add(dgvItems)
        GroupBox3.Location = New Point(9, 83)
        GroupBox3.Name = "GroupBox3"
        GroupBox3.Size = New Size(761, 251)
        GroupBox3.TabIndex = 17
        GroupBox3.TabStop = False
        GroupBox3.Text = "注文明細"
        ' 
        ' cboProduct
        ' 
        cboProduct.DropDownStyle = ComboBoxStyle.DropDownList
        cboProduct.FormattingEnabled = True
        cboProduct.Location = New Point(43, 220)
        cboProduct.Name = "cboProduct"
        cboProduct.Size = New Size(278, 23)
        cboProduct.TabIndex = 18
        ' 
        ' Label4
        ' 
        Label4.AutoSize = True
        Label4.Location = New Point(6, 223)
        Label4.Name = "Label4"
        Label4.Size = New Size(31, 15)
        Label4.TabIndex = 17
        Label4.Text = "商品"
        ' 
        ' lblItemTotal
        ' 
        lblItemTotal.BackColor = SystemColors.ControlLightLight
        lblItemTotal.Location = New Point(650, 224)
        lblItemTotal.Name = "lblItemTotal"
        lblItemTotal.Size = New Size(105, 15)
        lblItemTotal.TabIndex = 15
        lblItemTotal.Text = "99999999"
        lblItemTotal.TextAlign = ContentAlignment.TopRight
        ' 
        ' Label10
        ' 
        Label10.AutoSize = True
        Label10.Location = New Point(613, 223)
        Label10.Name = "Label10"
        Label10.Size = New Size(31, 15)
        Label10.TabIndex = 16
        Label10.Text = "金額"
        ' 
        ' btnItemRemove
        ' 
        btnItemRemove.Location = New Point(168, 156)
        btnItemRemove.Name = "btnItemRemove"
        btnItemRemove.Size = New Size(75, 23)
        btnItemRemove.TabIndex = 3
        btnItemRemove.Text = "明細を削除"
        btnItemRemove.UseVisualStyleBackColor = True
        ' 
        ' btnItemUpdate
        ' 
        btnItemUpdate.Location = New Point(87, 156)
        btnItemUpdate.Name = "btnItemUpdate"
        btnItemUpdate.Size = New Size(75, 23)
        btnItemUpdate.TabIndex = 2
        btnItemUpdate.Text = "明細を更新"
        btnItemUpdate.UseVisualStyleBackColor = True
        ' 
        ' btnItemAdd
        ' 
        btnItemAdd.Location = New Point(6, 156)
        btnItemAdd.Name = "btnItemAdd"
        btnItemAdd.Size = New Size(75, 23)
        btnItemAdd.TabIndex = 1
        btnItemAdd.Text = "明細を追加"
        btnItemAdd.UseVisualStyleBackColor = True
        ' 
        ' dgvItems
        ' 
        dgvItems.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        dgvItems.Location = New Point(6, 22)
        dgvItems.Name = "dgvItems"
        dgvItems.Size = New Size(749, 128)
        dgvItems.TabIndex = 0
        ' 
        ' OrderForm
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(800, 614)
        Controls.Add(GroupBox2)
        Controls.Add(GroupBox1)
        Controls.Add(btnClose)
        Controls.Add(lblCustomer)
        Controls.Add(Label1)
        Controls.Add(btnDelete)
        Controls.Add(btnNew)
        Controls.Add(btnSave)
        Name = "OrderForm"
        Text = "OrderForm"
        CType(dgvOrders, ComponentModel.ISupportInitialize).EndInit()
        CType(numQuantity, ComponentModel.ISupportInitialize).EndInit()
        CType(numUnitPrice, ComponentModel.ISupportInitialize).EndInit()
        GroupBox1.ResumeLayout(False)
        GroupBox1.PerformLayout()
        GroupBox2.ResumeLayout(False)
        GroupBox2.PerformLayout()
        GroupBox3.ResumeLayout(False)
        GroupBox3.PerformLayout()
        CType(dgvItems, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents Label1 As Label
    Friend WithEvents lblCustomer As Label
    Friend WithEvents dgvOrders As DataGridView
    Friend WithEvents lblOrderTotal As Label
    Friend WithEvents Label2 As Label
    Friend WithEvents Label3 As Label
    Friend WithEvents lblTotal As Label
    Friend WithEvents Label5 As Label
    Friend WithEvents dtpOrderDate As DateTimePicker
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
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents GroupBox2 As GroupBox
    Friend WithEvents GroupBox3 As GroupBox
    Friend WithEvents btnItemAdd As Button
    Friend WithEvents dgvItems As DataGridView
    Friend WithEvents lblItemTotal As Label
    Friend WithEvents Label10 As Label
    Friend WithEvents btnItemRemove As Button
    Friend WithEvents btnItemUpdate As Button
    Friend WithEvents cboProduct As ComboBox
    Friend WithEvents Label4 As Label
End Class
