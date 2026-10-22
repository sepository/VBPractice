-- V001: 顧客テーブル(Customers)
--
-- ■ マイグレーションスクリプトの書き方のルール
--   ・データベース名は書かない(CREATE DATABASE や USE は書かない)。接続先のデータベースに対して実行される。
--   ・「GO」だけの行で、バッチ(実行の単位)を区切る。
--   ・適用済みのスクリプトは、あとから書き換えない。変更したいときは、新しい番号(V005, V006 ...)のスクリプトを足す。
--     (書き換えると、チェックサムの不一致として検出され、アプリは起動しない)
--   ・サンプルデータは入れない。(開発用のサンプルは、Seed フォルダの別のスクリプトにある)
--   ・既に作成済みのデータベースに対しても、安全に実行できるよう、「無ければ作る」の形で書く。

IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customers
    (
        CustomerId INT IDENTITY(1, 1) NOT NULL
            CONSTRAINT PK_Customers PRIMARY KEY,
        Name       NVARCHAR(100) NOT NULL,
        Kana       NVARCHAR(100) NULL,
        Email      NVARCHAR(254) NULL,
        Phone      NVARCHAR(20)  NULL,
        Address    NVARCHAR(200) NULL,
        Memo       NVARCHAR(500) NULL,
        CreatedAt  DATETIME2     NOT NULL
            CONSTRAINT DF_Customers_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt  DATETIME2     NOT NULL
            CONSTRAINT DF_Customers_UpdatedAt DEFAULT SYSDATETIME()
    );

    CREATE INDEX IX_Customers_Name ON dbo.Customers (Name);
END
GO
