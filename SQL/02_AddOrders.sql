-- 顧客管理アプリ:注文テーブルの追加(顧客 1 : 注文 多)
-- 実行例: sqlcmd -S localhost\SQLEXPRESS -E -C -i 02_AddOrders.sql

USE CustomerDb;
GO

IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Orders
    (
        OrderId     INT IDENTITY(1, 1) NOT NULL
            CONSTRAINT PK_Orders PRIMARY KEY,

        -- 外部キー:存在しない顧客の注文は登録できない。
        -- 注文が残っている顧客は削除できない(ON DELETE の指定なし = NO ACTION)。
        CustomerId  INT            NOT NULL
            CONSTRAINT FK_Orders_Customers
            FOREIGN KEY REFERENCES dbo.Customers (CustomerId),

        OrderDate   DATE           NOT NULL
            CONSTRAINT DF_Orders_OrderDate DEFAULT (CONVERT(date, SYSDATETIME())),
        ProductName NVARCHAR(100)  NOT NULL,
        Quantity    INT            NOT NULL
            CONSTRAINT CK_Orders_Quantity CHECK (Quantity > 0),
        UnitPrice   DECIMAL(18, 2) NOT NULL
            CONSTRAINT CK_Orders_UnitPrice CHECK (UnitPrice >= 0),
        Note        NVARCHAR(500)  NULL,
        CreatedAt   DATETIME2      NOT NULL
            CONSTRAINT DF_Orders_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt   DATETIME2      NOT NULL
            CONSTRAINT DF_Orders_UpdatedAt DEFAULT SYSDATETIME()
    );

    -- 「顧客ごとの注文」を引く検索が多いので、外部キー列にインデックスを付ける
    CREATE INDEX IX_Orders_CustomerId ON dbo.Orders (CustomerId);
END
GO

-- 動作確認用のサンプルデータ(01 のサンプル顧客が残っている場合のみ登録される)
IF NOT EXISTS (SELECT 1 FROM dbo.Orders)
BEGIN
    INSERT INTO dbo.Orders (CustomerId, OrderDate, ProductName, Quantity, UnitPrice, Note)
    SELECT c.CustomerId, v.OrderDate, v.ProductName, v.Quantity, v.UnitPrice, v.Note
    FROM (VALUES
        (N'山田 太郎', '2026-10-01', N'ノートPC', 1, 120000, N'サンプル'),
        (N'山田 太郎', '2026-10-05', N'マウス',   2,   3000, NULL),
        (N'佐藤 花子', '2026-10-10', N'モニター', 1,  30000, NULL)
    ) AS v (CustomerName, OrderDate, ProductName, Quantity, UnitPrice, Note)
    INNER JOIN dbo.Customers AS c ON c.Name = v.CustomerName;
END
GO
