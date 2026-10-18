Public Module MessageUtils

    ''' <summary>
    ''' エラーメッセージ表示
    ''' </summary>
    ''' <param name="message">
    ''' メッセージ
    ''' </param>
    ''' <param name="ex">
    ''' 例外情報
    ''' </param>
    Public Sub ShowError(message As String, Optional ex As Exception = Nothing)
        Dim m As String = message
        If ex IsNot Nothing Then
            m &= $"{vbCrLf}{vbCrLf}{ex.Message}"
        End If

        AppLog.Error(m, ex)
        MessageBox.Show(m, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error)
    End Sub

    ''' <summary>
    ''' 警告メッセージ表示
    ''' </summary>
    ''' <param name="message">
    ''' メッセージ
    ''' </param>
    ''' <param name="title">
    ''' タイトル
    ''' </param>
    Public Sub ShowWarning(message As String, title As String)
        MessageBox.Show(
            message,
            title,
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning
            )
    End Sub

    ''' <summary>
    ''' 注意メッセージ表示
    ''' </summary>
    ''' <param name="message">
    ''' メッセージ
    ''' </param>
    ''' <param name="title">
    ''' タイトル
    ''' </param>
    Public Sub ShowInformaion(message As String, title As String)
        MessageBox.Show(
            message,
            title,
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
            )
    End Sub

    ''' <summary>
    ''' 確認メッセージ表示
    ''' </summary>
    ''' <param name="message">
    ''' メッセージ
    ''' </param>
    ''' <param name="title">
    ''' タイトル
    ''' </param>
    ''' <returns>
    ''' 選択結果（はい、いいえ）
    ''' </returns>
    Public Function ShowYesNoDialog(message As String, title As String)
        Return MessageBox.Show(
            message,
            title,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2
            )
    End Function

End Module
