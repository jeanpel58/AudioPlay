Imports System.IO
Imports NAudio.Wave
Imports System.Globalization

Module Program
    Sub Main(args As String())
        If args.Length = 0 Then
            Console.WriteLine("Usage: WavAnalyzerTool <folderPath> [--threshold dB] [--window ms] [--hop ms]")
            Return
        End If

        Dim thresholdDb As Double = -40.0
        Dim windowMs As Integer = 150
        Dim hopMs As Integer = 15
        Dim outPath As String = Nothing
        Dim folder As String = Nothing

        ' Flexible argument parsing: accept options in any order and detect first existing folder as input
        Dim idx As Integer = 0
        While idx < args.Length
            Dim a = args(idx)
            Select Case a
                Case "--threshold"
                    If idx + 1 < args.Length Then
                        Double.TryParse(args(idx + 1), NumberStyles.Float, CultureInfo.InvariantCulture, thresholdDb)
                        idx += 1
                    End If
                Case "--window"
                    If idx + 1 < args.Length Then Integer.TryParse(args(idx + 1), windowMs) : idx += 1
                Case "--hop"
                    If idx + 1 < args.Length Then Integer.TryParse(args(idx + 1), hopMs) : idx += 1
                Case "--out"
                    If idx + 1 < args.Length Then outPath = args(idx + 1) : idx += 1
                Case Else
                    ' If argument is a path that exists as directory, treat as folder input (first occurrence)
                    If folder Is Nothing AndAlso Directory.Exists(a) Then
                        folder = a
                    End If
            End Select
            idx += 1
        End While

        If String.IsNullOrEmpty(folder) Then
            Console.WriteLine("Error: no valid folder argument found. Provide the album folder path.")
            Return
        End If

        Dim out As String = If(String.IsNullOrEmpty(outPath), Path.Combine(folder, "silence_report.txt"), outPath)
        Try
            Dim outDir = Path.GetDirectoryName(out)
            If Not String.IsNullOrEmpty(outDir) AndAlso Not Directory.Exists(outDir) Then Directory.CreateDirectory(outDir)
        Catch
        End Try
        Using sw As New StreamWriter(out, False)
            sw.WriteLine("Silence analysis for: " & folder)
            sw.WriteLine(String.Format(CultureInfo.InvariantCulture, "Parameters: threshold={0} dB, window={1} ms, hop={2} ms", thresholdDb, windowMs, hopMs))

            Dim trackInfos As New List(Of Object)

            For Each f In Directory.GetFiles(folder, "*.wav")
                Try
                    Using reader As New WaveFileReader(f)
                        Dim sampleRate = reader.WaveFormat.SampleRate
                        Dim channels = reader.WaveFormat.Channels
                        Dim hopSamples = CInt(Math.Max(1, (sampleRate * hopMs) / 1000))

                        Dim provider = reader.ToSampleProvider()
                        Dim floatBuf(hopSamples * channels - 1) As Single

                        Dim windowCount As Integer = 0
                        Dim firstNonSilentIndex As Integer = -1
                        Dim lastNonSilentIndex As Integer = -1

                        Do
                            Dim read = provider.Read(floatBuf, 0, floatBuf.Length)
                            If read = 0 Then Exit Do

                            ' compute RMS across frames in this hop
                            Dim sumSquares As Double = 0
                            Dim frames = read / channels
                            For i As Integer = 0 To read - 1 Step channels
                                Dim frameSum As Double = 0
                                For ch As Integer = 0 To channels - 1
                                    frameSum += floatBuf(i + ch)
                                Next
                                Dim frameAvg = frameSum / channels
                                sumSquares += frameAvg * frameAvg
                            Next

                            Dim rms As Double = 0
                            If frames > 0 Then
                                rms = Math.Sqrt(sumSquares / frames)
                            End If

                            Dim db As Double = If(rms > 0, 20.0 * Math.Log10(rms), -200.0)

                            If db >= thresholdDb Then
                                If firstNonSilentIndex = -1 Then firstNonSilentIndex = windowCount
                                lastNonSilentIndex = windowCount
                            End If

                            windowCount += 1
                        Loop

                        Dim startNonSilentSec As Double = 0.0
                        Dim endNonSilentSec As Double = reader.TotalTime.TotalSeconds

                        If firstNonSilentIndex >= 0 Then
                            startNonSilentSec = firstNonSilentIndex * hopMs / 1000.0
                        Else
                            ' fully silent
                            startNonSilentSec = reader.TotalTime.TotalSeconds
                            lastNonSilentIndex = -1
                        End If

                        If lastNonSilentIndex >= 0 Then
                            endNonSilentSec = (lastNonSilentIndex + 1) * hopMs / 1000.0
                            If endNonSilentSec > reader.TotalTime.TotalSeconds Then endNonSilentSec = reader.TotalTime.TotalSeconds
                        Else
                            endNonSilentSec = 0.0
                        End If

                        sw.WriteLine(String.Format(CultureInfo.InvariantCulture, "{0}, duration={1:F2}s, sr={2}Hz, ch={3}, startNonSilent={4:F2}s, endNonSilent={5:F2}s", Path.GetFileName(f), reader.TotalTime.TotalSeconds, sampleRate, channels, startNonSilentSec, endNonSilentSec))

                        trackInfos.Add(New With {Key .FileName = Path.GetFileName(f), Key .Duration = reader.TotalTime.TotalSeconds, Key .SampleRate = sampleRate, Key .Channels = channels, Key .StartNonSilent = startNonSilentSec, Key .EndNonSilent = endNonSilentSec})
                    End Using
                Catch ex As Exception
                    sw.WriteLine(Path.GetFileName(f) & ", ERROR: " & ex.Message)
                End Try
            Next

            sw.WriteLine()
            sw.WriteLine("Inter-track analysis (positive = gap in seconds, negative = overlap seconds):")
            For i As Integer = 0 To trackInfos.Count - 2
                Dim a = CType(trackInfos(i), Object)
                Dim b = CType(trackInfos(i + 1), Object)
                ' compute gap = startNonSilent_next - endNonSilent_prev
                Dim gap = b.StartNonSilent - a.EndNonSilent
                Dim note As String = If(gap < -0.05, "OVERLAP", If(gap > 0.05, "GAP", "OK"))
                sw.WriteLine(String.Format(CultureInfo.InvariantCulture, "{0} -> {1}: delta={2:F3}s => {3}", a.FileName, b.FileName, gap, note))
            Next

        End Using

        Console.WriteLine("Report written to: " & out)
    End Sub
End Module
