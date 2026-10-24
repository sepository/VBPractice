<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ProductImportForm
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
        btnClose = New Button()
        txtResult = New TextBox()
        btnImport = New Button()
        lblFile = New Label()
        btnBrowse = New Button()
        SuspendLayout()
        ' 
        ' btnClose
        ' 
        btnClose.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnClose.Location = New Point(325, 309)
        btnClose.Name = "btnClose"
        btnClose.Size = New Size(100, 23)
        btnClose.TabIndex = 9
        btnClose.Text = "閉じる"
        btnClose.UseVisualStyleBackColor = True
        ' 
        ' txtResult
        ' 
        txtResult.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        txtResult.Font = New Font("ＭＳ ゴシック", 9F, FontStyle.Regular, GraphicsUnit.Point, CByte(128))
        txtResult.Location = New Point(12, 56)
        txtResult.Multiline = True
        txtResult.Name = "txtResult"
        txtResult.ReadOnly = True
        txtResult.ScrollBars = ScrollBars.Vertical
        txtResult.Size = New Size(413, 247)
        txtResult.TabIndex = 8
        ' 
        ' btnImport
        ' 
        btnImport.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        btnImport.Location = New Point(12, 309)
        btnImport.Name = "btnImport"
        btnImport.Size = New Size(100, 23)
        btnImport.TabIndex = 7
        btnImport.Text = "取込"
        btnImport.UseVisualStyleBackColor = True
        ' 
        ' lblFile
        ' 
        lblFile.AutoSize = True
        lblFile.Location = New Point(12, 38)
        lblFile.Name = "lblFile"
        lblFile.Size = New Size(139, 15)
        lblFile.TabIndex = 6
        lblFile.Text = "(ファイルが選ばれていません)"
        ' 
        ' btnBrowse
        ' 
        btnBrowse.Location = New Point(12, 12)
        btnBrowse.Name = "btnBrowse"
        btnBrowse.Size = New Size(100, 23)
        btnBrowse.TabIndex = 5
        btnBrowse.Text = "ファイルを選ぶ..."
        btnBrowse.UseVisualStyleBackColor = True
        ' 
        ' ProductImportForm
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(439, 344)
        Controls.Add(btnClose)
        Controls.Add(txtResult)
        Controls.Add(btnImport)
        Controls.Add(lblFile)
        Controls.Add(btnBrowse)
        Name = "ProductImportForm"
        Text = "ProductImportForm"
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents btnClose As Button
    Friend WithEvents txtResult As TextBox
    Friend WithEvents btnImport As Button
    Friend WithEvents lblFile As Label
    Friend WithEvents btnBrowse As Button
End Class
