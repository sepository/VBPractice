-- V005: 顧客のメールアドレスの重複を、データベースで禁止する
--
-- ■ なぜ必要か
--   CSV 取り込みや画面の保存で、アプリ側が「同じメールアドレスが無いか」を事前に調べても、
--   調べてから登録するまでの間に、別の人が同じメールアドレスを登録すると、重複が入ってしまう。
--   確実に防げるのは、データベース自身の一意インデックスだけ。
--
-- ■ 一意インデックスの条件
--   ・メールアドレスが空(NULL または空文字)の顧客は、何件あってもよい。
--     (SQL Server の通常の一意インデックスは、NULL を「1件だけ」しか許さないため、条件付きにしている)
--   ・大文字・小文字は、データベースの照合順序に従う。既定の照合順序では、区別しない(A@x.com と a@x.com は同じ)。
--
-- ■ 既に重複がある場合
--   データベースの中身を、勝手に消したり書き換えたりはしない。
--   重複を見つけたら、このスクリプトは、エラーで止まる(変更はすべて取り消され、アプリは起動しない)。
--   エラーのメッセージにある SQL で重複を確認し、不要な顧客の削除、またはメールアドレスの修正をしてから、もう一度起動する。

-- ① 事前チェック:既存データに重複があれば、分かりやすいメッセージで止める
IF EXISTS (SELECT 1
           FROM dbo.Customers
           WHERE Email IS NOT NULL AND Email <> N''
           GROUP BY Email
           HAVING COUNT(*) > 1)
BEGIN
    THROW 50005,
          N'顧客のメールアドレスに重複があるため、V005(メールアドレスの一意インデックス)を作成できません。次の SQL で重複を確認し、不要な顧客の削除またはメールアドレスの修正をしてから、もう一度起動してください。  SELECT Email, COUNT(*) AS Cnt FROM dbo.Customers WHERE LEN(Email) > 0 GROUP BY Email HAVING COUNT(*) > 1',
          1;
END
GO

-- ② 一意インデックスの作成(既にあれば何もしない)
IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = N'UX_Customers_Email'
                 AND object_id = OBJECT_ID(N'dbo.Customers'))
BEGIN
    CREATE UNIQUE INDEX UX_Customers_Email
        ON dbo.Customers (Email)
        WHERE Email IS NOT NULL AND Email <> N'';
END
GO
