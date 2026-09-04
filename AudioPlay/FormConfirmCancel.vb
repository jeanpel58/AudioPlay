Imports System.Windows.Forms
Imports System.Drawing

Public Class FormConfirmCancel
    Inherits Form

    Private labelMessage As Label
    Private buttonYes As Button
    Private buttonNo As Button

    Public Sub New(message As String, title As String)
        MyBase.New()
        InitializeComponent()
        Me.Text = title
        Me.labelMessage.Text = message
    End Sub

    Private Sub InitializeComponent()
        Me.labelMessage = New Label()
        Me.buttonYes = New Button()
        Me.buttonNo = New Button()

        Me.SuspendLayout()
        ' 
        ' labelMessage
        ' 
        Me.labelMessage.AutoSize = False
        Me.labelMessage.Location = New Point(12, 12)
        Me.labelMessage.Size = New Size(360, 80)
        Me.labelMessage.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        Me.labelMessage.TextAlign = ContentAlignment.MiddleLeft
        Me.labelMessage.BackColor = Color.Transparent
        ' 
        ' buttonYes
        ' 
        Me.buttonYes.Location = New Point(200, 100)
        Me.buttonYes.Size = New Size(80, 28)
        Me.buttonYes.TabIndex = 1
        Me.buttonYes.Text = "Yes"
        Me.buttonYes.DialogResult = DialogResult.Yes
        Me.buttonYes.UseVisualStyleBackColor = True
        ' 
        ' buttonNo
        ' 
        Me.buttonNo.Location = New Point(292, 100)
        Me.buttonNo.Size = New Size(80, 28)
        Me.buttonNo.TabIndex = 2
        Me.buttonNo.Text = "No"
        Me.buttonNo.DialogResult = DialogResult.No
        Me.buttonNo.UseVisualStyleBackColor = True
        ' 
        ' FormConfirmCancel
        ' 
        Me.ClientSize = New Size(384, 140)
        Me.Controls.Add(Me.labelMessage)
        Me.Controls.Add(Me.buttonYes)
        Me.Controls.Add(Me.buttonNo)
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.ShowIcon = False
        Me.ShowInTaskbar = False
        Me.StartPosition = FormStartPosition.CenterParent
        Me.AcceptButton = Me.buttonYes
        Me.CancelButton = Me.buttonNo
        Me.TopMost = False
        Me.ResumeLayout(False)
    End Sub

    Public Sub ApplyTheme(fromForm As Form)
        Try
            If fromForm Is Nothing Then Return
            Me.BackColor = fromForm.BackColor
            Me.ForeColor = fromForm.ForeColor
            For Each c As Control In Me.Controls
                c.ForeColor = fromForm.ForeColor
                If TypeOf c Is Button Then
                    Dim b = DirectCast(c, Button)
                    b.BackColor = ControlPaint.Light(fromForm.BackColor)
                    b.FlatStyle = FlatStyle.Standard
                End If
                If TypeOf c Is Label Then
                    Dim l = DirectCast(c, Label)
                    l.BackColor = Color.Transparent
                End If
            Next
        Catch
        End Try
    End Sub

    Public Sub LocalizeButtons()
        Try
            ' Use ResourceManager via My.Resources if available, otherwise keep existing text
            Dim yes As String = Nothing
            Dim no As String = Nothing
            Try
                yes = LanguageManager.GetString("Compressor_CancelConfirmYes")
            Catch
            End Try
            Try
                no = LanguageManager.GetString("Compressor_CancelConfirmNo")
            Catch
            End Try
            If Not String.IsNullOrEmpty(yes) Then Me.buttonYes.Text = yes
            If Not String.IsNullOrEmpty(no) Then Me.buttonNo.Text = no
        Catch
        End Try
    End Sub
End Class
