-- V003: 注文明細テーブル(OrderItems)の追加
--
--   変更前: Orders に ProductName / Quantity / UnitPrice がある(1注文 = 1商品)
--   変更後: Orders は注文の見出し(日付・メモ)だけを持ち、商品は OrderItems に持つ(1注文 = 複数商品)
--
-- 既存の注文は、「明細1行の注文」として OrderItems に引き継がれる(データ移行)。

-- ① 明細テーブルの作成
IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderItems
    (
        OrderItemId INT IDENTITY(1, 1) NOT NULL
            CONSTRAINT PK_OrderItems PRIMARY KEY,

        -- 外部キー:注文が削除されたら、その明細も一緒に削除する(ON DELETE CASCADE)。
        -- 「明細は注文の一部」なので連鎖削除が自然。
        OrderId     INT            NOT NULL
            CONSTRAINT FK_OrderItems_Orders
            FOREIGN KEY REFERENCES dbo.Orders (OrderId) ON DELETE CASCADE,

        ProductName NVARCHAR(100)  NOT NULL,
        Quantity    INT            NOT NULL
            CONSTRAINT CK_OrderItems_Quantity CHECK (Quantity > 0),
        UnitPrice   DECIMAL(18, 2) NOT NULL
            CONSTRAINT CK_OrderItems_UnitPrice CHECK (UnitPrice >= 0)
    );

    CREATE INDEX IX_OrderItems_OrderId ON dbo.OrderItems (OrderId);
END
GO

-- ② 既存の注文の商品情報を明細へ移し、Orders から列を削除する(1つのトランザクション)。
--    (列が既に無ければ何もしない)
--    存在しない列を参照する文は、バッチ全体のコンパイルで失敗するため、動的SQL(EXEC)で実行する。
IF COL_LENGTH(N'dbo.Orders', N'ProductName') IS NOT NULL
BEGIN
    EXEC (N'
        SET XACT_ABORT ON;      -- エラーが起きたら、トランザクション全体をロールバックする
        BEGIN TRANSACTION;

        INSERT INTO dbo.OrderItems (OrderId, ProductName, Quantity, UnitPrice)
        SELECT OrderId, ProductName, Quantity, UnitPrice
        FROM dbo.Orders;

        ALTER TABLE dbo.Orders DROP CONSTRAINT CK_Orders_Quantity;
        ALTER TABLE dbo.Orders DROP CONSTRAINT CK_Orders_UnitPrice;
        ALTER TABLE dbo.Orders DROP COLUMN ProductName, Quantity, UnitPrice;

        COMMIT TRANSACTION;
    ');
END
GO
