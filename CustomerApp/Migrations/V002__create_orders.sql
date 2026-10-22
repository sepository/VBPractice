-- V002: 注文テーブル(Orders)
--
-- この時点の Orders は、「1注文 = 1商品」の構造(商品名・数量・単価を、注文自身が持つ)。
-- 次の V003 で、商品を明細テーブル(OrderItems)へ移し、これらの列を削除する。
-- (過去の構造を、そのままスクリプトとして残すことで、古いデータベースからでも、順に最新へ移行できる)

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
