Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Contrôle VU-Meter personnalisé pour afficher le niveau audio en temps réel
''' </summary>
Public Class VUMeterControl
    Inherits Control

    Private m_level As Single = 0.0F ' Niveau actuel (0.0 à 1.0)
    Private m_peakLevel As Single = 0.0F ' Niveau de crête
    Private peakHoldTime As Integer = 0
    Private Const PEAK_HOLD_FRAMES As Integer = 20 ' Maintenir le pic pendant ~20 frames

    Public Sub New()
        Me.DoubleBuffered = True
        Me.Size = New Size(30, 200)
    End Sub

    ''' <summary>
    ''' Niveau audio actuel (0.0 = silence, 1.0 = maximum)
    ''' </summary>
    Public Property Level As Single
        Get
            Return m_level
        End Get
        Set(value As Single)
            m_level = Math.Max(0.0F, Math.Min(1.0F, value))

            ' Mettre à jour le pic
            If m_level > m_peakLevel Then
                m_peakLevel = m_level
                peakHoldTime = PEAK_HOLD_FRAMES
            Else
                ' Décrémenter le hold time
                If peakHoldTime > 0 Then
                    peakHoldTime -= 1
                Else
                    ' Faire descendre le pic lentement
                    m_peakLevel = Math.Max(m_level, m_peakLevel - 0.02F)
                End If
            End If

            Me.Invalidate()
        End Set
    End Property

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)

        Dim g As Graphics = e.Graphics
        g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

        ' Fond noir
        g.FillRectangle(Brushes.Black, ClientRectangle)

        ' Calculer la hauteur du niveau
        Dim levelHeight As Integer = CInt(Me.Height * m_level)
        Dim peakHeight As Integer = CInt(Me.Height * m_peakLevel)

        ' Dessiner les segments du VU-meter (style LED)
        Dim segmentCount As Integer = 20
        Dim segmentHeight As Single = Me.Height / CSng(segmentCount)
        Dim gap As Integer = 1

        For i As Integer = 0 To segmentCount - 1
            Dim segmentTop As Integer = Me.Height - CInt((i + 1) * segmentHeight)
            Dim segmentRect As New Rectangle(2, segmentTop + gap, Me.Width - 4, CInt(segmentHeight) - gap * 2)

            ' Déterminer la couleur selon le niveau
            Dim segmentThreshold As Single = (i + 1) / CSng(segmentCount)

            If segmentThreshold <= m_level Then
                ' Segment actif
                Dim segmentColor As Color
                If segmentThreshold > 0.9F Then
                    ' Rouge (clip)
                    segmentColor = Color.Red
                ElseIf segmentThreshold > 0.7F Then
                    ' Orange (chaud)
                    segmentColor = Color.Orange
                ElseIf segmentThreshold > 0.5F Then
                    ' Jaune
                    segmentColor = Color.Yellow
                Else
                    ' Vert (normal)
                    segmentColor = Color.Lime
                End If

                Using brush As New SolidBrush(segmentColor)
                    g.FillRectangle(brush, segmentRect)
                End Using
            Else
                ' Segment inactif (gris foncé)
                Using brush As New SolidBrush(Color.FromArgb(30, 30, 30))
                    g.FillRectangle(brush, segmentRect)
                End Using
            End If
        Next

        ' Dessiner l'indicateur de crête (ligne rouge)
        If m_peakLevel > 0 Then
            Dim peakY As Integer = Me.Height - peakHeight
            Using pen As New Pen(Color.White, 2)
                g.DrawLine(pen, 0, peakY, Me.Width, peakY)
            End Using
        End If

        ' Bordure
        g.DrawRectangle(Pens.Gray, 0, 0, Me.Width - 1, Me.Height - 1)
    End Sub
End Class
