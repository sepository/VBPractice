Namespace My
    ' The following events are available for MyApplication:
    ' Startup: Raised when the application starts, before the startup form is created.
    ' Shutdown: Raised after all application forms are closed. This event is not raised if the application terminates abnormally.
    ' UnhandledException: Raised if the application encounters an unhandled exception.
    ' StartupNextInstance: Raised when launching a single-instance application and the application is already active.
    ' NetworkAvailabilityChanged: Raised when the network connection is connected or disconnected.

    ' **NEW** ApplyApplicationDefaults: Raised when the application queries default values to be set for the application.

    ' Example:
    ' Private Sub MyApplication_ApplyApplicationDefaults(sender As Object, e As ApplyApplicationDefaultsEventArgs) Handles Me.ApplyApplicationDefaults
    '
    '   ' Setting the application-wide default Font:
    '   e.Font = New Font(FontFamily.GenericSansSerif, 12, FontStyle.Regular)
    '
    '   ' Setting the HighDpiMode for the Application:
    '   e.HighDpiMode = HighDpiMode.PerMonitorV2
    '
    '   ' If a splash dialog is used, this sets the minimum display time:
    '   e.MinimumSplashScreenDisplayTime = 4000
    ' End Sub

    Partial Friend Class MyApplication

        ''' <summary>
        ''' アプリの起動時
        ''' </summary>
        Private Sub MyApplication_Startup(sender As Object, e As ApplicationServices.StartupEventArgs) Handles Me.Startup
            AppLog.Initialize()
            AppLog.Info($"アプリケーションを起動しました。(バージョン {Info.Version}, .NET {Environment.Version})")

            ' 設定ファイル(appsettings.json)が読み込めるか確認する。
            Try
                AppConfig.Validate()

                Dim localState As String = If(AppConfig.HasLocalSettings(), "あり", "なし")
                AppLog.Info($"設定ファイルを読み込みました。(appsettings.local.json: {localState})")
            Catch ex As Exception
                AppLog.Error("設定ファイルの読み込みに失敗しました。", ex)
                ShowError($"設定ファイルを読み込めなかったため、アプリケーションを起動できません。{vbCrLf}{vbCrLf}" &
                          $"・appsettings.jsonが、実行ファイルと同じフォルダにあるか{vbCrLf}" &
                          $"・書式(カンマ、引用符、波かっこ)が正しいか{vbCrLf}" &
                          $"・接続文字列(ConnectionStrings:Default)が書かれているか" &
                          $"を確認してください。{vbCrLf}{vbCrLf}{GetFullMessage(ex)}")

                ' 起動を中止する
                e.Cancel = True
            End Try
            ' UI以外のスレッドで起きた、誰も捕まえなかった例外
            AddHandler AppDomain.CurrentDomain.UnhandledException, AddressOf OnDomainUnhandledException

            ' Awaitされずに放置されたタスクの中で起きた例外
            AddHandler TaskScheduler.UnobservedTaskException, AddressOf OnUnobservedTaskException
        End Sub

        ''' <summary>
        ''' アプリの終了時
        ''' </summary>
        Private Sub MyApplication_Shutdown(sender As Object, e As EventArgs) Handles Me.Shutdown
            AppLog.Info("アプリケーションを終了しました。")
        End Sub

        ''' <summary>
        ''' UIスレッドで起きた、誰も捕まえなかった例外。
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub MyApplication_UnhandledException(sender As Object, e As ApplicationServices.UnhandledExceptionEventArgs) Handles Me.UnhandledException
            AppLog.Error("UIスレッドで未処理の例外が発生しました。", e.Exception)
            ShowError($"予期しないエラーが発生したため、アプリケーションを終了します。{vbCrLf}" &
                      $"詳細はログに記録しました。{vbCrLf}{AppLog.LogDirectory}", Nothing)

            ' 未処理の例外の後は、アプリの状態が壊れている可能性があるため、続行せず終了する
            e.ExitApplication = True
        End Sub

        Private Shared Sub OnDomainUnhandledException(sender As Object, e As UnhandledExceptionEventArgs)
            AppLog.Error($"未処理の例外が発生しました。(アプリ終了中: {e.IsTerminating}", TryCast(e.ExceptionObject, Exception))
        End Sub

        Private Shared Sub OnUnobservedTaskException(sender As Object, e As UnobservedTaskExceptionEventArgs)
            AppLog.Error($"監視されなかったタスクの例外が発生しました。", e.Exception)
            e.SetObserved()
        End Sub

    End Class

End Namespace
