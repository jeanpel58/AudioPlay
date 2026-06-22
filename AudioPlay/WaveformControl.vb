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
    Private cueMarkers As New List(Of Single)() ' Positions des cue points

    Public Sub New()
        Me.DoubleBuffered = True
        Me.Size = New Size(400, 80)
        Me.BackColor = Color.Black
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

    ''' <summary>
    ''' Ajoute un marqueur de cue point
    ''' </summary>
    Public Sub AddCueMarker(position As Single)
        If Not cueMarkers.Contains(position) Then
            cueMarkers.Add(position)
            Me.Invalidate()
        End If
    End Sub

    ''' <summary>
    ''' Efface tous les marqueurs de cue
    ''' </summary>
    Public Sub ClearCueMarkers()
        cueMarkers.Clear()
        Me.Invalidate()
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)

        Dim g As Graphics = e.Graphics
        g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

        ' Fond noir
        g.Clear(Color.Black)

        If waveformData Is Nothing Then
            ' Pas de données, afficher un message
            Using font As New Font("Segoe UI", 10.0F)
                Dim text As String = "Aucune piste chargée"
                Dim textSize As SizeF = g.MeasureString(text, font)
                g.DrawString(text, font, Brushes.Gray, (Me.Width - textSize.Width) / 2, (Me.Height - textSize.Height) / 2)
            End Using
            Return
        End If

        ' Dessiner la forme d'onde
        Dim centerY As Integer = Me.Height \ 2
        Dim maxHeight As Integer = Me.Height \ 2 - 5

        Using pen As New Pen(Color.Cyan, 1)
            For x As Integer = 0 To Math.Min(Me.Width - 1, waveformData.Length - 1)
                Dim amplitude As Single = waveformData(x)
                Dim height As Integer = CInt(amplitude * maxHeight)

                ' Ligne verticale représentant l'amplitude
                g.DrawLine(pen, x, centerY - height, x, centerY + height)
            Next
        End Using

        ' Dessiner les marqueurs de cue (points rouges)
        For Each cuePos As Single In cueMarkers
            Dim x As Integer = CInt(cuePos * Me.Width)
            Using brush As New SolidBrush(Color.FromArgb(180, Color.Red))
                g.FillRectangle(brush, x - 2, 0, 4, Me.Height)
            End Using
        Next

        ' Dessiner la position actuelle (ligne jaune)
        Dim posX As Integer = CInt(m_currentPosition * Me.Width)
        Using pen As New Pen(Color.Yellow, 2)
            g.DrawLine(pen, posX, 0, posX, Me.Height)
        End Using

        ' Bordure
        g.DrawRectangle(Pens.Gray, 0, 0, Me.Width - 1, Me.Height - 1)
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
