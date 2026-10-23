Imports System.Text

''' <summary>
''' CSVの1行(レコード)
''' </summary>
Public Class CsvRecord

    Public ReadOnly Property LineNumber As Integer
    Public ReadOnly Property Fields As String()

    Public Sub New(lineNumber As Integer, fields As String())
        Me.LineNumber = lineNumber
        Me.Fields = fields
    End Sub

End Class

''' <summary>
''' CSVの書式が壊れている事を表す例外
''' </summary>
Public Class CsvFormatException
    Inherits Exception

    Public ReadOnly Property LineNumber As Integer

    Public Sub New(lineNumber As Integer, message As String)
        MyBase.New(message)
        Me.LineNumber = lineNumber
    End Sub

End Class

Public NotInheritable Class CsvParser

    Private Sub New()
    End Sub

    Public Shared Function Parse(text As String) As List(Of CsvRecord)
        If text Is Nothing Then
            Return New List(Of CsvRecord)()
        End If

        Return New CsvParser.Reader(text).ReadAll()
    End Function

    ' 読込の状態をまとめた内部クラス
    Private NotInheritable Class Reader

        Private ReadOnly _text As String
        Private ReadOnly _records As New List(Of CsvRecord)()
        Private ReadOnly _fields As New List(Of String)()
        Private ReadOnly _current As New StringBuilder()

        Private _inQuotes As Boolean = False            ' 今、引用符の中か
        Private _fieldWasQuoted As Boolean = False      ' この項目は引用符で始まったか
        Private _line As Integer = 1                    ' 今読んでいる物理的な行
        Private _recordStartLine As Integer = 1         ' このレコードが始まった行

        Public Sub New(text As String)
            ' 先頭のBOM(U+FEFF)はデータではないので捨てる
            If text.Length > 0 AndAlso text(0) = ChrW(&HFEFF) Then
                text = text.Substring(1)
            End If

            _text = text
        End Sub

        Public Function ReadAll() As List(Of CsvRecord)
            Dim i As Integer = 0

            While i < _text.Length
                Dim c As Char = _text(i)

                If _inQuotes Then
                    If c = """"c Then
                        If i + 1 < _text.Length AndAlso _text(i + 1) = """"c Then
                            _current.Append(""""c)      ' ""は、" 1文字の意味
                            i += 1
                        Else
                            _inQuotes = False           ' 引用符を閉じた
                        End If
                    Else
                        If c = ChrW(10) Then
                            _line += 1
                        End If
                        _current.Append(c)              ' 引用符の中の改行、カンマは値の一部
                    End If
                Else
                    Select Case c
                        Case """"c
                            ' 引用符は項目の先頭でだけ開始の意味を持つ
                            If _current.Length = 0 AndAlso Not _fieldWasQuoted Then
                                _inQuotes = True
                                _fieldWasQuoted = True
                            Else
                                Throw New CsvFormatException(_line, "引用符(" & """" & ")の位置が正しくありません。引用符を含む項目は全体を引用符で囲み、中の引用符は2つ重ねてください。")
                            End If

                        Case ","c
                            EndField()

                        Case ChrW(13)
                            If i + 1 < _text.Length AndAlso _text(i + 1) = ChrW(10) Then
                                i += 1
                            End If
                            EndRecord()

                        Case ChrW(10)
                            EndRecord()

                        Case Else
                            If _fieldWasQuoted Then
                                Throw New CsvFormatException(_line, "引用符で閉じた項目の後に余計な文字があります。")
                            End If
                            _current.Append(c)
                    End Select
                End If

                i += 1
            End While

            If _inQuotes Then
                Throw New CsvFormatException(_recordStartLine, "引用符(" & """" & ")が閉じられていません。")
            End If

            ' 最後の行に改行がないときの最後のレコード
            If _current.Length > 0 OrElse _fields.Count > 0 OrElse _fieldWasQuoted Then
                FinishRecord()
            End If

            Return _records
        End Function

        Private Sub EndField()
            _fields.Add(_current.ToString())
            _current.Clear()
            _fieldWasQuoted = False
        End Sub

        Private Sub EndRecord()
            FinishRecord()
            _line += 1
            _recordStartLine = _line
        End Sub

        Private Sub FinishRecord()
            EndField()

            ' 空行は読み飛ばす。(項目が1つで、中身が空)
            Dim isBlankLine As Boolean = (_fields.Count = 1 AndAlso _fields(0).Length = 0)
            If Not isBlankLine Then
                _records.Add(New CsvRecord(_recordStartLine, _fields.ToArray()))
            End If

            _fields.Clear()
        End Sub

    End Class

End Class