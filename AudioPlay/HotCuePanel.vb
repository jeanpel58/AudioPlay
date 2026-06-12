Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Contrôle visuel pour gérer les hotcues (8 boutons)
''' </summary>
Public Class HotCuePanel
    Inherits Panel

    Private hotcueButtons() As Button
    Private hotcueManager As HotCueManager
    Private Const BUTTON_COUNT As Integer = 8

    Public Event HotCueTriggered(index As Integer)
    Public Event HotCueSet(index As Integer, position As TimeSpan)
    Public Event HotCueDeleted(index As Integer)

    Public Sub New()
        Me.Size = New Size(400, 60)
        Me.BackColor = Color.FromArgb(30, 30, 30)

        InitializeButtons()
    End Sub

    Private Sub InitializeButtons()
        ReDim hotcueButtons(BUTTON_COUNT - 1)

        Dim buttonWidth As Integer = 45
        Dim buttonHeight As Integer = 45
        Dim spacing As Integer = 5
        Dim startX As Integer = 5
        Dim startY As Integer = 7

        For i As Integer = 0 To BUTTON_COUNT - 1
            Dim btn As New Button()
            btn.Size = New Size(buttonWidth, buttonHeight)
            btn.Location = New Point(startX + (i * (buttonWidth + spacing)), startY)
            btn.Text = (i + 1).ToString()
            btn.Tag = i + 1
            btn.FlatStyle = FlatStyle.Flat
            btn.FlatAppearance.BorderSize = 1
            btn.FlatAppearance.BorderColor = Color.Gray
            btn.BackColor = Color.FromArgb(60, 60, 60)
            btn.ForeColor = Color.White
            btn.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold)

            AddHandler btn.Click, AddressOf HotCueButton_Click
            AddHandler btn.MouseDown, AddressOf HotCueButton_MouseDown

            hotcueButtons(i) = btn
            Me.Controls.Add(btn)
        Next
    End Sub

    ''' <summary>
    ''' Définit le gestionnaire de hotcues
    ''' </summary>
    Public Sub SetHotCueManager(manager As HotCueManager)
        hotcueManager = manager
        RefreshDisplay()
    End Sub

    ''' <summary>
    ''' Rafraîchit l'affichage des boutons selon les hotcues définis
    ''' </summary>
    Public Sub RefreshDisplay()
        If hotcueManager Is Nothing Then Return

        For i As Integer = 0 To BUTTON_COUNT - 1
            Dim index As Integer = i + 1
            Dim btn As Button = hotcueButtons(i)
            Dim hotcue As HotCue = hotcueManager.GetHotCue(index)

            If hotcue IsNot Nothing Then
                ' Hotcue défini
                btn.BackColor = hotcue.Color
                btn.ForeColor = Color.White
                btn.Text = index.ToString()
            Else
                ' Hotcue non défini
                btn.BackColor = Color.FromArgb(60, 60, 60)
                btn.ForeColor = Color.Gray
                btn.Text = index.ToString()
            End If
        Next
    End Sub

    ''' <summary>
    ''' Clic gauche : déclencher le hotcue
    ''' </summary>
    Private Sub HotCueButton_Click(sender As Object, e As EventArgs)
        Dim btn As Button = CType(sender, Button)
        Dim index As Integer = CInt(btn.Tag)

        If hotcueManager IsNot Nothing AndAlso hotcueManager.HasHotCue(index) Then
            RaiseEvent HotCueTriggered(index)
        End If
    End Sub

    ''' <summary>
    ''' Clic droit : supprimer le hotcue / Shift+Clic : définir
    ''' </summary>
    Private Sub HotCueButton_MouseDown(sender As Object, e As MouseEventArgs)
        Dim btn As Button = CType(sender, Button)
        Dim index As Integer = CInt(btn.Tag)

        If e.Button = MouseButtons.Right Then
            ' Supprimer le hotcue
            If hotcueManager IsNot Nothing AndAlso hotcueManager.HasHotCue(index) Then
                hotcueManager.RemoveHotCue(index)
                RaiseEvent HotCueDeleted(index)
                RefreshDisplay()
            End If
        ElseIf e.Button = MouseButtons.Left AndAlso Control.ModifierKeys = Keys.Shift Then
            ' Définir un nouveau hotcue (Shift+Clic)
            ' L'événement sera géré par le formulaire parent qui connaît la position actuelle
            RaiseEvent HotCueSet(index, TimeSpan.Zero) ' Position sera mise à jour par le parent
        End If
    End Sub

    ''' <summary>
    ''' Efface tous les hotcues visuellement
    ''' </summary>
    Public Sub ClearAll()
        If hotcueManager IsNot Nothing Then
            hotcueManager.ClearAll()
        End If
        RefreshDisplay()
    End Sub
End Class
