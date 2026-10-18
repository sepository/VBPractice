''' <summary>
''' エクスポートするデータを、「見出し＋行」の表として持つだけのクラス。
''' </summary>
Public Class ExportTable

    ''' <summary>
    ''' Excelのシート名
    ''' </summary>
    Public Property SheetName As String = "Sheet1"

    ''' <summary>
    ''' 見出し
    ''' </summary>
    Public ReadOnly Property Headers As New List(Of String)()

    ''' <summary>
    ''' データ行
    ''' </summary>
    Public ReadOnly Property Rows As New List(Of Object())()

    ''' <summary>
    ''' Excelの列ごとの書式
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property ColumnFormats As New List(Of String)

End Class
