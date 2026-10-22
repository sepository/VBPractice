Imports System.Security.Cryptography
Imports System.Text

''' <summary>
''' SQLスクリプトを、「GO」の行でバッチに分ける。
''' </summary>
Public Module SqlBatchSplitter

    Public Function Split(script As String) As List(Of String)
        Dim batches As New List(Of String)()
        Dim current As New StringBuilder()

        For Each line As String In script.Split({vbCrLf, vbLf}, StringSplitOptions.None)
            If line.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase) Then
                AddIfNotBlank(batches, current)
                current.Clear()
            Else
                current.AppendLine(line)
            End If
        Next

        AddIfNotBlank(batches, current)

        Return batches
    End Function

    Private Sub AddIfNotBlank(batches As List(Of String), current As StringBuilder)
        Dim text As String = current.ToString()
        If Not String.IsNullOrWhiteSpace(text) Then
            batches.Add(text.Trim())
        End If
    End Sub

End Module

''' <summary>
''' スクリプトのチェックサムを求める。
''' 
''' 適用時にチェックサムを残しておき、次回起動時にチェックサムを比較する。
''' 違えば、適用済のスクリプトが書き換えられたと分かる。
''' 
''' 改行コードやBOMの違いでチェックサムが変わらないようにする。
''' エディタやGitの設定でCRLF, LFが変わったり、先頭にBOMが付いても中身が同じなら同じチェックサムになる。
''' </summary>
Public Module MigrationChecksum

    Public Function Compute(script As String) As String
        Dim normalized As String = script.TrimStart(ChrW(&HFEFF)) _
                                         .Replace(vbCrLf, vbLf) _
                                         .Replace(vbCr, vbLf) _
                                         .Trim()

        Dim hash As Byte() = SHA256.HashData(Encoding.UTF8.GetBytes(normalized))

        Return Convert.ToHexString(hash)
    End Function

End Module