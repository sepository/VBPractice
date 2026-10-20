Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Logging

Public Class AppDbContext
    Inherits DbContext

    ''' <summary>
    ''' データベースの接続文字列
    ''' </summary>
    Private ReadOnly _connectionString As String

    ''' <summary>
    ''' コンストラクタ
    ''' </summary>
    ''' <param name="connectionString">
    ''' データベースの接続文字列
    ''' </param>
    Public Sub New(connectionString As String)
        _connectionString = connectionString
    End Sub

#Region "DbSet"

    ''' <summary>
    ''' 顧客情報
    ''' </summary>
    Public Property Customers As DbSet(Of Customer)
    ''' <summary>
    ''' 注文情報
    ''' </summary>
    Public Property Orders As DbSet(Of CustomerOrder)
    ''' <summary>
    ''' 注文明細
    ''' </summary>
    Public Property OrderItems As DbSet(Of OrderItem)
    ''' <summary>
    ''' 商品マスタ
    ''' </summary>
    Public Property Products As DbSet(Of Product)

#End Region


    Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
        optionsBuilder.UseSqlServer(_connectionString)

#If DEBUG Then
        optionsBuilder.LogTo(Sub(message) Debug.WriteLine(message), LogLevel.Information)
#Else
        optionsBuilder.LogTo(Sub(message) AppLog.Warn("EF Core: " & message), LogLevel.Warning)
#End If

    End Sub

    Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
        ' 顧客
        modelBuilder.Entity(Of Customer)(
            Sub(e)
                e.ToTable("Customers")
                e.HasKey(Function(c) c.CustomerId)
                e.Property(Function(c) c.Name).IsRequired.HasMaxLength(100)
                e.Property(Function(c) c.Kana).HasMaxLength(100)
                e.Property(Function(c) c.Email).HasMaxLength(254)
                e.Property(Function(c) c.Phone).HasMaxLength(20)
                e.Property(Function(c) c.Address).HasMaxLength(200)
                e.Property(Function(c) c.Memo).HasMaxLength(500)
                e.Property(Function(c) c.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("SYSDATETIME()")
                e.Property(Function(c) c.UpdatedAt).HasColumnType("datetime2").HasDefaultValueSql("SYSDATETIME()")
                e.Ignore(Function(c) c.OrderCount)
                e.Ignore(Function(c) c.TotalAmount)
            End Sub)

        ' 注文
        modelBuilder.Entity(Of CustomerOrder)(
            Sub(e)
                e.ToTable("Orders")
                e.HasKey(Function(o) o.OrderId)
                e.Property(Function(o) o.OrderDate).HasColumnType("date")
                e.Property(Function(o) o.Note).HasMaxLength(500)
                e.Property(Function(o) o.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("SYSDATETIME()")
                e.Property(Function(o) o.UpdatedAt).HasColumnType("datetime2").HasDefaultValueSql("SYSDATETIME()")
                e.Ignore(Function(o) o.Amount)
                e.Ignore(Function(o) o.ItemCount)
                e.Ignore(Function(o) o.Summary)

                ' 顧客 1 : 注文 多
                ' Restrict => 注文があるなら、顧客は削除できない
                e.HasOne(Function(o) o.Customer) _
                    .WithMany(Function(c) c.Orders) _
                    .HasForeignKey(Function(o) o.CustomerId) _
                    .OnDelete(DeleteBehavior.Restrict)
            End Sub)

        ' 注文明細
        modelBuilder.Entity(Of OrderItem)(
            Sub(e)
                e.ToTable("OrderItems")
                e.HasKey(Function(i) i.OrderItemId)
                e.Property(Function(i) i.ProductName).IsRequired().HasMaxLength(100)
                e.Property(Function(i) i.UnitPrice).HasColumnType("decimal(18,2)")
                e.Ignore(Function(i) i.Amount)

                ' 注文 1 : 明細 多
                ' Cascade => 注文を削除したら、明細も削除
                e.HasOne(Function(i) i.Order) _
                    .WithMany(Function(o) o.Items) _
                    .HasForeignKey(Function(i) i.OrderId) _
                    .OnDelete(DeleteBehavior.Cascade)

                ' 商品 1 : 明細 多
                ' Restrict => 注文明細があるなら、商品は削除できない。
                e.HasOne(Function(i) i.Product) _
                    .WithMany() _
                    .HasForeignKey(Function(i) i.ProductId) _
                    .OnDelete(DeleteBehavior.Restrict)
            End Sub)

        ' 商品マスタ
        modelBuilder.Entity(Of Product)(
            Sub(e)
                e.ToTable("Products")
                e.HasKey(Function(p) p.ProductId)
                e.Property(Function(p) p.ProductCode).IsRequired().HasMaxLength(20)
                e.HasIndex(Function(p) p.ProductCode).IsUnique()
                e.Property(Function(p) p.Name).IsRequired().HasMaxLength(100)
                e.Property(Function(p) p.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("SYSDATETIME()")
                e.Property(Function(p) p.UpdatedAt).HasColumnType("datetime2").HasDefaultValueSql("SYSDATETIME()")
                e.Ignore(Function(p) p.DisplayName)
            End Sub)
    End Sub

End Class
