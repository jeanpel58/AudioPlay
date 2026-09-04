Public Class FormEditTracks

    ' API Windows pour enlever le bouton X
    Private Const SC_CLOSE As Integer = &HF060
    Private Const MF_BYCOMMAND As Integer = &H0

    <System.Runtime.InteropServices.DllImport("user32.dll")>
    Private Shared Function GetSystemMenu(hWnd As IntPtr, bRevert As Boolean) As IntPtr
    End Function

    <System.Runtime.InteropServices.DllImport("user32.dll")>
    Private Shared Function RemoveMenu(hMenu As IntPtr, uPosition As UInteger, uFlags As UInteger) As Boolean
    End Function

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        Dim hMenu As IntPtr = GetSystemMenu(Me.Handle, False)
        RemoveMenu(hMenu, SC_CLOSE, MF_BYCOMMAND)
    End Sub

    ' Référence vers le ListView de FormCompresser
    Private listViewCompressReference As ListView
    Private pisteActuelle As Integer = 0 ' Index de la piste actuelle (0-based)

    ''' <summary>
    ''' Gestionnaire de chargement du formulaire
    ''' </summary>
    Private Sub FormEditTracks_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Garder le formulaire au-dessus de Form1
        Me.TopMost = True
        System.Diagnostics.Debug.WriteLine("[FormEditTracks] TopMost activé")

        ' Appliquer le thème au formulaire
        ThemeManager.ApplyThemeToForm(Me)
        System.Diagnostics.Debug.WriteLine("[FormEditTracks] Thème appliqué")

        ' Appliquer les traductions
        AppliquerTraductions()
    End Sub

    ''' <summary>
    ''' Applique les traductions aux contrôles du formulaire
    ''' </summary>
    Private Sub AppliquerTraductions()
        ' Titre du formulaire
        Me.Text = LanguageManager.GetString("EditTracks_FormTitle")

        ' Labels
        Label_PisteText.Text = LanguageManager.GetString("EditTracks_TrackLabel")
        Label_TitreText.Text = LanguageManager.GetString("EditTracks_TitleLabel")
        Label_ArtisteText.Text = LanguageManager.GetString("EditTracks_ArtistLabel")

        ' Boutons
        Button_PistePrecedente.Text = LanguageManager.GetString("EditTracks_ButtonPrevious")
        Button_PisteSuivante.Text = LanguageManager.GetString("EditTracks_ButtonNext")
        ButtonQuitter.Text = LanguageManager.GetString("EditTracks_ButtonQuit")
    End Sub

    ''' <summary>
    ''' Initialise le formulaire avec la référence au ListView
    ''' </summary>
    Public Sub InitialiserAvecListView(listView As ListView)
        listViewCompressReference = listView
        If listView IsNot Nothing AndAlso listView.Items.Count > 0 Then
            pisteActuelle = 0
            ChargerPiste(0)
        End If
    End Sub

    ''' <summary>
    ''' Charge les données de la piste spécifiée
    ''' </summary>
    Private Sub ChargerPiste(index As Integer)
        Try
            If listViewCompressReference Is Nothing OrElse index < 0 OrElse index >= listViewCompressReference.Items.Count Then
                Return
            End If

            Dim item = listViewCompressReference.Items(index)

            ' Label_PisteNumber affiche le numéro de piste (index + 1)
            Label_PisteNumber.Text = (index + 1).ToString()

            ' Charger le titre (SubItems(1) = colonne Titre)
            If item.SubItems.Count > 1 Then
                TextBoxTitre.Text = item.SubItems(1).Text
            Else
                TextBoxTitre.Text = ""
            End If

            ' Charger l'artiste (SubItems(2) = colonne Artiste)
            If item.SubItems.Count > 2 Then
                TextBoxArtiste.Text = item.SubItems(2).Text
            Else
                TextBoxArtiste.Text = ""
            End If

            ' Activer/désactiver les boutons selon la position
            Button_PistePrecedente.Enabled = (index > 0)
            Button_PisteSuivante.Enabled = (index < listViewCompressReference.Items.Count - 1)

            System.Diagnostics.Debug.WriteLine($"[FormEditTracks] Piste {index + 1} chargée - Titre: {TextBoxTitre.Text}, Artiste: {TextBoxArtiste.Text}")
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormEditTracks] Erreur ChargerPiste: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Sauvegarde les modifications de la piste actuelle dans le ListView
    ''' </summary>
    Private Sub SauvegarderPiste()
        Try
            If listViewCompressReference Is Nothing OrElse pisteActuelle < 0 OrElse pisteActuelle >= listViewCompressReference.Items.Count Then
                Return
            End If

            Dim item = listViewCompressReference.Items(pisteActuelle)

            ' Mettre à jour le titre (SubItems(1))
            If item.SubItems.Count > 1 Then
                item.SubItems(1).Text = TextBoxTitre.Text.Trim()
            End If

            ' Mettre à jour l'artiste (SubItems(2))
            If item.SubItems.Count > 2 Then
                item.SubItems(2).Text = TextBoxArtiste.Text.Trim()
            End If

            System.Diagnostics.Debug.WriteLine($"[FormEditTracks] Piste {pisteActuelle + 1} sauvegardée - Titre: {TextBoxTitre.Text}, Artiste: {TextBoxArtiste.Text}")
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormEditTracks] Erreur SauvegarderPiste: {ex.Message}")
        End Try
    End Sub

    Private Sub Button_EffaceTitre_Click(sender As Object, e As EventArgs) Handles Button_EffaceTitre.Click
        TextBoxTitre.Text = ""
    End Sub

    Private Sub Button_EffaceArtiste_Click(sender As Object, e As EventArgs) Handles Button_EffaceArtiste.Click
        TextBoxArtiste.Text = ""
    End Sub

    Private Sub Button_PistePrecedente_Click(sender As Object, e As EventArgs) Handles Button_PistePrecedente.Click
        ' Sauvegarder la piste actuelle
        SauvegarderPiste()

        ' Aller à la piste précédente
        If pisteActuelle > 0 Then
            pisteActuelle -= 1
            ChargerPiste(pisteActuelle)
        End If
    End Sub

    Private Sub Button_PisteSuivante_Click(sender As Object, e As EventArgs) Handles Button_PisteSuivante.Click
        ' Sauvegarder la piste actuelle
        SauvegarderPiste()

        ' Aller à la piste suivante
        If listViewCompressReference IsNot Nothing AndAlso pisteActuelle < listViewCompressReference.Items.Count - 1 Then
            pisteActuelle += 1
            ChargerPiste(pisteActuelle)
        End If
    End Sub

    Private Sub ButtonQuitter_Click(sender As Object, e As EventArgs) Handles ButtonQuitter.Click
        ' Sauvegarder la piste actuelle avant de quitter
        SauvegarderPiste()

        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    ''' <summary>
    ''' Gestionnaire de fermeture du formulaire
    ''' </summary>
    Private Sub FormEditTracks_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        ' Désactiver TopMost avant de fermer
        Me.TopMost = False
        System.Diagnostics.Debug.WriteLine("[FormEditTracks] TopMost désactivé lors de la fermeture")
    End Sub
End Class
