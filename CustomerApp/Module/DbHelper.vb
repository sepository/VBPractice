Imports Microsoft.Data.SqlClient
Imports Microsoft.EntityFrameworkCore

Public Module DbHelper

    ''' <summary>
    ''' 保存・削除の失敗が外部キー制約違反かどうかを判定
    ''' </summary>
    Public Function IsForeignKeyViolation(ex As Exception) As Boolean
        Return HasSqlError(ex, 547)
    End Function

    ''' <summary>
    ''' 一意制約違反かどうかを判定する
    ''' </summary>
    Public Function IsUniqueViolation(ex As Exception) As Boolean
        Dim sqlEx As SqlException = TryCast(ex.InnerException, SqlException)
        Return HasSqlError(ex, 2627) OrElse HasSqlError(ex, 2601)
    End Function

    ''' <summary>
    ''' 例外の入れ子を最後まで辿り、指定した番号のSQL Serverエラーが含まれているかを調べる。
    ''' </summary>
    Private Function HasSqlError(ex As Exception, number As Integer) As Boolean
        Dim current As Exception = ex

        While current IsNot Nothing
            Dim sqlEx As SqlException = TryCast(current, SqlException)
            If sqlEx IsNot Nothing Then
                For Each err As SqlError In sqlEx.Errors
                    If err.Number = number Then
                        Return True
                    End If
                Next
            End If
            current = current.InnerException
        End While

        Return False
    End Function

    ''' <summary>
    ''' エラー表示用に、例外の入れ子のメッセージをすべて連結する。
    ''' </summary>
    Public Function GetFullMessage(ex As Exception) As String
        Dim messages As New List(Of String)()
        Dim current As Exception = ex

        While current IsNot Nothing
            messages.Add(current.Message)
            current = current.InnerException
        End While

        Return String.Join(vbCrLf & " → ", messages)
    End Function

    ''' <summary>
    ''' 空文字・空白のみの文字列はNothingにし、前後の空白は取り除く
    ''' </summary>
    Public Function NullIfBlank(value As String) As String
        If String.IsNullOrWhiteSpace(value) Then
            Return Nothing
        End If
        Return value.Trim()
    End Function

End Module
