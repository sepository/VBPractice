Imports Microsoft.Data.SqlClient
Imports Microsoft.EntityFrameworkCore

Public Module DbHelper

    ''' <summary>
    ''' 保存・削除の失敗が外部キー制約違反かどうかを判定
    ''' </summary>
    Public Function IsForeignKeyViolation(ex As DbUpdateException) As Boolean
        Dim sqlEx As SqlException = TryCast(ex.InnerException, SqlException)
        Return sqlEx IsNot Nothing AndAlso sqlEx.Number = 547
    End Function

    ''' <summary>
    ''' 一意制約違反かどうかを判定する
    ''' </summary>
    Public Function IsUniqueViolation(ex As DbUpdateException) As Boolean
        Dim sqlEx As SqlException = TryCast(ex.InnerException, SqlException)
        Return sqlEx IsNot Nothing AndAlso (sqlEx.Number = 2627 OrElse sqlEx.Number = 2601)
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
