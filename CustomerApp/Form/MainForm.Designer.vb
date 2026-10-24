<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class MainForm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        txtSearch = New TextBox()
        btnSearch = New Button()
        dgvCustomers = New DataGridView()
        Label2 = New Label()
        Label4 = New Label()
        txtName = New TextBox()
        Label5 = New Label()
        txtKana = New TextBox()
        GroupBox1 = New GroupBox()
        btnCancelSearch = New Button()
        GroupBox2 = New GroupBox()
        btnOrders = New Button()
        txtAddress = New TextBox()
        Label7 = New Label()
        txtMemo = New TextBox()
        Label6 = New Label()
        txtPhone = New TextBox()
        Label3 = New Label()
        txtEmail = New TextBox()
        Label1 = New Label()
        btnProducts = New Button()
        btnNew = New Button()
        btnSave = New Button()
        btnDelete = New Button()
        GroupBox3 = New GroupBox()
        GroupBox4 = New GroupBox()
        btnImport = New Button()
        btnExport = New Button()
        btnBackup = New Button()
        CType(dgvCustomers, ComponentModel.ISupportInitialize).BeginInit()
        GroupBox1.SuspendLayout()
        GroupBox2.SuspendLayout()
        GroupBox3.SuspendLayout()
        GroupBox4.SuspendLayout()
        SuspendLayout()
        ' 
        ' txtSearch
        ' 
        txtSearch.Location = New Point(6, 22)
        txtSearch.Name = "txtSearch"
        txtSearch.Size = New Size(100, 23)
        txtSearch.TabIndex = 0
        ' 
        ' btnSearch
        ' 
        btnSearch.Location = New Point(112, 22)
        btnSearch.Name = "btnSearch"
        btnSearch.Size = New Size(75, 23)
        btnSearch.TabIndex = 1
        btnSearch.Text = "検索"
        btnSearch.UseVisualStyleBackColor = True
        ' 
        ' dgvCustomers
        ' 
        dgvCustomers.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        dgvCustomers.Location = New Point(6, 51)
        dgvCustomers.Name = "dgvCustomers"
        dgvCustomers.Size = New Size(764, 150)
        dgvCustomers.TabIndex = 2
        ' 
        ' Label2
        ' 
        Label2.AutoSize = True
        Label2.Location = New Point(12, 53)
        Label2.Name = "Label2"
        Label2.Size = New Size(31, 15)
        Label2.TabIndex = 4
        Label2.Text = "一覧"
        ' 
        ' Label4
        ' 
        Label4.AutoSize = True
        Label4.Location = New Point(6, 25)
        Label4.Name = "Label4"
        Label4.Size = New Size(31, 15)
        Label4.TabIndex = 6
        Label4.Text = "名前"
        ' 
        ' txtName
        ' 
        txtName.Location = New Point(45, 22)
        txtName.Name = "txtName"
        txtName.Size = New Size(200, 23)
        txtName.TabIndex = 0
        ' 
        ' Label5
        ' 
        Label5.AutoSize = True
        Label5.Location = New Point(251, 25)
        Label5.Name = "Label5"
        Label5.Size = New Size(41, 15)
        Label5.TabIndex = 8
        Label5.Text = "フリガナ"
        ' 
        ' txtKana
        ' 
        txtKana.Location = New Point(298, 22)
        txtKana.Name = "txtKana"
        txtKana.Size = New Size(200, 23)
        txtKana.TabIndex = 1
        ' 
        ' GroupBox1
        ' 
        GroupBox1.Controls.Add(btnCancelSearch)
        GroupBox1.Controls.Add(txtSearch)
        GroupBox1.Controls.Add(btnSearch)
        GroupBox1.Controls.Add(dgvCustomers)
        GroupBox1.Location = New Point(12, 12)
        GroupBox1.Name = "GroupBox1"
        GroupBox1.Size = New Size(776, 209)
        GroupBox1.TabIndex = 0
        GroupBox1.TabStop = False
        GroupBox1.Text = "検索"
        ' 
        ' btnCancelSearch
        ' 
        btnCancelSearch.Enabled = False
        btnCancelSearch.Location = New Point(193, 22)
        btnCancelSearch.Name = "btnCancelSearch"
        btnCancelSearch.Size = New Size(75, 23)
        btnCancelSearch.TabIndex = 3
        btnCancelSearch.Text = "中止"
        btnCancelSearch.UseVisualStyleBackColor = True
        ' 
        ' GroupBox2
        ' 
        GroupBox2.Controls.Add(btnOrders)
        GroupBox2.Controls.Add(txtAddress)
        GroupBox2.Controls.Add(Label7)
        GroupBox2.Controls.Add(txtMemo)
        GroupBox2.Controls.Add(Label6)
        GroupBox2.Controls.Add(txtPhone)
        GroupBox2.Controls.Add(Label3)
        GroupBox2.Controls.Add(txtEmail)
        GroupBox2.Controls.Add(Label1)
        GroupBox2.Controls.Add(Label4)
        GroupBox2.Controls.Add(txtName)
        GroupBox2.Controls.Add(txtKana)
        GroupBox2.Controls.Add(Label5)
        GroupBox2.Location = New Point(12, 227)
        GroupBox2.Name = "GroupBox2"
        GroupBox2.Size = New Size(776, 173)
        GroupBox2.TabIndex = 1
        GroupBox2.TabStop = False
        GroupBox2.Text = "入力"
        ' 
        ' btnOrders
        ' 
        btnOrders.Location = New Point(298, 138)
        btnOrders.Name = "btnOrders"
        btnOrders.Size = New Size(75, 23)
        btnOrders.TabIndex = 5
        btnOrders.Text = "注文管理"
        btnOrders.UseVisualStyleBackColor = True
        ' 
        ' txtAddress
        ' 
        txtAddress.Location = New Point(45, 80)
        txtAddress.Name = "txtAddress"
        txtAddress.Size = New Size(200, 23)
        txtAddress.TabIndex = 4
        ' 
        ' Label7
        ' 
        Label7.AutoSize = True
        Label7.Location = New Point(6, 83)
        Label7.Name = "Label7"
        Label7.Size = New Size(31, 15)
        Label7.TabIndex = 16
        Label7.Text = "住所"
        ' 
        ' txtMemo
        ' 
        txtMemo.Location = New Point(45, 109)
        txtMemo.Multiline = True
        txtMemo.Name = "txtMemo"
        txtMemo.Size = New Size(200, 52)
        txtMemo.TabIndex = 5
        ' 
        ' Label6
        ' 
        Label6.AutoSize = True
        Label6.Location = New Point(6, 112)
        Label6.Name = "Label6"
        Label6.Size = New Size(31, 15)
        Label6.TabIndex = 14
        Label6.Text = "備考"
        ' 
        ' txtPhone
        ' 
        txtPhone.Location = New Point(298, 51)
        txtPhone.Name = "txtPhone"
        txtPhone.Size = New Size(200, 23)
        txtPhone.TabIndex = 3
        ' 
        ' Label3
        ' 
        Label3.AutoSize = True
        Label3.Location = New Point(251, 54)
        Label3.Name = "Label3"
        Label3.Size = New Size(31, 15)
        Label3.TabIndex = 12
        Label3.Text = "電話"
        ' 
        ' txtEmail
        ' 
        txtEmail.Location = New Point(45, 51)
        txtEmail.Name = "txtEmail"
        txtEmail.Size = New Size(200, 23)
        txtEmail.TabIndex = 2
        ' 
        ' Label1
        ' 
        Label1.AutoSize = True
        Label1.Location = New Point(6, 54)
        Label1.Name = "Label1"
        Label1.Size = New Size(33, 15)
        Label1.TabIndex = 10
        Label1.Text = "メール"
        ' 
        ' btnProducts
        ' 
        btnProducts.Location = New Point(6, 22)
        btnProducts.Name = "btnProducts"
        btnProducts.Size = New Size(100, 23)
        btnProducts.TabIndex = 17
        btnProducts.Text = "商品マスタ"
        btnProducts.UseVisualStyleBackColor = True
        ' 
        ' btnNew
        ' 
        btnNew.Location = New Point(6, 22)
        btnNew.Name = "btnNew"
        btnNew.Size = New Size(75, 23)
        btnNew.TabIndex = 2
        btnNew.Text = "新規"
        btnNew.UseVisualStyleBackColor = True
        ' 
        ' btnSave
        ' 
        btnSave.Location = New Point(87, 22)
        btnSave.Name = "btnSave"
        btnSave.Size = New Size(75, 23)
        btnSave.TabIndex = 3
        btnSave.Text = "保存"
        btnSave.UseVisualStyleBackColor = True
        ' 
        ' btnDelete
        ' 
        btnDelete.Location = New Point(168, 22)
        btnDelete.Name = "btnDelete"
        btnDelete.Size = New Size(75, 23)
        btnDelete.TabIndex = 4
        btnDelete.Text = "削除"
        btnDelete.UseVisualStyleBackColor = True
        ' 
        ' GroupBox3
        ' 
        GroupBox3.Controls.Add(btnNew)
        GroupBox3.Controls.Add(btnDelete)
        GroupBox3.Controls.Add(btnSave)
        GroupBox3.Location = New Point(12, 406)
        GroupBox3.Name = "GroupBox3"
        GroupBox3.Size = New Size(776, 60)
        GroupBox3.TabIndex = 5
        GroupBox3.TabStop = False
        GroupBox3.Text = "操作"
        ' 
        ' GroupBox4
        ' 
        GroupBox4.Controls.Add(btnBackup)
        GroupBox4.Controls.Add(btnImport)
        GroupBox4.Controls.Add(btnExport)
        GroupBox4.Controls.Add(btnProducts)
        GroupBox4.Location = New Point(12, 472)
        GroupBox4.Name = "GroupBox4"
        GroupBox4.Size = New Size(776, 60)
        GroupBox4.TabIndex = 6
        GroupBox4.TabStop = False
        GroupBox4.Text = "メニュー"
        ' 
        ' btnImport
        ' 
        btnImport.Location = New Point(218, 22)
        btnImport.Name = "btnImport"
        btnImport.Size = New Size(100, 23)
        btnImport.TabIndex = 19
        btnImport.Text = "CSV取込"
        btnImport.UseVisualStyleBackColor = True
        ' 
        ' btnExport
        ' 
        btnExport.Location = New Point(112, 22)
        btnExport.Name = "btnExport"
        btnExport.Size = New Size(100, 23)
        btnExport.TabIndex = 18
        btnExport.Text = "エクスポート"
        btnExport.UseVisualStyleBackColor = True
        ' 
        ' btnBackup
        ' 
        btnBackup.Location = New Point(324, 22)
        btnBackup.Name = "btnBackup"
        btnBackup.Size = New Size(100, 23)
        btnBackup.TabIndex = 20
        btnBackup.Text = "バックアップ・復元"
        btnBackup.UseVisualStyleBackColor = True
        ' 
        ' MainForm
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(800, 543)
        Controls.Add(GroupBox4)
        Controls.Add(GroupBox3)
        Controls.Add(GroupBox2)
        Controls.Add(GroupBox1)
        Controls.Add(Label2)
        Name = "MainForm"
        Text = "顧客管理システム"
        CType(dgvCustomers, ComponentModel.ISupportInitialize).EndInit()
        GroupBox1.ResumeLayout(False)
        GroupBox1.PerformLayout()
        GroupBox2.ResumeLayout(False)
        GroupBox2.PerformLayout()
        GroupBox3.ResumeLayout(False)
        GroupBox4.ResumeLayout(False)
        ResumeLayout(False)
        PerformLayout()
    End Sub
    Friend WithEvents txtSearch As TextBox
    Friend WithEvents btnSearch As Button
    Friend WithEvents dgvCustomers As DataGridView
    Friend WithEvents Label2 As Label
    Friend WithEvents Label4 As Label
    Friend WithEvents txtName As TextBox
    Friend WithEvents Label5 As Label
    Friend WithEvents txtKana As TextBox
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents GroupBox2 As GroupBox
    Friend WithEvents txtEmail As TextBox
    Friend WithEvents Label1 As Label
    Friend WithEvents txtMemo As TextBox
    Friend WithEvents Label6 As Label
    Friend WithEvents txtPhone As TextBox
    Friend WithEvents Label3 As Label
    Friend WithEvents btnNew As Button
    Friend WithEvents btnSave As Button
    Friend WithEvents btnDelete As Button
    Friend WithEvents txtAddress As TextBox
    Friend WithEvents Label7 As Label
    Friend WithEvents btnOrders As Button
    Friend WithEvents btnProducts As Button
    Friend WithEvents GroupBox3 As GroupBox
    Friend WithEvents GroupBox4 As GroupBox
    Friend WithEvents btnExport As Button
    Friend WithEvents btnImport As Button
    Friend WithEvents btnCancelSearch As Button
    Friend WithEvents btnBackup As Button

End Class
