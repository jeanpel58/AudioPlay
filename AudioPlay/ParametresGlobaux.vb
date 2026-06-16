Imports System.IO
Imports System.Threading

Public Module ParametresGlobaux
    Public ConfirmerEffacementChansons As Boolean = True

    ' === Effets audio ===
    ' Reverb
    Public EffetReverbActif As Boolean = False
    Public EffetReverbMix As Single = 0.3F ' 0.0 à 1.0

    ' Echo
    Public EffetEchoActif As Boolean = False
    Public EffetEchoMix As Single = 0.3F ' 0.0 à 1.0
    Public EffetEchoDelai As Integer = 300 ' ms (50 à 2000)
    Public EffetEchoFeedback As Single = 0.5F ' 0.0 à 0.9

    ' Time Stretch
    Public EffetTimeStretchActif As Boolean = False
    Public EffetTimeStretchRatio As Single = 1.0F ' 0.5 à 2.0

    ' Pitch Shift
    Public EffetPitchShiftActif As Boolean = False
    Public EffetPitchShiftSemiTones As Single = 0.0F ' -12 à +12 demi-tons

    ' Phaser
    Public EffetPhaserActif As Boolean = False
    Public EffetPhaserRate As Single = 0.5F ' 0.1 à 10.0 Hz - Vitesse modérée classique
    Public EffetPhaserDepth As Single = 0.7F ' 0.0 à 1.0 - Balayage audible mais musical (était 1.0)
    Public EffetPhaserFeedback As Single = 0.3F ' 0.0 à 0.95 - Résonance douce vintage (était 0.5)
    Public EffetPhaserMix As Single = 0.5F ' 0.0 à 1.0 - Équilibre parfait dry/wet (était 1.0)
    Public EffetPhaserStages As Integer = 4 ' 2, 4, 6, 8, 12 - Son vintage classique

    ' Mode Mixeur DJ
    Public ModeMixeurDJ As Boolean = False ' Mode lecteur simple (False) ou mixeur DJ (True)

    ' === Paramètres persistants globaux (répertoires utilisés) ===
    Public repertoireParDefaut As String = ""
    Public dernierRepertoireAjoutFichier As String = ""
    Public dernierRepertoireAjoutRepertoire As String = ""
    Public avantDernierRepertoireAjoutRepertoire As String = ""
    ' Conserver le dernier répertoire CHOISI (non modifié) afin de pouvoir ouvrir
    ' le parent direct au prochain affichage sans calculs cumulés
    Public dernierRepertoireAjoutRepertoireChoisi As String = ""
    Public dernierRepertoirePlaylist As String = ""
    Public dernierRepertoirePlaylist_Simple As String = ""
    ' === Clés spécifiques au mode DJ (mémoire séparée) ===
    Public dernierRepertoireAjoutFichier_DJ As String = ""
    Public dernierRepertoireAjoutRepertoire_DJ As String = ""
    Public dernierRepertoirePlaylist_DJ As String = ""
    Public avantDernierRepertoireAjoutRepertoire_DJ As String = ""
    Public dernierRepertoireAjoutRepertoireChoisi_DJ As String = ""
    ' Supprimer de manière robuste un dossier temporaire créé par AudioPlay
    Public Sub SupprimerDossierTemporaire(ByVal dossier As String)
        If String.IsNullOrEmpty(dossier) Then Return
        Try
            If Not Directory.Exists(dossier) Then Return
        Catch
            Return
        End Try

        ' Essayer de supprimer de façon récursive en nettoyant les attributs et en réessayant
        For attempt As Integer = 1 To 5
            Try
                ' Rendre tous les fichiers et dossiers accessibles
                Try
                    For Each f In Directory.GetFiles(dossier, "*", SearchOption.AllDirectories)
                        Try
                            File.SetAttributes(f, FileAttributes.Normal)
                        Catch
                        End Try
                    Next
                Catch
                End Try

                For Each d In Directory.GetDirectories(dossier, "*", SearchOption.AllDirectories)
                    Try
                        Dim attrs = File.GetAttributes(d)
                        File.SetAttributes(d, FileAttributes.Directory)
                    Catch
                    End Try
                Next

                Directory.Delete(dossier, True)
                Return
            Catch
                ' Attendre un court instant puis réessayer
                Thread.Sleep(150)
                GC.Collect()
                GC.WaitForPendingFinalizers()
            End Try
        Next

        ' Si la suppression échoue persistante, tenter une suppression simple en dernier recours
        Try
                If Directory.Exists(dossier) Then
                Directory.Delete(dossier, True)
            End If
        Catch
            ' Rien d'autre à faire ; on laisse le dossier si impossible
        End Try
    End Sub

    ' Balayer et supprimer les dossiers .AudioPlayTmp_* dans un répertoire donné
    Public Sub SupprimerTempRestantsDans(ByVal racine As String)
        If String.IsNullOrEmpty(racine) Then Return
        Try
            If Not Directory.Exists(racine) Then Return
        Catch
            Return
        End Try

        Try
            For Each d In Directory.GetDirectories(racine, ".AudioPlayTmp_*", SearchOption.TopDirectoryOnly)
                Try
                    SupprimerDossierTemporaire(d)
                Catch
                End Try
            Next
        Catch
        End Try
    End Sub

End Module

' Mettre à jour une seule clé dans le fichier parametres.txt sans modifier les autres
Public Module ParametresGlobauxHelpers
    Public Sub EcrireCleParametres(cle As String, valeur As String)
        Try
            Dim fichierParam = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioPlay", "parametres.txt")
            Dim lignes As New List(Of String)()
            If File.Exists(fichierParam) Then
                lignes = File.ReadAllLines(fichierParam).ToList()
            Else
                Dim dossier = Path.GetDirectoryName(fichierParam)
                If Not Directory.Exists(dossier) Then Directory.CreateDirectory(dossier)
            End If

            Dim found As Boolean = False
            For i = 0 To lignes.Count - 1
                If lignes(i).StartsWith(cle & "=") Then
                    lignes(i) = cle & "=" & valeur
                    found = True
                    Exit For
                End If
            Next

            If Not found Then
                lignes.Add(cle & "=" & valeur)
            End If

            File.WriteAllLines(fichierParam, lignes)
        Catch
            ' Ignorer les erreurs d'écriture
        End Try
    End Sub
End Module

' Écrire le fichier parametres.txt à partir des valeurs actuelles dans ParametresGlobaux
