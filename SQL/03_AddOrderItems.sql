-- 顧客管理アプリ:注文明細テーブル(OrderItems)の追加
--
-- 「注文(Orders)1件に、商品の行(明細)が複数ある」構造に変更します。
--   変更前: Orders に ProductName / Quantity / UnitPrice がある(1注文 = 1商品)
--   変更後: Orders は注文の見出し(日付・メモ)だけを持ち、商品は OrderItems に持つ(1注文 = 複数商品)
--
-- 【注意】このスクリプトは Orders から3つの列を削除します。実行前にバックアップを推奨します。
--         既存の注文は、移行処理によって「明細1行の注文」として OrderItems に引き継がれます。
-- 実行例: sqlcmd -S localhost\SQLEXPRESS -E -C -i 03_AddOrderItems.sql

USE CustomerDb;
GO

-- ① 明細テーブルの作成
IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderItems
    (
        OrderItemId INT IDENTITY(1, 1) NOT NULL
            CONSTRAINT PK_OrderItems PRIMARY KEY,

        -- 外部キー:注文が削除されたら、その明細も一緒に削除する(ON DELETE CASCADE)。
        -- 「明細は注文の一部」なので連鎖削除が自然。
        -- (Orders → Customers は、注文が残る顧客を消せないよう連鎖削除なしにしている点と対比)
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

-- ② 既存の注文の商品情報を明細へ移し、Orders から列を削除する。
--    途中で失敗したら全部元に戻せるよう、1つのトランザクションにまとめている。
--    (列が既に無ければ何もしない = 再実行しても安全)
--    存在しない列を参照する文は、バッチ全体のコンパイルで失敗するため、動的SQL(EXEC)で実行する。
IF COL_LENGTH(N'dbo.Orders', N'ProductName') IS NOT NULL
BEGIN
    EXEC (N'
        SET XACT_ABORT ON;      -- エラーが起きたら、自動でトランザクション全体をロールバックする
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

-- ③ 複数明細のサンプル:サンプル注文(メモが「サンプル」の注文)にもう1行追加する
INSERT INTO dbo.OrderItems (OrderId, ProductName, Quantity, UnitPrice)
SELECT o.OrderId, N'キーボード', 1, 8000
FROM dbo.Orders AS o
WHERE o.Note = N'サンプル'
  AND NOT EXISTS (SELECT 1 FROM dbo.OrderItems AS i
                  WHERE i.OrderId = o.OrderId AND i.ProductName = N'キーボード');
GO

-- JOIN の練習用クエリ(SSMS で試してみてください)
-- 注文ごとの明細数と合計金額:
-- SELECT o.OrderId, o.OrderDate, COUNT(i.OrderItemId) AS ItemCount,
--        ISNULL(SUM(i.Quantity * i.UnitPrice), 0) AS Amount
-- FROM dbo.Orders AS o
-- LEFT JOIN dbo.OrderItems AS i ON i.OrderId = o.OrderId
-- GROUP BY o.OrderId, o.OrderDate;
