Imports System.Windows.Forms
Imports System.Drawing

Public Class CustomProgressBar
    Inherits ProgressBar

    Public Sub New()
        ' Enable custom painting
        Me.SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer, True)
        Me.DoubleBuffered = True
        Me.Minimum = 0
        If Me.Maximum < 1 Then Me.Maximum = 100
    End Sub

    ' Fill color exposée pour le thème
    Private _FillColor As Color = SystemColors.Highlight
    Public Property FillColor As Color
        Get
            Return _FillColor
        End Get
        Set(value As Color)
            _FillColor = value
            Me.Invalidate()
        End Set
    End Property

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Try
            Dim g = e.Graphics
            g.Clear(Me.BackColor)

            Dim rect = New Rectangle(0, 0, Me.Width, Me.Height)

            ' Draw background (unfilled)
            Using b As New SolidBrush(SystemColors.ControlLight)
                g.FillRectangle(b, rect)
            End Using

            ' Compute filled width
            Dim range = Math.Max(1, Me.Maximum - Me.Minimum)
            Dim percent As Double = (Me.Value - Me.Minimum) / range
            Dim filledWidth As Integer = CInt(Math.Round(percent * Me.Width))

            If filledWidth > 0 Then
                Dim fillRect = New Rectangle(0, 0, filledWidth, Me.Height)
                Using b2 As New SolidBrush(Me.FillColor)
                    g.FillRectangle(b2, fillRect)
                End Using
            End If

            ' Draw border
            Using pen As New Pen(SystemColors.ControlDark)
                g.DrawRectangle(pen, 0, 0, Me.Width - 1, Me.Height - 1)
            End Using

            ' Draw percentage text centered
            Dim percText = CStr(CInt(Math.Round(percent * 100.0))) & "%"
            Using font = New Font(Me.Font.FontFamily, Me.Font.Size, FontStyle.Bold)
                Dim sf As New StringFormat()
                sf.Alignment = StringAlignment.Center
                sf.LineAlignment = StringAlignment.Center

                Dim textSize = g.MeasureString(percText, font)
                Dim textWidth = textSize.Width
                Dim textLeft = (Me.Width - textWidth) / 2

                ' Determine text color: black by default, white if covered by filled area
                Dim textColor As Color = Color.Black
                If filledWidth > textLeft Then
                    textColor = Color.White
                End If

                Using tb As New SolidBrush(textColor)
                    g.DrawString(percText, font, tb, New RectangleF(0, 0, Me.Width, Me.Height), sf)
                End Using
            End Using
        Catch
            MyBase.OnPaint(e)
        End Try
    End Sub

    ' ProgressBar does not expose OnValueChanged as overridable; use Value property change via Refresh
    Public Shadows Property Value As Integer
        Get
            Return MyBase.Value
        End Get
        Set(v As Integer)
            MyBase.Value = v
            Me.Invalidate()
        End Set
    End Property

    Protected Overrides Sub OnResize(e As EventArgs)
        MyBase.OnResize(e)
        Me.Invalidate()
    End Sub
End Class
