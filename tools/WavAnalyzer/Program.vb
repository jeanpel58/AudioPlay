Imports System.IO
Imports System.Text
Imports NAudio.Wave
Imports System.Globalization

Module Program
    Class TrackInfo
        Public Property FileName As String
        Public Property Duration As Double
        Public Property SampleRate As Integer
        Public Property Channels As Integer
        Public Property Leading As Double
        Public Property Trailing As Double
    End Class

    Sub Main(args As String())
        If args.Length = 0 Then
            Console.WriteLine("Usage: WavAnalyzerTool <folderPath> [--threshold dB] [--window ms] [--hop ms] [--fix]")
            Return
        End If

        Dim thresholdDb As Double = -40.0
        Dim windowMs As Integer = 150
        Dim hopMs As Integer = 15
        Dim outPath As String = Nothing
        Dim folder As String = Nothing
        Dim fixFlag As Boolean = False

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
                Case "--fix"
                    fixFlag = True
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

            Dim trackInfos As New List(Of TrackInfo)

            For Each f In Directory.GetFiles(folder, "*.wav")
                Try
                    Using reader As New WaveFileReader(f)
                        Dim sampleRate = reader.WaveFormat.SampleRate
                        Dim channels = reader.WaveFormat.Channels
                        Dim hopSamples = CInt(Math.Max(1, (sampleRate * hopMs) / 1000))

                        Dim floatBuf(hopSamples * channels - 1) As Single

                        Dim windowCount As Integer = 0
                        Dim firstNonSilentIndex As Integer = -1
                        Dim lastNonSilentIndex As Integer = -1

                        Dim provider = reader.ToSampleProvider()
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

                        Dim trailingNonSilentSec As Double = reader.TotalTime.TotalSeconds - endNonSilentSec
                        If trailingNonSilentSec < 0 Then trailingNonSilentSec = 0.0

                        sw.WriteLine(String.Format(CultureInfo.InvariantCulture, "{0}, duration={1:F2}s, sr={2}Hz, ch={3}, leading={4:F2}s, trailing={5:F2}s", Path.GetFileName(f), reader.TotalTime.TotalSeconds, sampleRate, channels, startNonSilentSec, trailingNonSilentSec))

                        trackInfos.Add(New TrackInfo With {.FileName = Path.GetFileName(f), .Duration = reader.TotalTime.TotalSeconds, .SampleRate = sampleRate, .Channels = channels, .Leading = startNonSilentSec, .Trailing = trailingNonSilentSec})
                    End Using
                Catch ex As Exception
                    sw.WriteLine(Path.GetFileName(f) & ", ERROR: " & ex.Message)
                End Try
            Next

            sw.WriteLine()
            sw.WriteLine("Inter-track analysis (positive = gap in seconds, negative = overlap seconds):")
            For i As Integer = 0 To trackInfos.Count - 2
                Dim a = trackInfos(i)
                Dim b = trackInfos(i + 1)
                ' compute inter-track delta as trailing silence of previous + leading silence of next
                Dim delta = a.Trailing + b.Leading
                Dim note As String = If(delta > 0.05, "GAP", "OK")
                sw.WriteLine(String.Format(CultureInfo.InvariantCulture, "{0} -> {1}: trailing_prev={2:F3}s, leading_next={3:F3}s, delta={4:F3}s => {5}", a.FileName, b.FileName, a.Trailing, b.Leading, delta, note))
            Next

            sw.WriteLine()
            If fixFlag Then
                sw.WriteLine("Applying conservative non-destructive fixes: creating *_fixed.wav files")
                Dim logSb As New StringBuilder()
                logSb.AppendLine("Silence corrections log for: " & folder)
                logSb.AppendLine(String.Format(CultureInfo.InvariantCulture, "Parameters: threshold={0} dB, window={1} ms, hop={2} ms", thresholdDb, windowMs, hopMs))
                logSb.AppendLine()

                For Each t In trackInfos
                    Try
                        Dim srcPath = Path.Combine(folder, t.FileName)
                        If Not File.Exists(srcPath) Then
                            logSb.AppendLine(t.FileName & ": source file not found, skipped.")
                            Continue For
                        End If

                        Using r As New WaveFileReader(srcPath)
                            Dim bytesPerSecond As Double = r.WaveFormat.AverageBytesPerSecond
                            Dim blockAlign As Integer = r.WaveFormat.BlockAlign
                            If bytesPerSecond <= 0 OrElse blockAlign <= 0 Then
                                logSb.AppendLine(t.FileName & ": invalid wave format values, skipped.")
                                Continue For
                            End If

                            Dim leadBytes As Long = CLng(Math.Round(t.Leading * bytesPerSecond))
                            Dim trailBytes As Long = CLng(Math.Round(t.Trailing * bytesPerSecond))

                            leadBytes -= (leadBytes Mod blockAlign)
                            trailBytes -= (trailBytes Mod blockAlign)

                            If leadBytes < 0 Then leadBytes = 0
                            If trailBytes < 0 Then trailBytes = 0

                            Dim sourceLength As Long = r.Length
                            Dim startPos As Long = Math.Min(sourceLength, leadBytes)
                            Dim endTrim As Long = Math.Min(sourceLength - startPos, trailBytes)
                            Dim bytesToCopy As Long = sourceLength - startPos - endTrim

                            If bytesToCopy <= 0 Then
                                logSb.AppendLine(t.FileName & ": trim would remove all audio, skipped.")
                                Continue For
                            End If

                            Dim outPathFixed = Path.Combine(folder, Path.GetFileNameWithoutExtension(t.FileName) & "_fixed.wav")
                            Using w As New WaveFileWriter(outPathFixed, r.WaveFormat)
                                r.Position = startPos
                                Dim buf(8191) As Byte
                                Dim remaining As Long = bytesToCopy
                                While remaining > 0
                                    Dim toRead = CInt(Math.Min(buf.Length, remaining))
                                    Dim read = r.Read(buf, 0, toRead)
                                    If read <= 0 Then Exit While
                                    w.Write(buf, 0, read)
                                    remaining -= read
                                End While
                            End Using

                            Try
                                Dim fi = New FileInfo(outPathFixed)
                                If fi.Exists AndAlso fi.Length <= 100 Then
                                    ' Too small to be valid audio: delete and log
                                    fi.Delete()
                                    logSb.AppendLine(String.Format(CultureInfo.InvariantCulture, "{0}: trimmed file was too small and was deleted ({1} bytes)", t.FileName, If(fi.Exists, fi.Length, 0)))
                                Else
                                    logSb.AppendLine(String.Format(CultureInfo.InvariantCulture, "{0}: trimmed leading={1:F3}s ({2} bytes), trailing={3:F3}s ({4} bytes) -> {5}", t.FileName, t.Leading, startPos, t.Trailing, endTrim, Path.GetFileName(outPathFixed)))
                                End If
                            Catch
                            End Try
                        End Using
                    Catch ex As Exception
                        logSb.AppendLine(t.FileName & ": ERROR: " & ex.Message)
                    End Try
                Next

                Try
                    Dim correctionsLogPath = Path.Combine(folder, "silence_corrections_log.txt")
                    File.WriteAllText(correctionsLogPath, logSb.ToString())
                    sw.WriteLine("Corrections log written to: " & correctionsLogPath)
                Catch ex As Exception
                    sw.WriteLine("Failed to write corrections log: " & ex.Message)
                End Try
            End If

        End Using

        Console.WriteLine("Report written to: " & out)
    End Sub
End Module
