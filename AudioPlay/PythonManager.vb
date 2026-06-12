Imports System.IO
Imports System.IO.Compression
Imports System.Net.Http
Imports System.Diagnostics

''' <summary>
''' Gère l'installation et l'utilisation de Python Embedded pour librosa
''' </summary>
Public Class PythonManager
    ' URL de téléchargement de Python Embedded
    Private Const PYTHON_EMBED_URL As String = "https://www.python.org/ftp/python/3.11.9/python-3.11.9-embed-amd64.zip"
    Private Const GET_PIP_URL As String = "https://bootstrap.pypa.io/get-pip.py"

    ' Chemin du dossier Python dans l'application
    Private Shared ReadOnly PythonDir As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioPlay", "python_embedded")
    Private Shared ReadOnly PythonExe As String = Path.Combine(PythonDir, "python.exe")
    Private Shared ReadOnly BPMScriptPath As String = Path.Combine(PythonDir, "bpm_detector.py")

    ''' <summary>
    ''' Obtient le chemin de l'exécutable Python
    ''' </summary>
    Public Shared ReadOnly Property CheminPython As String
        Get
            Return PythonExe
        End Get
    End Property

    ''' <summary>
    ''' Obtient le chemin du dossier Python
    ''' </summary>
    Public Shared ReadOnly Property CheminDossierPython As String
        Get
            Return PythonDir
        End Get
    End Property

    ''' <summary>
    ''' Vérifie si Python Embedded est installé
    ''' </summary>
    Public Shared Function EstInstalle() As Boolean
        Return File.Exists(PythonExe) AndAlso File.Exists(BPMScriptPath)
    End Function

    ''' <summary>
    ''' Vérifie si librosa est installé
    ''' </summary>
    Public Shared Async Function LibrosaEstInstalle() As Task(Of Boolean)
        Try
            If Not EstInstalle() Then
                Return False
            End If

            Dim result = Await ExecuterPython("-c ""import librosa; print('OK')""")
            Return result.Contains("OK")
        Catch
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Télécharge et installe Python Embedded avec librosa
    ''' </summary>
    Public Shared Async Function InstallerPythonEmbedded(progress As IProgress(Of String)) As Task(Of Boolean)
        Try
            ' Créer le dossier
            If Not Directory.Exists(PythonDir) Then
                Directory.CreateDirectory(PythonDir)
            End If

            ' Étape 1 : Télécharger Python Embedded
            progress?.Report("Téléchargement de Python (±9 MB)...")
            Dim zipPath As String = Path.Combine(Path.GetTempPath(), "python_embedded.zip")

            Using client As New HttpClient()
                client.Timeout = TimeSpan.FromMinutes(10)
                Dim bytes = Await client.GetByteArrayAsync(PYTHON_EMBED_URL)
                File.WriteAllBytes(zipPath, bytes)
            End Using

            ' Étape 2 : Extraire
            progress?.Report("Extraction de Python...")
            ZipFile.ExtractToDirectory(zipPath, PythonDir, True)
            File.Delete(zipPath)

            ' Étape 3 : Configurer Python pour permettre pip
            progress?.Report("Configuration de Python...")
            ConfigurerPythonEmbedded()

            ' Étape 4 : Télécharger get-pip.py
            progress?.Report("Téléchargement de pip...")
            Using client As New HttpClient()
                Dim pipScript = Await client.GetStringAsync(GET_PIP_URL)
                File.WriteAllText(Path.Combine(PythonDir, "get-pip.py"), pipScript)
            End Using

            ' Étape 5 : Installer pip
            progress?.Report("Installation de pip...")
            Await ExecuterPython("get-pip.py")

            ' Étape 6 : Installer librosa et dépendances
            progress?.Report("Installation de librosa (±200 MB, peut prendre quelques minutes)...")
            Await ExecuterPython("-m pip install --no-warn-script-location librosa numpy scipy")

            ' Étape 7 : Créer le script Python de détection BPM
            progress?.Report("Création du script de détection BPM...")
            CreerScriptBPM()

            progress?.Report("Installation terminée avec succès !")
            Return True

        Catch ex As Exception
            progress?.Report($"Erreur : {ex.Message}")
            Debug.WriteLine($"Erreur installation Python : {ex}")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Configure Python Embedded pour supporter pip
    ''' </summary>
    Private Shared Sub ConfigurerPythonEmbedded()
        ' Modifier python311._pth pour décommenter import site
        Dim pthFiles = Directory.GetFiles(PythonDir, "python*._pth")
        If pthFiles.Length > 0 Then
            Dim content = File.ReadAllText(pthFiles(0))
            content = content.Replace("#import site", "import site")
            content = content.Replace("# import site", "import site")
            If Not content.Contains("import site") Then
                content &= vbCrLf & "import site"
            End If
            File.WriteAllText(pthFiles(0), content)
        End If
    End Sub

    ''' <summary>
    ''' Crée le script Python pour la détection BPM avec librosa
    ''' </summary>
    Private Shared Sub CreerScriptBPM()
        Dim scriptContent As String = "#!/usr/bin/env python
