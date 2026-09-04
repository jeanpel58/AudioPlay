Imports System.IO
Imports System.IO.Pipes
Imports System.Threading
Imports NAudio.Wave
Imports NAudio.Wave.SampleProviders
Imports SoundTouchSharp

Imports Microsoft.Win32

Public Class Form1
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

    Private toolTipForm1 As ToolTip = Nothing

    Private Version As String = "1.26.08.20"
    Private VersionChiffre As String = "1260820"

    ' === Volume global partagé ===
    Public Shared VolumeLecture As Integer = 50
    ' === Instance unique Mutex + NamedPipe ===
    Private ReadOnly MutexName As String = "Global\AudioPlay2026_Mutex"
    Private ReadOnly PipeName As String = "AudioPlay2026_Pipe"
    Private instanceMutex As Mutex = Nothing
    Private isFirstInstance As Boolean = False
    Private pipeServerThread As Thread = Nothing
    Private isPipeServerRunning As Boolean = False

    Private isMuted As Boolean = False
    Private menuAjoutOuvert As Boolean = False
    Private menuPlaylistOuvert As Boolean = False
    ' UI status strip created at runtime for metadata progress
    Private StatusStrip1 As StatusStrip = Nothing

    ' === Variables pour la fonctionnalité Loop (I-O) ===
    Private loopEnabled As Boolean = False
    Private loopStartPosition As TimeSpan = TimeSpan.Zero
    Private loopEndPosition As TimeSpan = TimeSpan.Zero
    Private hasLoopMarkers As Boolean = False
    Private labelLoopStart As Label = Nothing
    Private labelLoopEnd As Label = Nothing
    ' Labels pour l'échelle du TrackBar (0..100)
    Private trackBarScaleLabels As List(Of Label) = Nothing
    Private trackBarMinorTicks As List(Of Panel) = Nothing
    Private Sub Button_Parametres_Click(sender As Object, e As EventArgs) Handles Button_Parametres.Click
        Dim dlg As New FormParametres()
        dlg.ShowDialog(Me)

        ' Ne recharger et appliquer que si le formulaire n'a pas été fermé par un changement de mode
        If Not Me.IsDisposed AndAlso Not Me.Disposing Then
            ' Recharger les paramètres applicatifs (parametres.txt)
            ' Volume/Basses/Aigues ne sont PAS affectés car ils sont dans Son_Ajustement.txt
            ChargerParametres()

            ' Appliquer les paramètres rechargés aux contrôles UI
            AppliquerParametresAuxControles()

            ' Rafraîchir la langue pour mettre à jour tous les textes et tooltips
            RefreshLanguage()

            ' Abonner un handler inline pour rafraîchir automatiquement la langue
            Try
                AddHandler LanguageManager.LanguageChanged, Sub(c)
                                                                Try
                                                                    If Me.InvokeRequired Then
                                                                        Me.BeginInvoke(New Action(AddressOf RefreshLanguage))
                                                                    Else
                                                                        RefreshLanguage()
                                                                    End If
                                                                Catch
                                                                End Try
                                                            End Sub
            Catch
            End Try

            ' Appliquer le thème actuel (peut avoir changé dans FormParametres)
            ThemeManager.ApplyThemeToForm(Me)

            ' Mettre à jour la couleur des marqueurs de boucle au cas où le thème a changé
            MettreAJourCouleurMarqueursLoop()
            ListView1.Focus()
        End If
    End Sub

    ''' <summary>
    ''' Lit AudioDefaut.txt et applique les associations pour l'utilisateur courant (HKCU)
    ''' </summary>
    Private Sub AssurerAssociationsAudioParDefaut()
        Try
            Dim mp3 As Boolean = False
            Dim flac As Boolean = False
            Dim wma As Boolean = False
            Dim wav As Boolean = False
            Dim aac As Boolean = False
            AudioDefautManager.ChargerAudioDefaut(mp3, flac, wma, wav, aac)

            If mp3 Then RegisterProgForExtension(".mp3", "AudioPlay.mp3")
            If flac Then RegisterProgForExtension(".flac", "AudioPlay.flac")
            If wma Then RegisterProgForExtension(".wma", "AudioPlay.wma")
            If wav Then RegisterProgForExtension(".wav", "AudioPlay.wav")
            If aac Then RegisterProgForExtension(".aac", "AudioPlay.aac")
        Catch
        End Try
    End Sub

    ''' <summary>
    ''' Tente d'appeler IApplicationAssociationRegistration::SetAppAsDefault via COM (late-bound).
    ''' Retourne True si l'appel a été effectué sans lever d'exception.
    ''' </summary>
    Private Function TrySetAppAsDefault(progId As String, setValue As String) As Boolean
        Try
            Dim comType As Type = Nothing
            Try
                comType = Type.GetTypeFromProgID("ApplicationAssociationRegistration")
            Catch
            End Try

            If comType Is Nothing Then
                Try
                    comType = Type.GetTypeFromCLSID(New Guid("591209c7-767b-42b2-9fba-44ee4615f2c7"))
                Catch
                    comType = Nothing
                End Try
            End If

            If comType Is Nothing Then Return False

            Dim comObj = Activator.CreateInstance(comType)
            If comObj Is Nothing Then Return False

            Dim mi = comType.GetMethod("SetAppAsDefault")
            If mi Is Nothing Then
                mi = comObj.GetType().GetMethod("SetAppAsDefault")
            End If
            If mi Is Nothing Then Return False

            ' Paramètres: pszAppRegistryName, pszSet, at (association type)
            ' Utiliser le progId pour le nom de l'application et pour la valeur à définir
            ' pszAppRegistryName = progId, pszSet = extension or association (setValue), at = association type (0 = file extension)
            mi.Invoke(comObj, New Object() {progId, setValue, 0})
            Return True
        Catch
            Return False
        End Try
    End Function

    Private Sub RegisterProgForExtension(extension As String, progId As String)
        Try
            Dim exePath = Application.ExecutablePath
            ' Créer/mettre à jour le ProgID
            Using progKey = Registry.CurrentUser.CreateSubKey($"Software\Classes\{progId}")
                progKey.SetValue("", $"AudioPlay file ({extension})")
                Using iconKey = progKey.CreateSubKey("DefaultIcon")
                    iconKey.SetValue("", """" & exePath & """,0")
                End Using
                Using cmdKey = progKey.CreateSubKey("shell\open\command")
                    cmdKey.SetValue("", """" & exePath & """ ""%1""")
                End Using
            End Using

            ' Associer l'extension
            Using extKey = Registry.CurrentUser.CreateSubKey($"Software\Classes\{extension}")
                extKey.SetValue("", progId)
            End Using

            ' Ajouter à OpenWithProgids
            Using ow = Registry.CurrentUser.CreateSubKey($"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{extension}\OpenWithProgids")
                ow.SetValue(progId, String.Empty, RegistryValueKind.String)
            End Using
            ' Essayer de définir l'application par défaut via l'API COM (SetAppAsDefault)
            Try
                TrySetAppAsDefault(progId, extension)
            Catch
            End Try
        Catch
        End Try
    End Sub

    ' Basculer en mode DJ : afficher FormDJ et rendre Form1 invisible
    Private Sub ButtonModeDJ_Click(sender As Object, e As EventArgs) Handles ButtonModeDJ.Click


        Try
            Dim djForm As New FormDJ()
            Try
                djForm.Show(Me)
            Catch ex As Exception
                Try
                    djForm.Show()
                Catch ex2 As Exception
                    MessageBox.Show(String.Format(LanguageManager.GetString("Form1_OpenFormDJ_Error_Message"), ex2.Message), LanguageManager.GetString("Form1_OpenFormDJ_Error_Title"), MessageBoxButtons.OK, MessageBoxIcon.Error)
                    Return
                End Try
            End Try
            ' Masquer la form principale pour mode DJ
            Try
                Me.Hide()
            Catch ex As Exception
                ' afficher erreur si masquage échoue
                Try
                    MessageBox.Show(String.Format(LanguageManager.GetString("Form1_HideError_Message"), ex.Message), LanguageManager.GetString("Form1_HideError_Title"), MessageBoxButtons.OK, MessageBoxIcon.Error)
                Catch
                End Try
            End Try
        Catch ex As Exception
            Try
                MessageBox.Show(String.Format(LanguageManager.GetString("Form1_SwitchToDJ_Error_Message"), ex.Message), LanguageManager.GetString("Form1_SwitchToDJ_Error_Title"), MessageBoxButtons.OK, MessageBoxIcon.Error)
            Catch
            End Try
        End Try
    End Sub

    ' Cancellation token source for background metadata processing
    Private metadataCancellationTokenSource As Threading.CancellationTokenSource = Nothing

    Private Function IsMetadataCancellationRequested() As Boolean
        Try
            Return (metadataCancellationTokenSource IsNot Nothing) AndAlso metadataCancellationTokenSource.IsCancellationRequested
        Catch
            Return False
        End Try
    End Function
    ' Counters for metadata progress across all batches
    Private metadataTotal As Integer = 0
    Private metadataDone As Integer = 0

    ' Initialize a simple progress UI for metadata processing
    Private Sub InitMetadataProgress(totalItems As Integer)
        Try
            metadataTotal = totalItems
            metadataDone = 0
            ' Create a small progress strip label if not present
            If StatusStrip1 Is Nothing Then
                Dim ss As New StatusStrip()
                ss.Name = "StatusStrip1"
                Dim lbl As New ToolStripStatusLabel()
                lbl.Name = "ToolStripStatusLabel_Metadata"
                lbl.Text = ""
                ss.Items.Add(lbl)
                ' Progress bar
                Dim pb As New ToolStripProgressBar()
                pb.Name = "ToolStripProgressBar_Metadata"
                pb.Minimum = 0
                pb.Maximum = Math.Max(1, totalItems)
                pb.Value = 0
                pb.AutoSize = False
                pb.Size = New Size(200, 16)
                ss.Items.Add(pb)
                ' Cancel button
                Dim btn As New ToolStripButton()
                btn.Name = "ToolStripButton_MetadataCancel"
                btn.Text = "Annuler"
                AddHandler btn.Click, Sub()
                                          Try
                                              RequestCancelMetadataProcessing()
                                              btn.Enabled = False
                                          Catch
                                          End Try
                                      End Sub
                ss.Items.Add(btn)
                Me.Controls.Add(ss)
                ss.BringToFront()
                StatusStrip1 = ss
            End If

            Dim label = CType(StatusStrip1.Items.OfType(Of ToolStripItem)().FirstOrDefault(Function(it) it.Name = "ToolStripStatusLabel_Metadata"), ToolStripStatusLabel)
            If label Is Nothing Then
                label = New ToolStripStatusLabel("ToolStripStatusLabel_Metadata")
                StatusStrip1.Items.Add(label)
            End If
            label.Text = String.Format("Chargement playlist: 0/{0}", metadataTotal)
            Try
                Dim pb = CType(StatusStrip1.Items.OfType(Of ToolStripItem)().FirstOrDefault(Function(it) it.Name = "ToolStripProgressBar_Metadata"), ToolStripProgressBar)
                If pb IsNot Nothing Then
                    pb.Maximum = Math.Max(1, metadataTotal)
                    pb.Value = 0
                End If
            Catch
            End Try
        Catch
        End Try
    End Sub

    ' Update metadata progress label
    Private Sub UpdateMetadataProgress(done As Integer, total As Integer)
        Try
            If StatusStrip1 Is Nothing Then Return
            Dim label = CType(StatusStrip1.Items.OfType(Of ToolStripItem)().FirstOrDefault(Function(it) it.Name = "ToolStripStatusLabel_Metadata"), ToolStripStatusLabel)
            If label Is Nothing Then Return
            label.Text = String.Format("Chargement playlist: {0}/{1}", done, total)
            Try
                Dim pb = CType(StatusStrip1.Items.OfType(Of ToolStripItem)().FirstOrDefault(Function(it) it.Name = "ToolStripProgressBar_Metadata"), ToolStripProgressBar)
                If pb IsNot Nothing Then
                    pb.Value = Math.Min(pb.Maximum, done)
                End If
            Catch
            End Try
            ' If finished or cancelled, remove the progress UI
            Try
                If done >= total OrElse IsMetadataCancellationRequested() Then
                    If Not Me.IsDisposed AndAlso Me.IsHandleCreated Then
                        Me.BeginInvoke(Sub()
                                           Try
                                               If StatusStrip1 IsNot Nothing Then
                                                   Try
                                                       Me.Controls.Remove(StatusStrip1)
                                                       StatusStrip1.Dispose()
                                                   Catch
                                                   End Try
                                                   StatusStrip1 = Nothing
                                               End If
                                           Catch
                                           End Try
                                           metadataTotal = 0
                                           metadataDone = 0
                                       End Sub)
                    End If
                End If
            Catch
            End Try
        Catch
        End Try
    End Sub

    ' Request cancellation for metadata processing
    Private Sub RequestCancelMetadataProcessing()
        Try
            If metadataCancellationTokenSource IsNot Nothing Then
                Try
                    metadataCancellationTokenSource.Cancel()
                Catch
                End Try
            End If
        Catch
        End Try
    End Sub

    Private Sub ChargerPlaylistEnArrierePlan()
        Try
            Dim t As New Threading.Thread(Sub()
                                              Try
                                                  ' Charger la playlist en utilisant la méthode existante mais
                                                  ' sans bloquer le thread UI. On collecte d'abord les entrées
                                                  ' puis on ajoute en batch sur le thread UI pour rester réactif.
                                                  Dim dossier = Path.Combine(
                                                      Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                                      "AudioPlay")
                                                  Dim fichierPlaylist = Path.Combine(dossier, "playlist.txt")

                                                  ' Si le fichier principal n'existe pas ou est vide, essayer le backup
                                                  If Not File.Exists(fichierPlaylist) OrElse New FileInfo(fichierPlaylist).Length = 0 Then
                                                      Dim fichierBackup = Path.Combine(dossier, "playlist.txt.bak")
                                                      If File.Exists(fichierBackup) AndAlso New FileInfo(fichierBackup).Length > 0 Then
                                                          Try
                                                              File.Copy(fichierBackup, fichierPlaylist, True)
                                                          Catch
                                                              ' Si la copie échoue, utiliser directement le backup
                                                              fichierPlaylist = fichierBackup
                                                          End Try
                                                      Else
                                                          Return
                                                      End If
                                                  End If

                                                  Dim lignes = File.ReadAllLines(fichierPlaylist)
                                                  Dim entries As New List(Of Tuple(Of String, String, String, String))()
                                                  For Each ligne In lignes
                                                      If String.IsNullOrWhiteSpace(ligne) Then Continue For
                                                      Dim parties = ligne.Split("|"c)
                                                      If parties.Length >= 2 Then
                                                          Dim chemin = parties(0)
                                                          Dim nom = parties(1)
                                                          Dim bpm = If(parties.Length >= 3, parties(2), "")
                                                          Dim duree = If(parties.Length >= 4, parties(3), "")

                                                          ' Ignorer les pistes CD audio (CDDA://)
                                                          ' L'utilisateur doit les ajouter manuellement à chaque session
                                                          If chemin.StartsWith("CDDA://", StringComparison.OrdinalIgnoreCase) Then
                                                              Continue For
                                                          End If

                                                          ' Ajouter tous les fichiers, même s'ils n'existent pas temporairement
                                                          ' (par exemple, lecteur réseau non monté)
                                                          entries.Add(Tuple.Create(chemin, nom, bpm, duree))
                                                      End If
                                                  Next

                                                  Dim batchSize As Integer = 100
                                                  Dim firstBatchSize As Integer = Math.Min(20, batchSize)
                                                  ' Initiate cancellation token
                                                  Try
                                                      If metadataCancellationTokenSource IsNot Nothing Then
                                                          Try
                                                              metadataCancellationTokenSource.Dispose()
                                                          Catch
                                                          End Try
                                                      End If
                                                      metadataCancellationTokenSource = New Threading.CancellationTokenSource()
                                                  Catch
                                                  End Try
                                                  ' Initialize progress UI avec tous les fichiers (plus de CD dans la liste)
                                                  Dim totalFichiers = entries.Count
                                                  If Not Me.IsDisposed AndAlso Me.IsHandleCreated Then
                                                      Me.BeginInvoke(Sub()
                                                                         Try
                                                                             If totalFichiers > 0 Then
                                                                                 InitMetadataProgress(totalFichiers)
                                                                             End If
                                                                         Catch
                                                                         End Try
                                                                     End Sub)
                                                  End If
                                                  Dim index As Integer = 0
                                                  ' First, push a small first batch to show content quickly
                                                  If entries.Count > 0 Then
                                                      Dim firstBatch = entries.Take(firstBatchSize).ToList()
                                                      If Not Me.IsDisposed AndAlso Me.IsHandleCreated Then
                                                          Me.BeginInvoke(Sub()
                                                                             Try
                                                                                 ListView1.BeginUpdate()
                                                                                 For Each entry In firstBatch
                                                                                     AjouterItemLight(entry.Item1, entry.Item2, entry.Item3, entry.Item4)
                                                                                 Next
                                                                             Catch
                                                                             Finally
                                                                                 Try
                                                                                     ListView1.EndUpdate()
                                                                                 Catch
                                                                                 End Try
                                                                             End Try

                                                                             Try
                                                                                 MettreAJourNumerotation()
                                                                                 ' Plus besoin de filtrer, la liste ne contient que des fichiers normaux
                                                                                 If firstBatch.Count > 0 Then
                                                                                     DemarrerTraitementMetadonneesEnArrierePlan(firstBatch)
                                                                                 End If
                                                                             Catch
                                                                             End Try
                                                                         End Sub)
                                                      End If
                                                      index += firstBatchSize
                                                  End If

                                                  While index < entries.Count
                                                      Dim batch As New List(Of Tuple(Of String, String, String, String))()
                                                      Dim maxIndex As Integer = Math.Min(index + batchSize, entries.Count)
                                                      For i As Integer = index To maxIndex - 1
                                                          batch.Add(entries(i))
                                                      Next

                                                      Try
                                                          If Not Me.IsDisposed AndAlso Me.IsHandleCreated Then
                                                              Me.BeginInvoke(Sub()
                                                                                 Try
                                                                                     ListView1.BeginUpdate()
                                                                                     For Each entry In batch
                                                                                         AjouterItemLight(entry.Item1, entry.Item2, entry.Item3, entry.Item4)
                                                                                     Next
                                                                                 Catch
                                                                                 Finally
                                                                                     Try
                                                                                         ListView1.EndUpdate()
                                                                                     Catch
                                                                                     End Try
                                                                                 End Try

                                                                                 Try
                                                                                     MettreAJourNumerotation()
                                                                                     ' Plus besoin de filtrer, la liste ne contient que des fichiers normaux
                                                                                     If batch.Count > 0 Then
                                                                                         DemarrerTraitementMetadonneesEnArrierePlan(batch)
                                                                                     End If
                                                                                 Catch
                                                                                 End Try
                                                                             End Sub)
                                                          End If
                                                      Catch
                                                      End Try

                                                      Try
                                                          Threading.ThreadPool.QueueUserWorkItem(Sub()
                                                                                                     For Each cacheEntry In batch
                                                                                                         Try
                                                                                                             MetadataCache.GetCached(cacheEntry.Item1)
                                                                                                         Catch
                                                                                                         End Try
                                                                                                     Next
                                                                                                 End Sub)
                                                      Catch
                                                      End Try

                                                      index += batchSize
                                                      Threading.Thread.Sleep(10)
                                                  End While
                                              Catch
                                              End Try
                                          End Sub)
            t.IsBackground = True
            t.Start()
        Catch
        End Try
    End Sub

    ' Ajoute un item léger dans la ListView sans ouverture du fichier audio.
    Private Sub AjouterItemLight(chemin As String, nomFichier As String, bpm As String, duree As String)
        Try
            ' Pour les pistes CD, utiliser le nom sauvegardé dans playlist.txt
            ' (déjà formaté avec icône et traduction correcte)
            ' Pour les fichiers normaux, nomFichier est déjà correct

            Dim newItem As New ListViewItem()
            newItem.Text = ""
            newItem.SubItems.Add(nomFichier)
            newItem.SubItems.Add(If(String.IsNullOrWhiteSpace(bpm), "", bpm))
            newItem.SubItems.Add(If(String.IsNullOrWhiteSpace(duree), "--:--", duree))

            ' Marquer visuellement les fichiers inaccessibles
            Dim estCDAudio = CDAudioManager.EstCheminCDAudio(chemin)
            If Not estCDAudio AndAlso Not File.Exists(chemin) Then
                ' Fichier introuvable : utiliser une couleur atténuée basée sur le thème
                Dim theme = ThemeManager.GetCurrentTheme()
                Dim couleurNormale = theme.ListViewForeColor
                ' Atténuer la couleur : mélanger avec le fond (50% opacité)
                Dim couleurAttenuee = Color.FromArgb(
                    (couleurNormale.R + theme.ListViewBackColor.R) \ 2,
                    (couleurNormale.G + theme.ListViewBackColor.G) \ 2,
                    (couleurNormale.B + theme.ListViewBackColor.B) \ 2
                )
                newItem.ForeColor = couleurAttenuee
                ' Ajouter une indication visuelle dans le nom
                newItem.SubItems(1).Text = "⚠ " & nomFichier
            End If

            Dim tagDict As New Dictionary(Of String, Object) From {
                {"Chemin", chemin}
            }

            ' Détecter si c'est une piste CD pour ajouter les métadonnées
            If estCDAudio Then
                tagDict.Add("TypeSource", "CDAudio")
                Dim cdTrack = CDAudioManager.ParseCheminCDAudio(chemin)
                If cdTrack IsNot Nothing Then
                    tagDict.Add("CDDrive", cdTrack.Drive)
                    tagDict.Add("CDTrackNumber", cdTrack.TrackNumber)
                End If
            End If

            Dim bpmValue As Double = 0
            If Not String.IsNullOrWhiteSpace(bpm) AndAlso Double.TryParse(bpm, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, bpmValue) Then
                tagDict("BPM") = bpmValue
            End If

            newItem.Tag = tagDict
            ListView1.Items.Add(newItem)
        Catch
        End Try
    End Sub

    ' Traite les métadonnées (durée / BPM) en arrière-plan et met à jour l'UI via BeginInvoke.
    Private Sub DemarrerTraitementMetadonneesEnArrierePlan(batchEntries As List(Of Tuple(Of String, String, String, String)))
        Try
            If batchEntries Is Nothing OrElse batchEntries.Count = 0 Then Return

            Dim maxDegree As Integer = Math.Max(1, Math.Min(Environment.ProcessorCount, 4))
            Dim semaphore As New Threading.SemaphoreSlim(maxDegree)

            Dim total = batchEntries.Count
            Dim done As Integer = 0
            For Each entry In batchEntries
                Dim cheminLocal = entry.Item1
                Dim nomLocal = entry.Item2
                Dim bpmExistantLocal = entry.Item3
                Dim dureeExistanteLocal = entry.Item4
                If IsMetadataCancellationRequested() Then
                    Exit For
                End If

                Threading.ThreadPool.QueueUserWorkItem(Sub()
                                                           semaphore.Wait()
                                                           Try
                                                               If String.IsNullOrWhiteSpace(cheminLocal) OrElse Not File.Exists(cheminLocal) Then
                                                                   Dim currentDone = Interlocked.Increment(metadataDone)
                                                                   Try
                                                                       Me.BeginInvoke(Sub() UpdateMetadataProgress(currentDone, metadataTotal))
                                                                   Catch
                                                                   End Try
                                                                   Return
                                                               End If

                                                               Dim needDuree As Boolean = String.IsNullOrWhiteSpace(dureeExistanteLocal) OrElse dureeExistanteLocal = "--:--"
                                                               Dim needBpm As Boolean = String.IsNullOrWhiteSpace(bpmExistantLocal)
                                                               If Not needDuree AndAlso Not needBpm Then
                                                                   Dim currentDone = Interlocked.Increment(metadataDone)
                                                                   Try
                                                                       Me.BeginInvoke(Sub() UpdateMetadataProgress(currentDone, metadataTotal))
                                                                   Catch
                                                                   End Try
                                                                   Return
                                                               End If

                                                               Dim newDuree As String = Nothing
                                                               Dim newBpm As String = Nothing

                                                               If IsMetadataCancellationRequested() Then Return

                                                               If needDuree Then
                                                                   Try
                                                                       Using reader As New AudioFileReader(cheminLocal)
                                                                           Dim ts = reader.TotalTime
                                                                           newDuree = $"{CInt(ts.TotalMinutes):D2}:{ts.Seconds:D2}"
                                                                       End Using
                                                                   Catch
                                                                   End Try
                                                               End If

                                                               If needBpm Then
                                                                   Try
                                                                       Dim bpmMetadata = BPMMetadataManager.LireBPMPrecisDepuisMetadonnees(cheminLocal)
                                                                       If bpmMetadata > 0 Then
                                                                           newBpm = bpmMetadata.ToString("F2", Globalization.CultureInfo.InvariantCulture)
                                                                       End If
                                                                   Catch
                                                                   End Try
                                                               End If

                                                               If String.IsNullOrWhiteSpace(newDuree) AndAlso String.IsNullOrWhiteSpace(newBpm) Then
                                                                   ' Nothing new computed for this item
                                                                   Dim currentDone = Interlocked.Increment(metadataDone)
                                                                   Try
                                                                       Me.BeginInvoke(Sub() UpdateMetadataProgress(currentDone, metadataTotal))
                                                                   Catch
                                                                   End Try
                                                                   Return
                                                               End If

                                                               If Not Me.IsDisposed AndAlso Me.IsHandleCreated Then
                                                                   Me.BeginInvoke(Sub()
                                                                                      Try
                                                                                          Dim targetItem As ListViewItem = Nothing
                                                                                          For Each lvItem As ListViewItem In ListView1.Items
                                                                                              Dim tagChemin As String = ""
                                                                                              If TypeOf lvItem.Tag Is Dictionary(Of String, Object) Then
                                                                                                  Dim existingTag = DirectCast(lvItem.Tag, Dictionary(Of String, Object))
                                                                                                  If existingTag.ContainsKey("Chemin") Then
                                                                                                      tagChemin = existingTag("Chemin")?.ToString()
                                                                                                  End If
                                                                                              ElseIf TypeOf lvItem.Tag Is String Then
                                                                                                  tagChemin = lvItem.Tag.ToString()
                                                                                              End If

                                                                                              If String.Equals(tagChemin, cheminLocal, StringComparison.OrdinalIgnoreCase) Then
                                                                                                  targetItem = lvItem
                                                                                                  Exit For
                                                                                              End If
                                                                                          Next

                                                                                          If targetItem Is Nothing Then Return

                                                                                          If Not String.IsNullOrWhiteSpace(newDuree) Then
                                                                                              targetItem.SubItems(3).Text = newDuree
                                                                                          End If

                                                                                          If Not String.IsNullOrWhiteSpace(newBpm) Then
                                                                                              targetItem.SubItems(2).Text = newBpm

                                                                                              Dim tagDict As Dictionary(Of String, Object)
                                                                                              If TypeOf targetItem.Tag Is Dictionary(Of String, Object) Then
                                                                                                  tagDict = DirectCast(targetItem.Tag, Dictionary(Of String, Object))
                                                                                              Else
                                                                                                  tagDict = New Dictionary(Of String, Object) From {
                                                                                                      {"Chemin", cheminLocal}
                                                                                                  }
                                                                                              End If

                                                                                              Dim bpmValue As Double = 0
                                                                                              If Double.TryParse(newBpm, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, bpmValue) Then
                                                                                                  tagDict("BPM") = bpmValue
                                                                                              End If
                                                                                              targetItem.Tag = tagDict
                                                                                          End If
                                                                                      Catch
                                                                                      End Try
                                                                                  End Sub)
                                                               End If
                                                           Catch
                                                           Finally
                                                               semaphore.Release()
                                                           End Try
                                                       End Sub)
            Next
        Catch
        End Try
    End Sub

    Private Sub Button_Mute_Click(sender As Object, e As EventArgs) Handles Button_Mute.Click
        ' Toggle mute sans modifier dernierVolume ni le TrackBar
        isMuted = Not isMuted

        ' Appliquer le mute au système Windows
        WindowsVolumeControl.SetMute(isMuted)

        If isMuted Then
            ' Mute activé : bouton devient rouge
            If Button_Mute IsNot Nothing Then
                Button_Mute.BackgroundImage = AudioPlay.Resources.AudioPlay_Mute_Rouge
            End If
            Button_Mute.Text = "" ' Image seulement, pas de texte
        Else
            ' Mute désactivé : bouton redevient vert (si lecture en cours) ou gris
            If Button_Mute IsNot Nothing Then
                If lectureEnCours Then
                    Button_Mute.BackgroundImage = AudioPlay.Resources.AudioPlay_Mute_Vert
                Else
                    Button_Mute.BackgroundImage = AudioPlay.Resources.AudioPlay_Mute_Gris
                End If
            End If
            Button_Mute.Text = "" ' Image seulement, pas de texte
        End If

        ' Appliquer le volume : si mute, volume = 0, sinon volume = dernierVolume
        If volumeProvider IsNot Nothing Then
            Dim volumeActuel As Single = If(isMuted, 0.0F, dernierVolume)
            volumeProvider.Volume = volumeActuel * gainNormalisationActuel
        End If

        ' NE PAS toucher au TrackBar - il reste à sa position
    End Sub
    ' === Vérification application par défaut (diagnostic) ===

    ' === Vérification application par défaut Windows 10/11 ===

    ' === Variables de lecture audio ===
    Private lecteur As IWavePlayer = Nothing
    Private fichierAudio As WaveStream = Nothing ' Changé pour supporter CD et fichiers
    Private volumeProvider As VolumeSampleProvider = Nothing
    Private equalizerProvider As SimpleEqualizerProvider = Nothing

    ' === Effets audio ===
    Private reverbProvider As ReverbSampleProvider = Nothing
    Private echoProvider As EchoSampleProvider = Nothing
    Private timeStretchProvider As TimeStretchSampleProvider = Nothing
    Private pitchShiftProvider As PitchShiftSampleProvider = Nothing
    Private phaserProvider As PhaserSampleProvider = Nothing

    ' === Variables d'état ===
    Private cheminActuel As String = ""
    Private modeAleatoire As Boolean = False
    Private enPause As Boolean = False
    Private lectureEnCours As Boolean = False
    Private gardeReentrance As Boolean = False
    Private majTrackBarEnCours As Boolean = False
    Private gainNormalisationActuel As Single = 1.0F ' Gain de normalisation du fichier actuel
    Private dureeReelleActuelle As TimeSpan = TimeSpan.Zero ' Durée réelle du fichier actuel (sans silence final)
    Private tentativesLectureFichier As Integer = 0 ' Compteur pour éviter les boucles infinies avec fichiers manquants

    ' === Variables pour annulation calcul BPM ===
    Private bpmCancellationTokenSource As Threading.CancellationTokenSource = Nothing
    Private calculBPMEnCours As Boolean = False

    ' === Variables pour karaoke CDG ===
    Private formKaraoke As FormKaraoke = Nothing
    Private cheminCDGActuel As String = ""
    Private karaokeModeActif As Boolean = False

    ' === Variable pour LED métronome ===
    Private formLight As FormLight = Nothing
    Private metronomeEnCours As MetronomeProvider = Nothing
    Private metronomeBPM As Double = 0
    Private metronomeNombreBeats As Integer = 0
    Private metronomeDebutTime As DateTime
    Private metronomeTimer As System.Windows.Forms.Timer = Nothing
    Private metronomeBeatsPasses As HashSet(Of Integer) = Nothing
    Private metronomeMillisParBeat As Double = 0
    Private estEnFermeture As Boolean = False ' Indique si le form est en train de se fermer
    Private initialisationEnCours As Boolean = False ' Indique si l'initialisation est en cours

    ' === Timer pour progression ===
    Private WithEvents timerProgression As New System.Windows.Forms.Timer()
    ' Initialisation de l'intervalle dans Form1_Load

    ' === Paramètres persistants ===
    Public Shared repertoireParDefaut As String = ""
    ' Derniers répertoires utilisés (persistants)
    Public Shared dernierRepertoireAjout As String = ""       ' Button_Ajout (AjouterFichier / AjouterRepertoire)
    Public Shared dernierRepertoireAjoutFichier As String = "" ' Button_Ajout - option Ajouter un fichier
    Public Shared dernierRepertoireAjoutRepertoire As String = "" ' Button_Ajout - option Ajouter un répertoire
    Public Shared premierOuvertureAjoutRepertoire As Boolean = True
    Public Shared dernierRepertoirePlaylist As String = ""    ' Button_Playlist (OuvrirPlaylist / EnregistrerPlaylist)
    Private lectureEnContinu As Boolean = True
    Private dernierVolume As Single = 0.5F  ' Maintenant dans Son_Ajustement.txt
    Private dernieresBasses As Single = 0.0F  ' Maintenant dans Son_Ajustement.txt
    Private dernieresAigues As Single = 0.0F  ' Maintenant dans Son_Ajustement.txt
    Private normalisationVolumeActive As Boolean = True ' Normalisation activée par défaut
    Private metronomeActif As Boolean = False ' Métronome désactivé par défaut
    Private nombreBeatsMetronome As Integer = 4 ' 4 beats par défaut
    Private metronomeSonActif As Boolean = True ' Son du métronome activé par défaut
    Private metronomeLumiereActive As Boolean = True ' Lumière LED activée par défaut

    Private Const ListViewInternalDragFormat As String = "AudioPlay.ListView.InternalMove"

    ' ========================================
    ' DESSIN PERSONNALISÉ DU LISTVIEW (pour garder la sélection bleue)
    ' ========================================
    Private Sub ListView1_OnDrawColumnHeader(sender As Object, e As DrawListViewColumnHeaderEventArgs)
        ' Récupérer le thème actuel
        Dim theme = ThemeManager.GetCurrentTheme()

        ' Dessiner le fond de l'en-tête avec la couleur spécifique (MediumTurquoise par défaut)
        Using brush As New SolidBrush(theme.ListViewHeaderBackColor)
            e.Graphics.FillRectangle(brush, e.Bounds)
        End Using

        ' Dessiner la bordure
        e.Graphics.DrawRectangle(SystemPens.ControlDark, New Rectangle(e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1))

        ' Dessiner le texte centré
        Dim sf As New StringFormat()
        sf.Alignment = StringAlignment.Center
        sf.LineAlignment = StringAlignment.Center

        Using textBrush As New SolidBrush(theme.ListViewHeaderForeColor)
            e.Graphics.DrawString(e.Header.Text, e.Font, textBrush, e.Bounds, sf)
        End Using
    End Sub

    Private Sub ListView1_OnDrawItem(sender As Object, e As DrawListViewItemEventArgs)
        ' Ne rien faire ici, on gère dans DrawSubItem
    End Sub

    Private Sub ListView1_OnDrawSubItem(sender As Object, e As DrawListViewSubItemEventArgs)
        ' Déterminer si cet item est sélectionné
        Dim estSelectionne As Boolean = e.Item.Selected
        Dim theme = ThemeManager.GetCurrentTheme()

        ' Couleur de fond
        Dim couleurFond As Color
        If estSelectionne Then
            couleurFond = theme.ListViewSelectionBackColor
        Else
            couleurFond = e.Item.BackColor
        End If

        ' Couleur de texte
        Dim couleurTexte As Color
        If estSelectionne Then
            couleurTexte = theme.ListViewSelectionForeColor
        Else
            couleurTexte = e.Item.ForeColor
        End If

        ' Dessiner le fond
        Using brush As New SolidBrush(couleurFond)
            e.Graphics.FillRectangle(brush, e.Bounds)
        End Using

        ' Dessiner le texte
        Dim flags As TextFormatFlags = TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or TextFormatFlags.EndEllipsis

        ' Centrer les colonnes #, BPM et Durée
        If e.ColumnIndex = 0 OrElse e.ColumnIndex = 2 OrElse e.ColumnIndex = 3 Then
            flags = TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter
        End If

        TextRenderer.DrawText(e.Graphics, e.SubItem.Text, e.Item.Font, e.Bounds, couleurTexte, flags)

        ' Dessiner les lignes de grille
        Using pen As New Pen(Color.LightGray)
            e.Graphics.DrawLine(pen, e.Bounds.Right - 1, e.Bounds.Top, e.Bounds.Right - 1, e.Bounds.Bottom)
            e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1)
        End Using
    End Sub

    ' ========================================
    ' INITIALISATION DES IMAGES DES BOUTONS
    ' ========================================
    Private Sub InitialiserImagesButtons()
        ' Définir les images de fond pour tous les boutons
        ' Cette méthode est appelée dans Form1_Load pour éviter que le designer 
        ' ne régénère des références incorrectes lors de modifications de l'interface

        ' Bouton Ajout
        If Button_Ajout IsNot Nothing Then
            Button_Ajout.BackgroundImage = AudioPlay.Resources.AudioPlay_Ajout__Gris
            Button_Ajout.BackColor = Color.Transparent
        End If

        ' Bouton Métadonnées
        If Button_InfoSelect IsNot Nothing Then
            Button_InfoSelect.BackgroundImage = AudioPlay.Resources.AudioPlay_Metadonnees__Gris
            Button_InfoSelect.BackColor = Color.Transparent
        End If

        ' Bouton Playlist
        If Button_Playlist IsNot Nothing Then
            Button_Playlist.BackgroundImage = AudioPlay.Resources.AudioPlay_GererListe_Grise
            Button_Playlist.BackColor = Color.Transparent
        End If

        ' Bouton Paramètres
        If Button_Parametres IsNot Nothing Then
            Button_Parametres.BackgroundImage = AudioPlay.Resources.AudioPlay_Parametres_Gris
            Button_Parametres.BackColor = Color.Transparent
        End If

        ' Bouton Précédent
        If Button_Precedent IsNot Nothing Then
            Button_Precedent.BackgroundImage = AudioPlay.Resources.AudioPlay_Precedent_Gris
            Button_Precedent.BackColor = Color.Transparent
        End If

        ' Bouton Suivant
        If Button_Suivant IsNot Nothing Then
            Button_Suivant.BackgroundImage = AudioPlay.Resources.AudioPlay_Suivant_Gris
            Button_Suivant.BackColor = Color.Transparent
        End If

        ' Bouton Jouer
        If Button_Jouer IsNot Nothing Then
            Button_Jouer.BackgroundImage = AudioPlay.Resources.AudioPlay_Jouer_Gris
            Button_Jouer.BackColor = Color.Transparent
        End If

        ' Bouton Arrêter (rouge par défaut quand rien ne joue)
        If Button_Arreter IsNot Nothing Then
            Button_Arreter.BackgroundImage = AudioPlay.Resources.AudioPlay_Arreter_Rouge
            Button_Arreter.BackColor = Color.Transparent
        End If

        ' Bouton Mute
        If Button_Mute IsNot Nothing Then
            Button_Mute.BackgroundImage = AudioPlay.Resources.AudioPlay_Mute_Gris
            Button_Mute.BackColor = Color.Transparent
        End If

        ' Bouton BPM
        If Button_CalculBPM IsNot Nothing Then
            Button_CalculBPM.BackgroundImage = AudioPlay.Resources.AudioPlay_BPM_Gris
            Button_CalculBPM.BackColor = Color.Transparent
        End If

        ' Bouton Aléatoire
        If Button_Aleatoire IsNot Nothing Then
            Button_Aleatoire.BackgroundImage = AudioPlay.Resources.AudioPlay_Random_Gris
            Button_Aleatoire.BackColor = Color.Transparent
        End If

        ' Bouton Pause/Reprise
        If Button_PauseReprise IsNot Nothing Then
            Button_PauseReprise.BackgroundImage = AudioPlay.Resources.AudioPlay_Pause_Gris
            Button_PauseReprise.BackColor = Color.Transparent
        End If

        ' Bouton Loop
        If Button_Loop IsNot Nothing Then
            Button_Loop.BackgroundImage = AudioPlay.Resources.AudioPlay_Loop_Carre_Gris
            Button_Loop.BackColor = Color.Transparent
        End If

        ' Bouton Power
        If Button_Power IsNot Nothing Then
            Button_Power.BackgroundImage = AudioPlay.Resources.AudioPlay_Power_Bleu
            Button_Power.BackColor = Color.Transparent
        End If

        ' Bouton AudioPlay Aide
        If Button_AudioPlay_Aide IsNot Nothing Then
            Button_AudioPlay_Aide.BackgroundImage = AudioPlay.Resources.AudioPlay_Aide_Gris
            Button_AudioPlay_Aide.BackColor = Color.Transparent
        End If

        ' Bouton À Propos
        If Button_APropos IsNot Nothing Then
            Button_APropos.BackgroundImage = AudioPlay.Resources.AudioPlay_Vide__Carré
            Button_APropos.BackColor = Color.Transparent
            Button_APropos.ForeColor = Color.Black
        End If

        ' Bouton Mode DJ (icône)
        If ButtonModeDJ IsNot Nothing Then
            ButtonModeDJ.BackgroundImage = AudioPlay.Resources.AudioPlay_DJMixeur__Gris
            ButtonModeDJ.BackColor = Color.Transparent
        End If

        ' Bouton Aide Loop
        If Button_Loop_Aide IsNot Nothing Then
            Button_Loop_Aide.BackgroundImage = AudioPlay.Resources.AudioPlay_Vide__Carré
            Button_Loop_Aide.BackColor = Color.Transparent
            Button_Loop_Aide.ForeColor = Color.Black
        End If

    End Sub


    ' ========================================
    ' EFFETS DE SURVOL POUR LES BOUTONS
    ' ========================================
    Private Sub InitialiserEffetsSurvol()
        ' Configurer les effets de survol et de clic pour tous les boutons avec images
        ' Gris (normal) -> Vert (survol) -> Rouge (clic)

        ' Bouton Ajout
        ConfigurerSurvol(Button_Ajout, AudioPlay.Resources.AudioPlay_Ajout__Gris, AudioPlay.Resources.AudioPlay_Ajout__Vert, AudioPlay.Resources.AudioPlay_Ajout__Rouge)

        ' Bouton Métadonnées
        ConfigurerSurvol(Button_InfoSelect, AudioPlay.Resources.AudioPlay_Metadonnees__Gris, AudioPlay.Resources.AudioPlay_Metadonnees__Vert, AudioPlay.Resources.AudioPlay_Metadonnees__Rouge)

        ' Bouton Playlist
        ConfigurerSurvol(Button_Playlist, AudioPlay.Resources.AudioPlay_GererListe_Grise, AudioPlay.Resources.AudioPlay_GererListe_Vert, AudioPlay.Resources.AudioPlay_GererListe_Rouge)

        ' Bouton Paramètres
        ConfigurerSurvol(Button_Parametres, AudioPlay.Resources.AudioPlay_Parametres_Gris, AudioPlay.Resources.AudioPlay_Parametres_Vert, AudioPlay.Resources.AudioPlay_Parametres_Rouges)

        ' Bouton Précédent
        ConfigurerSurvol(Button_Precedent, AudioPlay.Resources.AudioPlay_Precedent_Gris, AudioPlay.Resources.AudioPlay_Precedent_Vert, AudioPlay.Resources.AudioPlay_Precedent_Rouge)

        ' Bouton Suivant
        ConfigurerSurvol(Button_Suivant, AudioPlay.Resources.AudioPlay_Suivant_Gris, AudioPlay.Resources.AudioPlay_Suivant_Vert, AudioPlay.Resources.AudioPlay_Suivant_Rouge)

        ' Bouton Jouer
        ConfigurerSurvol(Button_Jouer, AudioPlay.Resources.AudioPlay_Jouer_Gris, AudioPlay.Resources.AudioPlay_Jouer_Vert, AudioPlay.Resources.AudioPlay_Jouer_Rouge)

        ' Bouton Arrêter
        ConfigurerSurvol(Button_Arreter, AudioPlay.Resources.AudioPlay_Arreter_Gris, AudioPlay.Resources.AudioPlay_Arreter_Vert, AudioPlay.Resources.AudioPlay_Arreter_Rouge)

        ' Bouton Pause/Reprise
        ConfigurerSurvol(Button_PauseReprise, AudioPlay.Resources.AudioPlay_Pause_Gris, AudioPlay.Resources.AudioPlay_Pause_Vert, AudioPlay.Resources.AudioPlay_Pause_Rouge)

        ' Bouton Mute
        ConfigurerSurvol(Button_Mute, AudioPlay.Resources.AudioPlay_Mute_Gris, AudioPlay.Resources.AudioPlay_Mute_Vert, AudioPlay.Resources.AudioPlay_Mute_Rouge)

        ' Bouton BPM
        ConfigurerSurvol(Button_CalculBPM, AudioPlay.Resources.AudioPlay_BPM_Gris, AudioPlay.Resources.AudioPlay_BPM_Vert, AudioPlay.Resources.AudioPlay_BPM_Rouge)

        ' Bouton Loop
        ConfigurerSurvol(Button_Loop, AudioPlay.Resources.AudioPlay_Loop_Carre_Gris, AudioPlay.Resources.AudioPlay_Loop_Carre_Vert, AudioPlay.Resources.AudioPlay_Loop_Carre_Rouge)

        ' Bouton Aléatoire
        ConfigurerSurvol(Button_Aleatoire, AudioPlay.Resources.AudioPlay_Random_Gris, AudioPlay.Resources.AudioPlay_Random_Vert, AudioPlay.Resources.AudioPlay_Random_Rouge)

        ' Bouton Power
        ConfigurerSurvol(Button_Power, AudioPlay.Resources.AudioPlay_Power_Bleu, AudioPlay.Resources.AudioPlay_Power_Vert, AudioPlay.Resources.AudioPlay_Power_Rouge)

        ' Bouton AudioPlay Aide
        ConfigurerSurvol(Button_AudioPlay_Aide, AudioPlay.Resources.AudioPlay_Aide_Gris, AudioPlay.Resources.AudioPlay_Aide_Vert, AudioPlay.Resources.AudioPlay_Aide_Rouge)

        ' Bouton Mode DJ
        ConfigurerSurvol(ButtonModeDJ, AudioPlay.Resources.AudioPlay_DJMixeur__Gris, AudioPlay.Resources.AudioPlay_DJMixeur__Vert, AudioPlay.Resources.AudioPlay_DJMixeur__Rouge)

        ' Bouton À Propos (effets sur le texte uniquement)
        ConfigurerSurvolTexte(Button_APropos)
    End Sub

    Private Sub ConfigurerSurvol(bouton As Button, imageNormale As Image, imageSurvol As Image, Optional imageClick As Image = Nothing)
        If bouton Is Nothing Then Return

        ' Gestionnaire MouseEnter : afficher l'image de survol (verte)
        AddHandler bouton.MouseEnter, Sub()
                                          Try
                                              If estEnFermeture Then Return

                                              ' Ne pas changer l'image du Button_Jouer si une lecture est en cours
                                              If bouton Is Button_Jouer AndAlso lectureEnCours Then
                                                  Return
                                              End If
                                              ' Ne pas changer l'image du Button_PauseReprise si une lecture/pause est en cours
                                              If bouton Is Button_PauseReprise AndAlso lectureEnCours Then
                                                  Return
                                              End If
                                              ' Ne pas changer l'image du Button_Mute si une lecture est en cours ou si muté
                                              If bouton Is Button_Mute AndAlso (lectureEnCours OrElse isMuted) Then
                                                  Return
                                              End If
                                              ' Ne pas changer l'image du Button_Arreter si rien ne joue (garder rouge)
                                              If bouton Is Button_Arreter AndAlso Not lectureEnCours Then
                                                  Return
                                              End If
                                              ' Ne pas changer l'image du Button_Ajout si son menu est ouvert (garder vert)
                                              If bouton Is Button_Ajout AndAlso menuAjoutOuvert Then
                                                  Return
                                              End If
                                              ' Ne pas changer l'image du Button_Playlist si son menu est ouvert (garder vert)
                                              If bouton Is Button_Playlist AndAlso menuPlaylistOuvert Then
                                                  Return
                                              End If
                                              ' Ne pas changer l'image du Button_Aleatoire si le mode aléatoire est actif (garder vert)
                                              If bouton Is Button_Aleatoire AndAlso modeAleatoire Then
                                                  Return
                                              End If
                                              ' Ne pas changer l'image du Button_Loop si la boucle est active (garder vert)
                                              If bouton Is Button_Loop AndAlso loopEnabled Then
                                                  Return
                                              End If
                                              bouton.BackgroundImage = imageSurvol
                                          Catch
                                              ' Ignorer les erreurs pendant la fermeture
                                          End Try
                                      End Sub

        ' Gestionnaire MouseLeave : revenir à l'image normale (grise)
        AddHandler bouton.MouseLeave, Sub()
                                          Try
                                              If estEnFermeture Then Return

                                              ' Si Button_Jouer et lecture en cours, rester vert
                                              If bouton Is Button_Jouer AndAlso lectureEnCours Then
                                                  bouton.BackgroundImage = imageSurvol ' Rester vert
                                                  Return
                                              End If
                                              ' Si Button_PauseReprise et lecture en cours, garder la bonne couleur (vert si play, rouge si pause)
                                              If bouton Is Button_PauseReprise AndAlso lectureEnCours Then
                                                  If enPause Then
                                                      bouton.BackgroundImage = AudioPlay.Resources.AudioPlay_Pause_Rouge ' Rester rouge si en pause
                                                  Else
                                                      bouton.BackgroundImage = AudioPlay.Resources.AudioPlay_Pause_Vert ' Rester vert si en lecture
                                                  End If
                                                  Return
                                              End If
                                              ' Si Button_Mute, garder la bonne couleur (rouge si muté, vert si lecture, gris sinon)
                                              If bouton Is Button_Mute Then
                                                  If isMuted Then
                                                      bouton.BackgroundImage = AudioPlay.Resources.AudioPlay_Mute_Rouge ' Rouge si muté
                                                  ElseIf lectureEnCours Then
                                                      bouton.BackgroundImage = AudioPlay.Resources.AudioPlay_Mute_Vert ' Vert si lecture
                                                  Else
                                                      bouton.BackgroundImage = imageNormale ' Gris sinon
                                                  End If
                                                  Return
                                              End If
                                              ' Si Button_Arreter, garder la bonne couleur (vert si lecture, rouge sinon)
                                              If bouton Is Button_Arreter Then
                                                  If lectureEnCours Then
                                                      bouton.BackgroundImage = AudioPlay.Resources.AudioPlay_Arreter_Vert ' Vert si lecture
                                                  Else
                                                      bouton.BackgroundImage = AudioPlay.Resources.AudioPlay_Arreter_Rouge ' Rouge sinon
                                                  End If
                                                  Return
                                              End If
                                              ' Si Button_Ajout et son menu est ouvert, rester vert
                                              If bouton Is Button_Ajout AndAlso menuAjoutOuvert Then
                                                  bouton.BackgroundImage = AudioPlay.Resources.AudioPlay_Ajout__Vert ' Rester vert
                                                  Return
                                              End If
                                              ' Si Button_Playlist et son menu est ouvert, rester vert
                                              If bouton Is Button_Playlist AndAlso menuPlaylistOuvert Then
                                                  bouton.BackgroundImage = AudioPlay.Resources.AudioPlay_GererListe_Vert ' Rester vert
                                                  Return
                                              End If
                                              ' Si Button_Aleatoire et le mode aléatoire est actif, rester vert
                                              If bouton Is Button_Aleatoire AndAlso modeAleatoire Then
                                                  bouton.BackgroundImage = AudioPlay.Resources.AudioPlay_Random_Vert ' Rester vert
                                                  Return
                                              End If
                                              ' Si Button_Loop et la boucle est active, rester vert
                                              If bouton Is Button_Loop AndAlso loopEnabled Then
                                                  bouton.BackgroundImage = AudioPlay.Resources.AudioPlay_Loop_Carre_Vert ' Rester vert
                                                  Return
                                              End If
                                              bouton.BackgroundImage = imageNormale
                                          Catch
                                              ' Ignorer les erreurs pendant la fermeture
                                          End Try
                                      End Sub

        ' Si une image de clic est fournie, gérer MouseDown et MouseUp
        If imageClick IsNot Nothing Then
            ' Gestionnaire MouseDown : afficher l'image de clic (rouge)
            AddHandler bouton.MouseDown, Sub()
                                             Try
                                                 If estEnFermeture Then Return
                                                 bouton.BackgroundImage = imageClick
                                             Catch
                                                 ' Ignorer les erreurs pendant la fermeture
                                             End Try
                                         End Sub

            ' Gestionnaire MouseUp : revenir à l'image de survol (verte) si la souris est toujours dessus
            AddHandler bouton.MouseUp, Sub()
                                           Try
                                               ' Ne rien faire si le form est en train de se fermer
                                               If estEnFermeture Then Return

                                               ' Vérifier si la souris est toujours sur le bouton
                                               Dim mousePos = bouton.PointToClient(Cursor.Position)
                                               If bouton.ClientRectangle.Contains(mousePos) Then
                                                   bouton.BackgroundImage = imageSurvol
                                               Else
                                                   ' Si Button_Jouer et lecture en cours, rester vert
                                                   If bouton Is Button_Jouer AndAlso lectureEnCours Then
                                                       bouton.BackgroundImage = imageSurvol ' Rester vert
                                                       ' Si Button_PauseReprise et lecture en cours, garder la bonne couleur
                                                   ElseIf bouton Is Button_PauseReprise AndAlso lectureEnCours Then
                                                       If enPause Then
                                                           bouton.BackgroundImage = AudioPlay.Resources.AudioPlay_Pause_Rouge ' Rouge si en pause
                                                       Else
                                                           bouton.BackgroundImage = AudioPlay.Resources.AudioPlay_Pause_Vert ' Vert si en lecture
                                                       End If
                                                       ' Si Button_Mute, garder la bonne couleur
                                                   ElseIf bouton Is Button_Mute Then
                                                       If isMuted Then
                                                           bouton.BackgroundImage = AudioPlay.Resources.AudioPlay_Mute_Rouge ' Rouge si muté
                                                       ElseIf lectureEnCours Then
                                                           bouton.BackgroundImage = AudioPlay.Resources.AudioPlay_Mute_Vert ' Vert si lecture
                                                       Else
                                                           bouton.BackgroundImage = imageNormale ' Gris sinon
                                                       End If
                                                       ' Si Button_Arreter, garder la bonne couleur
                                                   ElseIf bouton Is Button_Arreter Then
                                                       If lectureEnCours Then
                                                           bouton.BackgroundImage = AudioPlay.Resources.AudioPlay_Arreter_Vert ' Vert si lecture
                                                       Else
                                                           bouton.BackgroundImage = AudioPlay.Resources.AudioPlay_Arreter_Rouge ' Rouge sinon
                                                       End If
                                                       ' Si Button_Aleatoire, garder la bonne couleur
                                                   ElseIf bouton Is Button_Aleatoire Then
                                                       If modeAleatoire Then
                                                           bouton.BackgroundImage = AudioPlay.Resources.AudioPlay_Random_Vert ' Vert si mode aléatoire actif
                                                       Else
                                                           bouton.BackgroundImage = imageNormale ' Gris sinon
                                                       End If
                                                   Else
                                                       bouton.BackgroundImage = imageNormale
                                                   End If
                                               End If
                                           Catch ex As Exception
                                               ' Ignorer les erreurs pendant la fermeture
                                           End Try
                                       End Sub
        End If
    End Sub

    Private Sub ConfigurerSurvolTexte(bouton As Button)
        ' Configuration spéciale pour les boutons avec texte et image de fond fixe
        ' Le background reste le même, seule la couleur du texte change
        If bouton Is Nothing Then Return

        ' État normal : texte noir
        bouton.ForeColor = Color.Black

        ' Gestionnaire MouseEnter : texte reste noir
        AddHandler bouton.MouseEnter, Sub()
                                          Try
                                              If estEnFermeture Then Return
                                              bouton.ForeColor = Color.Black
                                          Catch
                                              ' Ignorer les erreurs pendant la fermeture
                                          End Try
                                      End Sub

        ' Gestionnaire MouseLeave : texte noir
        AddHandler bouton.MouseLeave, Sub()
                                          Try
                                              If estEnFermeture Then Return
                                              bouton.ForeColor = Color.Black
                                          Catch
                                              ' Ignorer les erreurs pendant la fermeture
                                          End Try
                                      End Sub

        ' Gestionnaire MouseDown : texte rouge
        AddHandler bouton.MouseDown, Sub()
                                         Try
                                             If estEnFermeture Then Return
                                             bouton.ForeColor = Color.Red
                                         Catch
                                             ' Ignorer les erreurs pendant la fermeture
                                         End Try
                                     End Sub

        ' Gestionnaire MouseUp : revenir au texte vert lime si la souris est toujours dessus, sinon noir
        AddHandler bouton.MouseUp, Sub()
                                       Try
                                           If estEnFermeture Then Return

                                           ' Vérifier si la souris est toujours sur le bouton
                                           Dim mousePos = bouton.PointToClient(Cursor.Position)
                                           If bouton.ClientRectangle.Contains(mousePos) Then
                                               bouton.ForeColor = Color.Lime ' Hover
                                           Else
                                               bouton.ForeColor = Color.Black ' Normal
                                           End If
                                       Catch
                                           ' Ignorer les erreurs pendant la fermeture
                                       End Try
                                   End Sub
    End Sub

    ' ========================================
    ' FORM LOAD
    ' ========================================
    Private Async Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' === Charger les paramètres et effectuer un nettoyage des temporaires restants au démarrage ===
        Try
            ' Charger rapidement les paramètres pour connaître les chemins à nettoyer
            Dim fp As New FormParametres()
            Try
                fp.ChargerParametresAvantDemarrage()
            Catch
            End Try

            ' Nettoyer les dossiers .AudioPlayTmp_* dans les emplacements connus
            Try
                ParametresGlobaux.SupprimerTempRestantsDans(ParametresGlobaux.repertoireParDefaut)
            Catch
            End Try
            Try
                ParametresGlobaux.SupprimerTempRestantsDans(ParametresGlobaux.dernierRepertoireAjoutRepertoire)
            Catch
            End Try
            Try
                ParametresGlobaux.SupprimerTempRestantsDans(ParametresGlobaux.dernierRepertoireAjoutRepertoire_DJ)
            Catch
            End Try
        Catch
        End Try

        ' === Ne pas tronquer le fichier de diagnostics au démarrage de l'application ===
        ' Le log ne doit être réinitialisé que lors d'une extraction (ButtonExtraire).
        ' Ici on ajoute une ligne d'information si possible, sans écraser le contenu existant.
        Try
            Try
                CDAudioAnalyzer.DiagnosticWrite($"APPLICATION STARTUP: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - UI démarrée (pas une session d'extraction){Environment.NewLine}")
                System.Diagnostics.Debug.WriteLine($"[Form1] Diagnostics log append attempted: {CDAudioAnalyzer.DiagnosticsLogPath}")
            Catch ex As Exception
                System.Diagnostics.Debug.WriteLine($"[Form1] Impossible d'écrire dans le log de diagnostics au démarrage: {ex.Message}")
            End Try
        Catch
            ' Ignorer si CDAudioAnalyzer n'est pas disponible pour une raison quelconque
        End Try

        ' === Vérifier le Mode Mixeur DJ ===
        If ParametresGlobaux.ModeMixeurDJ Then
            ' L'utilisateur a activé le Mode DJ : afficher FormDJ au lieu de Form1
            Me.Hide()
            Dim formDJ As New FormDJ()
            formDJ.ShowDialog()
            Me.Close()
            Return
        End If

        ' Indiquer que l'initialisation est en cours (empêche les événements Scroll de sauvegarder)
        ' Connecter les handlers MouseMove pour l'affichage dynamique des ToolTip des TrackBars
        Try
            AddHandler TrackBar_Volume.MouseMove, AddressOf TrackBar_MouseMove_ShowValue
            AddHandler TrackBar_Aigues.MouseMove, AddressOf TrackBar_MouseMove_ShowValue
            AddHandler TrackBar_Basses.MouseMove, AddressOf TrackBar_MouseMove_ShowValue
            AddHandler TrackBar_Avancement.MouseMove, AddressOf TrackBar_MouseMove_ShowValue
        Catch
            ' Ignorer si contrôles non initialisés
        End Try
        initialisationEnCours = True

        ' Enlever tout texte sur les boutons
        Button_Precedent.Text = ""
        Button_Suivant.Text = ""
        Button_Jouer.Text = ""
        Button_PauseReprise.Text = ""
        Button_Mute.Text = ""
        Button_Arreter.Text = ""
        Button_Aleatoire.Text = ""
        Button_CalculBPM.Text = ""
        Button_Power.Text = ""
        Button_Ajout.Text = ""
        Button_InfoSelect.Text = ""
        Button_Playlist.Text = ""
        Button_Parametres.Text = ""
        Button_Loop.Text = ""
        Button_AudioPlay_Aide.Text = ""
        ButtonModeDJ.Text = ""

        ' Les associations seront vérifiées/appliquées après le chargement des paramètres
        Dim args = Environment.GetCommandLineArgs()
        ' === Instance unique Mutex + NamedPipe ===
        If instanceMutex Is Nothing Then
            instanceMutex = New Mutex(True, MutexName, isFirstInstance)
        End If
        If isFirstInstance Then
            ' Lancer le serveur NamedPipe pour recevoir les fichiers des autres instances
            isPipeServerRunning = True
            pipeServerThread = New Thread(AddressOf PipeServerLoop)
            pipeServerThread.IsBackground = True
            pipeServerThread.Start()
        Else
            ' Envoyer les arguments à l'instance principale puis fermer
            If args.Length > 1 Then
                Try
                    Using pipeClient As New NamedPipeClientStream(".", PipeName, PipeDirection.Out)
                        pipeClient.Connect(2000)
                        Using sw As New StreamWriter(pipeClient)
                            For i As Integer = 1 To args.Length - 1
                                sw.WriteLine(args(i))
                            Next
                            sw.Flush()
                        End Using
                    End Using
                Catch
                    ' Si l'instance principale n'est pas joignable, ignorer
                End Try
            End If
            ' Fermer cette instance
            Me.Close()
            Return
        End If


        Me.Text = String.Format(LanguageManager.GetString("App_Title"), Version)        ' Initialiser les contrôles
        ListView1.View = View.Details
        ListView1.FullRowSelect = True
        ListView1.GridLines = True
        ListView1.AllowDrop = True

        ' Configurer les TrackBars
        ' TrackBar_Volume : plage 0-50 pour avoir 9 ticks visibles (tous les 5)
        TrackBar_Volume.Minimum = 0
        TrackBar_Volume.Maximum = 50
        TrackBar_Volume.TickFrequency = 5
        TrackBar_Volume.TickStyle = TickStyle.BottomRight
        TrackBar_Volume.LargeChange = 5
        TrackBar_Volume.SmallChange = 1
        TrackBar_Volume.Value = 25
        TrackBar_Volume.Refresh()

        TrackBar_Avancement.Minimum = 0
        TrackBar_Avancement.Maximum = 1000
        TrackBar_Avancement.Value = 0

        If TrackBar_Basses IsNot Nothing Then
            TrackBar_Basses.Minimum = -20
            TrackBar_Basses.Maximum = 20
            TrackBar_Basses.Value = 0
            TrackBar_Basses.TickFrequency = 5
            TrackBar_Basses.TickStyle = TickStyle.BottomRight
            TrackBar_Basses.Refresh()
        End If

        If TrackBar_Aigues IsNot Nothing Then
            TrackBar_Aigues.Minimum = -20
            TrackBar_Aigues.Maximum = 20
            TrackBar_Aigues.Value = 0
            TrackBar_Aigues.TickFrequency = 5
            TrackBar_Aigues.TickStyle = TickStyle.BottomRight
            TrackBar_Aigues.Refresh()
        End If

        ' Charger les paramètres applicatifs
        ChargerParametres()

        ' Appliquer les associations audio par défaut enregistrées (AudioDefaut.txt)
        Try
            AssurerAssociationsAudioParDefaut()
        Catch
        End Try

        ' Vérifier maintenant si des types restent non associés et proposer d'ouvrir les Paramètres
        Try
            Dim nonAssociesAfter = AudioAssociationChecker.GetNonAssociatedTypes()
            If nonAssociesAfter.Count > 0 Then
                Dim msg = LanguageManager.GetString("FileAssociation_NotDefault", String.Join(", ", nonAssociesAfter)) & Environment.NewLine & Environment.NewLine & LanguageManager.GetString("FileAssociation_OfferOpenSettings")
                Dim res = MessageBox.Show(msg, LanguageManager.GetString("Info_Title"), MessageBoxButtons.YesNo, MessageBoxIcon.Information)
                If res = DialogResult.Yes Then
                    Try
                        Process.Start(New ProcessStartInfo("ms-settings:defaultapps") With {.UseShellExecute = True})
                    Catch
                        ' Fallback: ouvrir la page de paramètres via control panel
                        Try
                            Process.Start(New ProcessStartInfo("control", "/name Microsoft.DefaultPrograms") With {.UseShellExecute = True})
                        Catch
                        End Try
                    End Try
                End If
            End If
        Catch
        End Try

        ' Charger les paramètres audio depuis Son_Ajustement.txt (fichier séparé)
        ChargerAudioAjustements()

        ' Rafraîchir la langue de l'interface
        RefreshLanguage()

        ' Initialiser les images des boutons AVANT d'appliquer le thème
        ' pour que le ThemeManager sache que ces boutons ont des images
        InitialiserImagesButtons()

        ' Mettre à jour l'état visuel du bouton aléatoire selon l'état chargé
        MettreAJourBoutonAleatoire()

        ' Appliquer le thème visuel
        ThemeManager.ApplyThemeToForm(Me)

        ' Mettre à jour la couleur des marqueurs de boucle selon le thème
        MettreAJourCouleurMarqueursLoop()

        ' Initialiser les effets de survol des boutons
        InitialiserEffetsSurvol()

        ' Appliquer les valeurs chargées aux TrackBars
        ' (dernierVolume, dernieresBasses, dernieresAigues sont déjà chargés par ChargerParametres)
        If TrackBar_Volume IsNot Nothing Then
            Dim v = Math.Max(TrackBar_Volume.Minimum, Math.Min(TrackBar_Volume.Maximum, CInt(dernierVolume * TrackBar_Volume.Maximum)))
            TrackBar_Volume.Value = v
        End If
        If TrackBar_Basses IsNot Nothing Then
            Dim b = Math.Max(TrackBar_Basses.Minimum, Math.Min(TrackBar_Basses.Maximum, CInt(dernieresBasses)))
            TrackBar_Basses.Value = b
        End If
        If TrackBar_Aigues IsNot Nothing Then
            Dim a = Math.Max(TrackBar_Aigues.Minimum, Math.Min(TrackBar_Aigues.Maximum, CInt(dernieresAigues)))
            TrackBar_Aigues.Value = a
        End If

        ' Fin de l'initialisation : autoriser les événements Scroll à sauvegarder
        initialisationEnCours = False

        ' Initialiser le bouton aléatoire
        Button_Aleatoire.Text = "" ' Image seulement, pas de texte

        ' Si la lecture automatique est désactivée, désactiver le mode aléatoire
        If Not lectureEnContinu Then
            modeAleatoire = False
            MettreAJourBoutonAleatoire()
        End If

        ' Charger la playlist en arrière-plan pour accélérer l'affichage initial
        ChargerPlaylistEnArrierePlan()

        ' Ajouter les fichiers passés en argument (double-clic depuis l'explorateur)
        If args.Length > 1 Then
            For i As Integer = 1 To args.Length - 1
                Dim fichier = args(i)
                If File.Exists(fichier) Then
                    AjouterFichierAListe(fichier)
                End If
            Next
        End If
        ' Initialiser le bouton aléatoire
        Button_Aleatoire.Text = "" ' Image seulement, pas de texte

        ' Si la lecture automatique est désactivée, désactiver le mode aléatoire
        If Not lectureEnContinu Then
            modeAleatoire = False
            MettreAJourBoutonAleatoire()
        End If

        ' Charger la playlist en arrière-plan (déjà déclenché ci‑dessus)
        ' ChargerPlaylistEnArrierePlan()

        ' Activer le timer
        timerProgression.Interval = 200
        timerProgression.Start()

        ' Créer le menu contextuel
        CreerMenuContextuel()

        ' Activer le dessin personnalisé pour garder la sélection bleue
        AddHandler ListView1.DrawColumnHeader, AddressOf ListView1_OnDrawColumnHeader
        AddHandler ListView1.DrawItem, AddressOf ListView1_OnDrawItem
        AddHandler ListView1.DrawSubItem, AddressOf ListView1_OnDrawSubItem

        ' Activer la gestion du clavier pour le ListView
        AddHandler ListView1.KeyDown, AddressOf ListView1_KeyDown

        ' Initialiser les contrôles de recherche
        InitialiserRechercheControles()

        ' Initialiser les labels de boucle (I et O)
        InitialiserLabelsLoop()

        ' Initialiser l'échelle numérique 0..100 au-dessus du TrackBar
        InitialiserEchelleTrackBar()

        ' Vérifier et installer Python/librosa si nécessaire
        Await VerifierEtInstallerPython()

        ' Mettre le focus sur la ListView1 au démarrage
        ListView1.Focus()
    End Sub



    ' ========================================
    ' VÉRIFICATION ET INSTALLATION PYTHON
    ' ========================================
    Private Async Function VerifierEtInstallerPython() As Task
        Try
            ' Vérifier si Python est déjà installé
            If PythonManager.EstInstalle() Then
                ' Vérifier si librosa est installé
                Dim librosaOk = Await PythonManager.LibrosaEstInstalle()
                If librosaOk Then
                    System.Diagnostics.Debug.WriteLine("Python et librosa sont déjà installés.")
                    Return
                End If
            End If

            ' Proposer l'installation
            Dim result = MessageBox.Show(
                LanguageManager.GetString("BPM_PythonInstallPrompt"),
                LanguageManager.GetString("BPM_PythonInstallTitle"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            )

            If result = DialogResult.Yes Then
                ' Créer un formulaire de progression
                Dim progressForm As New Form()
                progressForm.Text = LanguageManager.GetString("BPM_PythonInstallTitle")
                progressForm.Size = New Size(500, 150)
                progressForm.StartPosition = FormStartPosition.CenterParent
                progressForm.FormBorderStyle = FormBorderStyle.FixedDialog
                progressForm.MaximizeBox = False
                progressForm.MinimizeBox = False
                progressForm.ControlBox = False

                Dim lblProgress As New Label()
                lblProgress.Text = "Initialisation..."
                lblProgress.AutoSize = False
                lblProgress.Size = New Size(460, 60)
                lblProgress.Location = New Point(20, 20)
                lblProgress.TextAlign = ContentAlignment.MiddleLeft

                progressForm.Controls.Add(lblProgress)
                progressForm.Show(Me)

                ' Créer le progress reporter
                Dim progress = New Progress(Of String)(Sub(msg)
                                                           lblProgress.Text = msg
                                                           Application.DoEvents()
                                                       End Sub)

                ' Lancer l'installation
                Dim success = Await PythonManager.InstallerPythonEmbedded(progress)

                progressForm.Close()

                If success Then
                    MessageBox.Show(
                        "Installation réussie !" & vbCrLf & vbCrLf &
                        "Le calcul de BPM utilisera maintenant librosa (précision maximale).",
                        "Installation terminée",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    )
                Else
                    MessageBox.Show(
                        "L'installation a échoué." & vbCrLf & vbCrLf &
                        "Le calcul de BPM utilisera SoundTouch (précision standard).",
                        "Installation échouée",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    )
                End If
            Else
                System.Diagnostics.Debug.WriteLine("Installation Python refusée par l'utilisateur.")
            End If

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"Erreur lors de la vérification Python : {ex.Message}")
            ' En cas d'erreur, continuer sans Python
        End Try
    End Function

    ' ========================================
    ' LECTURE AUDIO AVEC ÉGALISEUR
    ' ========================================
    Private Async Sub JouerItemSelectionne()
        ' Réinitialiser le compteur de tentatives au début d'une nouvelle demande de lecture
        tentativesLectureFichier = 0
        JouerItemSelectionneAvecTentatives()
    End Sub

    Private Async Sub JouerItemSelectionneAvecTentatives()
        ' Protection contre les boucles infinies : si tous les fichiers sont manquants
        If tentativesLectureFichier >= ListView1.Items.Count Then
            System.Diagnostics.Debug.WriteLine("Tous les fichiers de la liste semblent manquants. Arrêt de la lecture.")
            tentativesLectureFichier = 0
            Return
        End If

        If ListView1.SelectedItems.Count = 0 Then Return

        Dim item = ListView1.SelectedItems(0)

        ' Extraire le chemin depuis le Tag (peut être un Dictionary ou une String)
        Dim chemin As String = ""
        Dim estCDAudio As Boolean = False
        If TypeOf item.Tag Is Dictionary(Of String, Object) Then
            Dim tagDict = DirectCast(item.Tag, Dictionary(Of String, Object))
            If tagDict.ContainsKey("Chemin") Then
                chemin = tagDict("Chemin")?.ToString()
            End If
            ' Vérifier si c'est une piste CD
            If tagDict.ContainsKey("TypeSource") AndAlso tagDict("TypeSource")?.ToString() = "CDAudio" Then
                estCDAudio = True
            End If
        ElseIf TypeOf item.Tag Is String Then
            chemin = item.Tag.ToString()
            ' Vérifier si c'est un chemin CDDA://
            estCDAudio = CDAudioManager.EstCheminCDAudio(chemin)
        End If

        ' Pour les fichiers normaux, vérifier que le chemin n'est pas vide
        ' Ne PAS utiliser File.Exists() car il peut retourner False pour les chemins réseau/NAS
        If Not estCDAudio Then
            If String.IsNullOrEmpty(chemin) Then
                tentativesLectureFichier += 1
                System.Diagnostics.Debug.WriteLine($"Chemin vide ({tentativesLectureFichier}/{ListView1.Items.Count}), passage au suivant...")
                ' Sélectionner le prochain item (aléatoire ou séquentiel)
                SelectionnerItemSuivantSansTentative()
                ' Réessayer avec le nouveau fichier sélectionné
                JouerItemSelectionneAvecTentatives()
                Return
            End If
            ' Si le fichier n'est vraiment pas accessible, NAudio lancera une exception
            ' qui sera gérée plus bas dans ChargerFichierAudio()
        Else
            ' Pour les pistes CD, vérifier que le CD est toujours présent
            Dim cdTrack = CDAudioManager.ParseCheminCDAudio(chemin)
            If cdTrack Is Nothing OrElse Not CDAudioManager.EstCDAudioPresent(cdTrack.Drive) Then
                tentativesLectureFichier += 1
                System.Diagnostics.Debug.WriteLine($"CD absent ({tentativesLectureFichier}/{ListView1.Items.Count}) : {chemin}, passage au suivant...")
                ' Sélectionner le prochain item (aléatoire ou séquentiel)
                SelectionnerItemSuivantSansTentative()
                ' Réessayer avec le nouveau fichier sélectionné
                JouerItemSelectionneAvecTentatives()
                Return
            End If
        End If

        ' Ne pas réinitialiser le compteur ici : ne le remettre à zéro
        ' QUE si l'ouverture du fichier a réussi (évite boucle infinie si tous les fichiers sont corrompus)

        ' --- GESTION BPM MÉTRONOME ---
        If metronomeActif Then
            Dim bpmFichier As Double = 0
            If TypeOf item.Tag Is Dictionary(Of String, Object) Then
                Dim tagDict = DirectCast(item.Tag, Dictionary(Of String, Object))
                If tagDict.ContainsKey("BPM") Then
                    Dim bpmObj = tagDict("BPM")
                    If bpmObj IsNot Nothing Then
                        Double.TryParse(bpmObj.ToString(), bpmFichier)
                    End If
                End If
            End If
            If bpmFichier <= 0 Then
                Dim rep = MessageBox.Show(LanguageManager.GetString("BPM_Missing"), LanguageManager.GetString("BPM_Missing_Title"), MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                If rep = DialogResult.Yes Then
                    ' Calculer le BPM
                    Dim bpmCalcule As Double = Await BPMDetector.DetecterBPM(chemin)
                    If bpmCalcule > 0 Then
                        ' Stocker dans le Tag
                        If TypeOf item.Tag Is Dictionary(Of String, Object) Then
                            Dim tagDict = DirectCast(item.Tag, Dictionary(Of String, Object))
                            tagDict("BPM") = bpmCalcule
                        End If
                        ' Mettre à jour la colonne BPM si présente
                        If item.SubItems.Count > 2 Then
                            item.SubItems(2).Text = bpmCalcule.ToString("F2")
                        End If
                        ' Relancer la lecture avec le BPM trouvé
                        Await Task.Delay(100)
                        JouerItemSelectionne()
                    Else
                        MessageBox.Show(LanguageManager.GetString("BPM_Error"), LanguageManager.GetString("BPM_Error_Title"), MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End If
                End If
                Return ' On ne joue rien si l'utilisateur refuse ou si BPM non trouvé
            End If
        End If

        ' Use an iterative attempt loop to avoid recursion and ensure termination when all files are invalid
        Dim opened As Boolean = False
        Dim attempts As Integer = 0
        Dim maxAttempts As Integer = Math.Max(1, ListView1.Items.Count)
        Do While Not opened AndAlso attempts < maxAttempts
            attempts += 1
            Try
                ' Arrêter la lecture en cours
                ArreterLecture()

                ' Effacer les marqueurs de boucle pour la nouvelle chanson
                EffacerMarqueursLoop()

                ' Obtenir le gain de normalisation AVANT d'ouvrir le fichier
                If metronomeActif Then
                    gainNormalisationActuel = 1.0F
                Else
                    gainNormalisationActuel = ObtenirGainNormalisation(item, chemin)
                End If

                ' Récupérer ou calculer la durée réelle si l'option est activée (pas pour CD audio)
                dureeReelleActuelle = TimeSpan.Zero
                If Not estCDAudio AndAlso ParametresGlobaux.SupprimerSilenceFin AndAlso TypeOf item.Tag Is Dictionary(Of String, Object) Then
                    Dim tagDict = DirectCast(item.Tag, Dictionary(Of String, Object))
                    If tagDict.ContainsKey("DureeReelle") Then
                        Dim dureeObj = tagDict("DureeReelle")
                        If TypeOf dureeObj Is TimeSpan Then
                            dureeReelleActuelle = DirectCast(dureeObj, TimeSpan)
                        End If
                    Else
                        Try
                            dureeReelleActuelle = SilenceDetector.TrouverDureeReelle(chemin)
                            If dureeReelleActuelle > TimeSpan.Zero Then
                                tagDict.Add("DureeReelle", dureeReelleActuelle)
                            End If
                        Catch
                        End Try
                    End If
                End If

                ' Créer le nouveau lecteur (différent pour CD audio)
                If estCDAudio Then
                    Dim cdTrack = CDAudioManager.ParseCheminCDAudio(chemin)
                    If cdTrack IsNot Nothing Then
                        fichierAudio = CDAudioManager.CreerLecteurCDAudio(cdTrack)
                        If fichierAudio Is Nothing Then
                            Throw New Exception("Impossible de créer le lecteur CD audio")
                        End If
                    Else
                        Throw New Exception("Chemin CD audio invalide")
                    End If
                Else
                    Try
                        fichierAudio = New AudioFileReader(chemin)
                    Catch exReader As Exception
                        ' Log and skip silently to next track
                        Try
                            CDAudioAnalyzer.DiagnosticWrite($"PLAYBACK_ERROR: Failed opening audio {chemin}: {exReader.Message}")
                        Catch
                        End Try
                        ' mark attempt and select next
                        tentativesLectureFichier = attempts
                        SelectionnerItemSuivantSansTentative()
                        ' prepare for next loop iteration with new selection
                        If attempts >= maxAttempts Then Exit Do
                        ' update item and chemin for the newly selected item
                        If ListView1.SelectedItems.Count = 0 Then Exit Do
                        item = ListView1.SelectedItems(0)
                        If TypeOf item.Tag Is Dictionary(Of String, Object) Then
                            Dim tagDict2 = DirectCast(item.Tag, Dictionary(Of String, Object))
                            If tagDict2.ContainsKey("Chemin") Then chemin = tagDict2("Chemin")?.ToString()
                            If tagDict2.ContainsKey("TypeSource") AndAlso tagDict2("TypeSource")?.ToString() = "CDAudio" Then estCDAudio = True Else estCDAudio = False
                        ElseIf TypeOf item.Tag Is String Then
                            chemin = item.Tag.ToString()
                            estCDAudio = CDAudioManager.EstCheminCDAudio(chemin)
                        End If
                        Continue Do
                    End Try
                End If

                ' If we reach here, opening succeeded
                opened = True
                tentativesLectureFichier = 0
            Catch ex As Exception
                ' If unexpected exception occurs, log and stop attempts
                Try
                    CDAudioAnalyzer.DiagnosticWrite($"PLAYBACK_ERROR: Unexpected error preparing playback {chemin}: {ex.Message}")
                Catch
                End Try
                Exit Do
            End Try
        Loop

        If Not opened Then
            ' All attempts failed: stop without crashing
            System.Diagnostics.Debug.WriteLine("Lecture abandonnée : aucun fichier valide trouvé dans la liste.")
            tentativesLectureFichier = 0
            Return
        End If

        ' Mettre à jour le taux d'échantillonnage
        If Label_SampleRate IsNot Nothing Then
            If estCDAudio Then
                Label_SampleRate.Text = "44100 Hz" ' CD Audio standard
            Else
                Label_SampleRate.Text = $"{fichierAudio.WaveFormat.SampleRate} Hz"
            End If
        End If

        ' Calculer et afficher le bitrate
        If Label_Bitrate IsNot Nothing Then
            If estCDAudio Then
                Label_Bitrate.Text = "1411 kbps" ' CD Audio standard: 44.1kHz * 16-bit * 2 channels
            Else
                Try
                    ' Calculer le bitrate approximatif
                    Dim fileInfo As New FileInfo(chemin)
                    Dim fileSizeInBytes As Long = fileInfo.Length
                    Dim durationInSeconds As Double = fichierAudio.TotalTime.TotalSeconds

                    If durationInSeconds > 0 Then
                        ' Bitrate en kbps = (taille fichier en bits) / (durée en secondes) / 1000
                        Dim bitrateKbps As Integer = CInt((fileSizeInBytes * 8) / durationInSeconds / 1000)
                        Label_Bitrate.Text = $"{bitrateKbps} kbps"
                    Else
                        Label_Bitrate.Text = "-- kbps"
                    End If
                Catch
                    Label_Bitrate.Text = "-- kbps"
                End Try
            End If
        End If

        ' Convertir le fichier audio en SampleProvider
        Dim audioSampleProvider = fichierAudio.ToSampleProvider()

        ' Supprimer le silence au début si l'option est activée
        ' (ou si le métronome est actif, pour que la chanson démarre immédiatement après)
        ' Ne pas appliquer pour les pistes CD (qui peuvent ne pas encore avoir de vraie lecture audio)
        If Not estCDAudio AndAlso (ParametresGlobaux.SupprimerSilenceDebut OrElse metronomeActif) Then
            audioSampleProvider = New SkipSilenceSampleProvider(audioSampleProvider)
        End If

        ' DÉSACTIVÉ TEMPORAIREMENT : La suppression du silence à la fin cause des problèmes
        ' TODO : Implémenter une meilleure approche (détection anticipée du silence final)
        'If ParametresGlobaux.SupprimerSilenceFin Then
        '    audioSampleProvider = New TrimEndSilenceSampleProvider(audioSampleProvider)
        'End If

        ' Créer le provider de base
        Dim sampleProviderBase As ISampleProvider

        ' Si le métronome est activé, créer la séquence métronome + audio
        If metronomeActif Then
            ' Obtenir le BPM depuis les métadonnées ou le Tag
            Dim bpmFichier As Double = 120.0 ' BPM par défaut
            If TypeOf item.Tag Is Dictionary(Of String, Object) Then
                Dim tagDict = DirectCast(item.Tag, Dictionary(Of String, Object))
                If tagDict.ContainsKey("BPM") Then
                    Dim bpmObj = tagDict("BPM")
                    If bpmObj IsNot Nothing Then
                        Double.TryParse(bpmObj.ToString(), bpmFichier)
                    End If
                End If
            End If

            System.Diagnostics.Debug.WriteLine($"Création métronome avec BPM: {bpmFichier}")

            ' Sauvegarder les paramètres du métronome pour la LED
            metronomeBPM = bpmFichier
            metronomeNombreBeats = nombreBeatsMetronome

            ' Si le son du métronome est activé, créer le provider audio
            If metronomeSonActif Then
                ' Créer le métronome avec le BPM du fichier
                Dim metronome As New MetronomeProvider(
                        fichierAudio.WaveFormat.SampleRate,
                        bpmFichier,
                        nombreBeatsMetronome
                    )

                ' Sauvegarder la référence au métronome
                metronomeEnCours = metronome

                ' Créer le séquenceur qui joue métronome puis audio
                Dim sequenceur As New MetronomeAudioSequencer(metronome, audioSampleProvider)
                sampleProviderBase = sequenceur
            Else
                ' Pas de son métronome, mais si la lumière est activée, créer un délai silencieux
                If metronomeLumiereActive Then
                    ' Créer un délai silencieux pour la durée du métronome
                    Dim silentDelay As New SilentDelayProvider(
                            fichierAudio.WaveFormat.SampleRate,
                            bpmFichier,
                            nombreBeatsMetronome
                        )

                    ' Créer le séquenceur qui joue silence puis audio
                    Dim sequenceur As New SilentDelayAudioSequencer(silentDelay, audioSampleProvider)
                    sampleProviderBase = sequenceur

                    ' Créer un métronome invisible pour la LED (juste pour le timing)
                    metronomeEnCours = New MetronomeProvider(
                            fichierAudio.WaveFormat.SampleRate,
                            bpmFichier,
                            nombreBeatsMetronome
                        )
                Else
                    ' Ni son ni lumière, utiliser directement le fichier audio
                    sampleProviderBase = audioSampleProvider
                End If
            End If

            ' Ouvrir la fenêtre LED si la lumière est activée
            If metronomeLumiereActive Then
                If formLight Is Nothing Then
                    formLight = New FormLight()
                End If
                formLight.ShowLight()
            End If
        Else
            ' Pas de métronome, utiliser directement le fichier audio
            sampleProviderBase = audioSampleProvider
        End If

        ' Créer la chaîne audio avec égaliseur personnalisé
        equalizerProvider = New SimpleEqualizerProvider(sampleProviderBase, dernieresBasses, dernieresAigues)

        ' === Appliquer les effets audio dans l'ordre ===
        Dim currentProvider As ISampleProvider = equalizerProvider

        ' Debug: Afficher l'état des effets
        System.Diagnostics.Debug.WriteLine("=== État des effets audio ===")
        System.Diagnostics.Debug.WriteLine($"Reverb: Actif={ParametresGlobaux.EffetReverbActif}, Mix={ParametresGlobaux.EffetReverbMix}")
        System.Diagnostics.Debug.WriteLine($"Echo: Actif={ParametresGlobaux.EffetEchoActif}, Mix={ParametresGlobaux.EffetEchoMix}, Délai={ParametresGlobaux.EffetEchoDelai}ms, Feedback={ParametresGlobaux.EffetEchoFeedback}")
        System.Diagnostics.Debug.WriteLine($"TimeStretch: Actif={ParametresGlobaux.EffetTimeStretchActif}, Ratio={ParametresGlobaux.EffetTimeStretchRatio}")
        System.Diagnostics.Debug.WriteLine($"PitchShift: Actif={ParametresGlobaux.EffetPitchShiftActif}, SemiTones={ParametresGlobaux.EffetPitchShiftSemiTones}")
        System.Diagnostics.Debug.WriteLine($"Phaser: Actif={ParametresGlobaux.EffetPhaserActif}, Rate={ParametresGlobaux.EffetPhaserRate}, Depth={ParametresGlobaux.EffetPhaserDepth}, Feedback={ParametresGlobaux.EffetPhaserFeedback}, Mix={ParametresGlobaux.EffetPhaserMix}, Stages={ParametresGlobaux.EffetPhaserStages}")

        ' 1. Time Stretch (changement de tempo)
        timeStretchProvider = New TimeStretchSampleProvider(currentProvider)
        timeStretchProvider.Enabled = ParametresGlobaux.EffetTimeStretchActif
        timeStretchProvider.TempoChange = ParametresGlobaux.EffetTimeStretchRatio
        currentProvider = timeStretchProvider

        ' 2. Pitch Shift (changement de hauteur)
        pitchShiftProvider = New PitchShiftSampleProvider(currentProvider)
        pitchShiftProvider.Enabled = ParametresGlobaux.EffetPitchShiftActif
        pitchShiftProvider.PitchSemiTones = ParametresGlobaux.EffetPitchShiftSemiTones
        currentProvider = pitchShiftProvider

        ' 3. Phaser (effet de balayage de phase)
        phaserProvider = New PhaserSampleProvider(currentProvider)
        phaserProvider.Enabled = ParametresGlobaux.EffetPhaserActif
        phaserProvider.Rate = ParametresGlobaux.EffetPhaserRate
        phaserProvider.Depth = ParametresGlobaux.EffetPhaserDepth
        phaserProvider.Feedback = ParametresGlobaux.EffetPhaserFeedback
        phaserProvider.Mix = ParametresGlobaux.EffetPhaserMix
        phaserProvider.Stages = ParametresGlobaux.EffetPhaserStages
        currentProvider = phaserProvider

        ' 4. Reverb (réverbération)
        reverbProvider = New ReverbSampleProvider(currentProvider)
        reverbProvider.Enabled = ParametresGlobaux.EffetReverbActif
        reverbProvider.Mix = ParametresGlobaux.EffetReverbMix
        currentProvider = reverbProvider

        ' 5. Echo (écho)
        echoProvider = New EchoSampleProvider(currentProvider)
        echoProvider.Enabled = ParametresGlobaux.EffetEchoActif
        echoProvider.Mix = ParametresGlobaux.EffetEchoMix
        echoProvider.DelayMilliseconds = ParametresGlobaux.EffetEchoDelai
        echoProvider.Feedback = ParametresGlobaux.EffetEchoFeedback
        currentProvider = echoProvider

        System.Diagnostics.Debug.WriteLine("=== Chaîne audio créée avec effets ===")

        ' Appliquer le volume avec normalisation
        volumeProvider = New VolumeSampleProvider(currentProvider) With {
                .Volume = dernierVolume * gainNormalisationActuel
            }

        ' Initialiser le lecteur
        lecteur = New WaveOutEvent()
        AddHandler DirectCast(lecteur, WaveOutEvent).PlaybackStopped, AddressOf OnPlaybackStopped
        lecteur.Init(volumeProvider)
        lecteur.Play()

        cheminActuel = chemin
        lectureEnCours = True
        enPause = False

        ' Démarrer le timer LED si métronome actif ET lumière activée
        If metronomeActif AndAlso metronomeEnCours IsNot Nothing AndAlso metronomeLumiereActive Then
            DemarrerTimerMetronomeLED()
        End If

        ' Mettre le bouton Jouer en vert pendant la lecture
        If Button_Jouer IsNot Nothing Then
            Button_Jouer.BackgroundImage = AudioPlay.Resources.AudioPlay_Jouer_Vert
        End If

        ' Mettre le bouton Pause/Reprise en vert pendant la lecture
        If Button_PauseReprise IsNot Nothing Then
            Button_PauseReprise.BackgroundImage = AudioPlay.Resources.AudioPlay_Pause_Vert
        End If

        ' Mettre le bouton Arrêter en vert pendant la lecture
        If Button_Arreter IsNot Nothing Then
            Button_Arreter.BackgroundImage = AudioPlay.Resources.AudioPlay_Arreter_Vert
        End If

        ' Mettre le bouton Mute en vert (sauf si déjà muté = rouge)
        If Button_Mute IsNot Nothing Then
            If isMuted Then
                Button_Mute.BackgroundImage = AudioPlay.Resources.AudioPlay_Mute_Rouge
            Else
                Button_Mute.BackgroundImage = AudioPlay.Resources.AudioPlay_Mute_Vert
            End If
        End If

        Button_Jouer.Text = "" ' Image seulement, pas de texte
        Button_PauseReprise.Text = "" ' Image seulement, pas de texte

        ' Mettre à jour l'affichage avec le nom du fichier
        If TextBox_Display IsNot Nothing Then
            TextBox_Display.Text = item.SubItems(1).Text ' Nom du fichier depuis la colonne "Chansons"
        End If

        ' === DÉTECTION ET CHARGEMENT DU FICHIER CDG ===
        DetecterEtChargerCDG(chemin)

        ' Top-level Try/Catch removed: inner loop handles failures and ensures safe termination when no valid files remain.
    End Sub

    ' Obtenir le gain de normalisation pour un fichier
    Private Function ObtenirGainNormalisation(item As ListViewItem, cheminFichier As String) As Single
        ' Si la normalisation est désactivée, retourner 1.0 (pas de changement)
        If Not normalisationVolumeActive Then
            Return 1.0F
        End If

        ' Vérifier si le gain a déjà été calculé et stocké dans le Tag
        ' On utilise un Dictionary stocké dans item.Tag
        Dim gainStocke As Object = Nothing
        If TypeOf item.Tag Is Dictionary(Of String, Object) Then
            Dim tagDict = DirectCast(item.Tag, Dictionary(Of String, Object))
            If tagDict.ContainsKey("GainNormalisation") Then
                gainStocke = tagDict("GainNormalisation")
                If TypeOf gainStocke Is Single Then
                    Return CSng(gainStocke)
                End If
            End If
        ElseIf TypeOf item.Tag Is String Then
            ' Le Tag contient seulement le chemin, on doit créer un Dictionary
            Dim nouveauDict As New Dictionary(Of String, Object) From {
                {"Chemin", item.Tag}
            }
            item.Tag = nouveauDict
        End If

        ' Le gain n'est pas encore calculé, on le calcule maintenant
        Try
            ' Analyse rapide du fichier
            Dim volumeInfo = VolumeNormalizer.AnalyserFichierRapide(cheminFichier)

            ' Stocker le gain dans le Tag pour réutilisation
            If TypeOf item.Tag Is Dictionary(Of String, Object) Then
                Dim tagDict = DirectCast(item.Tag, Dictionary(Of String, Object))
                tagDict("GainNormalisation") = volumeInfo.GainSuggere
            End If

            Return volumeInfo.GainSuggere

        Catch ex As Exception
            ' En cas d'erreur, retourner un gain neutre
            System.Diagnostics.Debug.WriteLine($"Erreur calcul gain normalisation: {ex.Message}")
            Return 1.0F
        End Try
    End Function

    Private Sub ArreterLecture()
        Try
            timerProgression.Stop()

            ' Effacer les marqueurs de boucle
            EffacerMarqueursLoop()

            ' Arrêter le karaoke si actif
            If formKaraoke IsNot Nothing Then
                formKaraoke.StopPlayback()
            End If

            ' Arrêter le timer LED du métronome
            If metronomeTimer IsNot Nothing Then
                Try
                    metronomeTimer.Stop()
                    RemoveHandler metronomeTimer.Tick, AddressOf MetronomeTimer_Tick
                    metronomeTimer.Dispose()
                    metronomeTimer = Nothing
                Catch
                    ' Ignorer les erreurs
                End Try
            End If

            ' Nettoyer les références du métronome
            If metronomeEnCours IsNot Nothing Then
                metronomeEnCours = Nothing
                metronomeBPM = 0
                metronomeNombreBeats = 0
            End If

            ' Fermer la fenêtre LED si ouverte
            If formLight IsNot Nothing Then
                Try
                    formLight.HideLight()
                Catch
                    ' Ignorer les erreurs de fermeture de la LED
                End Try
            End If

            ' Arrêter le lecteur d'abord
            If lecteur IsNot Nothing Then
                Try
                    RemoveHandler DirectCast(lecteur, WaveOutEvent).PlaybackStopped, AddressOf OnPlaybackStopped
                    lecteur.Stop()
                Catch
                    ' Ignorer les erreurs lors de l'arrêt
                End Try
            End If

            ' Libérer volumeProvider et equalizerProvider avant le fichierAudio
            volumeProvider = Nothing
            equalizerProvider = Nothing

            ' Libérer le lecteur
            If lecteur IsNot Nothing Then
                Try
                    lecteur.Dispose()
                Catch
                    ' Ignorer les erreurs de dispose
                End Try
                lecteur = Nothing
            End If

            ' Libérer le fichier audio en dernier
            If fichierAudio IsNot Nothing Then
                Try
                    fichierAudio.Close()
                Catch
                    ' Ignorer les erreurs
                End Try

                Try
                    fichierAudio.Dispose()
                Catch
                    ' Ignorer les erreurs de dispose
                End Try
                fichierAudio = Nothing
            End If

            ' Forcer le garbage collector pour libérer immédiatement les ressources
            GC.Collect()
            GC.WaitForPendingFinalizers()

            lectureEnCours = False
            enPause = False
            cheminActuel = ""

            ' Remettre le bouton Jouer en gris quand rien ne joue
            If Button_Jouer IsNot Nothing Then
                Button_Jouer.BackgroundImage = AudioPlay.Resources.AudioPlay_Jouer_Gris
            End If

            ' Remettre le bouton Pause/Reprise en gris quand rien ne joue
            If Button_PauseReprise IsNot Nothing Then
                Button_PauseReprise.BackgroundImage = AudioPlay.Resources.AudioPlay_Pause_Gris
            End If

            ' Remettre le bouton Arrêter en rouge quand rien ne joue
            If Button_Arreter IsNot Nothing Then
                Button_Arreter.BackgroundImage = AudioPlay.Resources.AudioPlay_Arreter_Rouge
            End If

            ' Remettre le bouton Mute en gris (sauf si muté = rouge)
            If Button_Mute IsNot Nothing Then
                If isMuted Then
                    Button_Mute.BackgroundImage = AudioPlay.Resources.AudioPlay_Mute_Rouge
                Else
                    Button_Mute.BackgroundImage = AudioPlay.Resources.AudioPlay_Mute_Gris
                End If
            End If

            majTrackBarEnCours = True
            TrackBar_Avancement.Value = 0
            majTrackBarEnCours = False

            If Label_DureeRestante IsNot Nothing Then
                Label_DureeRestante.Text = "-00:00"
            End If

            If Label_SampleRate IsNot Nothing Then
                Label_SampleRate.Text = "-- Hz"
            End If

            If Label_Bitrate IsNot Nothing Then
                Label_Bitrate.Text = "-- kbps"
            End If

            If TextBox_Display IsNot Nothing Then
                TextBox_Display.Text = ""
            End If

            timerProgression.Start()

        Catch ex As Exception
            ' Ignorer les erreurs lors de l'arrêt
        End Try
    End Sub

    Private Sub OnPlaybackStopped(sender As Object, e As StoppedEventArgs)
        If gardeReentrance Then Return

        Me.Invoke(Sub()
                      If lectureEnContinu AndAlso Not enPause Then
                          ' Lecture automatique activée : jouer l'item suivant
                          JouerItemSuivant()
                      Else
                          ' Lecture automatique désactivée : juste sélectionner l'item suivant
                          SelectionnerItemSuivant()
                          ArreterLecture()
                      End If
                  End Sub)
    End Sub

    ' ========================================
    ' NAVIGATION
    ' ========================================
    Private Sub JouerItemSuivant()
        If gardeReentrance Then Return
        gardeReentrance = True

        Try
            If ListView1.Items.Count = 0 Then Return

            Dim indexActuel = If(ListView1.SelectedItems.Count > 0, ListView1.SelectedItems(0).Index, -1)
            Dim nouvelIndex As Integer

            If modeAleatoire Then
                Dim rnd As New Random()
                Dim tentatives = 0
                Do
                    nouvelIndex = rnd.Next(0, ListView1.Items.Count)
                    tentatives += 1
                Loop While nouvelIndex = indexActuel AndAlso ListView1.Items.Count > 1 AndAlso tentatives < 10
            Else
                nouvelIndex = (indexActuel + 1) Mod ListView1.Items.Count
            End If

            ListView1.SelectedItems.Clear()
            ListView1.Items(nouvelIndex).Selected = True
            ListView1.Items(nouvelIndex).EnsureVisible()
            ListView1.Focus() ' Donner le focus pour afficher la sélection en bleu

            ' Réinitialiser le compteur de tentatives avant de lancer la lecture
            tentativesLectureFichier = 0
            JouerItemSelectionne()

        Finally
            gardeReentrance = False
        End Try
    End Sub

    ' Sélectionner l'item suivant sans le jouer
    Private Sub SelectionnerItemSuivant()
        Try
            If ListView1.Items.Count = 0 Then Return

            Dim indexActuel = If(ListView1.SelectedItems.Count > 0, ListView1.SelectedItems(0).Index, -1)
            Dim nouvelIndex As Integer

            If modeAleatoire Then
                Dim rnd As New Random()
                Dim tentatives = 0
                Do
                    nouvelIndex = rnd.Next(0, ListView1.Items.Count)
                    tentatives += 1
                Loop While nouvelIndex = indexActuel AndAlso ListView1.Items.Count > 1 AndAlso tentatives < 10
            Else
                nouvelIndex = (indexActuel + 1) Mod ListView1.Items.Count
            End If

            ListView1.SelectedItems.Clear()
            ListView1.Items(nouvelIndex).Selected = True
            ListView1.Items(nouvelIndex).EnsureVisible()
            ListView1.Focus() ' Donner le focus pour afficher la sélection en bleu

        Catch ex As Exception
            ' Ignorer les erreurs de sélection
        End Try
    End Sub

    ' Sélectionner l'item suivant sans tentative (pour le saut des fichiers manquants)
    Private Sub SelectionnerItemSuivantSansTentative()
        Try
            If ListView1.Items.Count = 0 Then Return

            Dim indexActuel = If(ListView1.SelectedItems.Count > 0, ListView1.SelectedItems(0).Index, -1)
            Dim nouvelIndex As Integer

            If modeAleatoire Then
                Dim rnd As New Random()
                nouvelIndex = rnd.Next(0, ListView1.Items.Count)
            Else
                ' If we're at the last item, do NOT wrap to the first: signal end by
                ' setting tentativesLectureFichier so the caller will stop further attempts.
                If indexActuel >= ListView1.Items.Count - 1 Then
                    tentativesLectureFichier = ListView1.Items.Count
                    Return
                Else
                    nouvelIndex = indexActuel + 1
                End If
            End If

            ListView1.SelectedItems.Clear()
            ListView1.Items(nouvelIndex).Selected = True
            ListView1.Items(nouvelIndex).EnsureVisible()
            ListView1.Focus()

        Catch ex As Exception
            ' Ignorer les erreurs de sélection
        End Try
    End Sub

    Private Sub JouerItemPrecedent()
        If gardeReentrance Then Return
        gardeReentrance = True

        Try
            If ListView1.Items.Count = 0 Then Return

            Dim indexActuel = If(ListView1.SelectedItems.Count > 0, ListView1.SelectedItems(0).Index, -1)
            Dim nouvelIndex As Integer

            If modeAleatoire Then
                Dim rnd As New Random()
                Dim tentatives = 0
                Do
                    nouvelIndex = rnd.Next(0, ListView1.Items.Count)
                    tentatives += 1
                Loop While nouvelIndex = indexActuel AndAlso ListView1.Items.Count > 1 AndAlso tentatives < 10
            Else
                nouvelIndex = If(indexActuel <= 0, ListView1.Items.Count - 1, indexActuel - 1)
            End If

            ListView1.SelectedItems.Clear()
            ListView1.Items(nouvelIndex).Selected = True
            ListView1.Items(nouvelIndex).EnsureVisible()
            ListView1.Focus() ' Donner le focus pour afficher la sélection en bleu

            ' Réinitialiser le compteur de tentatives avant de lancer la lecture
            tentativesLectureFichier = 0
            JouerItemSelectionne()

        Finally
            gardeReentrance = False
        End Try
    End Sub

    ' ========================================
    ' BOUTONS
    ' ========================================
    Private Sub Button_APropos_Click(sender As Object, e As EventArgs) Handles Button_APropos.Click
        Dim dlg As New Form_APropos()
        dlg.ShowDialog(Me)
    End Sub

    Private Sub Button_Jouer_Click(sender As Object, e As EventArgs) Handles Button_Jouer.Click
        ' Jouer directement la chanson sélectionnée
        JouerItemSelectionne()
        ListView1.Focus()
    End Sub

    Private Sub Button_Precedent_Click(sender As Object, e As EventArgs) Handles Button_Precedent.Click
        ' Jouer la chanson précédente
        JouerItemPrecedent()
        ListView1.Focus()
    End Sub

    Private Sub Button_Suivant_Click(sender As Object, e As EventArgs) Handles Button_Suivant.Click
        ' Jouer la chanson suivante
        JouerItemSuivant()
        ListView1.Focus()
    End Sub

    Private Sub Button_Arreter_Click(sender As Object, e As EventArgs) Handles Button_Arreter.Click
        ' ✅ ARRÊTER SEULEMENT LA LECTURE AUDIO
        ' Le calcul/recalcul BPM continue en arrière-plan
        ArreterLecture()
        ListView1.Focus()
    End Sub

    Private Sub Button_PauseReprise_Click(sender As Object, e As EventArgs) Handles Button_PauseReprise.Click
        If lecteur Is Nothing OrElse Not lectureEnCours Then Return

        If enPause Then
            lecteur.Play()
            enPause = False
            ' Reprendre le karaoke
            If formKaraoke IsNot Nothing Then
                formKaraoke.ResumePlayback()
            End If
            ' Reprendre la lecture : bouton redevient vert
            If Button_PauseReprise IsNot Nothing Then
                Button_PauseReprise.BackgroundImage = AudioPlay.Resources.AudioPlay_Pause_Vert
            End If
            Button_PauseReprise.Text = "" ' Image seulement, pas de texte
        Else
            lecteur.Pause()
            enPause = True
            ' Mettre en pause le karaoke
            If formKaraoke IsNot Nothing Then
                formKaraoke.PausePlayback()
            End If
            ' En pause : bouton devient rouge
            If Button_PauseReprise IsNot Nothing Then
                Button_PauseReprise.BackgroundImage = AudioPlay.Resources.AudioPlay_Pause_Rouge
            End If
            Button_PauseReprise.Text = "" ' Image seulement, pas de texte
        End If
        ListView1.Focus()
    End Sub

    Private Sub Button_Aleatoire_Click(sender As Object, e As EventArgs) Handles Button_Aleatoire.Click
        ' Vérifier si la lecture automatique est activée
        If Not lectureEnContinu Then
            MessageBox.Show(LanguageManager.GetString("Random_RequiresAutoPlay"),
                          LanguageManager.GetString("Random_Disabled_Title"),
                          MessageBoxButtons.OK,
                          MessageBoxIcon.Information)
            ListView1.Focus()
            Return
        End If

        modeAleatoire = Not modeAleatoire
        MettreAJourBoutonAleatoire()
        SauvegarderParametres()
        ListView1.Focus()
    End Sub

    Private Sub MettreAJourBoutonAleatoire()
        ' Mettre à jour l'apparence du bouton selon l'état du mode aléatoire
        If Button_Aleatoire IsNot Nothing Then
            If modeAleatoire Then
                Button_Aleatoire.BackgroundImage = AudioPlay.Resources.AudioPlay_Random_Vert
            Else
                Button_Aleatoire.BackgroundImage = AudioPlay.Resources.AudioPlay_Random_Gris
            End If
            Button_Aleatoire.BackColor = Color.Transparent
            Button_Aleatoire.Text = "" ' Image seulement, pas de texte
        End If
    End Sub

    Private Sub MetronomeTimer_Tick(sender As Object, e As EventArgs)
        Try
            If metronomeEnCours Is Nothing OrElse formLight Is Nothing Then
                If metronomeTimer IsNot Nothing Then
                    metronomeTimer.Stop()
                End If
                Return
            End If

            ' Calculer le temps écoulé depuis le début
            Dim tempsEcoule As Double = (DateTime.Now - metronomeDebutTime).TotalMilliseconds

            ' Calculer quel beat devrait être joué maintenant
            Dim beatActuel As Integer = CInt(Math.Floor(tempsEcoule / metronomeMillisParBeat)) + 1

            ' Si on a atteint un nouveau beat qui n'a pas encore été traité
            If beatActuel <= metronomeNombreBeats AndAlso Not metronomeBeatsPasses.Contains(beatActuel) Then
                metronomeBeatsPasses.Add(beatActuel)
                formLight.FlashBeat()
                System.Diagnostics.Debug.WriteLine($"LED Beat {beatActuel}/{metronomeNombreBeats} à {tempsEcoule:F0}ms")
            End If

            ' Arrêter le timer si tous les beats sont passés
            If beatActuel > metronomeNombreBeats Then
                If metronomeTimer IsNot Nothing Then
                    metronomeTimer.Stop()
                End If
                ' Fermer la LED après 500ms
                Task.Delay(500).ContinueWith(Sub(t)
                                                 Try
                                                     If Not Me.IsDisposed AndAlso Me.IsHandleCreated Then
                                                         Me.Invoke(Sub()
                                                                       If formLight IsNot Nothing Then
                                                                           formLight.HideLight()
                                                                       End If
                                                                   End Sub)
                                                     End If
                                                 Catch
                                                     ' Ignorer les erreurs si le form est fermé
                                                 End Try
                                             End Sub)
            End If
        Catch
            ' Ignorer les erreurs
        End Try
    End Sub

    Private Sub DemarrerTimerMetronomeLED()
        ' Enregistrer l'heure de début
        metronomeDebutTime = DateTime.Now

        ' Initialiser les variables de suivi
        metronomeBeatsPasses = New HashSet(Of Integer)()
        metronomeMillisParBeat = (60000.0 / metronomeBPM) ' millisecondes par beat

        ' Créer un timer pour vérifier les beats
        If metronomeTimer IsNot Nothing Then
            RemoveHandler metronomeTimer.Tick, AddressOf MetronomeTimer_Tick
            metronomeTimer.Stop()
            metronomeTimer.Dispose()
        End If

        metronomeTimer = New System.Windows.Forms.Timer()
        metronomeTimer.Interval = 10 ' Vérifier toutes les 10ms pour précision
        AddHandler metronomeTimer.Tick, AddressOf MetronomeTimer_Tick
        metronomeTimer.Start()
    End Sub

    Private Sub OnMetronomeBeat(beatNumber As Integer, totalBeats As Integer)
        ' Ancienne méthode conservée pour compatibilité mais non utilisée
    End Sub

    Private Sub Button_Power_Click(sender As Object, e As EventArgs) Handles Button_Power.Click
        ' Fermer l'application proprement en déclenchant FormClosing
        Me.Close()
    End Sub


    Private Sub Button_InfoSelect_Click(sender As Object, e As EventArgs) Handles Button_InfoSelect.Click
        If ListView1.SelectedItems.Count = 0 Then
            MessageBox.Show(LanguageManager.GetString("Playlist_SelectItem"), LanguageManager.GetString("Info_Title"), MessageBoxButtons.OK, MessageBoxIcon.Information)
            ListView1.Focus()
            Return
        End If

        Dim formMeta As New FormMetadonnees()
        formMeta.DefinirItem(ListView1.SelectedItems(0))
        formMeta.ShowDialog(Me) ' Passer Form1 comme owner
        ListView1.Focus()
    End Sub

    Private Sub Button_Playlist_Click(sender As Object, e As EventArgs) Handles Button_Playlist.Click
        ' Créer un menu contextuel pour les options de playlist
        Dim menuPlaylist As New ContextMenuStrip

        ' Changer l'image du bouton en vert quand le menu s'ouvre
        AddHandler menuPlaylist.Opening, Sub()
                                             menuPlaylistOuvert = True
                                             If Button_Playlist IsNot Nothing Then
                                                 Button_Playlist.BackgroundImage = Resources.AudioPlay_GererListe_Vert
                                             End If
                                         End Sub

        ' Remettre l'image du bouton en gris quand le menu se ferme
        AddHandler menuPlaylist.Closed, Sub()
                                            menuPlaylistOuvert = False
                                            If Button_Playlist IsNot Nothing Then
                                                Button_Playlist.BackgroundImage = Resources.AudioPlay_GererListe_Grise
                                            End If
                                            ListView1.Focus()
                                        End Sub

        ' Option 1 : Ouvrir une liste
        Dim menuItemOuvrir As New ToolStripMenuItem(LanguageManager.GetString("Menu_OpenList"))
        AddHandler menuItemOuvrir.Click, Sub() OuvrirPlaylist()

        ' Option 2 : Enregistrer la liste
        Dim menuItemEnregistrer As New ToolStripMenuItem(LanguageManager.GetString("Menu_SaveList"))
        AddHandler menuItemEnregistrer.Click, Sub() EnregistrerPlaylist()

        ' Option 3 : Nouvelle liste (vide)
        Dim menuItemNouvelle As New ToolStripMenuItem(LanguageManager.GetString("Menu_NewList"))
        AddHandler menuItemNouvelle.Click, Sub() NouvellePlaylist()

        ' Option 4 : Effacer la liste
        Dim menuItemEffacer As New ToolStripMenuItem(LanguageManager.GetString("Menu_ClearList"))
        AddHandler menuItemEffacer.Click, Sub() NouvellePlaylist()

        ' Ajouter les options au menu
        menuPlaylist.Items.Add(menuItemOuvrir)
        menuPlaylist.Items.Add(menuItemEnregistrer)
        menuPlaylist.Items.Add(menuItemNouvelle)
        menuPlaylist.Items.Add(menuItemEffacer)

        ' Afficher le menu sous le bouton
        menuPlaylist.Show(Button_Playlist, New Point(0, Button_Playlist.Height))
    End Sub

    ' Méthode pour ouvrir une playlist
    Private Sub OuvrirPlaylist()
        Using ofd As New OpenFileDialog With {
            .Filter = LanguageManager.GetString("PlaylistFilesFilter"),
            .Title = LanguageManager.GetString("OpenPlaylist"),
            .RestoreDirectory = True
        }
            ' Utiliser le dernier répertoire spécifique pour les opérations de playlist
            If Not String.IsNullOrEmpty(dernierRepertoirePlaylist) AndAlso Directory.Exists(dernierRepertoirePlaylist) Then
                ofd.InitialDirectory = dernierRepertoirePlaylist
            ElseIf Not String.IsNullOrEmpty(repertoireParDefaut) AndAlso Directory.Exists(repertoireParDefaut) Then
                ofd.InitialDirectory = repertoireParDefaut
            End If

            If ofd.ShowDialog() = DialogResult.OK Then
                dernierRepertoirePlaylist = Path.GetDirectoryName(ofd.FileName)
                ' Écrire dans ParametresGlobaux uniquement la mémoire simple (ne pas laisser FormDJ l'écraser)
                Try
                    ParametresGlobaux.dernierRepertoirePlaylist_Simple = dernierRepertoirePlaylist
                    ParametresGlobauxHelpers.EcrireCleParametres("DernierRepertoirePlaylist", dernierRepertoirePlaylist)
                Catch
                End Try
                SauvegarderParametres()

                ' Ajouter la playlist à la suite de la liste actuelle
                Try
                    Dim lignes = File.ReadAllLines(ofd.FileName)
                    Dim bpmEnAttente As String = ""

                    For Each ligne In lignes
                        ligne = ligne.Trim()

                        If String.IsNullOrEmpty(ligne) Then
                            Continue For
                        End If

                        If ligne.StartsWith("#BPM=") Then
                            bpmEnAttente = ligne.Substring("#BPM=".Length).Trim()
                            Continue For
                        End If

                        If Not ligne.StartsWith("#") Then
                            ' Ajouter le fichier même s'il n'existe pas temporairement
                            ' (lecteur réseau non monté, etc.)
                            AjouterFichierAListe(ligne)

                            If Not String.IsNullOrEmpty(bpmEnAttente) AndAlso ListView1.Items.Count > 0 Then
                                Dim bpmCharge As Double
                                If Double.TryParse(bpmEnAttente, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, bpmCharge) Then
                                    ListView1.Items(ListView1.Items.Count - 1).SubItems(2).Text = bpmCharge.ToString("F2", Globalization.CultureInfo.InvariantCulture)
                                    ' Synchroniser aussi dans le Tag
                                    Dim itemAjoute = ListView1.Items(ListView1.Items.Count - 1)
                                    If TypeOf itemAjoute.Tag Is Dictionary(Of String, Object) Then
                                        Dim tagDict = DirectCast(itemAjoute.Tag, Dictionary(Of String, Object))
                                        If Not tagDict.ContainsKey("BPM") Then
                                            tagDict.Add("BPM", bpmCharge)
                                        Else
                                            tagDict("BPM") = bpmCharge
                                        End If
                                    End If
                                Else
                                    ListView1.Items(ListView1.Items.Count - 1).SubItems(2).Text = bpmEnAttente
                                    ' Essayer de synchroniser aussi dans le Tag si possible
                                    Dim itemAjoute = ListView1.Items(ListView1.Items.Count - 1)
                                    Dim bpmColValue As Double
                                    If Double.TryParse(bpmEnAttente, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, bpmColValue) Then
                                        If TypeOf itemAjoute.Tag Is Dictionary(Of String, Object) Then
                                            Dim tagDict = DirectCast(itemAjoute.Tag, Dictionary(Of String, Object))
                                            If Not tagDict.ContainsKey("BPM") Then
                                                tagDict.Add("BPM", bpmColValue)
                                            Else
                                                tagDict("BPM") = bpmColValue
                                            End If
                                        End If
                                    End If
                                End If
                            End If

                            bpmEnAttente = ""
                        End If
                    Next

                    MettreAJourNumerotation()
                    MessageBox.Show(LanguageManager.GetString("Playlist_Loaded", Path.GetFileName(ofd.FileName)), LanguageManager.GetString("Success_Title"), MessageBoxButtons.OK, MessageBoxIcon.Information)
                Catch ex As Exception
                    MessageBox.Show(LanguageManager.GetString("Error_LoadingPlaylist", ex.Message), LanguageManager.GetString("Error_Title"), MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            End If
        End Using
    End Sub

    ' Méthode pour enregistrer la playlist
    Private Sub EnregistrerPlaylist()
        Using sfd As New SaveFileDialog With {
            .Filter = LanguageManager.GetString("PlaylistFilesFilter"),
            .DefaultExt = "m3u",
            .Title = LanguageManager.GetString("SavePlaylistAs")
        }
            If Not String.IsNullOrEmpty(repertoireParDefaut) AndAlso Directory.Exists(repertoireParDefaut) Then
                sfd.InitialDirectory = repertoireParDefaut
            End If

            If sfd.ShowDialog() = DialogResult.OK Then
                repertoireParDefaut = Path.GetDirectoryName(sfd.FileName)
                SauvegarderParametres()

                Try
                    Using writer As New StreamWriter(sfd.FileName, False, System.Text.Encoding.UTF8)
                        writer.WriteLine("#EXTM3U")
                        For Each item As ListViewItem In ListView1.Items
                            Dim chemin As String = ""
                            If TypeOf item.Tag Is Dictionary(Of String, Object) Then
                                Dim tagDict = DirectCast(item.Tag, Dictionary(Of String, Object))
                                If tagDict.ContainsKey("Chemin") Then
                                    chemin = tagDict("Chemin")?.ToString()
                                End If
                            ElseIf TypeOf item.Tag Is String Then
                                chemin = item.Tag.ToString()
                            End If

                            If Not String.IsNullOrEmpty(chemin) Then
                                Dim bpmTexte As String = ""
                                If item.SubItems.Count > 2 Then
                                    bpmTexte = item.SubItems(2).Text.Trim()
                                End If

                                If Not String.IsNullOrEmpty(bpmTexte) Then
                                    Dim bpmValue As Double
                                    If Double.TryParse(bpmTexte, Globalization.NumberStyles.Float, Globalization.CultureInfo.CurrentCulture, bpmValue) OrElse
                                       Double.TryParse(bpmTexte, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, bpmValue) Then
                                        writer.WriteLine($"#BPM={bpmValue.ToString("F2", Globalization.CultureInfo.InvariantCulture)}")
                                    Else
                                        writer.WriteLine($"#BPM={bpmTexte}")
                                    End If
                                End If

                                writer.WriteLine(chemin)
                            End If
                        Next
                    End Using
                    MessageBox.Show(LanguageManager.GetString("Playlist_Saved", Path.GetFileName(sfd.FileName)), LanguageManager.GetString("Success_Title"), MessageBoxButtons.OK, MessageBoxIcon.Information)
                Catch ex As Exception
                    MessageBox.Show(LanguageManager.GetString("Error_SavingPlaylist", ex.Message), LanguageManager.GetString("Error_Title"), MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            End If
        End Using
    End Sub

    ' Méthode pour créer une nouvelle playlist vide
    Private Sub NouvellePlaylist()
        If ListView1.Items.Count > 0 Then
            Dim result = MessageBox.Show(
                LanguageManager.GetString("Playlist_ClearConfirm"),
                LanguageManager.GetString("Confirmation_Title"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            )
            If result = DialogResult.Yes Then
                ArreterLecture()
                ListView1.Items.Clear()
                SauvegarderPlaylist()

                ' Supprimer aussi le backup pour éviter qu'il soit restauré au prochain démarrage
                Try
                    Dim dossier = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "AudioPlay")
                    Dim fichierBackup = Path.Combine(dossier, "playlist.txt.bak")
                    If File.Exists(fichierBackup) Then
                        File.Delete(fichierBackup)
                    End If
                Catch
                    ' Ignorer les erreurs de suppression du backup
                End Try

                MessageBox.Show(LanguageManager.GetString("Playlist_NewCreated"), LanguageManager.GetString("Success_Title"), MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        Else
            MessageBox.Show(LanguageManager.GetString("Playlist_AlreadyEmpty"), LanguageManager.GetString("Info_Title"), MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
    End Sub

    Private Sub Button_AjoutFichier_Click(sender As Object, e As EventArgs) Handles Button_Ajout.Click
        ' Créer un menu contextuel pour les options d'ajout
        Dim menuAjout As New ContextMenuStrip

        ' Changer l'image du bouton en vert quand le menu s'ouvre
        AddHandler menuAjout.Opening, Sub()
                                          menuAjoutOuvert = True
                                          If Button_Ajout IsNot Nothing Then
                                              Button_Ajout.BackgroundImage = Resources.AudioPlay_Ajout__Vert
                                          End If
                                      End Sub

        ' Remettre l'image du bouton en gris quand le menu se ferme
        AddHandler menuAjout.Closed, Sub()
                                         menuAjoutOuvert = False
                                         If Button_Ajout IsNot Nothing Then
                                             Button_Ajout.BackgroundImage = Resources.AudioPlay_Ajout__Gris
                                         End If
                                         ListView1.Focus()
                                     End Sub

        ' Option 1 : Ajout d'un fichier
        Dim menuItemFichier As New ToolStripMenuItem(LanguageManager.GetString("Menu_AddFile"))
        AddHandler menuItemFichier.Click, Sub() AjouterFichier()

        ' Option 2 : Ajout d'un répertoire
        Dim menuItemRepertoire As New ToolStripMenuItem(LanguageManager.GetString("Menu_AddDirectory"))
        AddHandler menuItemRepertoire.Click, Sub() AjouterRepertoire()

        ' Option 3 : Ajout d'un CD audio (avec sous-menu des lecteurs)
        Dim menuItemCDAudio As New ToolStripMenuItem(LanguageManager.GetString("Menu_AddCDAudio"))

        ' ✅ Rafraîchir la liste des lecteurs à chaque ouverture du sous-menu
        AddHandler menuItemCDAudio.DropDownOpening, Sub(senderMenu, eMenu)
                                                        ' Vider le sous-menu existant
                                                        menuItemCDAudio.DropDownItems.Clear()

                                                        ' Détecter tous les lecteurs CD/DVD/Bluray
                                                        Dim lecteurs = CDAudioManager.DetecterLecteursCDAudio()
                                                        System.Diagnostics.Debug.WriteLine($"[Form1] {lecteurs.Count} lecteur(s) CD détecté(s)")

                                                        If lecteurs.Count > 0 Then
                                                            ' Créer un sous-menu pour chaque lecteur
                                                            For Each lecteurCD In lecteurs
                                                                Dim lecteurLocal = lecteurCD ' Capture locale pour le lambda

                                                                ' Vérifier si un CD est présent en essayant de lire les pistes
                                                                Dim cdPresent As Boolean = False
                                                                Dim nombrePistes As Integer = 0

                                                                Try
                                                                    System.Diagnostics.Debug.WriteLine($"[Form1] === Vérification lecteur {lecteurLocal} ===")

                                                                    ' Essayer directement de lire les pistes via DeviceIoControl
                                                                    Dim pistes = CDAudioManager.LirePistesCD(lecteurLocal)
                                                                    nombrePistes = pistes.Count
                                                                    cdPresent = nombrePistes > 0

                                                                    If cdPresent Then
                                                                        System.Diagnostics.Debug.WriteLine($"[Form1] ✓ Lecteur {lecteurLocal}: {nombrePistes} pistes détectées")
                                                                    Else
                                                                        System.Diagnostics.Debug.WriteLine($"[Form1] ✗ Lecteur {lecteurLocal}: Aucune piste (vide ou pas de CD audio)")
                                                                    End If
                                                                Catch ex As Exception
                                                                    System.Diagnostics.Debug.WriteLine($"[Form1] ✗ Erreur lecture lecteur {lecteurLocal}: {ex.Message}")
                                                                End Try

                                                                ' Créer le texte du menu avec traduction
                                                                Dim texteMenu As String
                                                                Dim tracksLabel As String = LanguageManager.GetString("Menu_CDTracks")
                                                                Dim emptyLabel As String = LanguageManager.GetString("Menu_CDEmpty")

                                                                If cdPresent Then
                                                                    texteMenu = $"💿 {lecteurLocal} ({nombrePistes} {tracksLabel})"
                                                                Else
                                                                    texteMenu = $"📀 {lecteurLocal} ({emptyLabel})"
                                                                End If

                                                                Dim menuLecteur As New ToolStripMenuItem(texteMenu)

                                                                ' Désactiver si pas de CD
                                                                menuLecteur.Enabled = cdPresent

                                                                ' Si CD présent, texte noir, sinon gris
                                                                If Not cdPresent Then
                                                                    menuLecteur.ForeColor = Color.Gray
                                                                End If

                                                                ' Action : ajouter les pistes de ce lecteur
                                                                AddHandler menuLecteur.Click, Sub() AjouterCDAudioDuLecteur(lecteurLocal)

                                                                menuItemCDAudio.DropDownItems.Add(menuLecteur)
                                                            Next
                                                        Else
                                                            ' Aucun lecteur détecté
                                                            Dim menuAucun As New ToolStripMenuItem(LanguageManager.GetString("Menu_NoCDDrive"))
                                                            menuAucun.Enabled = False
                                                            menuAucun.ForeColor = Color.Gray
                                                            menuItemCDAudio.DropDownItems.Add(menuAucun)
                                                        End If
                                                    End Sub

        ' Ajouter les options au menu
        menuAjout.Items.Add(menuItemFichier)
        menuAjout.Items.Add(menuItemRepertoire)
        menuAjout.Items.Add(menuItemCDAudio)

        ' Afficher le menu sous le bouton
        menuAjout.Show(Button_Ajout, New Point(0, Button_Ajout.Height))
    End Sub

    ' Méthode pour ajouter un fichier
    Private Sub AjouterFichier()
        Using ofd As New OpenFileDialog With {
            .Filter = LanguageManager.GetString("AudioFilesFilter"),
            .Multiselect = True,
            .Title = LanguageManager.GetString("SelectAudioFiles"),
            .RestoreDirectory = True
        }
            ' Utiliser le dernier répertoire spécifique pour l'ajout de fichiers
            If Not String.IsNullOrEmpty(ParametresGlobaux.dernierRepertoireAjoutFichier) AndAlso Directory.Exists(ParametresGlobaux.dernierRepertoireAjoutFichier) Then
                ofd.InitialDirectory = ParametresGlobaux.dernierRepertoireAjoutFichier
            ElseIf Not String.IsNullOrEmpty(ParametresGlobaux.repertoireParDefaut) AndAlso Directory.Exists(ParametresGlobaux.repertoireParDefaut) Then
                ofd.InitialDirectory = ParametresGlobaux.repertoireParDefaut
            End If
            ofd.RestoreDirectory = True

            If ofd.ShowDialog() = DialogResult.OK Then
                If ofd.FileNames IsNot Nothing AndAlso ofd.FileNames.Length > 0 Then
                    Dim chosen = Path.GetDirectoryName(ofd.FileNames(0))
                    Try
                        ParametresGlobaux.dernierRepertoireAjoutFichier = chosen
                    Catch
                    End Try
                    SauvegarderParametres()
                End If

                For Each fichier In ofd.FileNames
                    AjouterFichierAListe(fichier)
                Next
                MettreAJourNumerotation()
                SauvegarderPlaylist()
            End If
        End Using
    End Sub

    ' Méthode pour ajouter un répertoire
    Private Sub AjouterRepertoire()
        Using fbd As New FolderBrowserDialog With {
            .Description = LanguageManager.GetString("SelectFolder"),
            .ShowNewFolderButton = False
        }
            ' Déterminer de façon déterministe le répertoire d'ouverture :
            ' 1) si l'utilisateur a précédemment choisi un dossier exact (dernierRepertoireAjoutRepertoireChoisi),
            '    ouvrir dans le parent direct de ce dossier;
            ' 2) sinon, si un parent déjà enregistré existe (dernierRepertoireAjoutRepertoire), l'utiliser tel quel;
            ' 3) sinon utiliser repertoireParDefaut.
            Try
                Dim initialPath As String = Nothing
                If Not String.IsNullOrEmpty(ParametresGlobaux.dernierRepertoireAjoutRepertoireChoisi) AndAlso Directory.Exists(ParametresGlobaux.dernierRepertoireAjoutRepertoireChoisi) Then
                    Dim parent = Directory.GetParent(ParametresGlobaux.dernierRepertoireAjoutRepertoireChoisi)
                    If parent IsNot Nothing AndAlso Directory.Exists(parent.FullName) Then
                        initialPath = parent.FullName
                    End If
                End If
                If String.IsNullOrEmpty(initialPath) AndAlso Not String.IsNullOrEmpty(ParametresGlobaux.dernierRepertoireAjoutRepertoire) AndAlso Directory.Exists(ParametresGlobaux.dernierRepertoireAjoutRepertoire) Then
                    initialPath = ParametresGlobaux.dernierRepertoireAjoutRepertoire
                End If
                If String.IsNullOrEmpty(initialPath) AndAlso Not String.IsNullOrEmpty(repertoireParDefaut) AndAlso Directory.Exists(repertoireParDefaut) Then
                    initialPath = repertoireParDefaut
                End If
                If Not String.IsNullOrEmpty(initialPath) Then
                    fbd.SelectedPath = initialPath
                End If
            Catch
                ' Ignorer et laisser FolderBrowserDialog choisir par défaut
            End Try

            ' Préserver le répertoire courant pour éviter que FolderBrowserDialog change le WorkingDirectory
            Dim cwd = Environment.CurrentDirectory
            ' Contournement : créer un dossier temporaire DANS le parent voulu et ouvrir le dialogue dessus.
            ' Certaines versions de FolderBrowserDialog ignorent SelectedPath ; en ouvrant un dossier réel
            ' créé dans le parent voulu on force l'explorateur à afficher ce parent.
            Dim tmpFolder As String = Nothing
            Try
                If Not String.IsNullOrEmpty(fbd.SelectedPath) AndAlso Directory.Exists(fbd.SelectedPath) Then
                    Try
                        tmpFolder = Path.Combine(fbd.SelectedPath, ".AudioPlayTmp_" & Guid.NewGuid().ToString("N"))
                        Directory.CreateDirectory(tmpFolder)
                        fbd.SelectedPath = tmpFolder
                        Environment.CurrentDirectory = tmpFolder
                    Catch
                        tmpFolder = Nothing
                    End Try
                End If
            Catch
            End Try

            If fbd.ShowDialog() = DialogResult.OK Then
                Dim chosen = fbd.SelectedPath
                ' Enregistrer le parent du répertoire choisi afin qu'au prochain démarrage
                ' le dialogue s'ouvre dans le répertoire précédent et non dans celui choisi.
                ' Garder l'avant-dernier pour restaurer l'ouverture au dossier parent
                Try
                    ParametresGlobaux.avantDernierRepertoireAjoutRepertoire = ParametresGlobaux.dernierRepertoireAjoutRepertoire
                Catch
                End Try
                ' Conserver aussi le répertoire choisi (non transformé)
                Try
                    ParametresGlobaux.dernierRepertoireAjoutRepertoireChoisi = chosen
                Catch
                End Try

                Dim toSave As String = chosen
                Try
                    Dim parent = Directory.GetParent(chosen)
                    If parent IsNot Nothing AndAlso Directory.Exists(parent.FullName) Then
                        toSave = parent.FullName
                    End If
                Catch
                    toSave = chosen
                End Try

                Try
                    ParametresGlobaux.dernierRepertoireAjoutRepertoire = toSave
                Catch
                End Try
                ' Log après sauvegarde (debug temporaire)
                Try
                    Dim debugFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioPlay", "debug_repertoires.txt")
                    Dim msg = $"[{DateTime.Now:O}] AfterSave: chosen='{chosen}' savedAs='{toSave}' Form1.dernierRepertoireAjoutRepertoire='{dernierRepertoireAjoutRepertoire}' ParametresGlobaux.dernierRepertoireAjoutRepertoire='{ParametresGlobaux.dernierRepertoireAjoutRepertoire}'{Environment.NewLine}"
                    File.AppendAllText(debugFile, msg)
                Catch
                End Try
                SauvegarderParametres()
                ' Restaurer le répertoire courant et supprimer le dossier temporaire si créé (utiliser méthode robuste)
                Try
                    ParametresGlobaux.SupprimerDossierTemporaire(tmpFolder)
                Catch
                End Try
                Try
                    Environment.CurrentDirectory = cwd
                Catch
                End Try

                Dim extensions = {".mp3", ".wav", ".flac", ".wma", ".aac", ".ogg"}
                Dim fichiers = Directory.GetFiles(fbd.SelectedPath, "*.*", SearchOption.AllDirectories) _
                    .Where(Function(f) extensions.Contains(Path.GetExtension(f).ToLower()))

                For Each fichier In fichiers
                    AjouterFichierAListe(fichier)
                Next

                MettreAJourNumerotation()
                SauvegarderPlaylist()
            End If
        End Using
    End Sub

    ' Méthode pour ajouter un CD audio
    ' Méthode pour ajouter un CD audio depuis un lecteur spécifique
    Private Sub AjouterCDAudioDuLecteur(lecteur As String)
        Try
            System.Diagnostics.Debug.WriteLine($"[AjouterCDAudioDuLecteur] Ajout des pistes du lecteur {lecteur}")

            ' Lire les pistes du CD
            Dim pistes = CDAudioManager.LirePistesCD(lecteur)

            If pistes.Count = 0 Then
                MessageBox.Show($"Aucune piste audio détectée sur le CD dans le lecteur {lecteur}.",
                              "CD Audio",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            ' ✅ Ouvrir le formulaire de sélection des pistes
            Using formSelecteur As New FormSelecteurPistesCD(lecteur, pistes)
                Dim result = formSelecteur.ShowDialog(Me)

                ' Redonner le focus à ListView1 après fermeture du formulaire
                If ListView1 IsNot Nothing Then
                    ListView1.Focus()
                End If

                If result = DialogResult.OK Then
                    ' Récupérer les pistes sélectionnées
                    Dim pistesSelectionnees = formSelecteur.ObtenirPistesSelectionnees()

                    If pistesSelectionnees.Count = 0 Then
                        ' L'utilisateur n'a rien sélectionné
                        Return
                    End If

                    ' Ajouter chaque piste sélectionnée à la liste
                    Dim cdIcon As String = "💿 " ' Icône CD
                    Dim trackPrefix As String = LanguageManager.GetString("CDTrack_Prefix") ' "Piste", "Track", etc.
                    Dim cdInfo = formSelecteur.ObtenirMetadonnees()

                    For Each piste In pistesSelectionnees
                        ' Utiliser le titre réel si disponible, sinon format générique
                        Dim nomAffiche As String
                        If Not String.IsNullOrEmpty(piste.Title) Then
                            ' Format: 💿 01. Paul McCartney - Another Day
                            If Not String.IsNullOrEmpty(piste.Artist) Then
                                nomAffiche = $"{cdIcon}{piste.TrackNumber:D2}. {piste.Artist} - {piste.Title}"
                            Else
                                nomAffiche = $"{cdIcon}{piste.TrackNumber:D2}. {piste.Title}"
                            End If
                        Else
                            nomAffiche = $"{cdIcon}{trackPrefix} {piste.TrackNumber:D2}"
                        End If

                        Dim dureeStr As String = piste.Duration.ToString("mm\:ss")

                        ' Créer l'item de liste avec le chemin virtuel CDDA://
                        Dim item As New ListViewItem(New String() {"", nomAffiche, "", dureeStr}) With {
                            .Tag = New Dictionary(Of String, Object) From {
                                {"Chemin", piste.VirtualPath},
                                {"Nom", nomAffiche},
                                {"BPM", 0},
                                {"Duree", piste.Duration.TotalSeconds},
                                {"TypeSource", "CDAudio"},
                                {"CDDrive", piste.Drive},
                                {"CDTrackNumber", piste.TrackNumber}
                            }
                        }

                        ' Ajouter l'artiste et l'album si disponibles
                        If cdInfo IsNot Nothing Then
                            Dim tagDict = DirectCast(item.Tag, Dictionary(Of String, Object))
                            If Not String.IsNullOrEmpty(cdInfo.Artist) Then
                                tagDict.Add("CDArtist", cdInfo.Artist)
                            End If
                            If Not String.IsNullOrEmpty(cdInfo.Album) Then
                                tagDict.Add("CDAlbum", cdInfo.Album)
                            End If
                        End If

                        ' Ajouter l'artiste spécifique de la piste si disponible
                        If Not String.IsNullOrEmpty(piste.Artist) Then
                            Dim tagDict = DirectCast(item.Tag, Dictionary(Of String, Object))
                            tagDict("CDArtist") = piste.Artist ' Écraser avec l'artiste spécifique
                        End If

                        ListView1.Items.Add(item)
                    Next

                    MettreAJourNumerotation()
                    SauvegarderPlaylist()

                    System.Diagnostics.Debug.WriteLine($"[AjouterCDAudioDuLecteur] {pistesSelectionnees.Count} pistes ajoutées du lecteur {lecteur}")
                End If
            End Using

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[AjouterCDAudioDuLecteur] Erreur: {ex.Message}")
            MessageBox.Show($"Erreur lors de l'ajout du CD audio: {ex.Message}",
                          "Erreur",
                          MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub Button_CalculBPM_Click(sender As Object, e As EventArgs) Handles Button_CalculBPM.Click
        ' Bloquer le calcul BPM si une boucle est active
        If loopEnabled AndAlso hasLoopMarkers Then
            MessageBox.Show(LanguageManager.GetString("BPM_BlockedDuringLoop_Message"),
                          LanguageManager.GetString("Loop_Active_Title"),
                          MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        ' Créer un menu contextuel pour les options de calcul BPM
        Dim menuBPM As New ContextMenuStrip()

        ' Option 1 : Calcul du BPM de l'item sélectionné (seulement si pas de BPM)
        Dim menuItemSelectionne As New ToolStripMenuItem(LanguageManager.GetString("Menu_BPM_CalcSelected"))
        AddHandler menuItemSelectionne.Click, AddressOf CalculerBPMItemSelectionne
        menuBPM.Items.Add(menuItemSelectionne)

        ' Option 2 : Recalcul FORCÉ du BPM de l'item sélectionné
        Dim menuItemRecalculerSelectionne As New ToolStripMenuItem(LanguageManager.GetString("Menu_BPM_RecalcSelected"))
        AddHandler menuItemRecalculerSelectionne.Click, AddressOf RecalculerBPMItemSelectionne
        menuBPM.Items.Add(menuItemRecalculerSelectionne)

        ' Séparateur
        menuBPM.Items.Add(New ToolStripSeparator())

        ' Option 3 : Calcul de tous les items de la liste (seulement si pas de BPM)
        Dim menuItemTous As New ToolStripMenuItem(LanguageManager.GetString("Menu_BPM_CalcAll"))
        AddHandler menuItemTous.Click, AddressOf CalculerBPMTousLesItems
        menuBPM.Items.Add(menuItemTous)

        ' Option 4 : Recalcul FORCÉ de tous les items de la liste
        Dim menuItemRecalculerTous As New ToolStripMenuItem(LanguageManager.GetString("Menu_BPM_RecalcAll"))
        AddHandler menuItemRecalculerTous.Click, AddressOf RecalculerBPMTousLesItems
        menuBPM.Items.Add(menuItemRecalculerTous)

        ' Afficher le menu sous le bouton
        Dim btnCalculBPM As Button = DirectCast(sender, Button)
        menuBPM.Show(btnCalculBPM, New Point(0, btnCalculBPM.Height))
    End Sub

    ' Calculer le BPM de l'item sélectionné (seulement si pas de BPM dans la ListView)
    Private Async Sub CalculerBPMItemSelectionne(sender As Object, e As EventArgs)
        If ListView1.SelectedItems.Count = 0 Then
            MessageBox.Show(LanguageManager.GetString("Playlist_SelectItem"), LanguageManager.GetString("Info_Title"), MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim item = ListView1.SelectedItems(0)

        ' Vérifier si un BPM est déjà affiché dans la ListView
        Dim bpmListView As String = item.SubItems(2).Text.Trim()
        If Not String.IsNullOrEmpty(bpmListView) AndAlso Double.TryParse(bpmListView, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, Nothing) Then
            MessageBox.Show(
                LanguageManager.GetString("BPM_AlreadyExists", bpmListView),
                LanguageManager.GetString("BPM_Title"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            )
            Return
        End If

        ' Obtenir le chemin du fichier
        Dim chemin As String = ""
        If TypeOf item.Tag Is Dictionary(Of String, Object) Then
            Dim tagDict = DirectCast(item.Tag, Dictionary(Of String, Object))
            If tagDict.ContainsKey("Chemin") Then
                chemin = tagDict("Chemin")?.ToString()
            End If
        ElseIf TypeOf item.Tag Is String Then
            chemin = item.Tag.ToString()
        End If

        If String.IsNullOrEmpty(chemin) OrElse Not File.Exists(chemin) Then
            MessageBox.Show(LanguageManager.GetString("Error_FileNotFound"), LanguageManager.GetString("Error_Title"), MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End If

        ' Vérifier si le BPM existe déjà dans les métadonnées
        Dim bpmExistant = BPMMetadataManager.LireBPMPrecisDepuisMetadonnees(chemin)
        If bpmExistant > 0 Then
            Dim resultUtiliser = MessageBox.Show(
                LanguageManager.GetString("BPM_MetadataExists", bpmExistant.ToString("F2", Globalization.CultureInfo.InvariantCulture)),
                LanguageManager.GetString("BPM_Title"),
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question
            )

            If resultUtiliser = DialogResult.Cancel Then
                Return
            ElseIf resultUtiliser = DialogResult.Yes Then
                ' Utiliser le BPM existant
                item.SubItems(2).Text = bpmExistant.ToString("F2", Globalization.CultureInfo.InvariantCulture)
                SauvegarderPlaylist()
                MessageBox.Show(LanguageManager.GetString("BPM_FromMetadata", bpmExistant.ToString("F2", Globalization.CultureInfo.InvariantCulture)), LanguageManager.GetString("BPM_Title"), MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If
            ' Si Non, continuer le calcul
        End If

        ' Désactiver les contrôles pendant le calcul
        Button_CalculBPM.Enabled = False
        MettreCurseurAttenteListView(True)

        ' Indiquer visuellement que le calcul est en cours (couleur orange)
        item.SubItems(2).BackColor = Color.Orange
        item.SubItems(2).Text = LanguageManager.GetString("BPM_Status_Calculating")
        Application.DoEvents()

        Try
            ' ✅ EXÉCUTER EN ARRIÈRE-PLAN (thread worker) pour ne pas bloquer l'UI
            Await Task.Run(Async Function()
                               Try
                                   ' Calculer le BPM avec librosa (si disponible) ou SoundTouch
                                   Dim bpm As Double = Await BPMDetector.DetecterBPM(chemin)

                                   ' Mettre à jour l'UI depuis le thread UI
                                   Me.Invoke(Sub()
                                                 If bpm > 0 Then
                                                     item.SubItems(2).Text = bpm.ToString("F2")
                                                     item.SubItems(2).BackColor = Color.White
                                                     SauvegarderPlaylist()

                                                     ' Sauvegarder dans les métadonnées du fichier (avec précision)
                                                     Dim erreurMsg As String = ""
                                                     Dim sauvegardeOK = BPMMetadataManager.EcrireBPMAvecGestionLecture(chemin, bpm, Me, erreurMsg)

                                                     Dim methode As String = If(PythonManager.EstInstalle(), "librosa", "SoundTouch")
                                                     Dim message As String = LanguageManager.GetString("BPM_ItemCalculated", bpm.ToString("F2"), methode)

                                                     If sauvegardeOK Then
                                                         message &= vbCrLf & LanguageManager.GetString("BPM_MetadataSaved")
                                                     Else
                                                         message &= vbCrLf & LanguageManager.GetString("BPM_MetadataSaveError", erreurMsg)
                                                     End If

                                                     MessageBox.Show(message, LanguageManager.GetString("BPM_Calculation_Title"), MessageBoxButtons.OK, MessageBoxIcon.Information)
                                                 Else
                                                     item.SubItems(2).BackColor = Color.White
                                                     item.SubItems(2).Text = ""
                                                     MessageBox.Show(LanguageManager.GetString("BPM_DetectionFailed"), LanguageManager.GetString("BPM_Calculation_Title"), MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                                 End If
                                             End Sub)

                               Catch ex As Exception
                                   Me.Invoke(Sub()
                                                 item.SubItems(2).BackColor = Color.White
                                                 item.SubItems(2).Text = ""
                                                 MessageBox.Show(LanguageManager.GetString("Error_Calculation", ex.Message), LanguageManager.GetString("Error_Title"), MessageBoxButtons.OK, MessageBoxIcon.Error)
                                             End Sub)
                               End Try
                           End Function)

        Catch ex As Exception
            item.SubItems(2).BackColor = Color.White
            item.SubItems(2).Text = ""
            MessageBox.Show(LanguageManager.GetString("Error_Calculation", ex.Message), LanguageManager.GetString("Error_Title"), MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            ' Réactiver les contrôles
            Button_CalculBPM.Enabled = True
            MettreCurseurAttenteListView(False)
        End Try
    End Sub

    ' Recalculer FORCEMENT le BPM de l'item sélectionné (même s'il existe déjà)
    Private Async Sub RecalculerBPMItemSelectionne(sender As Object, e As EventArgs)
        If ListView1.SelectedItems.Count = 0 Then
            MessageBox.Show(LanguageManager.GetString("Playlist_SelectItem"), LanguageManager.GetString("Info_Title"), MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim item = ListView1.SelectedItems(0)

        ' Obtenir le chemin du fichier
        Dim chemin As String = ""
        If TypeOf item.Tag Is Dictionary(Of String, Object) Then
            Dim tagDict = DirectCast(item.Tag, Dictionary(Of String, Object))
            If tagDict.ContainsKey("Chemin") Then
                chemin = tagDict("Chemin")?.ToString()
            End If
        ElseIf TypeOf item.Tag Is String Then
            chemin = item.Tag.ToString()
        End If

        If String.IsNullOrEmpty(chemin) OrElse Not File.Exists(chemin) Then
            MessageBox.Show(LanguageManager.GetString("Error_FileNotFound"), LanguageManager.GetString("Error_Title"), MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End If

        ' Confirmation de recalcul
        Dim bpmActuel As String = item.SubItems(2).Text.Trim()
        Dim messageConfirm As String = LanguageManager.GetString("BPM_RecalculateSingle_Confirm")
        If Not String.IsNullOrEmpty(bpmActuel) Then
            messageConfirm = LanguageManager.GetString("BPM_RecalculateSingle_WithCurrent", bpmActuel)
        End If

        Dim result = MessageBox.Show(messageConfirm, LanguageManager.GetString("Confirmation_Title"), MessageBoxButtons.YesNo, MessageBoxIcon.Question)
        If result <> DialogResult.Yes Then
            Return
        End If

        ' Désactiver les contrôles pendant le calcul
        Button_CalculBPM.Enabled = False
        MettreCurseurAttenteListView(True)

        ' Indiquer visuellement que le calcul est en cours (couleur orange)
        item.SubItems(2).BackColor = Color.Orange
        item.SubItems(2).Text = LanguageManager.GetString("BPM_Status_Recalculating")
        Application.DoEvents()

        Try
            ' ✅ EXÉCUTER EN ARRIÈRE-PLAN (thread worker) pour ne pas bloquer l'UI
            Await Task.Run(Async Function()
                               Try
                                   ' Calculer le BPM (forcer le calcul)
                                   Dim bpm As Double = Await BPMDetector.DetecterBPM(chemin)

                                   ' Mettre à jour l'UI depuis le thread UI
                                   Me.Invoke(Sub()
                                                 If bpm > 0 Then
                                                     item.SubItems(2).Text = bpm.ToString("F2")
                                                     item.SubItems(2).BackColor = Color.White
                                                     SauvegarderPlaylist()

                                                     ' Sauvegarder dans les métadonnées du fichier (avec précision)
                                                     Dim erreurMsg As String = ""
                                                     Dim sauvegardeOK = BPMMetadataManager.EcrireBPMAvecGestionLecture(chemin, bpm, Me, erreurMsg)

                                                     Dim methode As String = If(PythonManager.EstInstalle(), "librosa", "SoundTouch")
                                                     Dim message As String = LanguageManager.GetString("BPM_ItemRecalculated", bpm.ToString("F2"), methode)

                                                     If sauvegardeOK Then
                                                         message &= vbCrLf & LanguageManager.GetString("BPM_MetadataSaved")
                                                     Else
                                                         message &= vbCrLf & LanguageManager.GetString("BPM_MetadataSaveError", erreurMsg)
                                                     End If

                                                     MessageBox.Show(message, LanguageManager.GetString("BPM_Recalculation_Title"), MessageBoxButtons.OK, MessageBoxIcon.Information)
                                                 Else
                                                     item.SubItems(2).BackColor = Color.White
                                                     item.SubItems(2).Text = ""
                                                     MessageBox.Show(LanguageManager.GetString("BPM_DetectionFailed"), LanguageManager.GetString("BPM_Recalculation_Title"), MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                                 End If
                                             End Sub)

                               Catch ex As Exception
                                   Me.Invoke(Sub()
                                                 item.SubItems(2).BackColor = Color.White
                                                 item.SubItems(2).Text = ""
                                                 MessageBox.Show(LanguageManager.GetString("Error_Calculation", ex.Message), LanguageManager.GetString("Error_Title"), MessageBoxButtons.OK, MessageBoxIcon.Error)
                                             End Sub)
                               End Try
                           End Function)
        Finally
            ' Réactiver les contrôles
            Button_CalculBPM.Enabled = True
            MettreCurseurAttenteListView(False)
        End Try
    End Sub

    ' Calculer le BPM de tous les items de la liste (seulement ceux sans BPM dans la ListView)
    Private Async Sub CalculerBPMTousLesItems(sender As Object, e As EventArgs)
        If ListView1.Items.Count = 0 Then
            MessageBox.Show(LanguageManager.GetString("Playlist_Empty"), LanguageManager.GetString("Info_Title"), MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        ' Compter combien d'items n'ont pas de BPM
        Dim itemsSansBPM As Integer = 0
        For Each item As ListViewItem In ListView1.Items
            Dim bpmListView As String = item.SubItems(2).Text.Trim()
            If String.IsNullOrEmpty(bpmListView) OrElse Not Double.TryParse(bpmListView, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, Nothing) Then
                itemsSansBPM += 1
            End If
        Next

        If itemsSansBPM = 0 Then
            MessageBox.Show(
                LanguageManager.GetString("BPM_AllHaveBPM"),
                LanguageManager.GetString("Info_Title"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            )
            Return
        End If

        ' Demander si on veut sauvegarder dans les métadonnées
        Dim sauvegarderMetadonnees As Boolean = False
        Dim resultSauvegarde = MessageBox.Show(
            LanguageManager.GetString("BPM_CalculateForFiles", itemsSansBPM),
            LanguageManager.GetString("BPM_Calculation_Title"),
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question
        )

        If resultSauvegarde = DialogResult.Cancel Then Return
        sauvegarderMetadonnees = (resultSauvegarde = DialogResult.Yes)

        ' Créer un nouveau token d'annulation
        bpmCancellationTokenSource = New Threading.CancellationTokenSource()
        Dim cancellationToken = bpmCancellationTokenSource.Token

        ' Désactiver les contrôles pendant le calcul
        Button_CalculBPM.Enabled = False
        calculBPMEnCours = True
        MettreCurseurAttenteListView(True)

        Try
            Dim compteur As Integer = 0
            Dim compteurReussi As Integer = 0
            Dim compteurEchec As Integer = 0
            Dim compteurSauvegardeOK As Integer = 0
            Dim compteurSauvegardeEchec As Integer = 0
            Dim compteurBPMExistant As Integer = 0
            Dim compteurIgnore As Integer = 0

            For Each item As ListViewItem In ListView1.Items
                ' Vérifier si l'annulation a été demandée
                If cancellationToken.IsCancellationRequested Then
                    Exit For
                End If

                ' Vérifier si un BPM est déjà affiché dans la ListView
                Dim bpmListView As String = item.SubItems(2).Text.Trim()
                If Not String.IsNullOrEmpty(bpmListView) AndAlso Double.TryParse(bpmListView, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, Nothing) Then
                    ' Ignorer cet item car il a déjà un BPM
                    compteurIgnore += 1
                    Continue For
                End If

                ' Obtenir le chemin du fichier
                Dim chemin As String = ""
                If TypeOf item.Tag Is Dictionary(Of String, Object) Then
                    Dim tagDict = DirectCast(item.Tag, Dictionary(Of String, Object))
                    If tagDict.ContainsKey("Chemin") Then
                        chemin = tagDict("Chemin")?.ToString()
                    End If
                ElseIf TypeOf item.Tag Is String Then
                    chemin = item.Tag.ToString()
                End If

                If Not String.IsNullOrEmpty(chemin) AndAlso File.Exists(chemin) Then
                    Try
                        ' Indiquer visuellement que le calcul est en cours (couleur orange)
                        item.SubItems(2).BackColor = Color.Orange
                        item.SubItems(2).Text = LanguageManager.GetString("BPM_Status_Calculating")
                        Application.DoEvents()

                        Dim bpm As Double = 0

                        ' Vérifier si BPM existe dans les métadonnées
                        Dim bpmExistant = BPMMetadataManager.LireBPMPrecisDepuisMetadonnees(chemin)
                        If bpmExistant > 0 Then
                            ' Utiliser le BPM existant (précis)
                            bpm = bpmExistant
                            compteurBPMExistant += 1
                        Else
                            ' ✅ Calculer le BPM EN ARRIÈRE-PLAN (thread worker)
                            bpm = Await Task.Run(Async Function() As Task(Of Double)
                                                     Return Await BPMDetector.DetecterBPM(chemin)
                                                 End Function)
                        End If

                        If bpm > 0 Then
                            item.SubItems(2).Text = bpm.ToString("F2")
                            item.SubItems(2).BackColor = Color.White ' Réinitialiser la couleur
                            compteurReussi += 1

                            ' Sauvegarder dans les métadonnées si demandé et si pas déjà existant (avec précision)
                            If sauvegarderMetadonnees AndAlso bpmExistant = 0 Then
                                Dim erreurMsg As String = ""
                                Dim sauvegardeOK = BPMMetadataManager.EcrireBPMAvecGestionLecture(chemin, bpm, Me, erreurMsg)
                                If sauvegardeOK Then
                                    compteurSauvegardeOK += 1
                                Else
                                    compteurSauvegardeEchec += 1
                                    System.Diagnostics.Debug.WriteLine($"Erreur sauvegarde BPM {chemin}: {erreurMsg}")
                                End If
                            End If
                        Else
                            item.SubItems(2).BackColor = Color.White ' Réinitialiser la couleur même en cas d'échec
                            item.SubItems(2).Text = ""
                            compteurEchec += 1
                            System.Diagnostics.Debug.WriteLine($"Échec détection BPM pour: {System.IO.Path.GetFileName(chemin)} (BPM=0 retourné)")
                        End If

                    Catch ex As Exception
                        item.SubItems(2).BackColor = Color.White ' Réinitialiser la couleur en cas d'erreur
                        item.SubItems(2).Text = ""
                        System.Diagnostics.Debug.WriteLine($"Exception BPM pour {System.IO.Path.GetFileName(chemin)}: {ex.Message}")
                        compteurEchec += 1
                    End Try
                End If

                compteur += 1

                ' Mettre à jour l'affichage tous les 5 items
                If compteur Mod 5 = 0 Then
                    Application.DoEvents()
                End If
            Next

            SauvegarderPlaylist()

            ' Vérifier si l'opération a été annulée
            If cancellationToken.IsCancellationRequested Then
                MessageBox.Show(
                    LanguageManager.GetString("BPM_CancelledByUser", compteur, compteurReussi, compteurEchec),
                    LanguageManager.GetString("Cancellation_Title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                )
            Else
                Dim methode As String = If(PythonManager.EstInstalle(), "librosa", "SoundTouch")
                Dim message As String = LanguageManager.GetString("BPM_BulkSummary", compteur, compteurIgnore, compteurReussi, compteurBPMExistant, compteurEchec, methode)

                If sauvegarderMetadonnees Then
                    message &= Environment.NewLine & Environment.NewLine &
                              LanguageManager.GetString("BPM_BulkMetadataSummary", compteurSauvegardeOK, compteurSauvegardeEchec)
                End If

                MessageBox.Show(message, LanguageManager.GetString("BPM_Calculation_Title"), MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If

        Catch ex As Exception
            MessageBox.Show(LanguageManager.GetString("Error_Calculation", ex.Message), LanguageManager.GetString("Error_Title"), MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            ' Réactiver les contrôles
            calculBPMEnCours = False
            Button_CalculBPM.Enabled = True
            MettreCurseurAttenteListView(False)
        End Try
    End Sub

    ' Recalculer FORCEMENT le BPM de tous les items (même ceux qui ont déjà un BPM)
    Private Async Sub RecalculerBPMTousLesItems(sender As Object, e As EventArgs)
        If ListView1.Items.Count = 0 Then
            MessageBox.Show(LanguageManager.GetString("Playlist_Empty"), LanguageManager.GetString("Info_Title"), MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        ' Confirmation
        Dim resultConfirm = MessageBox.Show(
            LanguageManager.GetString("BPM_RecalculateAll", ListView1.Items.Count),
            LanguageManager.GetString("BPM_Recalculation_Title"),
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Warning
        )

        If resultConfirm = DialogResult.Cancel Then Return
        Dim sauvegarderMetadonnees As Boolean = (resultConfirm = DialogResult.Yes)

        ' Créer un nouveau token d'annulation
        bpmCancellationTokenSource = New Threading.CancellationTokenSource()
        Dim cancellationToken = bpmCancellationTokenSource.Token

        ' Désactiver les contrôles pendant le calcul
        Button_CalculBPM.Enabled = False
        calculBPMEnCours = True
        MettreCurseurAttenteListView(True)

        Try
            Dim compteur As Integer = 0
            Dim compteurReussi As Integer = 0
            Dim compteurEchec As Integer = 0
            Dim compteurSauvegardeOK As Integer = 0
            Dim compteurSauvegardeEchec As Integer = 0

            For Each item As ListViewItem In ListView1.Items
                ' Vérifier si l'annulation a été demandée
                If cancellationToken.IsCancellationRequested Then
                    Exit For
                End If

                ' Obtenir le chemin du fichier
                Dim chemin As String = ""
                If TypeOf item.Tag Is Dictionary(Of String, Object) Then
                    Dim tagDict = DirectCast(item.Tag, Dictionary(Of String, Object))
                    If tagDict.ContainsKey("Chemin") Then
                        chemin = tagDict("Chemin")?.ToString()
                    End If
                ElseIf TypeOf item.Tag Is String Then
                    chemin = item.Tag.ToString()
                End If

                If Not String.IsNullOrEmpty(chemin) AndAlso File.Exists(chemin) Then
                    Try
                        ' Indiquer visuellement que le recalcul est en cours (couleur orange)
                        item.SubItems(2).BackColor = Color.Orange
                        item.SubItems(2).Text = LanguageManager.GetString("BPM_Status_Recalculating")
                        Application.DoEvents()

                        ' ✅ FORCER le calcul du BPM EN ARRIÈRE-PLAN (thread worker)
                        Dim bpm As Double = Await Task.Run(Async Function() As Task(Of Double)
                                                               Return Await BPMDetector.DetecterBPM(chemin)
                                                           End Function)

                        If bpm > 0 Then
                            item.SubItems(2).Text = bpm.ToString("F2")
                            item.SubItems(2).BackColor = Color.White ' Réinitialiser la couleur
                            compteurReussi += 1

                            ' Sauvegarder dans les métadonnées si demandé (avec précision)
                            If sauvegarderMetadonnees Then
                                Dim erreurMsg As String = ""
                                Dim sauvegardeOK = BPMMetadataManager.EcrireBPMAvecGestionLecture(chemin, bpm, Me, erreurMsg)
                                If sauvegardeOK Then
                                    compteurSauvegardeOK += 1
                                Else
                                    compteurSauvegardeEchec += 1
                                    System.Diagnostics.Debug.WriteLine($"Erreur sauvegarde BPM {chemin}: {erreurMsg}")
                                End If
                            End If
                        Else
                            item.SubItems(2).BackColor = Color.White ' Réinitialiser la couleur même en cas d'échec
                            item.SubItems(2).Text = ""
                            compteurEchec += 1
                            System.Diagnostics.Debug.WriteLine($"Échec recalcul BPM pour: {System.IO.Path.GetFileName(chemin)} (BPM=0 retourné)")
                        End If

                    Catch ex As Exception
                        item.SubItems(2).BackColor = Color.White ' Réinitialiser la couleur en cas d'erreur
                        item.SubItems(2).Text = ""
                        System.Diagnostics.Debug.WriteLine($"Exception recalcul BPM pour {System.IO.Path.GetFileName(chemin)}: {ex.Message}")
                        compteurEchec += 1
                    End Try
                End If

                compteur += 1

                ' Mettre à jour l'affichage tous les 5 items
                If compteur Mod 5 = 0 Then
                    Application.DoEvents()
                End If
            Next

            SauvegarderPlaylist()

            ' Vérifier si l'opération a été annulée
            If cancellationToken.IsCancellationRequested Then
                MessageBox.Show(
                    LanguageManager.GetString("BPM_RecalculationCancelledByUser", compteur, compteurReussi, compteurEchec),
                    LanguageManager.GetString("Cancellation_Title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                )
            Else
                Dim methode As String = If(PythonManager.EstInstalle(), "librosa", "SoundTouch")
                Dim message As String = LanguageManager.GetString("BPM_RebulkSummary", compteur, compteurReussi, compteurEchec, methode)

                If sauvegarderMetadonnees Then
                    message &= Environment.NewLine & Environment.NewLine &
                              LanguageManager.GetString("BPM_BulkMetadataSummary", compteurSauvegardeOK, compteurSauvegardeEchec)
                End If

                MessageBox.Show(message, LanguageManager.GetString("BPM_Recalculation_Title"), MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If

        Catch ex As Exception
            MessageBox.Show(LanguageManager.GetString("Error_Calculation", ex.Message), LanguageManager.GetString("Error_Title"), MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            ' Réactiver les contrôles
            calculBPMEnCours = False
            Button_CalculBPM.Enabled = True
            MettreCurseurAttenteListView(False)
        End Try
    End Sub

    ' ========================================
    ' TRACKBARS AVEC ÉGALISEUR EN TEMPS RÉEL
    ' ========================================
    Private Sub TrackBar_Volume_Scroll(sender As Object, e As EventArgs) Handles TrackBar_Volume.Scroll
        ' Ignorer les événements pendant l'initialisation
        If initialisationEnCours Then Return

        ' Si le mute est activé et qu'on bouge le volume, désactiver le mute
        If isMuted Then
            isMuted = False
            ' Désactiver le mute système également
            WindowsVolumeControl.SetMute(False)
            ' Remettre le bouton Mute à la bonne couleur
            If Button_Mute IsNot Nothing Then
                If lectureEnCours Then
                    Button_Mute.BackgroundImage = AudioPlay.Resources.AudioPlay_Mute_Vert
                Else
                    Button_Mute.BackgroundImage = AudioPlay.Resources.AudioPlay_Mute_Gris
                End If
            End If
        End If

        dernierVolume = TrackBar_Volume.Value / CSng(TrackBar_Volume.Maximum)

        ' Appliquer le volume au système Windows
        WindowsVolumeControl.SetVolume(dernierVolume)

        ' Optionnel : aussi appliquer au volumeProvider pour les effets qui en dépendent
        If volumeProvider IsNot Nothing Then
            ' Appliquer le volume avec le gain de normalisation
            volumeProvider.Volume = dernierVolume * gainNormalisationActuel
        End If

        SauvegarderVolume()
    End Sub

    Private Sub TrackBar_Avancement_Scroll(sender As Object, e As EventArgs) Handles TrackBar_Avancement.Scroll
        If majTrackBarEnCours OrElse fichierAudio Is Nothing Then Return

        Try
            Dim positionSouhaitee = (TrackBar_Avancement.Value / 1000.0) * fichierAudio.TotalTime.TotalSeconds
            fichierAudio.CurrentTime = TimeSpan.FromSeconds(positionSouhaitee)
        Catch ex As Exception
            ' Ignorer les erreurs de positionnement
        End Try
    End Sub

    Private Sub TrackBar_Basses_Scroll(sender As Object, e As EventArgs) Handles TrackBar_Basses.Scroll
        ' Ignorer les événements pendant l'initialisation
        If initialisationEnCours Then Return

        If TrackBar_Basses Is Nothing Then Return
        dernieresBasses = CSng(TrackBar_Basses.Value)

        ' Appliquer en temps réel si en lecture
        If equalizerProvider IsNot Nothing Then
            equalizerProvider.BassGain = dernieresBasses
        End If

        SauvegarderBasses()
    End Sub

    Private Sub TrackBar_Aigues_Scroll(sender As Object, e As EventArgs) Handles TrackBar_Aigues.Scroll
        ' Ignorer les événements pendant l'initialisation
        If initialisationEnCours Then Return

        If TrackBar_Aigues Is Nothing Then Return
        dernieresAigues = CSng(TrackBar_Aigues.Value)

        ' Appliquer en temps réel si en lecture
        If equalizerProvider IsNot Nothing Then
            equalizerProvider.TrebleGain = dernieresAigues
        End If

        SauvegarderAigues()
    End Sub

    ' ========================================
    ' TIMER PROGRESSION
    ' ========================================
    Private Sub TimerProgression_Tick(sender As Object, e As EventArgs) Handles timerProgression.Tick
        If fichierAudio Is Nothing OrElse enPause Then Return

        Try
            majTrackBarEnCours = True

            Dim total = fichierAudio.TotalTime.TotalSeconds
            Dim actuel = fichierAudio.CurrentTime.TotalSeconds

            ' Vérifier si la boucle est activée et si on a dépassé la fin de la boucle
            If loopEnabled AndAlso hasLoopMarkers Then
                If fichierAudio.CurrentTime >= loopEndPosition Then
                    fichierAudio.CurrentTime = loopStartPosition
                End If
            End If

            ' Si la suppression du silence final est active et qu'on a une durée réelle,
            ' arrêter la lecture avant le silence final
            If ParametresGlobaux.SupprimerSilenceFin AndAlso dureeReelleActuelle > TimeSpan.Zero Then
                If actuel >= dureeReelleActuelle.TotalSeconds Then
                    ' On a atteint la fin de la partie audible, arrêter
                    System.Diagnostics.Debug.WriteLine($"Fin de la durée réelle atteinte: {actuel:F2}s >= {dureeReelleActuelle.TotalSeconds:F2}s")
                    Me.Invoke(Sub()
                                  If lecteur IsNot Nothing Then
                                      lecteur.Stop()
                                  End If
                              End Sub)
                    majTrackBarEnCours = False
                    Return
                End If
                ' Utiliser la durée réelle pour le calcul de progression
                total = dureeReelleActuelle.TotalSeconds
            End If

            If total > 0 Then
                TrackBar_Avancement.Value = CInt((actuel / total) * 1000)
            End If

            If Label_DureeRestante IsNot Nothing Then
                Dim restante = total - actuel
                Dim minutes = CInt(Math.Floor(restante / 60))
                Dim secondes = CInt(restante Mod 60)
                Label_DureeRestante.Text = $"-{minutes:D2}:{secondes:D2}"
            End If

            majTrackBarEnCours = False

        Catch ex As Exception
            ' Ignorer les erreurs de mise à jour
        End Try
    End Sub

    ' ========================================
    ' PLAYLIST
    ' ========================================
    Public Sub AjouterFichierAListe(chemin As String, Optional bpmExistant As String = Nothing, Optional dureeExistante As String = Nothing)
        ' Vérifier si c'est une piste CD ou un fichier normal
        Dim estCDAudio = CDAudioManager.EstCheminCDAudio(chemin)

        ' Ne plus rejeter les fichiers inexistants - ils seront marqués visuellement
        ' If Not estCDAudio AndAlso Not File.Exists(chemin) Then Return

        ' Vérifier si le fichier existe déjà
        For Each item As ListViewItem In ListView1.Items
            Dim tagChemin As String = ""
            If TypeOf item.Tag Is Dictionary(Of String, Object) Then
                Dim existingTagDict = DirectCast(item.Tag, Dictionary(Of String, Object))
                If existingTagDict.ContainsKey("Chemin") Then
                    tagChemin = existingTagDict("Chemin")?.ToString()
                End If
            ElseIf TypeOf item.Tag Is String Then
                tagChemin = item.Tag.ToString()
            End If

            If tagChemin = chemin Then Return
        Next

        Dim nomFichier = Path.GetFileName(chemin)
        Dim duree = ""
        Dim bpm = ""
        Dim dureeReelle As TimeSpan = TimeSpan.Zero
        Dim fichierExiste As Boolean = estCDAudio OrElse File.Exists(chemin)

        Try
            If estCDAudio Then
                ' ===== PISTE CD AUDIO =====
                Dim cdTrack = CDAudioManager.ParseCheminCDAudio(chemin)
                If cdTrack IsNot Nothing Then
                    ' Utiliser les informations du chemin virtuel
                    Dim cdIcon As String = "💿 "
                    Dim trackPrefix As String = LanguageManager.GetString("CDTrack_Prefix")
                    nomFichier = $"{cdIcon}{trackPrefix} {cdTrack.TrackNumber:D2}"

                    ' Utiliser la durée depuis le chemin ou la durée fournie
                    If Not String.IsNullOrEmpty(dureeExistante) Then
                        duree = dureeExistante
                    Else
                        ' Si le CD est présent, lire la durée réelle
                        If CDAudioManager.EstCDAudioPresent(cdTrack.Drive) Then
                            Dim pistes = CDAudioManager.LirePistesCD(cdTrack.Drive)
                            Dim pisteInfo = pistes.FirstOrDefault(Function(p) p.TrackNumber = cdTrack.TrackNumber)
                            If pisteInfo IsNot Nothing Then
                                duree = pisteInfo.Duration.ToString("mm\:ss")
                            Else
                                duree = "00:00"
                            End If
                        Else
                            duree = "00:00" ' CD absent, durée inconnue
                        End If
                    End If

                    bpm = "" ' Pas de BPM pour les pistes CD
                End If
            Else
                ' ===== FICHIER AUDIO NORMAL =====
                ' ✅ SI UNE DURÉE EXISTANTE EST FOURNIE (depuis playlist.txt), L'UTILISER
                ' Sinon, lire la durée depuis le fichier audio SI LE FICHIER EXISTE
                If Not String.IsNullOrEmpty(dureeExistante) Then
                    duree = dureeExistante
                ElseIf fichierExiste Then
                    Using reader As New AudioFileReader(chemin)
                        Dim ts = reader.TotalTime
                        duree = $"{CInt(ts.TotalMinutes):D2}:{ts.Seconds:D2}"
                    End Using
                Else
                    ' Fichier introuvable : durée inconnue
                    duree = "--:--"
                End If

                ' NE PAS analyser la durée réelle au chargement pour accélérer le démarrage
                ' L'analyse sera faite à la demande lors de la première lecture du fichier


                ' Sinon, lire le BPM précis depuis les métadonnées s'il existe (pas pour CD)
                If Not String.IsNullOrEmpty(bpmExistant) Then
                    bpm = bpmExistant
                ElseIf fichierExiste Then
                    Dim bpmMetadata = BPMMetadataManager.LireBPMPrecisDepuisMetadonnees(chemin)
                    If bpmMetadata > 0 Then
                        bpm = bpmMetadata.ToString("F2", Globalization.CultureInfo.InvariantCulture)
                    End If
                End If
            End If ' Fin du bloc fichier normal
        Catch
            duree = "00:00"
        End Try

        Dim newItem As New ListViewItem()
        newItem.Text = "" ' Colonne Num (remplie par MettreAJourNumerotation)
        newItem.SubItems.Add(nomFichier) ' Colonne Chansons
        newItem.SubItems.Add(bpm) ' Colonne BPM (lu depuis métadonnées)
        newItem.SubItems.Add(duree) ' Colonne Durée

        ' Marquer visuellement les fichiers inaccessibles (sauf CD Audio)
        If Not estCDAudio AndAlso Not fichierExiste Then
            ' Fichier introuvable : utiliser une couleur atténuée basée sur le thème
            Dim theme = ThemeManager.GetCurrentTheme()
            Dim couleurNormale = theme.ListViewForeColor
            ' Atténuer la couleur : mélanger avec le fond (50% opacité)
            Dim couleurAttenuee = Color.FromArgb(
                (couleurNormale.R + theme.ListViewBackColor.R) \ 2,
                (couleurNormale.G + theme.ListViewBackColor.G) \ 2,
                (couleurNormale.B + theme.ListViewBackColor.B) \ 2
            )
            newItem.ForeColor = couleurAttenuee
            newItem.SubItems(1).Text = "⚠ " & nomFichier
        End If

        ' Utiliser un Dictionary pour le Tag afin de stocker le chemin, le BPM, le gain ET la durée réelle
        Dim tagDict As New Dictionary(Of String, Object) From {
            {"Chemin", chemin}
        }

        ' Marquer les pistes CD avec TypeSource
        If estCDAudio Then
            tagDict.Add("TypeSource", "CDAudio")
            Dim cdTrack = CDAudioManager.ParseCheminCDAudio(chemin)
            If cdTrack IsNot Nothing Then
                tagDict.Add("CDDrive", cdTrack.Drive)
                tagDict.Add("CDTrackNumber", cdTrack.TrackNumber)
            End If
        End If

        ' Ajouter le BPM au Tag s'il existe
        Dim bpmValue As Double = 0
        If Double.TryParse(bpm, bpmValue) AndAlso bpmValue > 0 Then
            tagDict.Add("BPM", bpmValue)
        End If

        ' Ajouter la durée réelle si elle a été calculée (pas pour CD)
        If Not estCDAudio AndAlso dureeReelle > TimeSpan.Zero Then
            tagDict.Add("DureeReelle", dureeReelle)
        End If

        newItem.Tag = tagDict

        ' Synchroniser le BPM de la colonne avec le Tag (toujours garder les décimales)
        Dim bpmCol As String = newItem.SubItems(2).Text.Trim()
        Dim bpmColValue As Double
        If Not String.IsNullOrEmpty(bpmCol) AndAlso Double.TryParse(bpmCol, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, bpmColValue) Then
            If Not tagDict.ContainsKey("BPM") Then
                tagDict.Add("BPM", bpmColValue)
            Else
                tagDict("BPM") = bpmColValue
            End If
        End If

        ListView1.Items.Add(newItem)
    End Sub

    Private Sub MettreAJourNumerotation()
        For i = 0 To ListView1.Items.Count - 1
            ListView1.Items(i).Text = (i + 1).ToString()
        Next
    End Sub

    Private Sub MettreCurseurAttenteListView(actif As Boolean)
        ' ✅ DÉSACTIVÉ : L'indicateur orange dans la colonne BPM suffit
        ' On ne change plus le curseur pour éviter la "roue d'attente"
        ' ListView1.Cursor = If(actif, Cursors.AppStarting, Cursors.Default)
    End Sub

    Private Sub ListView1_ItemDrag(sender As Object, e As ItemDragEventArgs) Handles ListView1.ItemDrag
        If e.Button <> MouseButtons.Left OrElse ListView1.SelectedIndices.Count = 0 Then
            Return
        End If

        Dim indices As New List(Of Integer)
        For Each idx As Integer In ListView1.SelectedIndices
            indices.Add(idx)
        Next
        indices.Sort()

        ListView1.DoDragDrop(New DataObject(ListViewInternalDragFormat, indices.ToArray()), DragDropEffects.Move)
    End Sub

    Private Sub ListView1_DragEnter(sender As Object, e As DragEventArgs) Handles ListView1.DragEnter
        If e.Data.GetDataPresent(ListViewInternalDragFormat) Then
            e.Effect = DragDropEffects.Move
        ElseIf e.Data.GetDataPresent(DataFormats.FileDrop) Then
            e.Effect = DragDropEffects.Copy
        End If
    End Sub

    Private Sub ListView1_DragOver(sender As Object, e As DragEventArgs) Handles ListView1.DragOver
        If e.Data.GetDataPresent(ListViewInternalDragFormat) Then
            e.Effect = DragDropEffects.Move
        ElseIf e.Data.GetDataPresent(DataFormats.FileDrop) Then
            e.Effect = DragDropEffects.Copy
        End If
    End Sub

    Private Function ObtenirIndexInsertionListView(clientPoint As Point) As Integer
        Dim targetItem = ListView1.GetItemAt(clientPoint.X, clientPoint.Y)
        If targetItem Is Nothing Then
            Return ListView1.Items.Count
        End If

        Dim milieu = targetItem.Bounds.Top + (targetItem.Bounds.Height \ 2)
        If clientPoint.Y > milieu Then
            Return targetItem.Index + 1
        End If

        Return targetItem.Index
    End Function

    Private Sub ListView1_DragDrop(sender As Object, e As DragEventArgs) Handles ListView1.DragDrop
        If e.Data.GetDataPresent(ListViewInternalDragFormat) Then
            Dim draggedIndicesArray = TryCast(e.Data.GetData(ListViewInternalDragFormat), Integer())
            If draggedIndicesArray Is Nothing OrElse draggedIndicesArray.Length = 0 Then
                Return
            End If

            Dim draggedIndices As New List(Of Integer)(draggedIndicesArray)
            draggedIndices.Sort()

            Dim clientPoint = ListView1.PointToClient(New Point(e.X, e.Y))
            Dim targetIndex = ObtenirIndexInsertionListView(clientPoint)

            If targetIndex >= draggedIndices(0) AndAlso targetIndex <= draggedIndices(draggedIndices.Count - 1) + 1 Then
                Return
            End If

            Dim movedItems As New List(Of ListViewItem)
            For Each idx In draggedIndices
                movedItems.Add(ListView1.Items(idx))
            Next

            Dim removedBefore As Integer = 0
            For Each idx In draggedIndices
                If idx < targetIndex Then removedBefore += 1
            Next

            For i As Integer = draggedIndices.Count - 1 To 0 Step -1
                ListView1.Items.RemoveAt(draggedIndices(i))
            Next

            targetIndex -= removedBefore
            If targetIndex < 0 Then targetIndex = 0
            If targetIndex > ListView1.Items.Count Then targetIndex = ListView1.Items.Count

            Dim insertStart = targetIndex
            For Each movedItem In movedItems
                ListView1.Items.Insert(targetIndex, movedItem)
                targetIndex += 1
            Next

            ListView1.SelectedItems.Clear()
            For i As Integer = insertStart To insertStart + movedItems.Count - 1
                ListView1.Items(i).Selected = True
            Next

            If movedItems.Count > 0 Then
                ListView1.Items(insertStart).EnsureVisible()
            End If

            MettreAJourNumerotation()
            SauvegarderPlaylist()
            Return
        End If

        If e.Data.GetDataPresent(DataFormats.FileDrop) Then
            Dim fichiers = CType(e.Data.GetData(DataFormats.FileDrop), String())
            Dim extensions = {".mp3", ".wav", ".flac", ".wma", ".aac", ".ogg"}

            For Each chemin In fichiers
                If File.Exists(chemin) AndAlso extensions.Contains(Path.GetExtension(chemin).ToLower()) Then
                    AjouterFichierAListe(chemin)
                ElseIf Directory.Exists(chemin) Then
                    Dim fichiersDansRep = Directory.GetFiles(chemin, "*.*", SearchOption.AllDirectories) _
                        .Where(Function(f) extensions.Contains(Path.GetExtension(f).ToLower()))
                    For Each fichier In fichiersDansRep
                        AjouterFichierAListe(fichier)
                    Next
                End If
            Next

            MettreAJourNumerotation()
            SauvegarderPlaylist()
        End If
    End Sub

    Private Sub ListView1_DoubleClick(sender As Object, e As EventArgs) Handles ListView1.DoubleClick
        JouerItemSelectionne()
    End Sub

    Private Sub DeplacerSelectionListView(direction As Integer)
        If ListView1.SelectedIndices.Count = 0 Then Return

        Dim selectedItems As New List(Of ListViewItem)
        For Each item As ListViewItem In ListView1.SelectedItems
            selectedItems.Add(item)
        Next

        selectedItems.Sort(Function(a, b) a.Index.CompareTo(b.Index))

        If direction < 0 AndAlso selectedItems(0).Index = 0 Then Return
        If direction > 0 AndAlso selectedItems(selectedItems.Count - 1).Index = ListView1.Items.Count - 1 Then Return

        If direction < 0 Then
            For Each item In selectedItems
                Dim currentIndex = item.Index
                ListView1.Items.RemoveAt(currentIndex)
                ListView1.Items.Insert(currentIndex - 1, item)
            Next
        ElseIf direction > 0 Then
            For i As Integer = selectedItems.Count - 1 To 0 Step -1
                Dim item = selectedItems(i)
                Dim currentIndex = item.Index
                ListView1.Items.RemoveAt(currentIndex)
                ListView1.Items.Insert(currentIndex + 1, item)
            Next
        End If

        ListView1.SelectedItems.Clear()
        For Each item In selectedItems
            item.Selected = True
        Next

        selectedItems(0).EnsureVisible()
        MettreAJourNumerotation()
        SauvegarderPlaylist()
    End Sub

    ' Gestion du clavier pour le ListView (Espace = lecture, Flèches haut/bas = déplacement)
    ' Gestion globale des raccourcis clavier (Ctrl+P, Ctrl+S, Ctrl+A, Ctrl+Espace)
    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        ' Désactiver les raccourcis clavier si le TextBox de recherche a le focus
        If TextBox_Recherche IsNot Nothing AndAlso TextBox_Recherche.Focused Then
            Return MyBase.ProcessCmdKey(msg, keyData)
        End If

        If keyData = Keys.Escape Then
            ' ✅ TOUCHE ÉCHAP : Annuler SEULEMENT le calcul/recalcul BPM (sans arrêter la chanson)
            If calculBPMEnCours Then
                ' Demander confirmation avant d'annuler
                Dim result = MessageBox.Show(
                    LanguageManager.GetString("BPM_CancelConfirm"),
                    LanguageManager.GetString("Cancellation_Title"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question)

                If result = DialogResult.Yes Then
                    If bpmCancellationTokenSource IsNot Nothing Then
                        bpmCancellationTokenSource.Cancel()
                        MessageBox.Show(LanguageManager.GetString("BPM_Cancelled"), LanguageManager.GetString("Cancellation_Title"), MessageBoxButtons.OK, MessageBoxIcon.Information)
                    End If
                End If
                ListView1.Focus()
                Return True
            End If
            ' Si aucun calcul BPM en cours, laisser passer (pour TextBox_Recherche, etc.)
            Return MyBase.ProcessCmdKey(msg, keyData)
        ElseIf keyData = Keys.Space Then
            ' Barre espace : Pause/Reprise si une lecture est en cours, sinon jouer la sélection
            If lectureEnCours Then
                Button_PauseReprise.PerformClick()
            ElseIf ListView1.SelectedItems.Count > 0 Then
                JouerItemSelectionne()
            End If
            Return True
        ElseIf keyData = (Keys.Control Or Keys.P) Then
            Button_PauseReprise.PerformClick()
            Return True
        ElseIf keyData = (Keys.Control Or Keys.S) Then
            Button_Mute.PerformClick()
            Return True
        ElseIf keyData = (Keys.Control Or Keys.A) Then
            Button_Aleatoire.PerformClick()
            Return True
        ElseIf keyData = (Keys.Control Or Keys.Space) Then
            ' ✅ CTRL+ESPACE : Arrêter la chanson ET annuler le calcul BPM
            ' Si un calcul BPM est en cours, l'annuler
            If calculBPMEnCours Then
                If bpmCancellationTokenSource IsNot Nothing Then
                    bpmCancellationTokenSource.Cancel()
                End If
            End If
            ' Arrêter la lecture audio
            Button_Arreter_Click(Button_Arreter, EventArgs.Empty)
            Return True
        ElseIf keyData = (Keys.Control Or Keys.Left) Then
            ' CTRL+Flèche gauche : Chanson précédente
            Button_Precedent.PerformClick()
            Return True
        ElseIf keyData = (Keys.Control Or Keys.Right) Then
            ' CTRL+Flèche droite : Chanson suivante
            Button_Suivant.PerformClick()
            Return True
        ElseIf keyData = Keys.I Then
            ' Marquer le début de la boucle (afficher immédiatement le marqueur I)
            If fichierAudio IsNot Nothing AndAlso Not enPause Then
                ' Si un marqueur O était déjà défini, l'effacer pour reprogrammer la fin
                If loopEndPosition > TimeSpan.Zero Then
                    loopEndPosition = TimeSpan.Zero
                    loopEnabled = False
                    ' Remettre le bouton Loop en gris
                    If Button_Loop IsNot Nothing Then
                        Button_Loop.BackgroundImage = AudioPlay.Resources.AudioPlay_Loop_Carre_Gris
                    End If
                ElseIf loopStartPosition > TimeSpan.Zero Then
                    ' Cas: I déjà posé sans O -> deuxième appui sur I = annuler le marqueur I
                    loopStartPosition = TimeSpan.Zero
                    hasLoopMarkers = False
                    loopEnabled = False
                    ' Cacher les labels de loop
                    If labelLoopStart IsNot Nothing Then labelLoopStart.Visible = False
                    If labelLoopEnd IsNot Nothing Then labelLoopEnd.Visible = False
                    ' Remettre le bouton Loop en gris
                    If Button_Loop IsNot Nothing Then
                        Button_Loop.BackgroundImage = AudioPlay.Resources.AudioPlay_Loop_Carre_Gris
                    End If
                    Return True
                End If

                ' Poser (ou reposer) le marqueur I
                loopStartPosition = fichierAudio.CurrentTime
                hasLoopMarkers = True
                MettreAJourPositionLabelsLoop()
            End If
            Return True
        ElseIf keyData = Keys.O Then
            ' Marquer la fin de la boucle (afficher immédiatement le marqueur O)
            If fichierAudio IsNot Nothing AndAlso Not enPause Then
                loopEndPosition = fichierAudio.CurrentTime
                hasLoopMarkers = True
                MettreAJourPositionLabelsLoop()
            End If
            Return True
        End If
        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    Private Sub ListView1_KeyDown(sender As Object, e As KeyEventArgs)
        ' Ctrl+Flèche haut/bas : déplacer l'item
        If e.Control AndAlso e.KeyCode = Keys.Up Then
            e.Handled = True
            e.SuppressKeyPress = True
            DeplacerSelectionListView(-1)
            Return
        End If
        If e.Control AndAlso e.KeyCode = Keys.Down Then
            e.Handled = True
            e.SuppressKeyPress = True
            DeplacerSelectionListView(1)
            Return
        End If
        ' Flèche haut/bas sans Ctrl : changer la sélection
        If Not e.Control AndAlso e.KeyCode = Keys.Up Then
            e.Handled = True
            e.SuppressKeyPress = True
            If ListView1.SelectedIndices.Count > 0 Then
                Dim idx = ListView1.SelectedIndices(0)
                If idx > 0 Then
                    ListView1.SelectedItems.Clear()
                    ListView1.Items(idx - 1).Selected = True
                    ListView1.Items(idx - 1).EnsureVisible()
                End If
            End If
            Return
        End If
        If Not e.Control AndAlso e.KeyCode = Keys.Down Then
            e.Handled = True
            e.SuppressKeyPress = True
            If ListView1.SelectedIndices.Count > 0 Then
                Dim idx = ListView1.SelectedIndices(0)
                If idx < ListView1.Items.Count - 1 Then
                    ListView1.SelectedItems.Clear()
                    ListView1.Items(idx + 1).Selected = True
                    ListView1.Items(idx + 1).EnsureVisible()
                End If
            End If
            Return
        End If
        ' Supprimer la sélection avec la touche Suppr
        If e.KeyCode = Keys.Delete Then
            e.Handled = True
            e.SuppressKeyPress = True
            If ListView1.SelectedItems.Count > 0 Then
                ' Confirmation si activée
                If ParametresGlobaux.ConfirmerEffacementChansons Then
                    Dim rep = MessageBox.Show(LanguageManager.GetString("Playlist_DeleteConfirm_Message"),
                                            LanguageManager.GetString("Confirmation_Title"),
                                            MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                    If rep <> DialogResult.Yes Then Return
                End If
                ' Arrêter la lecture si le fichier courant est supprimé
                Dim cheminsASupprimer As New List(Of String)
                For Each item As ListViewItem In ListView1.SelectedItems
                    If item.Tag IsNot Nothing Then
                        If TypeOf item.Tag Is Dictionary(Of String, Object) Then
                            Dim tagDict = DirectCast(item.Tag, Dictionary(Of String, Object))
                            If tagDict.ContainsKey("Chemin") Then
                                cheminsASupprimer.Add(tagDict("Chemin").ToString())
                            End If
                        ElseIf TypeOf item.Tag Is String Then
                            cheminsASupprimer.Add(item.Tag.ToString())
                        End If
                    End If
                Next
                If cheminsASupprimer.Contains(cheminActuel) Then
                    ArreterLecture()
                End If
                ' Supprimer tous les items sélectionnés
                For Each item As ListViewItem In ListView1.SelectedItems.Cast(Of ListViewItem).ToList()
                    ListView1.Items.Remove(item)
                Next
                MettreAJourNumerotation()
                SauvegarderPlaylist()
            End If
            Return
        End If
        ' Touche Début : sélectionner le premier item
        If e.KeyCode = Keys.Home Then
            e.Handled = True
            e.SuppressKeyPress = True
            If ListView1.Items.Count > 0 Then
                ListView1.SelectedItems.Clear()
                ListView1.Items(0).Selected = True
                ListView1.Items(0).EnsureVisible()
            End If
            Return
        End If
        ' Touche Fin : sélectionner le dernier item
        If e.KeyCode = Keys.End Then
            e.Handled = True
            e.SuppressKeyPress = True
            If ListView1.Items.Count > 0 Then
                ListView1.SelectedItems.Clear()
                Dim lastIdx As Integer = ListView1.Items.Count - 1
                ListView1.Items(lastIdx).Selected = True
                ListView1.Items(lastIdx).EnsureVisible()
            End If
            Return
        End If
        ' Touche Page Up : monter d'un écran
        If e.KeyCode = Keys.PageUp Then
            e.Handled = True
            e.SuppressKeyPress = True
            If ListView1.Items.Count > 0 Then
                Dim visibleCount As Integer = ListView1.ClientSize.Height \ ListView1.Items(0).Bounds.Height
                Dim idx As Integer = If(ListView1.SelectedIndices.Count > 0, ListView1.SelectedIndices(0), 0)
                Dim newIdx As Integer = Math.Max(0, idx - visibleCount)
                ListView1.SelectedItems.Clear()
                ListView1.Items(newIdx).Selected = True
                ListView1.Items(newIdx).EnsureVisible()
            End If
            Return
        End If
        ' Touche Page Down : descendre d'un écran
        If e.KeyCode = Keys.PageDown Then
            e.Handled = True
            e.SuppressKeyPress = True
            If ListView1.Items.Count > 0 Then
                Dim visibleCount As Integer = ListView1.ClientSize.Height \ ListView1.Items(0).Bounds.Height
                Dim idx As Integer = If(ListView1.SelectedIndices.Count > 0, ListView1.SelectedIndices(0), 0)
                Dim newIdx As Integer = Math.Min(ListView1.Items.Count - 1, idx + visibleCount)
                ListView1.SelectedItems.Clear()
                ListView1.Items(newIdx).Selected = True
                ListView1.Items(newIdx).EnsureVisible()
            End If
            Return
        End If
        ' Gestion avancée de la barre espace
        If e.KeyCode = Keys.Space Then
            e.Handled = True
            e.SuppressKeyPress = True
            If ListView1.SelectedItems.Count > 0 Then
                ' Si rien ne joue, on démarre la lecture
                If Not lectureEnCours Then
                    JouerItemSelectionne()
                Else
                    ' Si une chanson joue ou est en pause, on bascule pause/reprise
                    Button_PauseReprise.PerformClick()
                End If
            End If
        End If
        ' Ctrl+P = Pause/Reprise
        If e.Control AndAlso e.KeyCode = Keys.P Then
            e.Handled = True
            e.SuppressKeyPress = True
            Button_PauseReprise.PerformClick()
        End If
        ' Ctrl+S = Sourdine
        If e.Control AndAlso e.KeyCode = Keys.S Then
            e.Handled = True
            e.SuppressKeyPress = True
            Button_Mute.PerformClick()
        End If
        ' Ctrl+A = Aléatoire
        If e.Control AndAlso e.KeyCode = Keys.A Then
            e.Handled = True
            e.SuppressKeyPress = True
            Button_Aleatoire.PerformClick()
        End If
        ' Ctrl+Espace = Arrêter
        If e.Control AndAlso e.KeyCode = Keys.Space Then
            e.Handled = True
            e.SuppressKeyPress = True
            Button_Arreter_Click(Button_Arreter, EventArgs.Empty)
        End If
    End Sub


    ' ========================================
    ' MENU CONTEXTUEL
    ' ========================================
    Private Sub CreerMenuContextuel()
        Dim menu As New ContextMenuStrip()

        Dim itemCalculerBPM As New ToolStripMenuItem(LanguageManager.GetString("Context_CalculateBPM"))
        AddHandler itemCalculerBPM.Click, Sub() Button_CalculBPM_Click(Nothing, Nothing)

        Dim itemAfficherMetadonnees As New ToolStripMenuItem(LanguageManager.GetString("Context_ShowMetadata"))
        AddHandler itemAfficherMetadonnees.Click, Sub() Button_InfoSelect_Click(Nothing, Nothing)

        Dim itemSupprimerDeListe As New ToolStripMenuItem(LanguageManager.GetString("Context_RemoveFromList"))
        AddHandler itemSupprimerDeListe.Click, AddressOf SupprimerDeListe

        menu.Items.Add(itemCalculerBPM)
        menu.Items.Add(itemAfficherMetadonnees)
        menu.Items.Add(New ToolStripSeparator())
        menu.Items.Add(itemSupprimerDeListe)

        ListView1.ContextMenuStrip = menu
    End Sub

    Private Sub SupprimerDeListe(sender As Object, e As EventArgs)
        If ListView1.SelectedItems.Count = 0 Then Return

        ' Confirmation si activée
        If ParametresGlobaux.ConfirmerEffacementChansons Then
            Dim rep = MessageBox.Show(LanguageManager.GetString("Playlist_DeleteConfirm_Message"),
                                    LanguageManager.GetString("Confirmation_Title"),
                                    MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If rep <> DialogResult.Yes Then Return
        End If

        ' Arrêter la lecture si le fichier courant est supprimé
        Dim cheminsASupprimer As New List(Of String)
        For Each item As ListViewItem In ListView1.SelectedItems
            If item.Tag IsNot Nothing Then
                If TypeOf item.Tag Is Dictionary(Of String, Object) Then
                    Dim tagDict = DirectCast(item.Tag, Dictionary(Of String, Object))
                    If tagDict.ContainsKey("Chemin") Then
                        cheminsASupprimer.Add(tagDict("Chemin").ToString())
                    End If
                ElseIf TypeOf item.Tag Is String Then
                    cheminsASupprimer.Add(item.Tag.ToString())
                End If
            End If
        Next
        If cheminsASupprimer.Contains(cheminActuel) Then
            ArreterLecture()
        End If

        ' Supprimer tous les items sélectionnés
        For Each item As ListViewItem In ListView1.SelectedItems.Cast(Of ListViewItem).ToList()
            ListView1.Items.Remove(item)
        Next
        MettreAJourNumerotation()
        SauvegarderPlaylist()
    End Sub

    ' ========================================
    ' PERSISTANCE
    ' ========================================
    Private Sub ChargerParametres()
        Dim fichierParam = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AudioPlay",
            "parametres.txt")
        Dim langueChargee As Boolean = False

        If Not File.Exists(fichierParam) Then
            ' Premier lancement : détecter la langue système
            LanguageManager.DetectSystemLanguage()
            Return
        End If

        Try
            Dim lignes = File.ReadAllLines(fichierParam)
            For Each ligne In lignes
                If ligne.StartsWith("RepertoireParDefaut=") Then
                    repertoireParDefaut = ligne.Substring("RepertoireParDefaut=".Length)
                ElseIf ligne.StartsWith("DernierRepertoireAjout=") Then
                    dernierRepertoireAjout = ligne.Substring("DernierRepertoireAjout=".Length)
                ElseIf ligne.StartsWith("DernierRepertoirePlaylist=") Then
                    dernierRepertoirePlaylist = ligne.Substring("DernierRepertoirePlaylist=".Length)
                ElseIf ligne.StartsWith("LectureEnContinu=") Then
                    lectureEnContinu = Boolean.Parse(ligne.Substring("LectureEnContinu=".Length))
                    ' Volume, Basses, Aigues ignorés ici (maintenant dans Son_Ajustement.txt)
                ElseIf ligne.StartsWith("NormalisationVolume=") Then
                    normalisationVolumeActive = Boolean.Parse(ligne.Substring("NormalisationVolume=".Length))
                ElseIf ligne.StartsWith("MethodeBPM=") Then
                    BPMDetector.MethodeChoisie = ligne.Substring("MethodeBPM=".Length)
                ElseIf ligne.StartsWith("MetronomeActif=") Then
                    metronomeActif = Boolean.Parse(ligne.Substring("MetronomeActif=".Length))
                ElseIf ligne.StartsWith("NombreBeatsMetronome=") Then
                    nombreBeatsMetronome = Integer.Parse(ligne.Substring("NombreBeatsMetronome=".Length))
                ElseIf ligne.StartsWith("MetronomeSonActif=") Then
                    metronomeSonActif = Boolean.Parse(ligne.Substring("MetronomeSonActif=".Length))
                ElseIf ligne.StartsWith("MetronomeLumiereActive=") Then
                    metronomeLumiereActive = Boolean.Parse(ligne.Substring("MetronomeLumiereActive=".Length))
                ElseIf ligne.StartsWith("SupprimerSilenceDebut=") Then
                    ParametresGlobaux.SupprimerSilenceDebut = Boolean.Parse(ligne.Substring("SupprimerSilenceDebut=".Length))
                ElseIf ligne.StartsWith("SupprimerSilenceFin=") Then
                    ParametresGlobaux.SupprimerSilenceFin = Boolean.Parse(ligne.Substring("SupprimerSilenceFin=".Length))
                ElseIf ligne.StartsWith("ModeAleatoire=") Then
                    modeAleatoire = Boolean.Parse(ligne.Substring("ModeAleatoire=".Length))
                ElseIf ligne.StartsWith("Langue=") Then
                    Dim langue = ligne.Substring("Langue=".Length)
                    If Not String.IsNullOrEmpty(langue) Then
                        LanguageManager.ChangeLanguage(langue)
                        langueChargee = True
                    End If
                    ' === Effets Audio ===
                ElseIf ligne.StartsWith("EffetReverbActif=") Then
                    Boolean.TryParse(ligne.Substring("EffetReverbActif=".Length), ParametresGlobaux.EffetReverbActif)
                ElseIf ligne.StartsWith("EffetReverbMix=") Then
                    Dim mix As Single
                    If Single.TryParse(ligne.Substring("EffetReverbMix=".Length), Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, mix) Then
                        ParametresGlobaux.EffetReverbMix = mix
                    End If
                ElseIf ligne.StartsWith("EffetEchoActif=") Then
                    Boolean.TryParse(ligne.Substring("EffetEchoActif=".Length), ParametresGlobaux.EffetEchoActif)
                ElseIf ligne.StartsWith("EffetEchoMix=") Then
                    Dim mix As Single
                    If Single.TryParse(ligne.Substring("EffetEchoMix=".Length), Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, mix) Then
                        ParametresGlobaux.EffetEchoMix = mix
                    End If
                ElseIf ligne.StartsWith("EffetEchoDelai=") Then
                    Integer.TryParse(ligne.Substring("EffetEchoDelai=".Length), ParametresGlobaux.EffetEchoDelai)
                ElseIf ligne.StartsWith("EffetEchoFeedback=") Then
                    Dim fb As Single
                    If Single.TryParse(ligne.Substring("EffetEchoFeedback=".Length), Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, fb) Then
                        ParametresGlobaux.EffetEchoFeedback = fb
                    End If
                ElseIf ligne.StartsWith("EffetTimeStretchActif=") Then
                    Boolean.TryParse(ligne.Substring("EffetTimeStretchActif=".Length), ParametresGlobaux.EffetTimeStretchActif)
                ElseIf ligne.StartsWith("EffetTimeStretchRatio=") Then
                    Dim ratio As Single
                    If Single.TryParse(ligne.Substring("EffetTimeStretchRatio=".Length), Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, ratio) Then
                        ParametresGlobaux.EffetTimeStretchRatio = ratio
                    End If
                ElseIf ligne.StartsWith("RepertoireExtractionCD=") Then
                    ParametresGlobaux.repertoireExtractionCD = ligne.Substring("RepertoireExtractionCD=".Length)
                ElseIf ligne.StartsWith("VolumeExtractionCD=") Then
                    Dim vol As Integer
                    If Integer.TryParse(ligne.Substring("VolumeExtractionCD=".Length), vol) Then
                        If vol >= 1 AndAlso vol <= 100 Then
                            ParametresGlobaux.volumeExtractionCD = vol
                        End If
                    End If
                ElseIf ligne.StartsWith("ModeTOCPrecis=") Then
                    Boolean.TryParse(ligne.Substring("ModeTOCPrecis=".Length), ParametresGlobaux.ModeTOCPrecis)
                End If
            Next

            ' Si aucune langue n'a été trouvée dans le fichier, détecter la langue système
            If Not langueChargee Then
                LanguageManager.DetectSystemLanguage()
            End If
        Catch ex As Exception
            ' Ignorer les erreurs de chargement
            LanguageManager.DetectSystemLanguage()
        End Try
    End Sub

    Public Sub SauvegarderParametres()
        Dim fichierParam = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AudioPlay",
            "parametres.txt")
        Try
            ' Créer le répertoire s'il n'existe pas
            Dim dossier As String = Path.GetDirectoryName(fichierParam)
            If Not Directory.Exists(dossier) Then
                Directory.CreateDirectory(dossier)
            End If

            ' Synchroniser les valeurs locales vers ParametresGlobaux avant d'écrire
            Try
                ParametresGlobaux.dernierRepertoirePlaylist = dernierRepertoirePlaylist
                ParametresGlobaux.repertoireParDefaut = repertoireParDefaut
            Catch
            End Try

            ' Volume, Basses, Aigues sont maintenant dans Son_Ajustement.txt (fichier séparé)
            Dim lignes As New List(Of String) From {
                $"RepertoireParDefaut={ParametresGlobaux.repertoireParDefaut}",
                $"DernierRepertoireAjoutFichier={ParametresGlobaux.dernierRepertoireAjoutFichier}",
                $"DernierRepertoireAjoutRepertoire={ParametresGlobaux.dernierRepertoireAjoutRepertoire}",
                $"DernierRepertoirePlaylist={ParametresGlobaux.dernierRepertoirePlaylist}",
                $"DernierRepertoireAjoutFichier_DJ={ParametresGlobaux.dernierRepertoireAjoutFichier_DJ}",
                $"DernierRepertoireAjoutRepertoire_DJ={ParametresGlobaux.dernierRepertoireAjoutRepertoire_DJ}",
                $"DernierRepertoirePlaylist_DJ={ParametresGlobaux.dernierRepertoirePlaylist_DJ}",
                $"AvantDernierRepertoireAjoutRepertoire={ParametresGlobaux.avantDernierRepertoireAjoutRepertoire}",
                $"AvantDernierRepertoireAjoutRepertoire_DJ={ParametresGlobaux.avantDernierRepertoireAjoutRepertoire_DJ}",
                $"LectureEnContinu={lectureEnContinu}",
                $"NormalisationVolume={normalisationVolumeActive}",
                $"MethodeBPM={BPMDetector.MethodeChoisie}",
                $"MetronomeActif={metronomeActif}",
                $"NombreBeatsMetronome={nombreBeatsMetronome}",
                $"MetronomeSonActif={metronomeSonActif}",
                $"MetronomeLumiereActive={metronomeLumiereActive}",
                $"SupprimerSilenceDebut={ParametresGlobaux.SupprimerSilenceDebut}",
                $"SupprimerSilenceFin={ParametresGlobaux.SupprimerSilenceFin}",
                $"ModeAleatoire={modeAleatoire}",
                $"Langue={LanguageManager.GetCurrentLanguageCode()}",
                $"EffetReverbActif={ParametresGlobaux.EffetReverbActif}",
                $"EffetReverbMix={ParametresGlobaux.EffetReverbMix.ToString(Globalization.CultureInfo.InvariantCulture)}",
                $"EffetEchoActif={ParametresGlobaux.EffetEchoActif}",
                $"EffetEchoMix={ParametresGlobaux.EffetEchoMix.ToString(Globalization.CultureInfo.InvariantCulture)}",
                $"EffetEchoDelai={ParametresGlobaux.EffetEchoDelai}",
                $"EffetEchoFeedback={ParametresGlobaux.EffetEchoFeedback.ToString(Globalization.CultureInfo.InvariantCulture)}",
                $"EffetTimeStretchActif={ParametresGlobaux.EffetTimeStretchActif}",
                $"EffetTimeStretchRatio={ParametresGlobaux.EffetTimeStretchRatio.ToString(Globalization.CultureInfo.InvariantCulture)}",
                $"EffetPitchShiftActif={ParametresGlobaux.EffetPitchShiftActif}",
                $"EffetPitchShiftSemiTones={ParametresGlobaux.EffetPitchShiftSemiTones.ToString(Globalization.CultureInfo.InvariantCulture)}",
                $"EffetPhaserActif={ParametresGlobaux.EffetPhaserActif}",
                $"EffetPhaserRate={ParametresGlobaux.EffetPhaserRate.ToString(Globalization.CultureInfo.InvariantCulture)}",
                $"EffetPhaserDepth={ParametresGlobaux.EffetPhaserDepth.ToString(Globalization.CultureInfo.InvariantCulture)}",
                $"EffetPhaserFeedback={ParametresGlobaux.EffetPhaserFeedback.ToString(Globalization.CultureInfo.InvariantCulture)}",
                $"EffetPhaserMix={ParametresGlobaux.EffetPhaserMix.ToString(Globalization.CultureInfo.InvariantCulture)}",
                $"EffetPhaserStages={ParametresGlobaux.EffetPhaserStages}",
                $"ModeMixeurDJ={ParametresGlobaux.ModeMixeurDJ}",
                $"RepertoireExtractionCD={ParametresGlobaux.repertoireExtractionCD}",
                $"VolumeExtractionCD={ParametresGlobaux.volumeExtractionCD}",
                $"ModeTOCPrecis={ParametresGlobaux.ModeTOCPrecis}"
            }
            ' Preserve specific keys that may have been written elsewhere (e.g., FormCompresser_Agrandir)
            Try
                Dim existing As New Dictionary(Of String, String)(StringComparer.InvariantCultureIgnoreCase)
                If File.Exists(fichierParam) Then
                    For Each l In File.ReadAllLines(fichierParam)
                        Try
                            Dim i = l.IndexOf("=")
                            If i > 0 Then
                                Dim k = l.Substring(0, i).Trim()
                                Dim v = l.Substring(i + 1).Trim()
                                If Not existing.ContainsKey(k) Then existing.Add(k, v)
                            End If
                        Catch
                        End Try
                    Next
                End If

                ' If FormCompresser_Agrandir exists in existing file, ensure it's kept
                If existing.ContainsKey("FormCompresser_Agrandir") Then
                    ' remove any entry we might have added already
                    For i = lignes.Count - 1 To 0 Step -1
                        Try
                            If lignes(i).StartsWith("FormCompresser_Agrandir=", StringComparison.InvariantCultureIgnoreCase) Then
                                lignes.RemoveAt(i)
                            End If
                        Catch
                        End Try
                    Next
                    lignes.Add($"FormCompresser_Agrandir={existing("FormCompresser_Agrandir")}")
                End If

                File.WriteAllLines(fichierParam, lignes)
            Catch exWriteAll As Exception
                ' fallback: try a best-effort write
                Try
                    File.WriteAllLines(fichierParam, lignes)
                Catch
                End Try
            End Try
        Catch ex As Exception
            ' Ignorer les erreurs de sauvegarde
        End Try
    End Sub

    ' === GESTION FICHIER SON_AJUSTEMENT.TXT (NOUVEAU) ===
    ' Fichier dédié pour les paramètres audio temps réel (Volume, Basses, Aigues)
    ' Séparé de parametres.txt pour éviter tout risque d'écrasement

    Private Sub SauvegarderAudioAjustements()
        Dim fichierAudio = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AudioPlay",
            "Son_Ajustement.txt")
        Try
            ' Créer le répertoire s'il n'existe pas
            Dim dossier As String = Path.GetDirectoryName(fichierAudio)
            If Not Directory.Exists(dossier) Then
                Directory.CreateDirectory(dossier)
            End If

            ' Sauvegarder les 3 valeurs audio uniquement
            Dim lignes As New List(Of String) From {
                $"Volume={dernierVolume.ToString(Globalization.CultureInfo.InvariantCulture)}",
                $"Basses={dernieresBasses.ToString(Globalization.CultureInfo.InvariantCulture)}",
                $"Aigues={dernieresAigues.ToString(Globalization.CultureInfo.InvariantCulture)}"
            }
            File.WriteAllLines(fichierAudio, lignes)
            System.Diagnostics.Debug.WriteLine($"[Form1] ✅ Audio ajustements sauvegardés: Volume={dernierVolume:F3}, Basses={dernieresBasses:F1}, Aigues={dernieresAigues:F1}")
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[Form1] ❌ Erreur sauvegarde audio: {ex.Message}")
        End Try
    End Sub

    Private Sub ChargerAudioAjustements()
        Dim fichierAudio = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AudioPlay",
            "Son_Ajustement.txt")

        ' Charger le volume depuis le système Windows
        Try
            dernierVolume = WindowsVolumeControl.GetVolume()
            System.Diagnostics.Debug.WriteLine($"[Form1] ✅ Volume système chargé: {dernierVolume:F3}")
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[Form1] ⚠️ Erreur lecture volume système: {ex.Message}")
            dernierVolume = 0.5F ' Valeur par défaut
        End Try

        If Not File.Exists(fichierAudio) Then
            ' Fichier manquant : tenter migration depuis parametres.txt
            System.Diagnostics.Debug.WriteLine("[Form1] ⚠️ Son_Ajustement.txt manquant, tentative de migration...")
            MigrerAudioDepuisParametres()
            Return
        End If

        Try
            Dim lignes = File.ReadAllLines(fichierAudio)
            For Each ligne In lignes
                If ligne.StartsWith("Volume=") Then
                    ' Ignorer le volume du fichier, utiliser le volume système à la place
                    ' (commenté pour référence)
                    ' dernierVolume = Single.Parse(ligne.Substring("Volume=".Length), Globalization.CultureInfo.InvariantCulture)
                ElseIf ligne.StartsWith("Basses=") Then
                    dernieresBasses = Single.Parse(ligne.Substring("Basses=".Length), Globalization.CultureInfo.InvariantCulture)
                ElseIf ligne.StartsWith("Aigues=") Then
                    dernieresAigues = Single.Parse(ligne.Substring("Aigues=".Length), Globalization.CultureInfo.InvariantCulture)
                End If
            Next
            System.Diagnostics.Debug.WriteLine($"[Form1] ✅ Audio ajustements chargés: Volume={dernierVolume:F3} (système), Basses={dernieresBasses:F1}, Aigues={dernieresAigues:F1}")
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[Form1] ❌ Erreur chargement audio: {ex.Message}")
            ' En cas d'erreur, créer le fichier avec valeurs par défaut
            SauvegarderAudioAjustements()
        End Try
    End Sub

    Private Sub MigrerAudioDepuisParametres()
        ' Tenter de lire les valeurs depuis parametres.txt pour migration
        Dim fichierParam = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AudioPlay",
            "parametres.txt")

        Dim valeursTrouvees As Boolean = False

        If File.Exists(fichierParam) Then
            Try
                Dim lignes = File.ReadAllLines(fichierParam)
                For Each ligne In lignes
                    If ligne.StartsWith("Volume=") Then
                        dernierVolume = Single.Parse(ligne.Substring("Volume=".Length), Globalization.CultureInfo.InvariantCulture)
                        If dernierVolume > 1.0F Then dernierVolume = dernierVolume / 100.0F
                        If dernierVolume < 0.0F Then dernierVolume = 0.0F
                        If dernierVolume > 1.0F Then dernierVolume = 1.0F
                        valeursTrouvees = True
                    ElseIf ligne.StartsWith("Basses=") Then
                        dernieresBasses = Single.Parse(ligne.Substring("Basses=".Length), Globalization.CultureInfo.InvariantCulture)
                        valeursTrouvees = True
                    ElseIf ligne.StartsWith("Aigues=") Then
                        dernieresAigues = Single.Parse(ligne.Substring("Aigues=".Length), Globalization.CultureInfo.InvariantCulture)
                        valeursTrouvees = True
                    End If
                Next

                If valeursTrouvees Then
                    System.Diagnostics.Debug.WriteLine("[Form1] ✅ Migration audio depuis parametres.txt réussie")
                Else
                    System.Diagnostics.Debug.WriteLine("[Form1] ⚠️ Aucune valeur audio trouvée dans parametres.txt, utilisation des valeurs par défaut")
                End If
            Catch ex As Exception
                System.Diagnostics.Debug.WriteLine($"[Form1] ⚠️ Erreur migration audio: {ex.Message}")
            End Try
        Else
            System.Diagnostics.Debug.WriteLine("[Form1] ℹ️ parametres.txt absent, utilisation des valeurs par défaut pour audio")
        End If

        ' Créer Son_Ajustement.txt avec les valeurs (migrées ou par défaut)
        SauvegarderAudioAjustements()
    End Sub

    ' === MÉTHODES SIMPLIFIÉES (appellent maintenant SauvegarderAudioAjustements) ===

    Private Sub SauvegarderVolume()
        SauvegarderAudioAjustements()
    End Sub

    Private Sub SauvegarderBasses()
        SauvegarderAudioAjustements()
    End Sub

    Private Sub SauvegarderAigues()
        SauvegarderAudioAjustements()
    End Sub

    Private Sub AppliquerParametresAuxControles()
        ' Appliquer les valeurs chargées aux TrackBars
        ' Cette méthode est appelée après ChargerParametres() pour synchroniser l'UI

        ' Protéger contre les événements Scroll pendant la mise à jour
        initialisationEnCours = True

        Try
            If TrackBar_Volume IsNot Nothing Then
                Dim v = Math.Max(TrackBar_Volume.Minimum, Math.Min(TrackBar_Volume.Maximum, CInt(dernierVolume * TrackBar_Volume.Maximum)))
                TrackBar_Volume.Value = v
            End If

            If TrackBar_Basses IsNot Nothing Then
                Dim b = Math.Max(TrackBar_Basses.Minimum, Math.Min(TrackBar_Basses.Maximum, CInt(dernieresBasses)))
                TrackBar_Basses.Value = b
            End If

            If TrackBar_Aigues IsNot Nothing Then
                Dim a = Math.Max(TrackBar_Aigues.Minimum, Math.Min(TrackBar_Aigues.Maximum, CInt(dernieresAigues)))
                TrackBar_Aigues.Value = a
            End If
        Finally
            ' Réactiver les événements Scroll
            initialisationEnCours = False
        End Try
    End Sub

    Private Sub ChargerPlaylist()
        Dim dossier = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AudioPlay")
        Dim fichierPlaylist = Path.Combine(dossier, "playlist.txt")

        ' Si le fichier principal n'existe pas ou est vide, essayer le backup
        If Not File.Exists(fichierPlaylist) OrElse New FileInfo(fichierPlaylist).Length = 0 Then
            Dim fichierBackup = Path.Combine(dossier, "playlist.txt.bak")
            If File.Exists(fichierBackup) AndAlso New FileInfo(fichierBackup).Length > 0 Then
                Try
                    File.Copy(fichierBackup, fichierPlaylist, True)
                Catch
                    ' Si la copie échoue, utiliser directement le backup
                    fichierPlaylist = fichierBackup
                End Try
            Else
                Return
            End If
        End If

        Try
            Dim lignes = File.ReadAllLines(fichierPlaylist)
            For Each ligne In lignes
                If Not String.IsNullOrWhiteSpace(ligne) Then
                    Dim parties = ligne.Split("|"c)
                    If parties.Length >= 2 Then
                        Dim chemin = parties(0)
                        Dim bpm = If(parties.Length >= 3, parties(2), "")
                        Dim duree = If(parties.Length >= 4, parties(3), "")

                        ' Ignorer les pistes CD audio (CDDA://)
                        ' L'utilisateur doit les ajouter manuellement à chaque session
                        If chemin.StartsWith("CDDA://", StringComparison.OrdinalIgnoreCase) Then
                            Continue For
                        End If

                        ' Charger uniquement les fichiers audio normaux qui existent
                        If File.Exists(chemin) Then
                            ' ✅ PASSER LE BPM ET LA DURÉE DEPUIS playlist.txt POUR ÉVITER DE RELIRE LES FICHIERS
                            AjouterFichierAListe(chemin, bpm, duree)
                        End If
                    End If
                End If
            Next
            MettreAJourNumerotation()
        Catch ex As Exception
            ' Ignorer les erreurs de chargement
        End Try
    End Sub

    Private Sub SauvegarderPlaylist()
        Dim fichierPlaylist = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AudioPlay",
            "playlist.txt")
        Try
            ' Créer le répertoire s'il n'existe pas
            Dim dossier As String = Path.GetDirectoryName(fichierPlaylist)
            If Not Directory.Exists(dossier) Then
                Directory.CreateDirectory(dossier)
            End If

            ' Backup automatique si le fichier existe et contient des données
            If File.Exists(fichierPlaylist) Then
                Dim infoFichier As New FileInfo(fichierPlaylist)
                If infoFichier.Length > 0 Then
                    Dim fichierBackup = Path.Combine(dossier, "playlist.txt.bak")
                    File.Copy(fichierPlaylist, fichierBackup, True)
                End If
            End If

            Dim lignes As New List(Of String)
            For Each item As ListViewItem In ListView1.Items
                ' Extraire le chemin du Tag
                Dim chemin As String = ""
                If TypeOf item.Tag Is Dictionary(Of String, Object) Then
                    Dim tagDict = DirectCast(item.Tag, Dictionary(Of String, Object))
                    If tagDict.ContainsKey("Chemin") Then
                        chemin = tagDict("Chemin")?.ToString()
                    End If
                ElseIf TypeOf item.Tag Is String Then
                    chemin = item.Tag.ToString()
                End If

                ' Exclure les fichiers CD audio (CDDA://) de la sauvegarde
                ' L'utilisateur devra les recharger à chaque session
                If Not String.IsNullOrEmpty(chemin) AndAlso Not chemin.StartsWith("CDDA://", StringComparison.OrdinalIgnoreCase) Then
                    Dim nom = item.SubItems(1).Text
                    Dim bpm = item.SubItems(2).Text
                    Dim duree = item.SubItems(3).Text
                    lignes.Add($"{chemin}|{nom}|{bpm}|{duree}")
                End If
            Next
            File.WriteAllLines(fichierPlaylist, lignes)
        Catch ex As Exception
            ' Ignorer les erreurs de sauvegarde
        End Try
    End Sub

    ' ========================================
    ' MÉTHODES PUBLIQUES (pour FormMetadonnees)
    ' ========================================
    Public Function EstEnLecture() As Boolean
        Return lectureEnCours
    End Function

    Public Function ObtenirEtatPause() As Boolean
        Return enPause
    End Function

    Public Function ObtenirPositionLecture() As TimeSpan
        If fichierAudio IsNot Nothing Then
            Return fichierAudio.CurrentTime
        End If
        Return TimeSpan.Zero
    End Function

    Public Function ObtenirCheminFichierEnCours() As String
        Return cheminActuel
    End Function

    Public Sub ArreterLecturePublic()
        ArreterLecture()
    End Sub

    Public Sub JouerItemSelectionnePublic()
        JouerItemSelectionne()
    End Sub

    Public Sub JouerFichierAPosition(cheminFichier As String, position As TimeSpan)
        Try
            ' S'assurer qu'on joue le bon fichier
            If String.IsNullOrEmpty(cheminFichier) OrElse Not File.Exists(cheminFichier) Then
                Return
            End If

            ' Trouver l'item dans la liste
            Dim itemTrouve As ListViewItem = Nothing
            For Each item As ListViewItem In ListView1.Items
                Dim tagChemin As String = ""
                If TypeOf item.Tag Is Dictionary(Of String, Object) Then
                    Dim tagDict = DirectCast(item.Tag, Dictionary(Of String, Object))
                    If tagDict.ContainsKey("Chemin") Then
                        tagChemin = tagDict("Chemin")?.ToString()
                    End If
                ElseIf TypeOf item.Tag Is String Then
                    tagChemin = item.Tag.ToString()
                End If

                If tagChemin = cheminFichier Then
                    itemTrouve = item
                    Exit For
                End If
            Next

            If itemTrouve Is Nothing Then
                Return
            End If

            ' Sélectionner l'item
            ListView1.SelectedItems.Clear()
            itemTrouve.Selected = True
            itemTrouve.EnsureVisible()
            ListView1.Focus()

            ' Jouer le fichier
            JouerItemSelectionne()

            ' Attendre que le fichier soit chargé
            System.Threading.Thread.Sleep(500)

            ' Repositionner à la position sauvegardée
            If fichierAudio IsNot Nothing AndAlso position.TotalSeconds > 0 AndAlso position.TotalSeconds < fichierAudio.TotalTime.TotalSeconds Then
                Try
                    fichierAudio.CurrentTime = position
                Catch
                    ' Ignorer les erreurs de repositionnement
                End Try
            End If

        Catch ex As Exception
            ' En cas d'erreur, ne rien faire (la lecture peut avoir déjà démarré)
            ' Ne pas tenter de rejouer pour éviter une boucle d'erreurs
        End Try
    End Sub

    Public Sub BasculerPauseReprisePublic()
        Button_PauseReprise_Click(Nothing, Nothing)
    End Sub

    ' ========================================
    ' FERMETURE
    ' ========================================
    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        ' Indiquer que le form est en train de se fermer
        estEnFermeture = True

        ' Arrêter le timer LED avant tout
        If metronomeTimer IsNot Nothing Then
            Try
                metronomeTimer.Stop()
                RemoveHandler metronomeTimer.Tick, AddressOf MetronomeTimer_Tick
                metronomeTimer.Dispose()
                metronomeTimer = Nothing
            Catch
                ' Ignorer les erreurs
            End Try
        End If

        ArreterLecture()
        SauvegarderParametres()
        SauvegarderPlaylist()

        ' Fermer et libérer la fenêtre LED
        If formLight IsNot Nothing Then
            Try
                formLight.Close()
                formLight.Dispose()
                formLight = Nothing
            Catch
                ' Ignorer les erreurs
            End Try
        End If
    End Sub

    Private Sub Form1_Activated(sender As Object, e As EventArgs) Handles MyBase.Activated
        ' Redonner automatiquement le focus à ListView1 quand Form1 redevient actif
        ' (utile après fermeture de FormCompresser ou autres dialogues)
        If ListView1 IsNot Nothing AndAlso Not initialisationEnCours Then
            Try
                ListView1.Focus()
            Catch
                ' Ignorer les erreurs
            End Try
        End If

        ' Restaurer l'échelle du TrackBar si elle a disparu
        ' (cela peut arriver quand on revient de FormParametres qui applique le thème)
        If TrackBar_Avancement IsNot Nothing AndAlso GroupBox_Avancement IsNot Nothing Then
            Try
                ' Réinitialiser complètement l'échelle pour recréer les ticks et labels
                InitialiserEchelleTrackBar()
            Catch
                ' Ignorer les erreurs
            End Try
        End If
    End Sub

    ' ========================================
    ' GESTION MULTI-LANGUE
    ' ========================================
    Private Function GetAboutButtonText() As String
        Select Case LanguageManager.GetCurrentLanguageCode().ToLowerInvariant()
            Case "en"
                Return "About"
            Case "es"
                Return "Acerca de"
            Case "it"
                Return "Informazioni"
            Case "de"
                Return "Über"
            Case Else
                Return "À propos"
        End Select
    End Function

    Private Function GetAboutDialogTitle() As String
        Select Case LanguageManager.GetCurrentLanguageCode().ToLowerInvariant()
            Case "en"
                Return "About AudioPlay"
            Case "es"
                Return "Acerca de AudioPlay"
            Case "it"
                Return "Informazioni su AudioPlay"
            Case "de"
                Return "Über AudioPlay"
            Case Else
                Return "À propos d'AudioPlay"
        End Select
    End Function

    Private Function GetAboutDialogMessage() As String
        Select Case LanguageManager.GetCurrentLanguageCode().ToLowerInvariant()
            Case "en"
                Return "AudioPlay was created by Jean Pelletier, hoping this application will meet your needs. AudioPlay is easy to use and completely free. You may distribute AudioPlay free of charge and must never sell it. I remain open to your constructive comments at: jeanpel58@gmail.com. If you like this application and use it regularly, a small donation sent to the same email address would be greatly appreciated and would encourage me to continue its development, fixes and improvements. Thank you for using AudioPlay!"
            Case "es"
                Return "AudioPlay fue creado por Jean Pelletier, con la esperanza de que esta aplicación responda a sus necesidades. AudioPlay es fácil de usar y completamente gratuito. Puede distribuir AudioPlay gratuitamente y nunca venderlo. Estoy abierto a sus comentarios constructivos en: jeanpel58@gmail.com. Si esta aplicación le gusta y le sirve regularmente, una pequeña donación enviada a la misma dirección de correo sería muy apreciada y me animaría a continuar su desarrollo, correcciones y mejoras. ¡Gracias por usar AudioPlay!"
            Case "it"
                Return "AudioPlay è stato creato da Jean Pelletier, con la speranza che questa applicazione soddisfi le vostre esigenze. AudioPlay è semplice da usare e completamente gratuito. È possibile distribuire AudioPlay gratuitamente e non venderlo mai. Rimango aperto ai vostri commenti costruttivi a: jeanpel58@gmail.com. Se questa applicazione vi piace e vi è utile regolarmente, una piccola donazione inviata allo stesso indirizzo email sarebbe molto apprezzata e mi incoraggerebbe a continuare lo sviluppo, le correzioni e i miglioramenti. Grazie per usare AudioPlay!"
            Case "de"
                Return "AudioPlay wurde von Jean Pelletier erstellt, in der Hoffnung, dass diese Anwendung Ihren Anforderungen entspricht. AudioPlay ist einfach zu bedienen und vollständig kostenlos. Sie dürfen AudioPlay kostenlos weitergeben und niemals verkaufen. Konstruktives Feedback ist willkommen unter: jeanpel58@gmail.com. Wenn Ihnen diese Anwendung gefällt und Sie sie regelmäßig nutzen, wäre eine kleine Spende an dieselbe E-Mail-Adresse sehr willkommen und würde mich ermutigen, die Weiterentwicklung, Korrekturen und Verbesserungen fortzusetzen. Vielen Dank, dass Sie AudioPlay verwenden!"
            Case Else
                Return "AudioPlay a été créé par Jean Pelletier, en espérant que cette application répondra à vos besoins. AudioPlay est simple d'utilisation et complètement gratuit. Vous pouvez distribuer AudioPlay gratuitement et ne jamais le vendre. Je demeure ouvert à vos commentaires constructifs que vous pourrez me faire parvenir au courriel suivant : jeanpel58@gmail.com. Si cette application vous plaît et qu'elle vous sert régulièrement, un petit don envoyé à la même adresse courriel serait grandement apprécié, ce qui m'encouragerait à continuer son développement, ses corrections et ses améliorations... Merci d'utiliser AudioPlay !"
        End Select
    End Function

    Public Sub RefreshLanguage()
        ' Rafraîchir les labels
        Label3.Text = LanguageManager.GetString("Label_Treble")
        Label1.Text = LanguageManager.GetString("Label_Bass")
        LabelVolume.Text = LanguageManager.GetString("Label_Volume")
        Label2.Text = LanguageManager.GetString("Label_RemainingTime")
        LabelSampleRateTitre.Text = LanguageManager.GetString("Label_SampleRate")
        LabelBitrateTitre.Text = LanguageManager.GetString("Label_Bitrate")

        ' Rafraîchir les boutons
        ' Button_Jouer.Text = LanguageManager.GetString("Button_Play")
        Button_Jouer.Text = "" ' Image seulement, pas de texte
        ' Button_Arreter.Text = LanguageManager.GetString("Button_Stop")
        Button_Arreter.Text = "" ' Image seulement, pas de texte
        ' Button_CalculBPM.Text = LanguageManager.GetString("Button_CalculBPM")
        Button_CalculBPM.Text = "" ' Image seulement, pas de texte
        ' Button_InfoSelect.Text = LanguageManager.GetString("Button_InfoSelect")
        Button_InfoSelect.Text = "" ' Image seulement, pas de texte
        'Button_Ajout.Text = LanguageManager.GetString("Button_Add")
        Button_Ajout.Text = "" ' Image seulement, pas de texte
        'Button_Playlist.Text = LanguageManager.GetString("Button_ManageList")
        Button_Playlist.Text = "" ' Image seulement, pas de texte
        Button_APropos.Text = LanguageManager.GetString("Button_APropos")
        ' Button_Parametres.Text = LanguageManager.GetString("Button_Parametres")
        Button_Parametres.Text = "" ' Image seulement, pas de texte
        Button_Loop_Aide.Text = LanguageManager.GetString("Button_Help")

        ' Tooltips : réinitialisation complète pour changement de langue
        Try
            ' Disposer l'ancien tooltip s'il existe
            If toolTipForm1 IsNot Nothing Then
                toolTipForm1.RemoveAll()
                toolTipForm1.Dispose()
                toolTipForm1 = Nothing
            End If

            ' Créer un nouveau tooltip
            toolTipForm1 = New ToolTip()
            toolTipForm1.AutoPopDelay = 5000
            toolTipForm1.InitialDelay = 500
            toolTipForm1.ReshowDelay = 200
            toolTipForm1.ShowAlways = True

            ' Boutons
            toolTipForm1.SetToolTip(Button_Jouer, LanguageManager.GetString("Tip_Button_Play"))
            toolTipForm1.SetToolTip(Button_Arreter, LanguageManager.GetString("Tip_Button_Stop"))
            toolTipForm1.SetToolTip(Button_Ajout, LanguageManager.GetString("Tip_Button_Add"))
            toolTipForm1.SetToolTip(Button_Playlist, LanguageManager.GetString("Tip_Button_ManageList"))
            toolTipForm1.SetToolTip(ButtonModeDJ, LanguageManager.GetString("Tip_Button_DJMode"))
            toolTipForm1.SetToolTip(Button_Parametres, LanguageManager.GetString("Tip_Button_Parametres"))
            toolTipForm1.SetToolTip(Button_Loop_Aide, LanguageManager.GetString("Tip_Button_Loop_Help"))
            toolTipForm1.SetToolTip(Button_Loop, LanguageManager.GetString("Tip_Button_Loop"))
            toolTipForm1.SetToolTip(Button_InfoSelect, LanguageManager.GetString("Tip_Button_InfoSelect"))
            toolTipForm1.SetToolTip(Button_APropos, LanguageManager.GetString("Tip_Button_APropos"))
            toolTipForm1.SetToolTip(Button_AudioPlay_Aide, LanguageManager.GetString("Tip_Button_AudioPlay_Help"))
            toolTipForm1.SetToolTip(Button_ClearRecherche, LanguageManager.GetString("Tip_Button_ClearSearch"))
            toolTipForm1.SetToolTip(Button_Power, LanguageManager.GetString("Tip_Button_Power"))
            toolTipForm1.SetToolTip(Button_CalculBPM, LanguageManager.GetString("Tip_Button_CalculBPM"))
            toolTipForm1.SetToolTip(Button_Aleatoire, LanguageManager.GetString("Tip_Button_Random"))
            toolTipForm1.SetToolTip(Button_Mute, LanguageManager.GetString("Tip_Button_Mute"))
            toolTipForm1.SetToolTip(Button_PauseReprise, LanguageManager.GetString("Tip_Button_PauseResume"))
            toolTipForm1.SetToolTip(Button_Suivant, LanguageManager.GetString("Tip_Button_Next"))
            toolTipForm1.SetToolTip(Button_Precedent, LanguageManager.GetString("Tip_Button_Previous"))

            ' Trackbars
            toolTipForm1.SetToolTip(TrackBar_Aigues, LanguageManager.GetString("Tip_TrackBar_Aigues"))
            toolTipForm1.SetToolTip(TrackBar_Basses, LanguageManager.GetString("Tip_TrackBar_Basses"))
            toolTipForm1.SetToolTip(TrackBar_Volume, LanguageManager.GetString("Tip_TrackBar_Volume"))
            toolTipForm1.SetToolTip(TrackBar_Avancement, LanguageManager.GetString("Tip_TrackBar_Position"))

            ' ComboBox / ListView / GroupBox / Labels
            toolTipForm1.SetToolTip(ComboBox_TypeRecherche, LanguageManager.GetString("Tip_Combo_TypeRecherche"))
            toolTipForm1.SetToolTip(ListView1, LanguageManager.GetString("Tip_ListView1"))
            toolTipForm1.SetToolTip(GroupBox_Avancement, LanguageManager.GetString("Tip_GroupBox_Avancement"))
            toolTipForm1.SetToolTip(Label_DureeRestante, LanguageManager.GetString("Tip_Label_DureeRestante"))
            toolTipForm1.SetToolTip(LabelVolume, LanguageManager.GetString("Tip_Label_Volume"))

            System.Diagnostics.Debug.WriteLine("[RefreshLanguage] Tooltips rafraîchis avec succès")
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[RefreshLanguage] Erreur lors de la configuration des tooltips: {ex.Message}")
        End Try

        ' Rafraîchir le bouton Pause/Reprendre selon l'état
        If enPause Then
            ' Button_PauseReprise.Text = LanguageManager.GetString("Button_Resume")
            Button_PauseReprise.Text = "" ' Image seulement, pas de texte
        Else
            '  Button_PauseReprise.Text = LanguageManager.GetString("Button_Pause")
            Button_PauseReprise.Text = "" ' Image seulement, pas de texte
        End If

        ' Rafraîchir le bouton Aléatoire selon l'état
        If modeAleatoire Then
            ' Button_Aleatoire.Text = LanguageManager.GetString("Button_Random_On")
            Button_Aleatoire.Text = "" ' Image seulement, pas de texte
        Else
            ' Button_Aleatoire.Text = LanguageManager.GetString("Button_Random_Off")
            Button_Aleatoire.Text = "" ' Image seulement, pas de texte
        End If

        ' Rafraîchir le bouton Sourdine
        If isMuted Then
            ' Button_Mute.Text = LanguageManager.GetString("Button_Mute_On")
            Button_Mute.Text = "" ' Image seulement, pas de texte
        Else
            ' Button_Mute.Text = LanguageManager.GetString("Button_Mute_Off")
            Button_Mute.Text = "" ' Image seulement, pas de texte
        End If

        ' Rafraîchir les colonnes du ListView
        Num.Text = LanguageManager.GetString("Column_Num")
        Chansons.Text = LanguageManager.GetString("Column_Songs")
        BPM.Text = LanguageManager.GetString("Column_BPM")
        Durée.Text = LanguageManager.GetString("Column_Duration")

        ' Rafraîchir la ComboBox de type de recherche
        Dim indexActuel As Integer = ComboBox_TypeRecherche.SelectedIndex
        ComboBox_TypeRecherche.Items.Clear()
        ComboBox_TypeRecherche.Items.Add(LanguageManager.GetString("Search_ByFileName"))
        ComboBox_TypeRecherche.Items.Add(LanguageManager.GetString("Search_ByBPM"))
        ComboBox_TypeRecherche.Items.Add(LanguageManager.GetString("Search_ByDuration"))
        If indexActuel >= 0 AndAlso indexActuel < ComboBox_TypeRecherche.Items.Count Then
            ComboBox_TypeRecherche.SelectedIndex = indexActuel
        Else
            ComboBox_TypeRecherche.SelectedIndex = 0
        End If

        ' Rafraîchir le placeholder du TextBox de recherche
        TextBox_Recherche.PlaceholderText = LanguageManager.GetString("Search_Placeholder")

        ' Recréer le menu contextuel pour appliquer les traductions
        CreerMenuContextuel()

        ' Vérification automatique (log/debug) des clés de localisation liées à la recherche
        Try
            Dim keysToCheck As New Dictionary(Of String, String) From {
                {"Search_Placeholder", LanguageManager.GetString("Search_Placeholder")},
                {"Search_ByFileName", LanguageManager.GetString("Search_ByFileName")},
                {"Search_ByBPM", LanguageManager.GetString("Search_ByBPM")},
                {"Search_ByDuration", LanguageManager.GetString("Search_ByDuration")}
            }

            Dim missing As New List(Of String)
            For Each kvp In keysToCheck
                If String.IsNullOrEmpty(kvp.Value) Then
                    missing.Add(kvp.Key)
                End If
            Next

            If missing.Count > 0 Then
                System.Diagnostics.Debug.WriteLine($"[Localisation][Form1] Clés manquantes: {String.Join(", ", missing)}")
            Else
                System.Diagnostics.Debug.WriteLine("[Localisation][Form1] Clés de recherche présentes")
            End If
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[Localisation][Form1] Erreur vérification clés: {ex.Message}")
        End Try

        System.Diagnostics.Debug.WriteLine("Langue rafraîchie dans Form1")
    End Sub

    ''' <summary>
    ''' Méthode publique pour restaurer l'échelle du TrackBar après l'application du thème
    ''' </summary>
    Public Sub RestaurerEchelleTrackBar()
        Try
            InitialiserEchelleTrackBar()
            System.Diagnostics.Debug.WriteLine("[RestaurerEchelleTrackBar] Échelle du TrackBar restaurée")
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[RestaurerEchelleTrackBar] Erreur: {ex.Message}")
        End Try
    End Sub

    Private Sub TrackBar_MouseMove_ShowValue(sender As Object, e As MouseEventArgs)
        Try
            Dim tb = TryCast(sender, TrackBar)
            If tb Is Nothing OrElse toolTipForm1 Is Nothing Then Return
            Dim val As String = tb.Value.ToString()
            Dim prefix As String = LanguageManager.GetString("Tip_TrackBar_ValuePrefix")
            If String.IsNullOrEmpty(prefix) Then
                prefix = "Valeur: "
            End If
            Dim tipText As String = prefix & val
            ' Montrer la bulle près du curseur (convertir la position globale en locale)
            Dim pt As Point = tb.PointToClient(Cursor.Position)
            toolTipForm1.Show(tipText, tb, pt.X + 15, pt.Y + 15, 1500)
        Catch
            ' Ignorer erreurs
        End Try
    End Sub

    ' === Serveur NamedPipe pour recevoir les fichiers des autres instances ===
    Private Sub PipeServerLoop()
        While isPipeServerRunning
            Try
                Using pipeServer As New NamedPipeServerStream(PipeName, PipeDirection.In)
                    pipeServer.WaitForConnection()
                    Using sr As New StreamReader(pipeServer)
                        Dim lignes As New List(Of String)
                        While pipeServer.IsConnected AndAlso Not sr.EndOfStream
                            Dim ligne = sr.ReadLine()
                            If Not String.IsNullOrWhiteSpace(ligne) Then
                                lignes.Add(ligne)
                            End If
                        End While
                        If lignes.Count > 0 Then
                            Me.Invoke(Sub()
                                          For Each fichier In lignes
                                              If File.Exists(fichier) Then
                                                  AjouterFichierAListe(fichier)
                                              End If
                                          Next
                                          MettreAJourNumerotation()
                                          ListView1.Focus()
                                      End Sub)
                        End If
                    End Using
                End Using
            Catch
                Thread.Sleep(100)
            End Try
        End While
    End Sub

    ' Nettoyage Mutex/Pipe à la fermeture
    Private Sub Form1_FormClosed(sender As Object, e As FormClosedEventArgs) Handles Me.FormClosed
        isPipeServerRunning = False
        If pipeServerThread IsNot Nothing AndAlso pipeServerThread.IsAlive Then
            Try
                pipeServerThread.Join(500)
            Catch
            End Try
        End If
        If instanceMutex IsNot Nothing AndAlso isFirstInstance Then
            instanceMutex.ReleaseMutex()
            instanceMutex.Dispose()
            instanceMutex = Nothing
        End If
    End Sub

    ' Donne des infos détaillées sur l'association par défaut pour debug
    Public Function GetAssociationDebugInfo(extension As String) As (IsDefault As Boolean, ProgId As String, OpenCmd As String, OpenCmdExe As String, ExeName As String)
        Try
            Dim userChoiceKey = "Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\" & extension & "\UserChoice"
            Dim userChoiceProgId = Registry.CurrentUser.OpenSubKey(userChoiceKey)?.GetValue("ProgId")?.ToString()
            Dim progId As String = Nothing
            If Not String.IsNullOrEmpty(userChoiceProgId) Then
                progId = userChoiceProgId
            Else
                progId = Registry.GetValue("HKEY_CLASSES_ROOT\" & extension, "", Nothing)?.ToString()
            End If
            If String.IsNullOrEmpty(progId) Then Return (False, "", "", "", "")
            Dim openCmd = Registry.GetValue("HKEY_CLASSES_ROOT\" & progId & "\shell\open\command", "", Nothing)
            If openCmd Is Nothing Then Return (False, progId, "", "", "")
            Dim exePath = System.Reflection.Assembly.GetExecutingAssembly().Location
            Dim exeName = Path.GetFileName(exePath).ToLowerInvariant()
            Dim openCmdStr = openCmd.ToString().ToLowerInvariant().Trim()
            ' Extraire le chemin de l'exécutable cible (tolérant)
            Dim openCmdExe As String = openCmdStr
            Dim idx As Integer
            If openCmdStr.StartsWith("""") Then
                idx = openCmdStr.IndexOf(""""c, 1)
                If idx > 1 Then openCmdExe = openCmdStr.Substring(1, idx - 1)
            Else
                idx = openCmdStr.IndexOf(" "c)
                If idx > 0 Then openCmdExe = openCmdStr.Substring(0, idx)
            End If
            openCmdExe = openCmdExe.Trim(""""c)
            Dim openCmdExeName = Path.GetFileName(openCmdExe).ToLowerInvariant()
            ' Tolérance maximale : comparer uniquement le nom du fichier exécutable, ignorer le chemin
            Dim isDefault = (openCmdExeName = exeName)
            Return (isDefault, progId, openCmdStr, openCmdExe, exeName)
        Catch ex As Exception
            Return (False, "", "", "", "")
        End Try
    End Function

    ' ========================================
    ' KARAOKE CDG
    ' ========================================
    ''' <summary>
    ''' Détecte et charge un fichier CDG associé au fichier audio
    ''' Si un fichier CDG est trouvé, ouvre automatiquement la fenêtre karaoke
    ''' </summary>
    Private Sub DetecterEtChargerCDG(cheminAudio As String)
        Try
            ' Construire le chemin du fichier CDG (même nom, extension .cdg)
            Dim cheminCDG As String = Path.ChangeExtension(cheminAudio, ".cdg")

            ' Vérifier si le fichier CDG existe
            If File.Exists(cheminCDG) Then
                cheminCDGActuel = cheminCDG
                karaokeModeActif = True

                ' Ouvrir automatiquement la fenêtre karaoke si elle n'existe pas
                If formKaraoke Is Nothing OrElse formKaraoke.IsDisposed Then
                    formKaraoke = New FormKaraoke()
                    formKaraoke.Show()
                End If

                ' Charger et démarrer le fichier CDG
                If formKaraoke.LoadCDGFile(cheminCDG) Then
                    ' Démarrer la lecture karaoke synchronisée
                    formKaraoke.StartPlayback(Function() ObtenirTempsLectureActuel())
                    System.Diagnostics.Debug.WriteLine($"Karaoke CDG chargé et démarré: {Path.GetFileName(cheminCDG)}")
                Else
                    System.Diagnostics.Debug.WriteLine($"Erreur chargement CDG: {Path.GetFileName(cheminCDG)}")
                End If
            Else
                ' Pas de fichier CDG trouvé : fermer la fenêtre karaoke si elle est ouverte
                cheminCDGActuel = ""
                karaokeModeActif = False
                If formKaraoke IsNot Nothing AndAlso Not formKaraoke.IsDisposed Then
                    formKaraoke.Close()
                    formKaraoke = Nothing
                End If
                System.Diagnostics.Debug.WriteLine($"Aucun fichier CDG trouvé pour: {Path.GetFileName(cheminAudio)}")
            End If
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"Erreur détection CDG: {ex.Message}")
            cheminCDGActuel = ""
            karaokeModeActif = False
        End Try
    End Sub

    ''' <summary>
    ''' Obtient le temps de lecture actuel en secondes
    ''' </summary>
    Private Function ObtenirTempsLectureActuel() As Double
        Try
            If fichierAudio IsNot Nothing Then
                Return fichierAudio.CurrentTime.TotalSeconds
            End If
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"Erreur obtention temps lecture: {ex.Message}")
        End Try
        Return 0.0
    End Function

    ' ========================================
    ' FONCTIONNALITÉ LOOP (I-O)
    ' ========================================

    Private Sub InitialiserLabelsLoop()
        ' Créer le label I (début de boucle)
        labelLoopStart = New Label With {
            .Text = "I",
            .Font = New Font("Arial", 8, FontStyle.Bold),
            .ForeColor = ObtenirCouleurMarqueursLoop(),
            .BackColor = Color.Transparent,
            .AutoSize = True,
            .Visible = False
        }
        GroupBox_Avancement.Controls.Add(labelLoopStart)
        labelLoopStart.BringToFront()

        ' Créer le label O (fin de boucle)
        labelLoopEnd = New Label With {
            .Text = "O",
            .Font = New Font("Arial", 8, FontStyle.Bold),
            .ForeColor = ObtenirCouleurMarqueursLoop(),
            .BackColor = Color.Transparent,
            .AutoSize = True,
            .Visible = False
        }
        GroupBox_Avancement.Controls.Add(labelLoopEnd)
        labelLoopEnd.BringToFront()
    End Sub

    ''' <summary>
    ''' Initialise et affiche une échelle numérique (0..100) au-dessus de TrackBar_Avancement
    ''' </summary>
    Private Sub InitialiserEchelleTrackBar()
        Try
            If TrackBar_Avancement Is Nothing OrElse GroupBox_Avancement Is Nothing Then Return

            ' Supprimer les anciens labels et ticks si présents
            If trackBarScaleLabels IsNot Nothing Then
                For Each l In trackBarScaleLabels
                    Try
                        GroupBox_Avancement.Controls.Remove(l)
                        l.Dispose()
                    Catch
                    End Try
                Next
            End If
            If trackBarMinorTicks IsNot Nothing Then
                For Each t In trackBarMinorTicks
                    Try
                        GroupBox_Avancement.Controls.Remove(t)
                        t.Dispose()
                    Catch
                    End Try
                Next
            End If

            trackBarScaleLabels = New List(Of Label)()
            trackBarMinorTicks = New List(Of Panel)()

            ' Créer des repères de 0 à 100 tous les 10 (labels majeurs)
            For i As Integer = 0 To 10
                Dim value As Integer = i * 10
                Dim lbl As New Label()
                lbl.AutoSize = True
                lbl.Text = value.ToString()
                lbl.Font = New Font("Segoe UI", 7)
                lbl.BackColor = Color.Transparent
                lbl.ForeColor = SystemColors.ControlText
                lbl.Name = "TrackBarScaleLabel_" & value  ' Identifiant unique pour le ThemeManager
                lbl.Tag = value
                GroupBox_Avancement.Controls.Add(lbl)
                lbl.BringToFront()
                trackBarScaleLabels.Add(lbl)
            Next

            ' Créer des petites lignes (minor ticks) entre les chiffres.
            ' Ici on crée un tick tous les 2 points (valeurs 0..100 step 2) sauf les valeurs majeures (multiples de 10)
            For v As Integer = 0 To 100 Step 2
                If v Mod 10 <> 0 Then
                    Dim tick As New Panel()
                    tick.Size = New Size(1, 6)
                    tick.BackColor = Color.Gray
                    tick.Name = "TrackBarMinorTick_" & v  ' Identifiant unique pour le ThemeManager
                    tick.Tag = v
                    GroupBox_Avancement.Controls.Add(tick)
                    tick.BringToFront()
                    trackBarMinorTicks.Add(tick)
                End If
            Next

            ' Positionner maintenant les labels et les ticks correctement
            MettreAJourEchelleTrackBar(Nothing, EventArgs.Empty)

            ' Retirer les anciens handlers pour éviter les doublons
            Try
                RemoveHandler TrackBar_Avancement.SizeChanged, AddressOf MettreAJourEchelleTrackBar
                RemoveHandler GroupBox_Avancement.SizeChanged, AddressOf MettreAJourEchelleTrackBar
                RemoveHandler TrackBar_Avancement.MouseDown, AddressOf TrackBar_Avancement_MouseDown
            Catch
            End Try

            ' Réagir au redimensionnement du TrackBar ou du groupe
            AddHandler TrackBar_Avancement.SizeChanged, AddressOf MettreAJourEchelleTrackBar
            AddHandler GroupBox_Avancement.SizeChanged, AddressOf MettreAJourEchelleTrackBar
            AddHandler TrackBar_Avancement.MouseDown, AddressOf TrackBar_Avancement_MouseDown
        Catch
        End Try
    End Sub

    Private Sub TrackBar_Avancement_MouseDown(sender As Object, e As MouseEventArgs)
        Try
            If TrackBar_Avancement Is Nothing Then Return

            Dim tb = TrackBar_Avancement
            Dim thumbPadding As Integer = ObtenirTrackBarPadding(tb)
            Dim trackWidth As Integer = Math.Max(1, tb.Width - (2 * thumbPadding))
            Dim localX As Integer = e.X - thumbPadding
            Dim ratio As Double = 0.0
            If localX <= 0 Then
                ratio = 0.0
            ElseIf localX >= trackWidth Then
                ratio = 1.0
            Else
                ratio = localX / CDbl(trackWidth)
            End If

            ' Mettre à jour la valeur du TrackBar
            Dim newValue As Integer = tb.Minimum + CInt(Math.Round(ratio * (tb.Maximum - tb.Minimum)))
            newValue = Math.Max(tb.Minimum, Math.Min(tb.Maximum, newValue))
            tb.Value = newValue

            ' Déplacer la lecture audio à la position correspondante
            If fichierAudio IsNot Nothing AndAlso fichierAudio.TotalTime.TotalSeconds > 0 Then
                Dim targetSeconds = ratio * fichierAudio.TotalTime.TotalSeconds
                Try
                    fichierAudio.CurrentTime = TimeSpan.FromSeconds(targetSeconds)
                Catch
                End Try
                MettreAJourPositionLabelsLoop()
            End If

            ' Si lecture en pause, reprendre; sinon si aucune lecture, lancer la lecture de l'item sélectionné
            If lectureEnCours Then
                If enPause Then
                    Try
                        Button_PauseReprise.PerformClick()
                    Catch
                    End Try
                End If
            Else
                If ListView1.SelectedItems.Count > 0 Then
                    Try
                        JouerItemSelectionne()
                    Catch
                    End Try
                End If
            End If
        Catch
        End Try
    End Sub

    Private Sub MettreAJourEchelleTrackBar(sender As Object, e As EventArgs)
        Try
            If TrackBar_Avancement Is Nothing OrElse trackBarScaleLabels Is Nothing Then Return
            If TrackBar_Avancement.Width <= 0 Then Return

            Dim thumbPadding As Integer = ObtenirTrackBarPadding(TrackBar_Avancement)
            Dim trackWidth As Integer = Math.Max(1, TrackBar_Avancement.Width - (2 * thumbPadding))
            Dim trackStartX As Integer = TrackBar_Avancement.Left + thumbPadding

            For Each lbl In trackBarScaleLabels
                Dim value = CInt(lbl.Tag)
                Dim ratio As Double = value / 100.0
                Dim x As Integer = trackStartX + CInt(Math.Round(ratio * trackWidth)) - (lbl.Width \ 2)
                Dim y As Integer = TrackBar_Avancement.Top - lbl.Height - 6
                lbl.Location = New Point(x, y)
                lbl.Visible = True
            Next

            ' Positionner les minor ticks s'ils existent
            If trackBarMinorTicks IsNot Nothing Then
                For Each tick In trackBarMinorTicks
                    Dim value = CInt(tick.Tag)
                    Dim ratio As Double = value / 100.0
                    Dim x As Integer = trackStartX + CInt(Math.Round(ratio * trackWidth)) - (tick.Width \ 2)
                    Dim y As Integer = TrackBar_Avancement.Top - tick.Height - 2
                    tick.Location = New Point(x, y)
                    tick.Visible = True
                Next
            End If
        Catch
        End Try
    End Sub

    ''' <summary>
    ''' Calcule le padding horizontal du TrackBar (marge avant le début du track effectif)
    ''' </summary>
    Private Function ObtenirTrackBarPadding(tb As TrackBar) As Integer
        ' Le padding dépend de la largeur du thumb et du style visuel
        ' Formule empirique basée sur les TrackBars Windows standards
        ' Typiquement entre 9 et 11 pixels selon le DPI et le style

        ' Estimation basée sur la plage du TrackBar
        ' Plus la plage est grande, plus le calcul doit être précis
        Dim padding As Integer = 10

        ' Ajustement si le TrackBar a une orientation ou un style particulier
        If tb.Width < 100 Then
            padding = 8  ' TrackBar petit
        ElseIf tb.Width > 500 Then
            padding = 11  ' TrackBar large
        End If

        Return padding
    End Function

    ''' <summary>
    ''' Détermine la couleur des marqueurs I et O en fonction de la couleur de fond
    ''' Rouge si le fond est clair, Jaune si le fond est rouge ou sombre
    ''' </summary>
    Private Function ObtenirCouleurMarqueursLoop() As Color
        ' Obtenir la couleur de fond actuelle du formulaire
        Dim fondCouleur As Color = Me.BackColor

        ' Calculer la luminosité (brightness) de la couleur de fond
        ' Formule : (R*0.299 + G*0.587 + B*0.114)
        Dim luminosite As Double = (fondCouleur.R * 0.299 + fondCouleur.G * 0.587 + fondCouleur.B * 0.114)

        ' Vérifier si la couleur de fond est rouge (dominance de rouge)
        Dim estRouge As Boolean = (fondCouleur.R > 150 AndAlso fondCouleur.R > fondCouleur.G + 50 AndAlso fondCouleur.R > fondCouleur.B + 50)

        ' Si le fond est rouge ou très sombre, utiliser jaune
        If estRouge OrElse luminosite < 128 Then
            Return Color.Yellow
        Else
            ' Sinon, utiliser rouge
            Return Color.Red
        End If
    End Function

    ''' <summary>
    ''' Met à jour la couleur des marqueurs I et O selon le thème actuel
    ''' </summary>
    Private Sub MettreAJourCouleurMarqueursLoop()
        If labelLoopStart IsNot Nothing Then
            labelLoopStart.ForeColor = ObtenirCouleurMarqueursLoop()
        End If
        If labelLoopEnd IsNot Nothing Then
            labelLoopEnd.ForeColor = ObtenirCouleurMarqueursLoop()
        End If
    End Sub

    Private Sub MettreAJourPositionLabelsLoop()
        If Not hasLoopMarkers Then
            ' Aucun marqueur posé -> masquer les labels
            If labelLoopStart IsNot Nothing Then labelLoopStart.Visible = False
            If labelLoopEnd IsNot Nothing Then labelLoopEnd.Visible = False
            Return
        End If

        If fichierAudio IsNot Nothing AndAlso fichierAudio.TotalTime.TotalSeconds > 0 Then
            Dim total = fichierAudio.TotalTime.TotalSeconds

            ' Calculer la position exacte du thumb sur le TrackBar
            ' Utiliser la fonction helper pour obtenir le padding précis
            Dim thumbPadding As Integer = ObtenirTrackBarPadding(TrackBar_Avancement)

            ' Largeur utilisable du track (sans les marges)
            Dim trackWidth As Integer = TrackBar_Avancement.Width - (2 * thumbPadding)
            Dim trackStartX As Integer = TrackBar_Avancement.Left + thumbPadding

            ' Position du label I - si défini
            If loopStartPosition > TimeSpan.Zero AndAlso labelLoopStart IsNot Nothing Then
                Dim startRatio As Double = Math.Min(1.0, Math.Max(0.0, loopStartPosition.TotalSeconds / total))
                Dim startThumbX As Integer = trackStartX + CInt(Math.Round(startRatio * trackWidth))
                Dim startX As Integer = startThumbX - (labelLoopStart.Width \ 2)
                Dim startY As Integer = TrackBar_Avancement.Top - labelLoopStart.Height - 2
                labelLoopStart.Location = New Point(startX, startY)
                labelLoopStart.Visible = True
                labelLoopStart.BringToFront()
            ElseIf labelLoopStart IsNot Nothing Then
                labelLoopStart.Visible = False
            End If

            ' Position du label O - si défini
            If loopEndPosition > TimeSpan.Zero AndAlso labelLoopEnd IsNot Nothing Then
                Dim endRatio As Double = Math.Min(1.0, Math.Max(0.0, loopEndPosition.TotalSeconds / total))
                Dim endThumbX As Integer = trackStartX + CInt(Math.Round(endRatio * trackWidth))
                Dim endX As Integer = endThumbX - (labelLoopEnd.Width \ 2)
                Dim endY As Integer = TrackBar_Avancement.Top - labelLoopEnd.Height - 2
                labelLoopEnd.Location = New Point(endX, endY)
                labelLoopEnd.Visible = True
                labelLoopEnd.BringToFront()
            ElseIf labelLoopEnd IsNot Nothing Then
                labelLoopEnd.Visible = False
            End If
        End If
    End Sub

    Private Sub EffacerMarqueursLoop()
        hasLoopMarkers = False
        loopEnabled = False
        loopStartPosition = TimeSpan.Zero
        loopEndPosition = TimeSpan.Zero

        If labelLoopStart IsNot Nothing Then
            labelLoopStart.Visible = False
        End If

        If labelLoopEnd IsNot Nothing Then
            labelLoopEnd.Visible = False
        End If

        ' Désactiver visuellement le bouton Loop
        If Button_Loop IsNot Nothing Then
            Button_Loop.BackgroundImage = AudioPlay.Resources.AudioPlay_Loop_Carre_Gris
        End If
    End Sub

    Private Sub Button_Loop_Click(sender As Object, e As EventArgs) Handles Button_Loop.Click
        If Not hasLoopMarkers Then
            MessageBox.Show(LanguageManager.GetString("Loop_NotDefined_Message"),
                          LanguageManager.GetString("Loop_NotDefined_Title"),
                          MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        loopEnabled = Not loopEnabled

        If loopEnabled Then
            Button_Loop.BackgroundImage = AudioPlay.Resources.AudioPlay_Loop_Carre_Vert
            ' Commencer la lecture à partir du début de la boucle
            If fichierAudio IsNot Nothing Then
                fichierAudio.CurrentTime = loopStartPosition
            End If
        Else
            ' Désactiver la boucle et effacer les marqueurs I et O
            EffacerMarqueursLoop()
        End If
    End Sub

    Private Sub Button_Loop_Aide_Click(sender As Object, e As EventArgs) Handles Button_Loop_Aide.Click
        Try
            ' Déterminer la langue actuelle
            Dim langueActuelle = LanguageManager.CurrentCulture.TwoLetterISOLanguageName.ToLower
            Dim suffixeLangue = ""

            Select Case langueActuelle
                Case "fr"
                    suffixeLangue = ".fr"
                Case "en"
                    suffixeLangue = ".en"
                Case "es"
                    suffixeLangue = ".es"
                Case "de"
                    suffixeLangue = ".de"
                Case "it"
                    suffixeLangue = ".it"
                Case Else
                    suffixeLangue = ".en" ' Par défaut en anglais
            End Select

            Dim cheminHtml = Path.Combine(Application.StartupPath, $"LOOP_GUIDE_USER{suffixeLangue}.html")

            ' Ouvrir le fichier HTML dans le navigateur par défaut
            If File.Exists(cheminHtml) Then
                Process.Start(New ProcessStartInfo(cheminHtml) With {.UseShellExecute = True})
            Else
                Dim title As String = LanguageManager.GetString("Loop_Help_Title")
                MessageBox.Show(LanguageManager.GetString("Help_FilesNotFound") & Environment.NewLine &
                              LanguageManager.GetString("Help_ExpectedFiles") & Environment.NewLine &
                              "- " & cheminHtml,
                              title,
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Warning)
            End If

        Catch ex As Exception
            MessageBox.Show(LanguageManager.GetString("Help_ErrorOpenFile", ex.Message),
                          LanguageManager.GetString("Error_Title"),
                          MessageBoxButtons.OK,
                          MessageBoxIcon.Error)
        End Try
    End Sub

    ' Ouvrir le dernier crash.log si présent (menu Aide)
    Private Sub Button_ViewCrashLog_Click(sender As Object, e As EventArgs)
        Try
            Dim logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioPlay")
            Dim logFile = Path.Combine(logDir, "crash.log")
            If File.Exists(logFile) Then
                Process.Start(New ProcessStartInfo(logFile) With {.UseShellExecute = True})
            Else
                MessageBox.Show(LanguageManager.GetString("CrashLog_NotFound"), LanguageManager.GetString("CrashLog_Title"), MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        Catch ex As Exception
            MessageBox.Show(LanguageManager.GetString("Error_Generic"), LanguageManager.GetString("Error_Title"), MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub Button_AudioPlay_Aide_Click(sender As Object, e As EventArgs) Handles Button_AudioPlay_Aide.Click
        Try
            ' Déterminer la langue actuelle
            Dim langueActuelle = LanguageManager.CurrentCulture.TwoLetterISOLanguageName.ToLower
            Dim suffixeLangue = ""

            Select Case langueActuelle
                Case "fr"
                    suffixeLangue = ".fr"
                Case "en"
                    suffixeLangue = ".en"
                Case "es"
                    suffixeLangue = ".es"
                Case "de"
                    suffixeLangue = ".de"
                Case "it"
                    suffixeLangue = ".it"
                Case Else
                    suffixeLangue = ".en" ' Par défaut en anglais
            End Select

            Dim cheminHtml = Path.Combine(Application.StartupPath, $"AUDIOPLAY_GUIDE_COMPLET{suffixeLangue}.html")

            ' Ouvrir le fichier HTML dans le navigateur par défaut
            If File.Exists(cheminHtml) Then
                Process.Start(New ProcessStartInfo(cheminHtml) With {.UseShellExecute = True})
            Else
                MessageBox.Show(LanguageManager.GetString("Help_FilesNotFound") & Environment.NewLine &
                              LanguageManager.GetString("Help_ExpectedFiles") & Environment.NewLine &
                              "- " & cheminHtml,
                              LanguageManager.GetString("Help_Title"),
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Warning)
            End If

        Catch ex As Exception
            MessageBox.Show(LanguageManager.GetString("Help_ErrorOpenFile", ex.Message),
                          LanguageManager.GetString("Error_Title"),
                          MessageBoxButtons.OK,
                          MessageBoxIcon.Error)
        End Try
    End Sub

    ' ========================================
    ' FONCTIONNALITÉ DE RECHERCHE INSTANTANÉE
    ' ========================================

    Private Sub InitialiserRechercheControles()
        ' Initialiser le ComboBox avec les 3 options de recherche
        ComboBox_TypeRecherche.Items.Clear()
        ComboBox_TypeRecherche.Items.Add(LanguageManager.GetString("Search_ByFileName"))
        ComboBox_TypeRecherche.Items.Add(LanguageManager.GetString("Search_ByBPM"))
        ComboBox_TypeRecherche.Items.Add(LanguageManager.GetString("Search_ByDuration"))
        ComboBox_TypeRecherche.SelectedIndex = 0 ' Par défaut : recherche par nom de fichier

        ' Définir le placeholder du TextBox
        TextBox_Recherche.PlaceholderText = LanguageManager.GetString("Search_Placeholder")

        ' Note: La configuration visuelle de Button_ClearRecherche (FlatStyle, couleurs)
        ' est gérée automatiquement par le Designer via FlatAppearance

        ' Connecter les événements pour la recherche instantanée
        AddHandler TextBox_Recherche.TextChanged, AddressOf TextBox_Recherche_TextChanged
        AddHandler ComboBox_TypeRecherche.SelectedIndexChanged, AddressOf ComboBox_TypeRecherche_SelectedIndexChanged
        AddHandler TextBox_Recherche.KeyDown, AddressOf TextBox_Recherche_KeyDown
        AddHandler Button_ClearRecherche.Click, AddressOf Button_ClearRecherche_Click
        ' Note: MouseEnter/Leave/Down/Up supprimés - FlatAppearance gère automatiquement les couleurs
    End Sub

    Private Sub Button_ClearRecherche_Click(sender As Object, e As EventArgs)
        ' Vider le TextBox et remettre le focus sur le ListView
        TextBox_Recherche.Clear()
        ListView1.Focus()
    End Sub

    Private Sub TextBox_Recherche_KeyDown(sender As Object, e As KeyEventArgs)
        ' Touche Échap : vider le TextBox et remettre le focus sur le ListView
        If e.KeyCode = Keys.Escape Then
            TextBox_Recherche.Clear()
            ListView1.Focus()
            e.Handled = True
            e.SuppressKeyPress = True
        End If
    End Sub

    Private Sub TextBox_Recherche_TextChanged(sender As Object, e As EventArgs)
        ' Recherche instantanée au fur et à mesure que l'utilisateur tape
        EffectuerRecherche()
    End Sub

    Private Sub ComboBox_TypeRecherche_SelectedIndexChanged(sender As Object, e As EventArgs)
        ' Relancer la recherche quand le type change
        EffectuerRecherche()
    End Sub

    Private Sub EffectuerRecherche()
        If ListView1.Items.Count = 0 Then Return

        Dim texteRecherche As String = TextBox_Recherche.Text.Trim()
        Dim typeRecherche As Integer = ComboBox_TypeRecherche.SelectedIndex

        ' Si le texte de recherche est vide, réinitialiser la sélection
        If String.IsNullOrWhiteSpace(texteRecherche) Then
            ListView1.SelectedItems.Clear()
            Return
        End If

        ' Désactiver temporairement les événements de sélection pour éviter les effets de bord
        ListView1.BeginUpdate()
        ListView1.SelectedItems.Clear()

        Try
            Select Case typeRecherche
                Case 0 ' Recherche par nom de fichier
                    RechercherParNomFichier(texteRecherche)
                Case 1 ' Recherche par BPM
                    RechercherParBPM(texteRecherche)
                Case 2 ' Recherche par durée
                    RechercherParDuree(texteRecherche)
            End Select
        Finally
            ListView1.EndUpdate()
        End Try

        ' Faire défiler vers le premier élément trouvé
        If ListView1.SelectedItems.Count > 0 Then
            ListView1.EnsureVisible(ListView1.SelectedItems(0).Index)
        End If
    End Sub

    Private Sub RechercherParNomFichier(texteRecherche As String)
        ' Recherche insensible à la casse dans le nom de fichier (colonne Chansons)
        For Each item As ListViewItem In ListView1.Items
            If item.SubItems(1).Text.IndexOf(texteRecherche, StringComparison.OrdinalIgnoreCase) >= 0 Then
                item.Selected = True
            End If
        Next
    End Sub

    Private Sub RechercherParBPM(texteRecherche As String)
        ' Recherche par BPM (colonne BPM)
        For Each item As ListViewItem In ListView1.Items
            Dim bpmText As String = item.SubItems(2).Text
            If bpmText.IndexOf(texteRecherche, StringComparison.OrdinalIgnoreCase) >= 0 Then
                item.Selected = True
            End If
        Next
    End Sub

    Private Sub RechercherParDuree(texteRecherche As String)
        ' Recherche par durée (colonne Durée)
        ' Support de formats comme "3:45", "3:", ":45", "3", etc.
        For Each item As ListViewItem In ListView1.Items
            Dim dureeText As String = item.SubItems(3).Text
            If dureeText.IndexOf(texteRecherche, StringComparison.OrdinalIgnoreCase) >= 0 Then
                item.Selected = True
            End If
        Next
    End Sub

    ''' <summary>
    ''' Met à jour les paramètres des effets audio en temps réel sans relancer la chanson
    ''' </summary>
    Public Sub MettreAJourEffetsAudio()
        ' Mettre à jour Reverb
        If reverbProvider IsNot Nothing Then
            reverbProvider.Enabled = ParametresGlobaux.EffetReverbActif
            reverbProvider.Mix = ParametresGlobaux.EffetReverbMix
            System.Diagnostics.Debug.WriteLine($"Reverb mis à jour: Enabled={reverbProvider.Enabled}, Mix={reverbProvider.Mix}")
        End If

        ' Mettre à jour Echo
        If echoProvider IsNot Nothing Then
            echoProvider.Enabled = ParametresGlobaux.EffetEchoActif
            echoProvider.Mix = ParametresGlobaux.EffetEchoMix
            echoProvider.DelayMilliseconds = ParametresGlobaux.EffetEchoDelai
            echoProvider.Feedback = ParametresGlobaux.EffetEchoFeedback
            System.Diagnostics.Debug.WriteLine($"Echo mis à jour: Enabled={echoProvider.Enabled}, Mix={echoProvider.Mix}, Delay={echoProvider.DelayMilliseconds}ms, Feedback={echoProvider.Feedback}")
        End If

        ' Mettre à jour Time Stretch
        If timeStretchProvider IsNot Nothing Then
            timeStretchProvider.Enabled = ParametresGlobaux.EffetTimeStretchActif
            timeStretchProvider.TempoChange = ParametresGlobaux.EffetTimeStretchRatio
            System.Diagnostics.Debug.WriteLine($"TimeStretch mis à jour: Enabled={timeStretchProvider.Enabled}, Ratio={timeStretchProvider.TempoChange}")
        End If

        ' Mettre à jour Pitch Shift
        If pitchShiftProvider IsNot Nothing Then
            pitchShiftProvider.Enabled = ParametresGlobaux.EffetPitchShiftActif
            pitchShiftProvider.PitchSemiTones = ParametresGlobaux.EffetPitchShiftSemiTones
            System.Diagnostics.Debug.WriteLine($"PitchShift mis à jour: Enabled={pitchShiftProvider.Enabled}, SemiTones={pitchShiftProvider.PitchSemiTones}")
        End If

        ' Mettre à jour Phaser
        If phaserProvider IsNot Nothing Then
            phaserProvider.Enabled = ParametresGlobaux.EffetPhaserActif
            phaserProvider.Rate = ParametresGlobaux.EffetPhaserRate
            phaserProvider.Depth = ParametresGlobaux.EffetPhaserDepth
            phaserProvider.Feedback = ParametresGlobaux.EffetPhaserFeedback
            phaserProvider.Mix = ParametresGlobaux.EffetPhaserMix
            phaserProvider.Stages = ParametresGlobaux.EffetPhaserStages
            System.Diagnostics.Debug.WriteLine($"Phaser mis à jour: Enabled={phaserProvider.Enabled}, Rate={phaserProvider.Rate}, Depth={phaserProvider.Depth}, Feedback={phaserProvider.Feedback}, Mix={phaserProvider.Mix}, Stages={phaserProvider.Stages}")
        End If
    End Sub

End Class

' === Classe utilitaire pour l'égaliseur ===
Public Class SimpleEqualizerProvider
    Implements ISampleProvider

    Private ReadOnly sourceProvider As ISampleProvider
    Private bassGainValue As Single = 0.0F
    Private trebleGainValue As Single = 0.0F

    ' Filtres passe-bas et passe-haut simples
    Private bassLastSample As Single = 0.0F
    Private trebleLastSample As Single = 0.0F

    Public Sub New(source As ISampleProvider, bassGain As Single, trebleGain As Single)
        sourceProvider = source
        bassGainValue = bassGain
        trebleGainValue = trebleGain
    End Sub

    Public Property BassGain As Single
        Get
            Return bassGainValue
        End Get
        Set(value As Single)
            bassGainValue = value
        End Set
    End Property

    Public Property TrebleGain As Single
        Get
            Return trebleGainValue
        End Get
        Set(value As Single)
            trebleGainValue = value
        End Set
    End Property

    Public ReadOnly Property WaveFormat As WaveFormat Implements ISampleProvider.WaveFormat
        Get
            Return sourceProvider.WaveFormat
        End Get
    End Property

    Public Function Read(buffer() As Single, offset As Integer, count As Integer) As Integer Implements ISampleProvider.Read
        Dim samplesRead = sourceProvider.Read(buffer, offset, count)

        ' Appliquer l'égalisation simple
        For i = offset To offset + samplesRead - 1
            Dim sample = buffer(i)

            ' Filtre passe-bas simple pour les basses (coefficient ~ 0.3)
            bassLastSample = bassLastSample * 0.7F + sample * 0.3F
            Dim bassComponent = bassLastSample * (bassGainValue / 20.0F)

            ' Filtre passe-haut simple pour les aigües
            Dim trebleComponent = (sample - bassLastSample) * (trebleGainValue / 20.0F)

            ' Mixer le signal
            buffer(i) = sample + bassComponent + trebleComponent

            ' Limiter pour éviter la saturation
            If buffer(i) > 1.0F Then buffer(i) = 1.0F
            If buffer(i) < -1.0F Then buffer(i) = -1.0F
        Next

        Return samplesRead
    End Function

End Class
