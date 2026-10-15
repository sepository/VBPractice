-- 顧客管理アプリ用 DB・テーブル作成スクリプト
-- SSMS(SQL Server Management Studio)または sqlcmd で実行してください。
--   例: sqlcmd -S localhost\SQLEXPRESS -E -i 01_CreateDatabase.sql

IF DB_ID(N'CustomerDb') IS NULL
BEGIN
    CREATE DATABASE CustomerDb;
END
GO

USE CustomerDb;
GO

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

-- 動作確認用のサンプルデータ(不要なら削除してください)
IF NOT EXISTS (SELECT 1 FROM dbo.Customers)
BEGIN
    INSERT INTO dbo.Customers (Name, Kana, Email, Phone, Address, Memo)
    VALUES
        (N'山田 太郎', N'ヤマダ タロウ', N'yamada@example.com', N'090-0000-0001', N'愛知県名古屋市', N'サンプル1'),
        (N'佐藤 花子', N'サトウ ハナコ', N'sato@example.com',   N'090-0000-0002', N'東京都千代田区', N'サンプル2');
END
GO
