-- 開発・動作確認用のサンプルデータ。
--
-- ■ マイグレーション(Migrations フォルダ)とは、別物
--   マイグレーションは、データベースの「構造」を作る。アプリが自動で適用する。
--   このスクリプトは、動作確認のための「データ」を入れる。開発者が、必要なときに手で実行する。
--   本番のデータベースには、実行しないこと。
--
-- 実行例(接続先のデータベースを -d で指定する):
--   sqlcmd -S localhost\SQLEXPRESS -d CustomerDb -E -C -i dev_sample_data.sql
--
-- 何度実行しても、重複して登録されない(各テーブルが空のときだけ登録する)。

IF NOT EXISTS (SELECT 1 FROM dbo.Customers)
BEGIN
    INSERT INTO dbo.Customers (Name, Kana, Email, Phone, Address, Memo)
    VALUES
        (N'山田 太郎', N'ヤマダ タロウ', N'yamada@example.com', N'090-0000-0001', N'愛知県名古屋市', N'サンプル1'),
        (N'佐藤 花子', N'サトウ ハナコ', N'sato@example.com',   N'090-0000-0002', N'東京都千代田区', N'サンプル2');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Products)
BEGIN
    INSERT INTO dbo.Products (ProductCode, Name, UnitPrice)
    VALUES (N'P0001', N'ノートPC',   120000),
           (N'P0002', N'マウス',       3000),
           (N'P0003', N'モニター',    30000),
           (N'P0004', N'キーボード',   8000);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Orders)
BEGIN
    DECLARE @OrderId INT;

    -- 山田さんの注文:ノートPC + キーボード(複数明細のサンプル)
    INSERT INTO dbo.Orders (CustomerId, OrderDate, Note)
    SELECT CustomerId, '2026-10-01', N'サンプル' FROM dbo.Customers WHERE Name = N'山田 太郎';
    SET @OrderId = SCOPE_IDENTITY();

    INSERT INTO dbo.OrderItems (OrderId, ProductId, ProductName, Quantity, UnitPrice)
    SELECT @OrderId, ProductId, Name, 1, UnitPrice FROM dbo.Products WHERE ProductCode IN (N'P0001', N'P0004');

    -- 山田さんの2件目:マウス2個
    INSERT INTO dbo.Orders (CustomerId, OrderDate)
    SELECT CustomerId, '2026-10-05' FROM dbo.Customers WHERE Name = N'山田 太郎';
    SET @OrderId = SCOPE_IDENTITY();

    INSERT INTO dbo.OrderItems (OrderId, ProductId, ProductName, Quantity, UnitPrice)
    SELECT @OrderId, ProductId, Name, 2, UnitPrice FROM dbo.Products WHERE ProductCode = N'P0002';

    -- 佐藤さんの注文:モニター
    INSERT INTO dbo.Orders (CustomerId, OrderDate)
    SELECT CustomerId, '2026-10-10' FROM dbo.Customers WHERE Name = N'佐藤 花子';
    SET @OrderId = SCOPE_IDENTITY();

    INSERT INTO dbo.OrderItems (OrderId, ProductId, ProductName, Quantity, UnitPrice)
    SELECT @OrderId, ProductId, Name, 1, UnitPrice FROM dbo.Products WHERE ProductCode = N'P0003';
END
GO
