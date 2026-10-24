<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ProductForm
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
        dgvProducts = New DataGridView()
        GroupBox1 = New GroupBox()
        GroupBox2 = New GroupBox()
        chkIsActive = New CheckBox()
        Label3 = New Label()
        numUnitPrice = New NumericUpDown()
        Label2 = New Label()
        txtName = New TextBox()
        txtProductCode = New TextBox()
        Label1 = New Label()
        btnNew = New Button()
        btnSave = New Button()
        btnDelete = New Button()
        btnClose = New Button()
        btnImport = New Button()
        CType(dgvProducts, ComponentModel.ISupportInitialize).BeginInit()
        GroupBox1.SuspendLayout()
        GroupBox2.SuspendLayout()
        CType(numUnitPrice, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' dgvProducts
        ' 
        dgvProducts.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        dgvProducts.Location = New Point(6, 22)
        dgvProducts.Name = "dgvProducts"
        dgvProducts.Size = New Size(764, 167)
        dgvProducts.TabIndex = 0
        ' 
        ' GroupBox1
        ' 
        GroupBox1.Controls.Add(dgvProducts)
        GroupBox1.Location = New Point(12, 12)
        GroupBox1.Name = "GroupBox1"
        GroupBox1.Size = New Size(776, 202)
        GroupBox1.TabIndex = 1
        GroupBox1.TabStop = False
        GroupBox1.Text = "一覧"
        ' 
        ' GroupBox2
        ' 
        GroupBox2.Controls.Add(chkIsActive)
        GroupBox2.Controls.Add(Label3)
        GroupBox2.Controls.Add(numUnitPrice)
        GroupBox2.Controls.Add(Label2)
        GroupBox2.Controls.Add(txtName)
        GroupBox2.Controls.Add(txtProductCode)
        GroupBox2.Controls.Add(Label1)
        GroupBox2.Location = New Point(12, 220)
        GroupBox2.Name = "GroupBox2"
        GroupBox2.Size = New Size(776, 108)
        GroupBox2.TabIndex = 2
        GroupBox2.TabStop = False
        GroupBox2.Text = "入力"
        ' 
        ' chkIsActive
        ' 
        chkIsActive.AutoSize = True
        chkIsActive.Location = New Point(200, 19)
        chkIsActive.Name = "chkIsActive"
        chkIsActive.Size = New Size(62, 19)
        chkIsActive.TabIndex = 7
        chkIsActive.Text = "販売中"
        chkIsActive.UseVisualStyleBackColor = True
        ' 
        ' Label3
        ' 
        Label3.AutoSize = True
        Label3.Location = New Point(6, 76)
        Label3.Name = "Label3"
        Label3.Size = New Size(31, 15)
        Label3.TabIndex = 5
        Label3.Text = "単価"
        ' 
        ' numUnitPrice
        ' 
        numUnitPrice.Location = New Point(67, 74)
        numUnitPrice.Maximum = New Decimal(New Integer() {99999999, 0, 0, 0})
        numUnitPrice.Name = "numUnitPrice"
        numUnitPrice.Size = New Size(120, 23)
        numUnitPrice.TabIndex = 4
        numUnitPrice.TextAlign = HorizontalAlignment.Right
        ' 
        ' Label2
        ' 
        Label2.AutoSize = True
        Label2.Location = New Point(6, 48)
        Label2.Name = "Label2"
        Label2.Size = New Size(43, 15)
        Label2.TabIndex = 3
        Label2.Text = "商品名"
        ' 
        ' txtName
        ' 
        txtName.Location = New Point(67, 45)
        txtName.Name = "txtName"
        txtName.Size = New Size(127, 23)
        txtName.TabIndex = 2
        txtName.Text = "12345678901234567890"
        ' 
        ' txtProductCode
        ' 
        txtProductCode.Location = New Point(67, 16)
        txtProductCode.Name = "txtProductCode"
        txtProductCode.Size = New Size(127, 23)
        txtProductCode.TabIndex = 1
        txtProductCode.Text = "12345678901234567890"
        ' 
        ' Label1
        ' 
        Label1.AutoSize = True
        Label1.Location = New Point(6, 19)
        Label1.Name = "Label1"
        Label1.Size = New Size(55, 15)
        Label1.TabIndex = 0
        Label1.Text = "商品コード"
        ' 
        ' btnNew
        ' 
        btnNew.Location = New Point(12, 334)
        btnNew.Name = "btnNew"
        btnNew.Size = New Size(75, 23)
        btnNew.TabIndex = 3
        btnNew.Text = "新規"
        btnNew.UseVisualStyleBackColor = True
        ' 
        ' btnSave
        ' 
        btnSave.Location = New Point(93, 334)
        btnSave.Name = "btnSave"
        btnSave.Size = New Size(75, 23)
        btnSave.TabIndex = 4
        btnSave.Text = "保存"
        btnSave.UseVisualStyleBackColor = True
        ' 
        ' btnDelete
        ' 
        btnDelete.Location = New Point(174, 334)
        btnDelete.Name = "btnDelete"
        btnDelete.Size = New Size(75, 23)
        btnDelete.TabIndex = 5
        btnDelete.Text = "削除"
        btnDelete.UseVisualStyleBackColor = True
        ' 
        ' btnClose
        ' 
        btnClose.Location = New Point(713, 334)
        btnClose.Name = "btnClose"
        btnClose.Size = New Size(75, 23)
        btnClose.TabIndex = 6
        btnClose.Text = "閉じる"
        btnClose.UseVisualStyleBackColor = True
        ' 
        ' btnImport
        ' 
        btnImport.Location = New Point(255, 334)
        btnImport.Name = "btnImport"
        btnImport.Size = New Size(75, 23)
        btnImport.TabIndex = 7
        btnImport.Text = "CSV取込"
        btnImport.UseVisualStyleBackColor = True
        ' 
        ' ProductForm
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(800, 366)
        Controls.Add(btnImport)
        Controls.Add(btnClose)
        Controls.Add(btnDelete)
        Controls.Add(btnSave)
        Controls.Add(btnNew)
        Controls.Add(GroupBox2)
        Controls.Add(GroupBox1)
        Name = "ProductForm"
        Text = "商品マスタ"
        CType(dgvProducts, ComponentModel.ISupportInitialize).EndInit()
        GroupBox1.ResumeLayout(False)
        GroupBox2.ResumeLayout(False)
        GroupBox2.PerformLayout()
        CType(numUnitPrice, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
    End Sub

    Friend WithEvents dgvProducts As DataGridView
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents GroupBox2 As GroupBox
    Friend WithEvents Label3 As Label
    Friend WithEvents numUnitPrice As NumericUpDown
    Friend WithEvents Label2 As Label
    Friend WithEvents txtName As TextBox
    Friend WithEvents txtProductCode As TextBox
    Friend WithEvents Label1 As Label
    Friend WithEvents chkIsActive As CheckBox
    Friend WithEvents btnNew As Button
    Friend WithEvents btnSave As Button
    Friend WithEvents btnDelete As Button
    Friend WithEvents btnClose As Button
    Friend WithEvents btnImport As Button
End Class
