Imports System
Imports System.Globalization
Imports System.Numerics
Imports System.Security.Cryptography
Imports System.Text

Module Program
    Sub Main(args As String())
        Dim inputText As String = ""
        Dim userInput As String
        Dim isMatched As Boolean = False
        Dim baseString As String = ""

        Console.WriteLine("ターゲットとなる文字列を入力してください: ")

        userInput = Console.ReadLine()

        Console.WriteLine("探索中")

        While Not isMatched
            baseString = GenerateRandomString()
            inputText = EncodeHashToBase36(GetSha256Hash(baseString))

            If inputText.StartsWith(userInput) Then
                isMatched = True
            End If
        End While
        Console.WriteLine("指定した文字列：" & userInput)
        Console.WriteLine("ハッシュ元文字列：" & baseString)
        Console.WriteLine("出力されたハッシュ値：" & inputText.Substring(0, Math.Min(userInput.Length, inputText.Length)))

    End Sub

    ''' <summary>
    ''' ランダム文字列を生成する関数
    ''' </summary>
    ''' <returns></returns>
    Public Function GenerateRandomString() As String
        Const chars As String = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"
        Dim rand As New Random()
        Dim length As Integer = rand.Next(1, 11)
        Dim result As New StringBuilder(length)

        For i As Integer = 0 To length - 1
            result.Append(chars(rand.Next(chars.Length)))
        Next

        Return result.ToString()
    End Function

    ''' <summary>
    ''' ハッシュ値を取得する関数
    ''' </summary>
    ''' <param name="inputString"></param>
    ''' <returns></returns>
    Public Function GetSha256Hash(ByVal inputString As String) As String
        ' SHA256オブジェクトを作成（Usingを使うことでメモリ解放を安全に行う）
        Using sha256 As SHA256 = SHA256.Create()

            ' 文字列をUTF-8のバイト配列に変換
            Dim inputBytes As Byte() = Encoding.UTF8.GetBytes(inputString)

            ' ハッシュ値を計算
            Dim hashBytes As Byte() = sha256.ComputeHash(inputBytes)

            ' バイト配列を16進数の文字列に変換
            Dim sb As New StringBuilder()
            For Each b As Byte In hashBytes
                sb.Append(b.ToString("x2")) ' "x2" で2桁の小文字16進数に変換
            Next

            Return sb.ToString()
        End Using
    End Function

    ''' <summary>
    ''' Base36に変換する関数
    ''' </summary>
    ''' <param name="hexHashString"></param>
    ''' <returns></returns>
    Public Function EncodeHashToBase36(ByVal hexHashString As String) As String
        ' Base36で使用する36文字の文字セット
        Const Base36Chars As String = "0123456789abcdefghijklmnopqrstuvwxyz"

        ' 1. 16進数のハッシュ文字列を「巨大な整数 (BigInteger)」に変換する
        Dim bigNum As BigInteger = BigInteger.Parse("0" & hexHashString, NumberStyles.HexNumber)

        If bigNum = 0 Then Return "0"

        ' 2. BigIntegerをBase36に変換（ここでは \ ではなく / を使用する）
        Dim result As New StringBuilder()
        While bigNum > 0
            Dim remainder As Integer = CInt(bigNum Mod 36)
            result.Insert(0, Base36Chars(remainder))

            ' BigIntegerの場合は / で整数除算になる
            bigNum = bigNum / 36
        End While

        Return result.ToString()
    End Function

End Module