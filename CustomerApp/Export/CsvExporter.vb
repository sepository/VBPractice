Imports System.Globalization
Imports System.IO
Imports System.Text
Imports System.Threading

''' <summary>
''' 表をCSVファイルに書き出す。
''' </summary>
Public Module CsvExporter

    ''' <summary>
    ''' CSV書き出し
    ''' </summary>
    Public Sub Save(table As ExportTable, path As String, Optional cancellationToken As CancellationToken = Nothing)
        ' UTF8Encoding(True) = BOMあり
        Using writer As New StreamWriter(path, False, New UTF8Encoding(True))
            ' RFC 4180 は改行をCRLFと定めている。
            writer.NewLine = vbCrLf

            writer.WriteLine(String.Join(",", table.Headers.Select(Function(h) Escape(h, False))))

            For Each row As Object() In table.Rows
                ' 同期の処理は、自分で中止されたかを確かめる。
                ' 確かめれなければ、中止を要求しても最後まで止めない。
                cancellationToken.ThrowIfCancellationRequested()

                writer.WriteLine(String.Join(",", row.Select(Function(v) FormatValue(v))))
            Next
        End Using
    End Sub

    ''' <summary>
    ''' CSV用の書式に変換する
    ''' </summary>
    Private Function FormatValue(value As Object) As String
        If value Is Nothing OrElse value Is DBNull.Value Then
            Return String.Empty
        End If

        If TypeOf value Is String Then
            Return Escape(DirectCast(value, String), True)
        End If

        If TypeOf value Is DateTime Then
            Return DirectCast(value, DateTime).ToString("yyyy/MM/dd", CultureInfo.InvariantCulture)
        End If

        If TypeOf value Is Decimal Then
            Return DirectCast(value, Decimal).ToString("0.##", CultureInfo.InvariantCulture)
        End If

        ' 整数など。地域設定に左右されないようInvariantCultureを使う
        Return Escape(Convert.ToString(value, CultureInfo.InvariantCulture), False)
    End Function

    ''' <summary>
    ''' 必要なら、""で囲み、"を""に置き換える。
    ''' guardFormulaがTrueなら、数式として実行されるのを防ぐ。
    ''' </summary>
    ''' <param name="text"></param>
    ''' <param name="guardFormula"></param>
    ''' <returns></returns>
    Private Function Escape(text As String, guardFormula As Boolean) As String
        If String.IsNullOrEmpty(text) Then
            Return String.Empty
        End If

        If guardFormula Then
            Select Case text(0)
                Case "="c, "+"c, "-"c, "@"c, ChrW(9), ChrW(13)
                    text = "'" & text
            End Select
        End If

        Dim needsQuote As Boolean = text.IndexOfAny({","c, """"c, ChrW(13), ChrW(10)}) >= 0
        If needsQuote Then
            Return """" & text.Replace("""", """""") & """"
        End If

        Return text
    End Function

End Module
