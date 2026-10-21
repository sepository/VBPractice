Imports System.IO
Imports System.Text
Imports System.Text.Unicode
Imports Microsoft.Data.SqlClient

''' <summary>
''' ログの重要度
''' </summary>
Public Enum LogSeverity
    ''' <summary>
    ''' 開発中だけ見たい詳細な情報
    ''' </summary>
    Verbose = 0
    ''' <summary>
    ''' 通常の動作の記録（起動・終了など）
    ''' </summary>
    Info = 1
    ''' <summary>
    ''' 異常ではないが、気にしておきたいこと（業務上の拒否、制約違反など）
    ''' </summary>
    Warn = 2
    ''' <summary>
    ''' 失敗（例外）、調査が必要なもの
    ''' </summary>
    [Error] = 3
End Enum

''' <summary>
''' ログ出力
''' </summary>
Public Module AppLog

    Private ReadOnly SyncRoot As New Object()

    ''' <summary>
    ''' ログファイルを残す日数
    ''' </summary>
    Private Const DefaultRetentionDays As Integer = 30

    ''' <summary>
    ''' ログの保存先フォルダ
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property LogDirectory As String =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CustomerApp", "logs")

    ''' <summary>
    ''' この重要度より低いログは出力しない。
    ''' </summary>
#If DEBUG Then
    Public Property MinimumSeverity As LogSeverity = LogSeverity.Verbose
#Else
    Public Property MinimumSeverity As LogSeverity = LogSeverity.Info
#End If

    ''' <summary>
    ''' ログフォルダの作成と、古いログファイルの削除
    ''' </summary>
    Public Sub Initialize()
        Try
            Directory.CreateDirectory(LogDirectory)

            ApplyConfiguration()

            Dim retentionDays As Integer = AppConfig.GetInt("Logging:RetentionDays", DefaultRetentionDays)
            Dim limit As DateTime = DateTime.Now.AddDays(-retentionDays)
            For Each path As String In Directory.GetFiles(LogDirectory, "app-*.log")
                If File.GetLastWriteTime(path) < limit Then
                    File.Delete(path)
                End If
            Next
        Catch
            ' ログの準備に失敗してもアプリは止めない。
        End Try
    End Sub

    Private Sub ApplyConfiguration()
        Try
            Dim text As String = AppConfig.GetString("Logging:MinimumSeverity", Nothing)
            Dim parsed As LogSeverity

            If Not String.IsNullOrWhiteSpace(text) AndAlso [Enum].TryParse(text, True, parsed) Then
                MinimumSeverity = parsed
            End If
        Catch
            ' 設定が読み込めなくても、規定値のまま続ける。
        End Try
    End Sub

    Public Sub Verbose(message As String)
        Write(LogSeverity.Verbose, message, Nothing)
    End Sub

    Public Sub Info(message As String)
        Write(LogSeverity.Info, message, Nothing)
    End Sub

    Public Sub Warn(message As String, Optional ex As Exception = Nothing)
        Write(LogSeverity.Warn, message, ex)
    End Sub

    Public Sub [Error](message As String, Optional ex As Exception = Nothing)
        Write(LogSeverity.Error, message, ex)
    End Sub

    Private Sub Write(severity As LogSeverity, message As String, ex As Exception)
        If severity < MinimumSeverity Then
            Return
        End If

        Try
            Dim sb As New StringBuilder()
            sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
            sb.Append(" [").Append(Label(severity)).Append("] ")
            sb.AppendLine(message)

            If ex IsNot Nothing Then
                sb.AppendLine(Describe(ex))
            End If

            SyncLock SyncRoot
                Directory.CreateDirectory(LogDirectory)
                File.AppendAllText(CurrentLogFile(), sb.ToString(), Encoding.UTF8)
            End SyncLock
        Catch
            ' ログの書き込みに失敗してもアプリを止めない。
        End Try
    End Sub

    Private Function CurrentLogFile() As String
        Return Path.Combine(LogDirectory, $"app-{DateTime.Now:yyyyMMdd}.log")
    End Function

    Private Function Label(severity As LogSeverity) As String
        Select Case severity
            Case LogSeverity.Verbose
                Return "VERB "
            Case LogSeverity.Info
                Return "INFO "
            Case LogSeverity.Warn
                Return "WARN "
            Case Else
                Return "ERROR"
        End Select
    End Function

    Private Function Describe(ex As Exception) As String
        Dim sb As New StringBuilder()
        sb.AppendLine(ex.ToString())

        Dim current As Exception = ex
        While current IsNot Nothing
            Dim sqlEx As SqlException = TryCast(current, SqlException)
            If sqlEx IsNot Nothing Then
                For Each err As SqlError In sqlEx.Errors
                    sb.AppendLine($" [SQL Server] 番号={err.Number}, 重要度={err.Class}, 状態={err.State}, メッセージ={err.Message}")
                Next
            End If

            current = current.InnerException
        End While

        Return sb.ToString().TrimEnd()
    End Function

End Module
