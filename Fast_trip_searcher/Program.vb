Imports System.Numerics
Imports System.Security.Cryptography
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks

Module Program

    ' Randomはスレッドセーフではないため、スレッドごとに独立したインスタンスを持たせる
    Private ReadOnly threadRand As New ThreadLocal(Of Random)(Function() New Random())

    Sub Main(args As String())
        Console.ForegroundColor = ConsoleColor.Green
        Dim userInput As String             '入力されたテキスト
        Dim endFlag As Boolean = False      '終了フラグ
        Dim sw As Stopwatch                 'stopwatch
        Dim ts As TimeSpan                  '経過時間

        While Not endFlag

            Console.WriteLine("ターゲットとなる文字列を入力してください: ")
            userInput = Console.ReadLine()

            If userInput = "exit" Then
                endFlag = True
            Else
                sw = Stopwatch.StartNew()
                Console.WriteLine("探索中 (並列処理)...")

                Dim matchedBaseString As String = ""
                Dim matchedInputText As String = ""
                Dim found As Boolean = False

                ' 見つかったら他のスレッドの処理をキャンセルするためのトークンソース
                Using cts As New CancellationTokenSource()
                    Try
                        ' Parallel.Forで並列処理を実行（Long.MaxValueまでループ）
                        Parallel.For(0, Long.MaxValue,
                            New ParallelOptions() With {.CancellationToken = cts.Token},
                            Sub(i, state)
                                ' 1. ランダム文字列生成
                                Dim baseStr As String = GenerateRandomString()
                                ' 2. ハッシュ計算
                                Dim hashBytes As Byte() = GetSha256Hash(baseStr)
                                ' 3. Base36変換
                                Dim encoded As String = EncodeHashToBase36(hashBytes)

                                ' 4. 一致判定
                                If encoded.StartsWith(userInput) Then
                                    SyncLock cts
                                        If Not found Then
                                            found = True
                                            matchedBaseString = baseStr
                                            matchedInputText = encoded
                                            cts.Cancel() ' 他のスレッドをキャンセル
                                        End If
                                    End SyncLock
                                End If
                            End Sub)
                    Catch ex As OperationCanceledException
                        ' キャンセルによる例外は正常終了として扱う
                    End Try
                End Using

                sw.Stop()
                ts = sw.Elapsed
                Console.WriteLine("--------------------------------------------------")
                Console.WriteLine("指定した文字列：" & userInput)
                Console.WriteLine("ハッシュ元文字列：" & matchedBaseString)
                Console.WriteLine("出力されたハッシュ値：" & matchedInputText.Substring(0, Math.Min(10, matchedInputText.Length)))
                Console.WriteLine("かかった時間：" & ts.ToString("hh\:mm\:ss\.ffff"))
                Console.WriteLine("--------------------------------------------------")
                Console.WriteLine()
            End If
        End While
    End Sub

    ''' <summary>
    ''' ランダム文字列を生成する関数（スレッドセーフ版）
    ''' </summary>
    Public Function GenerateRandomString() As String
        Const chars As String = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"
        Dim r As Random = threadRand.Value
        Dim length As Integer = r.Next(1, 11)
        Dim result As New StringBuilder(length)

        For i As Integer = 0 To length - 1
            result.Append(chars(r.Next(chars.Length)))
        Next

        Return result.ToString()
    End Function

    ''' <summary>
    ''' SHA256ハッシュのバイト配列を取得する関数
    ''' </summary>
    Public Function GetSha256Hash(inputString As String) As Byte()
        Using sha256 As SHA256 = SHA256.Create()
            Dim inputBytes As Byte() = Encoding.UTF8.GetBytes(inputString)
            Dim hashBytes As Byte() = sha256.ComputeHash(inputBytes)
            Return hashBytes
        End Using
    End Function

    ''' <summary>
    ''' Base36に変換する関数
    ''' </summary>
    Public Function EncodeHashToBase36(hashBytes As Byte()) As String
        Const Base36Chars As String = "0123456789abcdefghijklmnopqrstuvwxyz"

        If hashBytes Is Nothing OrElse hashBytes.Length = 0 Then
            Return "0"
        End If

        Dim tmp(hashBytes.Length) As Byte
        For i As Integer = 0 To hashBytes.Length - 1
            tmp(i) = hashBytes(hashBytes.Length - 1 - i)
        Next

        Dim bigNum As BigInteger = New BigInteger(tmp)

        If bigNum = 0 Then Return "0"

        Dim result As New StringBuilder()
        While bigNum > 0
            Dim remainder As Integer = CInt(bigNum Mod 36)
            result.Append(Base36Chars(remainder))
            bigNum = bigNum / 36
        End While

        Dim chars() As Char = result.ToString().ToCharArray()
        Array.Reverse(chars)
        Return New String(chars)
    End Function

End Module