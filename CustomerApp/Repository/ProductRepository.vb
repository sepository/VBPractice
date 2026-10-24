Imports System.Threading
Imports Microsoft.EntityFrameworkCore

Public Class ProductRepository

    Private ReadOnly _connectionString As String

    Public Sub New(connectionString As String)
        _connectionString = connectionString
    End Sub

    ''' <summary>
    ''' 全件取得
    ''' </summary>
    Public Async Function GetAllAsync() As Task(Of List(Of Product))
        Using db As New AppDbContext(_connectionString)
            Return Await db.Products.AsNoTracking().
                OrderBy(Function(p) p.ProductCode).
                ToListAsync()
        End Using
    End Function

    ''' <summary>
    ''' 販売中の商品のみ取得
    ''' </summary>
    Public Async Function GetActiveAsync() As Task(Of List(Of Product))
        Using db As New AppDbContext(_connectionString)
            Return Await db.Products.AsNoTracking().
                Where(Function(p) p.IsActive).
                OrderBy(Function(p) p.ProductCode).
                ToListAsync()
        End Using
    End Function

    ''' <summary>
    ''' 商品コードを全件取得
    ''' </summary>
    ''' <param name="cancellationToken"></param>
    ''' <returns></returns>
    Public Async Function GetCodesAsync(Optional cancellationToken As CancellationToken = Nothing) As Task(Of List(Of String))
        Using db As New AppDbContext(_connectionString)
            Return Await db.Products.AsNoTracking().
                Select(Function(p) p.ProductCode).
                ToListAsync(cancellationToken)
        End Using
    End Function

    ''' <summary>
    ''' 新規
    ''' </summary>
    Public Async Function InsertAsync(product As Product) As Task(Of Integer)
        Using db As New AppDbContext(_connectionString)
            Dim entity As New Product With {
                .ProductCode = product.ProductCode.Trim(),
                .Name = product.Name.Trim(),
                .UnitPrice = product.UnitPrice,
                .IsActive = product.IsActive
            }

            db.Products.Add(entity)
            Await db.SaveChangesAsync()

            Return entity.ProductId
        End Using
    End Function

    ''' <summary>
    ''' 更新
    ''' </summary>
    Public Async Function UpdateAsync(product As Product) As Task(Of Boolean)
        Using db As New AppDbContext(_connectionString)
            Dim productId As Integer = product.ProductId

            Dim entity As Product = Await db.Products.FirstOrDefaultAsync(Function(p) p.ProductId = productId)
            If entity Is Nothing Then
                Return False
            End If

            entity.ProductCode = product.ProductCode.Trim()
            entity.Name = product.Name.Trim()
            entity.UnitPrice = product.UnitPrice
            entity.IsActive = product.IsActive
            entity.UpdatedAt = DateTime.Now

            Await db.SaveChangesAsync()
            Return True
        End Using
    End Function

    ''' <summary>
    ''' 削除
    ''' </summary>
    Public Async Function DeleteAsync(productId As Integer) As Task(Of Boolean)
        Using db As New AppDbContext(_connectionString)
            Dim entity As Product = Await db.Products.FirstOrDefaultAsync(Function(p) p.ProductId = productId)
            If entity Is Nothing Then
                Return False
            End If

            db.Products.Remove(entity)
            Await db.SaveChangesAsync()
            Return True
        End Using
    End Function

    ''' <summary>
    ''' CSVインポート
    ''' </summary>
    Public Async Function ImportAsync(products As IEnumerable(Of Product)) As Task(Of Integer)
        Using db As New AppDbContext(_connectionString)
            Dim entities As New List(Of Product)()

            For Each p As Product In products
                entities.Add(New Product With {
                    .ProductCode = p.ProductCode.Trim(),
                    .Name = p.Name.Trim(),
                    .UnitPrice = p.UnitPrice,
                    .IsActive = p.IsActive
                })
            Next

            db.Products.AddRange(entities)
            Await db.SaveChangesAsync()

            Return entities.Count
        End Using
    End Function

End Class
