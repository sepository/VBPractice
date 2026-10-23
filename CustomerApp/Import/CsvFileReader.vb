Imports System.IO
Imports System.Text

''' <summary>
''' CSVファイルを読み、文字コードを自動で判定して文字列にする。
''' </summary>
Public Module CsvFileReader

    ''' <summary>
    ''' ファイルを読み、中身と判定した文字コードの名前を返す。
    ''' </summary>
    Public Function ReadFile(path As String, ByRef encodingName As String) As String
        Return Decode(File.ReadAllBytes(path), encodingName)
    End Function

    ''' <summary>
    ''' バイト列を、文字コードを判定して文字列にする。
    ''' </summary>
    Public Function Decode(bytes As Byte(), ByRef encodingName As String) As String
        If bytes.Length >= 3 AndAlso bytes(0) = &HEF AndAlso bytes(1) = &HBB AndAlso bytes(2) = &HBF Then
            encodingName = "UTF-8 (BOMあり)"
            Return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3)
        End If

        If bytes.Length >= 2 AndAlso bytes(0) = &HFF AndAlso bytes(1) = &HFE Then
            encodingName = "UTF-16 LE"
            Return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2)
        End If

        If bytes.Length >= 2 AndAlso bytes(0) = &HFE AndAlso bytes(1) = &HFF Then
            encodingName = "UTF-16 BE"
            Return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2)
        End If

        Try
            ' New UTF8Encoding(False, True) の2つ目の True = 不正なバイト列があれば、例外にする(黙って「?」に置き換えない)
            Dim strictUTF8 As New UTF8Encoding(False, True)
            Dim text As String = strictUTF8.GetString(bytes)
            encodingName = "UTF-8"
            Return text
        Catch ex As DecoderFallbackException
            ' UTF-8として読めない => Shift_JISとみなす
        End Try

        ' .NET(Core 以降)で Shift_JIS などの古い文字コードを使うには、最初に、その提供元を登録する必要がある。
        ' (標準で入っている部品だが、登録するまでは使えない)
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)

        encodingName = "Shift_JIS (CP932)"
        Return Encoding.GetEncoding(932).GetString(bytes)
    End Function

End Module
