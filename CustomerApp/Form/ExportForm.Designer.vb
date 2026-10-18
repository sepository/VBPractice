<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ExportForm
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
        rdoCustomers = New RadioButton()
        rdoOrders = New RadioButton()
        dtpFrom = New DateTimePicker()
        Label2 = New Label()
        dtpTo = New DateTimePicker()
        rdoExcel = New RadioButton()
        rdoCsv = New RadioButton()
        GroupBox1 = New GroupBox()
        GroupBox2 = New GroupBox()
        GroupBox3 = New GroupBox()
        btnExport = New Button()
        btnClose = New Button()
        GroupBox1.SuspendLayout()
        GroupBox2.SuspendLayout()
        GroupBox3.SuspendLayout()
        SuspendLayout()
        ' 
        ' rdoCustomers
        ' 
        rdoCustomers.AutoSize = True
        rdoCustomers.Location = New Point(6, 22)
        rdoCustomers.Name = "rdoCustomers"
        rdoCustomers.Size = New Size(73, 19)
        rdoCustomers.TabIndex = 0
        rdoCustomers.TabStop = True
        rdoCustomers.Text = "顧客一覧"
        rdoCustomers.UseVisualStyleBackColor = True
        ' 
        ' rdoOrders
        ' 
        rdoOrders.AutoSize = True
        rdoOrders.Location = New Point(85, 22)
        rdoOrders.Name = "rdoOrders"
        rdoOrders.Size = New Size(73, 19)
        rdoOrders.TabIndex = 1
        rdoOrders.TabStop = True
        rdoOrders.Text = "注文明細"
        rdoOrders.UseVisualStyleBackColor = True
        ' 
        ' dtpFrom
        ' 
        dtpFrom.Format = DateTimePickerFormat.Short
        dtpFrom.Location = New Point(6, 22)
        dtpFrom.Name = "dtpFrom"
        dtpFrom.Size = New Size(87, 23)
        dtpFrom.TabIndex = 3
        ' 
        ' Label2
        ' 
        Label2.AutoSize = True
        Label2.Location = New Point(99, 28)
        Label2.Name = "Label2"
        Label2.Size = New Size(19, 15)
        Label2.TabIndex = 4
        Label2.Text = "～"
        ' 
        ' dtpTo
        ' 
        dtpTo.Format = DateTimePickerFormat.Short
        dtpTo.Location = New Point(124, 22)
        dtpTo.Name = "dtpTo"
        dtpTo.Size = New Size(87, 23)
        dtpTo.TabIndex = 5
        ' 
        ' rboExcel
        ' 
        rdoExcel.AutoSize = True
        rdoExcel.Location = New Point(6, 22)
        rdoExcel.Name = "rboExcel"
        rdoExcel.Size = New Size(86, 19)
        rdoExcel.TabIndex = 6
        rdoExcel.TabStop = True
        rdoExcel.Text = "Excel (.xlsx)"
        rdoExcel.UseVisualStyleBackColor = True
        ' 
        ' rdoCsv
        ' 
        rdoCsv.AutoSize = True
        rdoCsv.Location = New Point(98, 22)
        rdoCsv.Name = "rdoCsv"
        rdoCsv.Size = New Size(86, 19)
        rdoCsv.TabIndex = 7
        rdoCsv.TabStop = True
        rdoCsv.Text = "CSV (UTF-8)"
        rdoCsv.UseVisualStyleBackColor = True
        ' 
        ' GroupBox1
        ' 
        GroupBox1.Controls.Add(rdoCustomers)
        GroupBox1.Controls.Add(rdoOrders)
        GroupBox1.Location = New Point(12, 12)
        GroupBox1.Name = "GroupBox1"
        GroupBox1.Size = New Size(230, 60)
        GroupBox1.TabIndex = 8
        GroupBox1.TabStop = False
        GroupBox1.Text = "出力"
        ' 
        ' GroupBox2
        ' 
        GroupBox2.Controls.Add(dtpFrom)
        GroupBox2.Controls.Add(Label2)
        GroupBox2.Controls.Add(dtpTo)
        GroupBox2.Location = New Point(12, 78)
        GroupBox2.Name = "GroupBox2"
        GroupBox2.Size = New Size(230, 60)
        GroupBox2.TabIndex = 9
        GroupBox2.TabStop = False
        GroupBox2.Text = "期間"
        ' 
        ' GroupBox3
        ' 
        GroupBox3.Controls.Add(rdoExcel)
        GroupBox3.Controls.Add(rdoCsv)
        GroupBox3.Location = New Point(12, 144)
        GroupBox3.Name = "GroupBox3"
        GroupBox3.Size = New Size(230, 60)
        GroupBox3.TabIndex = 10
        GroupBox3.TabStop = False
        GroupBox3.Text = "形式"
        ' 
        ' btnExport
        ' 
        btnExport.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        btnExport.Location = New Point(12, 216)
        btnExport.Name = "btnExport"
        btnExport.Size = New Size(75, 23)
        btnExport.TabIndex = 11
        btnExport.Text = "出力"
        btnExport.UseVisualStyleBackColor = True
        ' 
        ' btnClose
        ' 
        btnClose.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnClose.Location = New Point(405, 216)
        btnClose.Name = "btnClose"
        btnClose.Size = New Size(75, 23)
        btnClose.TabIndex = 12
        btnClose.Text = "閉じる"
        btnClose.UseVisualStyleBackColor = True
        ' 
        ' ExportForm
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(492, 251)
        Controls.Add(btnClose)
        Controls.Add(btnExport)
        Controls.Add(GroupBox3)
        Controls.Add(GroupBox2)
        Controls.Add(GroupBox1)
        Name = "ExportForm"
        Text = "ExportForm"
        GroupBox1.ResumeLayout(False)
        GroupBox1.PerformLayout()
        GroupBox2.ResumeLayout(False)
        GroupBox2.PerformLayout()
        GroupBox3.ResumeLayout(False)
        GroupBox3.PerformLayout()
        ResumeLayout(False)
    End Sub

    Friend WithEvents rdoCustomers As RadioButton
    Friend WithEvents rdoOrders As RadioButton
    Friend WithEvents dtpFrom As DateTimePicker
    Friend WithEvents Label2 As Label
    Friend WithEvents dtpTo As DateTimePicker
    Friend WithEvents rdoExcel As RadioButton
    Friend WithEvents rdoCsv As RadioButton
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents GroupBox2 As GroupBox
    Friend WithEvents GroupBox3 As GroupBox
    Friend WithEvents btnExport As Button
    Friend WithEvents btnClose As Button
End Class
