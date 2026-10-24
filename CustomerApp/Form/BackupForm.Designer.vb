<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class BackupForm
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
        lblServer = New Label()
        txtPath = New TextBox()
        btnBrowse = New Button()
        btnBackup = New Button()
        btnRestore = New Button()
        txtResult = New TextBox()
        btnClose = New Button()
        SuspendLayout()
        ' 
        ' lblServer
        ' 
        lblServer.AutoSize = True
        lblServer.Location = New Point(12, 9)
        lblServer.Name = "lblServer"
        lblServer.Size = New Size(43, 30)
        lblServer.TabIndex = 0
        lblServer.Text = "接続先" & vbCrLf & "表示"
        ' 
        ' txtPath
        ' 
        txtPath.Location = New Point(12, 42)
        txtPath.Name = "txtPath"
        txtPath.Size = New Size(300, 23)
        txtPath.TabIndex = 1
        ' 
        ' btnBrowse
        ' 
        btnBrowse.Location = New Point(318, 42)
        btnBrowse.Name = "btnBrowse"
        btnBrowse.Size = New Size(75, 23)
        btnBrowse.TabIndex = 2
        btnBrowse.Text = "選ぶ..."
        btnBrowse.UseVisualStyleBackColor = True
        ' 
        ' btnBackup
        ' 
        btnBackup.Location = New Point(12, 71)
        btnBackup.Name = "btnBackup"
        btnBackup.Size = New Size(100, 23)
        btnBackup.TabIndex = 3
        btnBackup.Text = "バックアップを取る"
        btnBackup.UseVisualStyleBackColor = True
        ' 
        ' btnRestore
        ' 
        btnRestore.Location = New Point(118, 71)
        btnRestore.Name = "btnRestore"
        btnRestore.Size = New Size(100, 23)
        btnRestore.TabIndex = 4
        btnRestore.Text = "復元する..."
        btnRestore.UseVisualStyleBackColor = True
        ' 
        ' txtResult
        ' 
        txtResult.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        txtResult.Location = New Point(12, 100)
        txtResult.Multiline = True
        txtResult.Name = "txtResult"
        txtResult.ReadOnly = True
        txtResult.ScrollBars = ScrollBars.Vertical
        txtResult.Size = New Size(776, 309)
        txtResult.TabIndex = 5
        ' 
        ' btnClose
        ' 
        btnClose.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnClose.Location = New Point(713, 415)
        btnClose.Name = "btnClose"
        btnClose.Size = New Size(75, 23)
        btnClose.TabIndex = 6
        btnClose.Text = "閉じる"
        btnClose.UseVisualStyleBackColor = True
        ' 
        ' BackupForm
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(800, 450)
        Controls.Add(btnClose)
        Controls.Add(txtResult)
        Controls.Add(btnRestore)
        Controls.Add(btnBackup)
        Controls.Add(btnBrowse)
        Controls.Add(txtPath)
        Controls.Add(lblServer)
        Name = "BackupForm"
        Text = "BackupForm"
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents lblServer As Label
    Friend WithEvents txtPath As TextBox
    Friend WithEvents btnBrowse As Button
    Friend WithEvents btnBackup As Button
    Friend WithEvents btnRestore As Button
    Friend WithEvents txtResult As TextBox
    Friend WithEvents btnClose As Button
End Class
