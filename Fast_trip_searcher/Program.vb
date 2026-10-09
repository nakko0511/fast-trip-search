Imports System.Numerics
Imports System.Security.Cryptography
Imports System.Text

Module Program

    Private ReadOnly rand As New Random()
    Sub Main(args As String())
        Console.ForegroundColor = ConsoleColor.Green
        Dim inputText As String = ""        '出力されたハッシュ値
        Dim baseString As String = ""       '変換元となる文字列
        Dim userInput As String             '入力されたテキスト
        Dim isMatched As Boolean = False    'マッチしたかどうかのフラグ
        Dim endFlag As Boolean = False      '終了フラグ
        Dim sw As Stopwatch                 'stopwatch
        Dim ts As TimeSpan                  '経過時間

        While Not endFlag

            Console.WriteLine("ターゲットとなる文字列を入力してください: ")
            userInput = Console.ReadLine()

            If userInput = "exit" Then
                endFlag = True
            Else
                ' 次の探索に入る前にマッチフラグをリセットする
                isMatched = False
                sw = Stopwatch.StartNew()
                Console.WriteLine("探索中")

                While Not isMatched
                    baseString = GenerateRandomString()
                    inputText = EncodeHashToBase36(GetSha256Hash(baseString))

                    If inputText.StartsWith(userInput) Then
                        isMatched = True
                    End If
                End While

                sw.Stop()
                ts = sw.Elapsed
                Console.WriteLine("--------------------------------------------------")
                Console.WriteLine("指定した文字列：" & userInput)
                Console.WriteLine("ハッシュ元文字列：" & baseString)
                Console.WriteLine("出力されたハッシュ値：" & inputText.Substring(0, Math.Min(10, inputText.Length)))
                Console.WriteLine("かかった時間：" & ts.ToString("hh\:mm\:ss\.ffff"))
                Console.WriteLine("--------------------------------------------------")
                Console.WriteLine()
            End If
        End While
    End Sub

    ''' <summary>
    ''' ランダム文字列を生成する関数
    ''' </summary>
    ''' <returns></returns>
    Public Function GenerateRandomString() As String
        Const chars As String = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"
        Dim length As Integer = rand.Next(1, 11)
        Dim result As New StringBuilder(length)

        For i As Integer = 0 To length - 1
            result.Append(chars(rand.Next(chars.Length)))
        Next

        Return result.ToString()
    End Function

    ''' <summary>
    ''' SHA256ハッシュのバイト配列を取得する関数
    ''' </summary>
    ''' <param name="inputString"></param>
    ''' <returns>ハッシュのバイト配列（big-endian）</returns>
    Public Function GetSha256Hash(inputString As String) As Byte()
        ' SHA256オブジェクトを作成（Usingを使うことでメモリ解放を安全に行う）
        Using sha256 As SHA256 = SHA256.Create()
            ' 文字列をUTF-8のバイト配列に変換
            Dim inputBytes As Byte() = Encoding.UTF8.GetBytes(inputString)

            ' ハッシュ値を計算してバイト配列で返す
            Dim hashBytes As Byte() = sha256.ComputeHash(inputBytes)
            Return hashBytes
        End Using
    End Function

    ''' <summary>
    ''' Base36に変換する関数（SHA256のバイト配列を直接BigIntegerに変換して処理）
    ''' </summary>
    ''' <param name="hashBytes">SHA256ハッシュのバイト配列（big-endian）</param>
    ''' <returns>Base36表現の文字列</returns>
    Public Function EncodeHashToBase36(hashBytes As Byte()) As String
        ' Base36で使用する36文字の文字セット
        Const Base36Chars As String = "0123456789abcdefghijklmnopqrstuvwxyz"

        If hashBytes Is Nothing OrElse hashBytes.Length = 0 Then
            Return "0"
        End If

        ' SHA256のハッシュは big-endian で得られるので、BigInteger コンストラクタ用に
        ' little-endian に反転しかつ符号ビット対策のために上位に 0 バイトを追加する
        Dim tmp(hashBytes.Length) As Byte ' 長さ = hashBytes.Length + 1、初期値は 0
        For i As Integer = 0 To hashBytes.Length - 1
            tmp(i) = hashBytes(hashBytes.Length - 1 - i)
        Next

        Dim bigNum As BigInteger = New BigInteger(tmp)

        If bigNum = 0 Then Return "0"

        ' 2. BigIntegerをBase36に変換
        Dim result As New StringBuilder()
        While bigNum > 0
            Dim remainder As Integer = CInt(bigNum Mod 36)
            result.Append(Base36Chars(remainder))
            bigNum = bigNum / 36
        End While

        ' 今は末尾から構築しているので反転して返す
        Dim chars() As Char = result.ToString().ToCharArray()
        Array.Reverse(chars)
        Return New String(chars)
    End Function

End Module