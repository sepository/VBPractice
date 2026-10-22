-- V004: 商品マスタ(Products)の追加
--
-- 「明細が商品名を直接持つ」構造から、「明細が商品マスタを参照する」構造に変更する。
--   OrderItems.ProductId を追加し、Products(ProductId) への外部キーにする。
--   OrderItems の ProductName / UnitPrice は、あえて残す。
--     → 「注文した時点の商品名・単価」の記録(スナップショット)として使うため。
--       マスタの価格を後で変更しても、過去の注文の金額は変わらない。
--
-- ※ GO で区切って複数のバッチに分けているのは、前のバッチで追加した列を、
--   次のバッチで参照するため(同じバッチ内だと、列がまだ無いものとして解析されエラーになる)。

-- ① 商品マスタの作成
IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products
    (
        ProductId   INT IDENTITY(1, 1) NOT NULL
            CONSTRAINT PK_Products PRIMARY KEY,

        -- 商品コード:業務上の識別子。重複を許さない(UNIQUE 制約)
        ProductCode NVARCHAR(20)   NOT NULL
            CONSTRAINT UQ_Products_ProductCode UNIQUE,

        Name        NVARCHAR(100)  NOT NULL,
        UnitPrice   DECIMAL(18, 2) NOT NULL
            CONSTRAINT CK_Products_UnitPrice CHECK (UnitPrice >= 0),

        -- 販売中かどうか。注文で使われた商品は削除できないため、使わなくなったら 0(販売停止)にする
        IsActive    BIT            NOT NULL
            CONSTRAINT DF_Products_IsActive DEFAULT (1),

        CreatedAt   DATETIME2      NOT NULL
            CONSTRAINT DF_Products_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt   DATETIME2      NOT NULL
            CONSTRAINT DF_Products_UpdatedAt DEFAULT SYSDATETIME()
    );
END
GO

-- ② 明細に ProductId 列を追加(既存データがあるので、まずは NULL を許可)
IF COL_LENGTH(N'dbo.OrderItems', N'ProductId') IS NULL
BEGIN
    ALTER TABLE dbo.OrderItems ADD ProductId INT NULL;
END
GO

-- ③ 既存の明細を、商品マスタに紐づける(1つのトランザクション)
--    マスタに無い商品名は、新しい商品として自動登録する(単価は、その商品名の最新の明細の単価)。
IF EXISTS (SELECT 1 FROM dbo.OrderItems WHERE ProductId IS NULL)
BEGIN
    SET XACT_ABORT ON;      -- エラーが起きたら、トランザクション全体をロールバックする
    BEGIN TRANSACTION;

    INSERT INTO dbo.Products (ProductCode, Name, UnitPrice)
    SELECT
        N'P' + RIGHT(N'0000' + CAST(ROW_NUMBER() OVER (ORDER BY n.ProductName)
                                    + (SELECT COUNT(*) FROM dbo.Products) AS NVARCHAR(10)), 4),
        n.ProductName,
        (SELECT TOP (1) x.UnitPrice
         FROM dbo.OrderItems AS x
         WHERE x.ProductName = n.ProductName
         ORDER BY x.OrderItemId DESC)
    FROM (SELECT DISTINCT ProductName FROM dbo.OrderItems WHERE ProductId IS NULL) AS n
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Products AS p WHERE p.Name = n.ProductName);

    UPDATE i
    SET i.ProductId = p.ProductId
    FROM dbo.OrderItems AS i
    INNER JOIN dbo.Products AS p ON p.Name = i.ProductName
    WHERE i.ProductId IS NULL;

    COMMIT TRANSACTION;
END
GO

-- ④ 全明細が紐づいたので、ProductId を必須(NOT NULL)にする
IF NOT EXISTS (SELECT 1 FROM dbo.OrderItems WHERE ProductId IS NULL)
   AND COLUMNPROPERTY(OBJECT_ID(N'dbo.OrderItems'), N'ProductId', 'AllowsNull') = 1
BEGIN
    ALTER TABLE dbo.OrderItems ALTER COLUMN ProductId INT NOT NULL;
END
GO

-- ⑤ 外部キー:存在しない商品の明細は登録できない。
--    連鎖削除なし(NO ACTION)なので、注文で使われている商品は削除できない。
IF OBJECT_ID(N'dbo.FK_OrderItems_Products', N'F') IS NULL
BEGIN
    ALTER TABLE dbo.OrderItems
        ADD CONSTRAINT FK_OrderItems_Products
        FOREIGN KEY (ProductId) REFERENCES dbo.Products (ProductId);

    CREATE INDEX IX_OrderItems_ProductId ON dbo.OrderItems (ProductId);
END
GO
