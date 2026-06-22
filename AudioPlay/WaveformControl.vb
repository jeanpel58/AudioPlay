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