# -*- coding: utf-8 -*-
import sys
import librosa
import warnings

# Ignorer les avertissements
warnings.filterwarnings('ignore')

def detect_bpm(filepath):
    try:
        # Charger le fichier audio COMPLET (pas de limite de durée)
        y, sr = librosa.load(filepath, duration=None)  # None = fichier entier

        # Détecter le tempo sur tout le fichier
        tempo, beats = librosa.beat.beat_track(y=y, sr=sr)

        # Retourner le BPM avec 2 décimales
        print(f'{tempo:.2f}')
        return 0
    except Exception as e:
        print(f'ERROR: {str(e)}', file=sys.stderr)
        return 1

if __name__ == '__main__':
    if len(sys.argv) != 2:
        print('Usage: python bpm_detector.py <audio_file>', file=sys.stderr)
        sys.exit(1)

    sys.exit(detect_bpm(sys.argv[1]))
"
        File.WriteAllText(BPMScriptPath, scriptContent)
    End Sub

    ''' <summary>
    ''' Exécute un script Python et retourne la sortie
    ''' </summary>
    Public Shared Async Function ExecuterPython(arguments As String) As Task(Of String)
        Try
            Dim process As New Process()
            process.StartInfo.FileName = PythonExe
            process.StartInfo.Arguments = arguments
            process.StartInfo.WorkingDirectory = PythonDir
            process.StartInfo.UseShellExecute = False
            process.StartInfo.RedirectStandardOutput = True
            process.StartInfo.RedirectStandardError = True
            process.StartInfo.CreateNoWindow = True

            process.Start()

            Dim output = Await process.StandardOutput.ReadToEndAsync()
            Dim errorOutput = Await process.StandardError.ReadToEndAsync()

            Await Task.Run(Sub() process.WaitForExit())

            If Not String.IsNullOrEmpty(errorOutput) Then
                Debug.WriteLine($"Python stderr: {errorOutput}")
            End If

            Return output

        Catch ex As Exception
            Debug.WriteLine($"Erreur exécution Python : {ex.Message}")
            Return String.Empty
        End Try
    End Function

    ''' <summary>
    ''' Détecte le BPM d'un fichier audio avec librosa
    ''' </summary>
    Public Shared Async Function DetecterBPMAvecLibrosa(cheminFichier As String) As Task(Of Double)
        Try
            If Not EstInstalle() Then
                Return 0
            End If

            ' Échapper les guillemets dans le chemin
            Dim cheminEscaped As String = cheminFichier.Replace("""", "\""")

            ' Exécuter le script Python
            Dim arguments As String = $"""{BPMScriptPath}"" ""{cheminEscaped}"""
            Dim output = Await ExecuterPython(arguments)

            ' Parser le résultat (maintenant avec décimales)
            If Not String.IsNullOrWhiteSpace(output) Then
                Dim bpm As Double
                If Double.TryParse(output.Trim(), Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, bpm) Then
                    Return Math.Round(bpm, 2)
                End If
            End If

            Return 0

        Catch ex As Exception
            Debug.WriteLine($"Erreur détection BPM librosa : {ex.Message}")
            Return 0
        End Try
    End Function

    ''' <summary>
    ''' Désinstalle Python Embedded
    ''' </summary>
    Public Shared Function Desinstaller() As Boolean
        Try
            If Directory.Exists(PythonDir) Then
                Directory.Delete(PythonDir, True)
            End If
            Return True
        Catch ex As Exception
            Debug.WriteLine($"Erreur désinstallation Python : {ex.Message}")
            Return False
        End Try
    End Function
End Class
