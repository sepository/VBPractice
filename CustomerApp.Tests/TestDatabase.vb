Imports System.Text
Imports Microsoft.Data.SqlClient
Imports Microsoft.EntityFrameworkCore

Public Module TestDatabase

    Private ReadOnly SyncRoot As New Object()
    Private _isReady As Boolean = False

    Public ReadOnly Property ConnectionString As String
        Get
            Dim fromEnvironment = Environment.GetEnvironmentVariable("CUSTOMERAPP_TEST_CONNECTION")
            If Not String.IsNullOrWhiteSpace(fromEnvironment) Then
                Return fromEnvironment
            End If

            Return "Server=localhost\SQLEXPRESS;Database=CustomerDb_Test;Trusted_Connection=True;TrustServerCertificate=True;"
        End Get
    End Property

    ''' <summary>
    ''' データベース名が「_Test」で終わっていることを確認する。
    ''' </summary>
    Private Sub EnsureSafeTarget()
        Dim builder As New SqlConnectionStringBuilder(ConnectionString)

        If Not builder.InitialCatalog.EndsWith("_Test", StringComparison.OrdinalIgnoreCase) Then
            Throw New InvalidOperationException(
                $"安全のため中止しました。テスト用データベースの名前は「_Test」で終わる必要があります。(現在: {builder.InitialCatalog}")
        End If
    End Sub

    ''' <summary>
    ''' テスト全体で最初の一度だけ、データベースを削除して作り直す。
    ''' </summary>
    Public Sub EnsureReady()
        SyncLock SyncRoot
            If _isReady Then
                Return
            End If

            EnsureSafeTarget()

            Using db As New AppDbContext(ConnectionString)
                db.Database.EnsureDeleted()
                db.Database.EnsureCreated()
            End Using

            _isReady = True
        End SyncLock
    End Sub

    ''' <summary>
    ''' 全テーブルのデータを削除する。
    ''' 各テストの前に呼び、テスト同士が影響し合わないようにする。
    ''' </summary>
    Public Sub ClearAllData()
        EnsureSafeTarget()

        Dim sb As New StringBuilder()
        sb.Append("DELETE FROM OrderItems; ")
        sb.Append("DELETE FROM Orders; ")
        sb.Append("DELETE FROM Products; ")
        sb.Append("DELETE FROM Customers; ")

        Using db As New AppDbContext(ConnectionString)
            db.Database.ExecuteSqlRaw(sb.ToString())
        End Using
    End Sub

End Module
