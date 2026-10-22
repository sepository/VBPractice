''' <summary>
''' アプリの起動時にデータベースを最新の状態にする。
''' 設定ファイルの「Database」の項目で実行するかどうか切り替えられる。
''' </summary>
Public Module DatabaseInitializer

    ''' <summary>
    ''' データベースを準備する。
    ''' </summary>
    Public Sub Run(connectionString As String)
        Dim autoCreate As Boolean = AppConfig.GetBool("Database:AutoCreate", True)
        Dim autoMigrate As Boolean = AppConfig.GetBool("Database:AutoMigrate", True)
        Dim runner As New MigrationRunner(connectionString, EmbeddedMigrations.Load())

        If autoCreate AndAlso runner.EnsureDatabaseExists() Then
            AppLog.Info("データベースが無かったため、作成しました。")
        End If

        If autoMigrate Then
            For Each script As MigrationScript In runner.ApplyPending()
                AppLog.Info($"マイグレーションを適用しました: {script.Name}")
            Next
        Else
            Dim pending As List(Of MigrationScript) = runner.GetPendingScripts()
            If pending.Count > 0 Then
                Throw New MigrationException($"データベースが最新ではありません。管理者にデータベースの更新を依頼してください。{vbCrLf}" &
                                             $"未適用: {String.Join(", ", pending.Select(Function(s) s.Name))}")
            End If
        End If

        AppLog.Info($"データベースのバージョン: V{runner.GetCurrentVersion():000}")
    End Sub

End Module
