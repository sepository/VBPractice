''' <summary>
''' アプリ全体の設定。まずは定数で持ち、慣れてきたら appsettings.json などに移す。
''' </summary>
Public Module AppConfig

    ' SQL Server Express の既定インスタンス名は SQLEXPRESS。
    ' 最近のドライバは暗号化が既定で有効なため、ローカル開発では TrustServerCertificate=True を付ける。
    Public Const ConnectionString As String =
        "Server=localhost\SQLEXPRESS;Database=CustomerDb;Trusted_Connection=True;TrustServerCertificate=True;"

End Module
