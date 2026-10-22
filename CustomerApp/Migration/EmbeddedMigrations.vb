Imports System.IO
Imports System.Reflection
Imports System.Text
Imports System.Text.RegularExpressions

''' <summary>
''' 埋め込みリソースからマイグレーションスクリプトを読み込む。
''' </summary>
Public Module EmbeddedMigrations

    Private Const ResourcePrefix As String = "Migrations."

    Private ReadOnly NamePattern As New Regex("^V(?<version>\d+)__(?<name>.+)\.sql$", RegexOptions.IgnoreCase)

    ''' <summary>
    ''' このアプリ自信に埋め込まれたスクリプトを番号順に読み込む。
    ''' </summary>
    Public Function Load() As List(Of MigrationScript)
        Return Load(GetType(EmbeddedMigrations).Assembly)
    End Function

    Public Function Load(sourceAssembly As Assembly) As List(Of MigrationScript)
        Dim scripts As New List(Of MigrationScript)()

        For Each resourceName As String In sourceAssembly.GetManifestResourceNames()
            If Not resourceName.StartsWith(ResourcePrefix, StringComparison.Ordinal) Then
                Continue For
            End If

            Dim fileName As String = resourceName.Substring(ResourcePrefix.Length)

            Dim match As Match = NamePattern.Match(fileName)
            If Not match.Success Then
                Throw New MigrationException($"マイグレーションのファイル名が規則に合っていません。: {fileName}(正しい例: V001__create_customer.sql)")
            End If

            Using stream As Stream = sourceAssembly.GetManifestResourceStream(resourceName)
                ' 3つ目の引数 True = ファイル先頭の BOM(UTF-8 などの目印)を見て、文字コードを判別する
                Using reader As New StreamReader(stream, Encoding.UTF8, True)
                    Dim version As Integer = Integer.Parse(match.Groups("version").Value)
                    Dim name As String = fileName.Substring(0, fileName.Length - ".sql".Length)

                    scripts.Add(New MigrationScript(version, name, reader.ReadToEnd()))
                End Using
            End Using
        Next

        If scripts.Count = 0 Then
            Throw New MigrationException("マイグレーションスクリプトが、アプリに埋め込まれていません。" &
                                         "Migrationsフォルダの.sqlファイルが、「埋め込みリソース」として登録されているか確認してください。")
        End If

        Return scripts.OrderBy(Function(s) s.Version).ToList()
    End Function

End Module
