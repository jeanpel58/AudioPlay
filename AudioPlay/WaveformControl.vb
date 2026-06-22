Imports System.Drawing
Imports System.Windows.Forms
Imports NAudio.Wave

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
        Catch ex As Exception
            ' Erreur silencieuse
        End Try
    End Sub

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
