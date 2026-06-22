Imports System.Drawing
Imports System.Windows.Forms
Imports NAudio.Wave
Imports System.Diagnostics
Imports System.Text.Json
Imports System.IO
Imports System.Security.Cryptography
Imports System.Text
Imports System.Threading.Tasks

''' <summary>
''' Contrôle de visualisation de forme d'onde (waveform) pour afficher la piste audio
''' </summary>
Public Class WaveformControl
    Inherits Control

    Public Enum DisplayMode
        Audacity
        VirtualDJ
        Spectrogram
        Line
        Serato
    End Enum

    Private m_displayMode As DisplayMode = DisplayMode.Audacity

    Public Property DisplayModeSetting As DisplayMode
        Get
            Return m_displayMode
        End Get
        Set(value As DisplayMode)
            m_displayMode = value
            Me.Invalidate()
        End Set
    End Property

    Private waveformData() As Single = Nothing
    Private m_currentPosition As Single = 0.0F ' Position actuelle (0.0 à 1.0)
    ' Propriétés attendues par FormDJ
    Public Property Zoom As Single = 1.0F
    Public Property WaveformColor As Color = Color.Cyan
    Public ReadOnly Property LastTrimStartPixel As Integer = 0
    Public ReadOnly Property LastTrimLengthPixels As Integer = 0

    Public Sub New()
        Me.DoubleBuffered = True
        Me.Size = New Size(400, 80)
        Me.BackColor = Color.Black
    End Sub

    ' API events used by FormDJ when interacting with the waveform control
    Public Event DragStarted()
    Public Event DragMoved(position As Single)
    Public Event DragEnded()

    ' Set waveform samples (called from background workers)
    Public Sub SetWaveformSamples(samples() As Single)
        waveformData = samples
        ' Recompute beat info when samples are set
        cachedBeatPositions = Nothing
        cachedBeatInterval = 0
        Me.Invalidate()
    End Sub

    ' Layout helper called by FormDJ
    Public Sub UpdateLayoutToParent()
        ' By default do nothing — designer handles placement
    End Sub

    ' Center view helper (placeholder)
    Public Sub CenterViewOnCurrentPosition()
        ' Placeholder: no-op for now
    End Sub

    ' Onset markers API (no-op — markers disabled by user request)
    Public Sub SetOnsetMarkers(markers() As Integer)
        ' Intentionally ignored
    End Sub

    ' Overload accepting Single() as some callers provide Single arrays
    Public Sub SetOnsetMarkers(markers() As Single)
        ' Intentionally ignored
    End Sub

    ''' <summary>
    ''' Génère la forme d'onde à partir d'un fichier audio
    ''' </summary>
    Public Sub GenerateWaveform(audioFilePath As String)
        Try
            Using reader As New AudioFileReader(audioFilePath)
                Dim samplesPerPixel As Integer = CInt(reader.Length / (Me.Width * 4)) ' Moyenne par pixel
                Dim pixelCount As Integer = Me.Width
                ReDim waveformData(pixelCount - 1)

                Dim buffer(samplesPerPixel * reader.WaveFormat.Channels - 1) As Single

                For pixel As Integer = 0 To pixelCount - 1
                    Dim bytesRead As Integer = reader.Read(buffer, 0, buffer.Length)
                    If bytesRead = 0 Then Exit For

                    ' Calculer le maximum pour ce pixel
                    Dim maxVal As Single = 0
                    For i As Integer = 0 To bytesRead - 1
                        maxVal = Math.Max(maxVal, Math.Abs(buffer(i)))
                    Next
                    waveformData(pixel) = maxVal
                Next
            End Using

            Me.Invalidate()

            ' Check cache first (AppData). If present and valid, use it. Otherwise run detection async and cache result.
            Try
                Dim durationSeconds As Double = 0
                Try
                    Using reader2 As New AudioFileReader(audioFilePath)
                        durationSeconds = reader2.TotalTime.TotalSeconds
                    End Using
                Catch
                End Try

                Dim beatsSeconds As List(Of Double) = Nothing
                Dim tempo As Double = 0
                If TryLoadCache(audioFilePath, beatsSeconds, tempo) AndAlso beatsSeconds IsNot Nothing AndAlso beatsSeconds.Count > 0 Then
                    ' convert to pixels
                    Dim peaks As New List(Of Integer)()
                    For Each t In beatsSeconds
                        Try
                            If durationSeconds > 0 Then
                                Dim px = CInt((t / durationSeconds) * waveformData.Length)
                                If px < 0 Then px = 0
                                If px >= waveformData.Length Then px = waveformData.Length - 1
                                peaks.Add(px)
                            End If
                        Catch
                        End Try
                    Next
                    If peaks.Count > 0 Then
                        cachedBeatPositions = peaks
                        ' compute median diff
                        Dim diffs As New List(Of Integer)()
                        For i As Integer = 1 To cachedBeatPositions.Count - 1
                            diffs.Add(cachedBeatPositions(i) - cachedBeatPositions(i - 1))
                        Next
                        If diffs.Count > 0 Then
                            diffs.Sort()
                            cachedBeatInterval = diffs(diffs.Count \ 2)
                            If cachedBeatInterval < 4 Then cachedBeatInterval = 4
                        End If
                        Me.Invalidate()
                    End If
                Else
                    ' Run detection in background to avoid blocking UI
                    Task.Run(Sub()
                                 Try
                                     Dim (detBeats, detTempo) = RunPythonDetectBeats(audioFilePath)
                                     If detBeats IsNot Nothing AndAlso detBeats.Count > 0 Then
                                         ' save cache
                                         Try
                                             SaveCacheAtomic(audioFilePath, detBeats, detTempo)
                                         Catch
                                         End Try

                                         ' convert to pixels on UI thread
                                         Try
                                             Me.BeginInvoke(New Action(Sub()
                                                                          Try
                                                                              Dim peaksLocal As New List(Of Integer)()
                                                                              For Each t In detBeats
                                                                                  If durationSeconds > 0 Then
                                                                                      Dim px = CInt((t / durationSeconds) * waveformData.Length)
                                                                                      If px < 0 Then px = 0
                                                                                      If px >= waveformData.Length Then px = waveformData.Length - 1
                                                                                      peaksLocal.Add(px)
                                                                                  End If
                                                                              Next
                                                                              If peaksLocal.Count > 0 Then
                                                                                  cachedBeatPositions = peaksLocal
                                                                                  Dim diffs As New List(Of Integer)()
                                                                                  For i As Integer = 1 To cachedBeatPositions.Count - 1
                                                                                      diffs.Add(cachedBeatPositions(i) - cachedBeatPositions(i - 1))
                                                                                  Next
                                                                                  If diffs.Count > 0 Then
                                                                                      diffs.Sort()
                                                                                      cachedBeatInterval = diffs(diffs.Count \ 2)
                                                                                      If cachedBeatInterval < 4 Then cachedBeatInterval = 4
                                                                                  End If
                                                                                  Me.Invalidate()
                                                                              End If
                                                                          Catch
                                                                          End Try
                                                                      End Sub))
                                         Catch
                                         End Try
                                     Else
                                         ' fallback: attempt local peak picking
                                         Try
                                             Me.BeginInvoke(New Action(Sub()
                                                                          Try
                                                                              ComputeBeatsIfNeeded()
                                                                              Me.Invalidate()
                                                                          Catch
                                                                          End Try
                                                                      End Sub))
                                         Catch
                                         End Try
                                     End If
                                 Catch
                                 End Try
                             End Sub)
                End If
            Catch
            End Try
        Catch ex As Exception
            ' Erreur silencieuse
        End Try
    End Sub

    ' Cache helpers
    Private Function GetCacheDirectory() As String
        Try
            Dim d = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioPlay", "CalculatedBeats")
            If Not Directory.Exists(d) Then
                Directory.CreateDirectory(d)
            End If
            Return d
        Catch
            Return Nothing
        End Try
    End Function

    Private Function ComputeCacheFileName(audioPath As String) As String
        Try
            Using sha As SHA1 = SHA1.Create()
                Dim bytes = Encoding.UTF8.GetBytes(Path.GetFullPath(audioPath).ToLowerInvariant())
                Dim hash = sha.ComputeHash(bytes)
                Dim sb As New StringBuilder()
                For Each b In hash
                    sb.Append(b.ToString("x2"))
                Next
                Return sb.ToString() & ".beats.json"
            End Using
        Catch
            Return Nothing
        End Try
    End Function

    Private Function TryLoadCache(audioPath As String, ByRef beatsSeconds As List(Of Double), ByRef tempo As Double) As Boolean
        beatsSeconds = Nothing
        tempo = 0
        Try
            Dim cacheDir = GetCacheDirectory()
            Dim cachePath As String = Nothing
            If Not String.IsNullOrEmpty(cacheDir) Then
                Dim fileName = ComputeCacheFileName(audioPath)
                If Not String.IsNullOrEmpty(fileName) Then
                    cachePath = Path.Combine(cacheDir, fileName)
                End If
            End If

            ' fallback: no cache dir
            If String.IsNullOrEmpty(cachePath) OrElse Not File.Exists(cachePath) Then
                Return False
            End If

            Dim json = File.ReadAllText(cachePath)
            Dim doc = JsonDocument.Parse(json)
            Dim root = doc.RootElement
            If root.TryGetProperty("audioPath", Nothing) Then
                Dim savedPath = root.GetProperty("audioPath").GetString()
                If Not String.Equals(Path.GetFullPath(savedPath), Path.GetFullPath(audioPath), StringComparison.OrdinalIgnoreCase) Then
                    Return False
                End If
            End If
            ' validate mtime/size
            Dim fileInfo = New FileInfo(audioPath)
            If root.TryGetProperty("mtimeTicks", Nothing) Then
                Dim savedTicks = root.GetProperty("mtimeTicks").GetInt64()
                If savedTicks <> fileInfo.LastWriteTimeUtc.Ticks Then
                    Return False
                End If
            End If
            If root.TryGetProperty("size", Nothing) Then
                Dim savedSize = root.GetProperty("size").GetInt64()
                If savedSize <> fileInfo.Length Then
                    Return False
                End If
            End If

            If root.TryGetProperty("tempo", Nothing) Then
                tempo = root.GetProperty("tempo").GetDouble()
            End If
            If root.TryGetProperty("beats", Nothing) Then
                beatsSeconds = New List(Of Double)()
                For Each el In root.GetProperty("beats").EnumerateArray()
                    beatsSeconds.Add(el.GetDouble())
                Next
                Return True
            End If
        Catch
        End Try
        Return False
    End Function

    Private Sub SaveCacheAtomic(audioPath As String, beatsSeconds As List(Of Double), tempo As Double)
        Try
            Dim cacheDir = GetCacheDirectory()
            If String.IsNullOrEmpty(cacheDir) Then
                ' fallback: save next to audio file
                cacheDir = Path.GetDirectoryName(audioPath)
            End If
            Dim fileName = ComputeCacheFileName(audioPath)
            If String.IsNullOrEmpty(fileName) Then Return
            Dim targetPath = Path.Combine(cacheDir, fileName)

            Dim fileInfo = New FileInfo(audioPath)
            Dim payload As New Dictionary(Of String, Object) From {
                {"audioPath", Path.GetFullPath(audioPath)},
                {"mtimeTicks", fileInfo.LastWriteTimeUtc.Ticks},
                {"size", fileInfo.Length},
                {"tempo", tempo},
                {"beats", beatsSeconds}
            }

            Dim tmp = targetPath & ".tmp"
            Dim json = JsonSerializer.Serialize(payload)
            File.WriteAllText(tmp, json)
            If File.Exists(targetPath) Then
                File.Delete(targetPath)
            End If
            File.Move(tmp, targetPath)
        Catch
        End Try
    End Sub

    Private Function RunPythonDetectBeats(audioFilePath As String) As (List(Of Double), Double)
        Try
            Dim scriptPath As String = Path.Combine(Application.StartupPath, "Tools", "detect_beats.py")
            If Not File.Exists(scriptPath) Then
                Return (Nothing, 0)
            End If
            Dim pythonExe As String = "python"
            Try
                If Not String.IsNullOrEmpty(ParametresGlobaux.PythonPath) AndAlso File.Exists(ParametresGlobaux.PythonPath) Then
                    pythonExe = ParametresGlobaux.PythonPath
                End If
            Catch
            End Try

            Dim psi As New ProcessStartInfo(pythonExe, $"""{scriptPath}""" & " " & $"""{audioFilePath}"""") With {
                .UseShellExecute = False,
                .RedirectStandardOutput = True,
                .RedirectStandardError = True,
                .CreateNoWindow = True
            }
            Using p As Process = Process.Start(psi)
                If p Is Nothing Then Return (Nothing, 0)
                Dim stdout As String = p.StandardOutput.ReadToEnd()
                Dim stderr As String = p.StandardError.ReadToEnd()
                p.WaitForExit(60000)
                If String.IsNullOrEmpty(stdout) Then Return (Nothing, 0)
                Try
                    Dim doc = JsonDocument.Parse(stdout)
                    Dim root = doc.RootElement
                    Dim beats As New List(Of Double)()
                    Dim tempo As Double = 0
                    If root.TryGetProperty("beats", Nothing) Then
                        For Each el In root.GetProperty("beats").EnumerateArray()
                            beats.Add(el.GetDouble())
                        Next
                    End If
                    If root.TryGetProperty("tempo", Nothing) Then
                        tempo = root.GetProperty("tempo").GetDouble()
                    End If
                    Return (beats, tempo)
                Catch
                    Return (Nothing, 0)
                End Try
            End Using
        Catch
        End Try
        Return (Nothing, 0)
    End Function

    ' Cache des positions de beats calculées (en pixels)
    Private cachedBeatPositions As List(Of Integer) = Nothing
    Private cachedBeatInterval As Integer = 0
    Private Sub ComputeBeatsIfNeeded()
        Try
            If waveformData Is Nothing OrElse waveformData.Length = 0 Then Return
            If cachedBeatPositions IsNot Nothing AndAlso cachedBeatPositions.Count > 0 Then Return

            Dim widthPx As Integer = Math.Max(1, Me.Width)
            Dim maxVal As Single = 0.0F
            For i As Integer = 0 To waveformData.Length - 1
                If waveformData(i) > maxVal Then maxVal = waveformData(i)
            Next
            If maxVal <= 0 Then Return

            ' Peak picking: threshold relative to maxVal
            Dim threshold As Single = Math.Max(0.02F, maxVal * 0.25F)
            Dim minDistance As Integer = Math.Max(8, CInt(widthPx * 0.02F)) ' minimal distance between beats in pixels

            Dim peaks As New List(Of Integer)()
            For i As Integer = 1 To Math.Min(waveformData.Length - 2, widthPx - 2)
                Dim val As Single = waveformData(i)
                If val >= threshold Then
                    ' local maximum within small neighborhood
                    If val >= waveformData(Math.Max(0, i - 1)) AndAlso val >= waveformData(Math.Min(waveformData.Length - 1, i + 1)) Then
                        If peaks.Count = 0 OrElse Math.Abs(i - peaks(peaks.Count - 1)) >= minDistance Then
                            peaks.Add(i)
                        End If
                    End If
                End If
            Next

            ' If too many peaks (noise), try raising threshold progressively
            Dim thr As Single = threshold
            While peaks.Count > 0 AndAlso peaks.Count < 6
                ' if too few peaks, lower threshold
                thr = thr * 0.8F
                peaks.Clear()
                For i As Integer = 1 To Math.Min(waveformData.Length - 2, widthPx - 2)
                    Dim val As Single = waveformData(i)
                    If val >= thr Then
                        If val >= waveformData(Math.Max(0, i - 1)) AndAlso val >= waveformData(Math.Min(waveformData.Length - 1, i + 1)) Then
                            If peaks.Count = 0 OrElse Math.Abs(i - peaks(peaks.Count - 1)) >= minDistance Then
                                peaks.Add(i)
                            End If
                        End If
                    End If
                Next
                If thr < 0.005F Then Exit While
            End While

            cachedBeatPositions = peaks

            ' Compute beat interval as median of differences
            If cachedBeatPositions.Count >= 2 Then
                Dim diffs As New List(Of Integer)()
                For i As Integer = 1 To cachedBeatPositions.Count - 1
                    diffs.Add(cachedBeatPositions(i) - cachedBeatPositions(i - 1))
                Next
                diffs.Sort()
                cachedBeatInterval = diffs(diffs.Count 
                                            2)
                ' clamp to reasonable range
                If cachedBeatInterval < 4 Then cachedBeatInterval = 4
            Else
                cachedBeatInterval = 0
            End If
        Catch
            cachedBeatPositions = Nothing
            cachedBeatInterval = 0
        End Try
    End Sub

    ''' <summary>
    ''' Position de lecture actuelle (0.0 = début, 1.0 = fin)
    ''' </summary>
    Public Property CurrentPosition As Single
        Get
            Return m_currentPosition
        End Get
        Set(value As Single)
            m_currentPosition = Math.Max(0.0F, Math.Min(1.0F, value))
            Me.Invalidate()
        End Set
    End Property

    ' Les marqueurs de cue ont été retirés volontairement (ne pas afficher de marqueurs)

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)

        Dim g As Graphics = e.Graphics
        g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

        ' Fond noir
        g.Clear(Color.Black)

        ' Déléguer le rendu principal selon le mode d'affichage
        Select Case m_displayMode
            Case DisplayMode.Audacity
                RenderAudacity(g)
            Case DisplayMode.VirtualDJ
                RenderVirtualDJ(g)
            Case Else
                RenderAudacity(g)
        End Select

        ' Ligne verticale fixe au centre (repère jaune)
        Dim centerX As Integer = Me.Width \ 2
        Using centerPen As New Pen(Color.Yellow, 1)
            g.DrawLine(centerPen, centerX, 0, centerX, Me.Height)
        End Using

        ' Les marqueurs de cue ont été désactivés par demande de l'utilisateur

        ' Dessiner la position actuelle (ligne jaune fine)
        Dim posX As Integer = CInt(m_currentPosition * Me.Width)
        Using pen As New Pen(Color.Yellow, 1)
            g.DrawLine(pen, posX, 0, posX, Me.Height)
        End Using

        ' Bordure
        g.DrawRectangle(Pens.Gray, 0, 0, Me.Width - 1, Me.Height - 1)
    End Sub

    Private Sub RenderVirtualDJ(g As Graphics)
        If waveformData Is Nothing Then
            RenderAudacity(g)
            Return
        End If

        ' Assurer que les beats sont calculés
        ComputeBeatsIfNeeded()

        Dim centerY As Integer = Me.Height \\ 2
        Dim maxHeight As Integer = Me.Height \\ 2 - 5

        Dim centerLineX As Integer = CInt(Me.Width * 0.5F)
        Dim offset As Integer = 1

        ' Dessiner forme d'onde minimisée (ligne fine) sur une bande
        Using pen As New Pen(WaveformColor, 1)
            For xIndex As Integer = 0 To Math.Min(Me.Width - 1 - centerLineX - offset, waveformData.Length - 1)
                Dim amplitude As Single = waveformData(xIndex)
                Dim height As Integer = CInt(amplitude * maxHeight * 0.6F)
                Dim x As Integer = centerLineX + offset + xIndex
                g.DrawLine(pen, x, centerY - height, x, centerY + height)
            Next
        End Using

        ' Dessiner les marqueurs de beat (petits triangles ou barres) au-dessus
        If cachedBeatPositions IsNot Nothing AndAlso cachedBeatPositions.Count > 0 Then
            Using beatPen As New Pen(Color.FromArgb(200, 255, 90, 90), 2)
                For Each p In cachedBeatPositions
                    Dim x As Integer = centerLineX + offset + p
                    ' Dessiner petit rectangle/ligne pour représenter le beat
                    g.DrawLine(beatPen, x, centerY - maxHeight, x, centerY - maxHeight + 8)
                Next
            End Using
        End If

        ' Dessiner la Computed Beat Grid (CBG) : carrés plus larges qui marquent la mesure
        If cachedBeatInterval > 0 Then
            Dim measureStep As Integer = cachedBeatInterval * 4 ' approximativement 4 temps par mesure
            Using measurePen As New Pen(Color.FromArgb(120, 200, 200, 255), 2)
                Dim xStart As Integer = centerLineX + offset + If(cachedBeatPositions IsNot Nothing AndAlso cachedBeatPositions.Count > 0, cachedBeatPositions(0), 0)
                For x As Integer = xStart To centerLineX + offset + waveformData.Length Step measureStep
                    g.DrawRectangle(measurePen, x - 3, centerY - maxHeight, 6, maxHeight * 2)
                Next
            End Using
        End If
    End Sub

    Private Sub RenderAudacity(g As Graphics)
        If waveformData Is Nothing Then
            ' Pas de données, afficher un message
            Using font As New Font("Segoe UI", 10.0F)
                Dim text As String = "Aucune piste chargée"
                Dim textSize As SizeF = g.MeasureString(text, font)
                g.DrawString(text, font, Brushes.Gray, (Me.Width - textSize.Width) / 2, (Me.Height - textSize.Height) / 2)
            End Using
            Return
        End If

        ' Dessiner la forme d'onde (style Audacity actuel)
        Dim centerY As Integer = Me.Height \ 2
        Dim maxHeight As Integer = Me.Height \ 2 - 5

        ' Décalage: garder la waveform à droite de la ligne centrale (ligne jaune)
        ' On décale l'origine horizontale pour que x=0 commence juste à droite de la ligne centrale
        Dim centerLineX As Integer = CInt(Me.Width * 0.5F)
        Dim offset As Integer = 1 ' petit décalage pour que la waveform commence après la ligne

        Using pen As New Pen(Color.Cyan, 1)
            For xIndex As Integer = 0 To Math.Min(Me.Width - 1 - centerLineX - offset, waveformData.Length - 1)
                Dim amplitude As Single = waveformData(xIndex)
                Dim height As Integer = CInt(amplitude * maxHeight)

                Dim x As Integer = centerLineX + offset + xIndex
                ' Ligne verticale représentant l'amplitude (à droite du centre)
                g.DrawLine(pen, x, centerY - height, x, centerY + height)
            Next
        End Using
    End Sub

    ''' <summary>
    ''' Permet de cliquer sur la waveform pour changer la position
    ''' </summary>
    Protected Overrides Sub OnMouseClick(e As MouseEventArgs)
        MyBase.OnMouseClick(e)
        If e.Button = MouseButtons.Left Then
            Dim newPosition As Single = e.X / CSng(Me.Width)
            RaiseEvent PositionClicked(newPosition)
        End If
    End Sub

    ''' <summary>
    ''' Événement levé quand l'utilisateur clique sur la waveform
    ''' </summary>
    Public Event PositionClicked(position As Single)
End Class
