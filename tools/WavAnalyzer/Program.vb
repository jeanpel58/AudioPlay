Imports System.IO
Imports NAudio.Wave

Module Program
    Sub Main(args As String())
        If args.Length = 0 Then
            Console.WriteLine("Usage: WavAnalyzerTool <folderPath>")
            Return
        End If
        Dim folder = args(0)
        Dim out = Path.Combine(folder, "silence_report.txt")
        Using sw As New StreamWriter(out, False)
            sw.WriteLine("Silence report for: " & folder)
            For Each f In Directory.GetFiles(folder, "*.wav")
                Try
                    Using r As New WaveFileReader(f)
                        Dim duration = r.TotalTime.TotalSeconds
                        sw.WriteLine(Path.GetFileName(f) & ", " & duration.ToString("F2") & "s, " & r.WaveFormat.SampleRate.ToString() & "Hz, " & r.WaveFormat.Channels.ToString() & "ch, " & r.Length.ToString() & " bytes")
                    End Using
                Catch ex As Exception
                    sw.WriteLine(Path.GetFileName(f) & ", ERROR: " & ex.Message)
                End Try
            Next
        End Using
        Console.WriteLine("Report written to: " & out)
    End Sub
End Module
