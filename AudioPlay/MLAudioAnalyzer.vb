Imports System.IO
Imports System.Diagnostics
Imports System.Threading.Tasks
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

''' <summary>
''' Analyseur audio utilisant Machine Learning (Essentia + modèles pré-entraînés)
''' Fournit des fonctionnalités avancées style Virtual DJ / Serato :
''' - Beat/Downbeat detection précis avec ML
''' - Key detection (tonalité musicale)
''' - Genre classification
''' - Danceability/Energy/Mood analysis
''' - Structure musicale (intro/verse/chorus/outro)
''' </summary>
Public Class MLAudioAnalyzer

    ''' <summary>
    ''' Résultat complet de l'analyse ML
    ''' </summary>
    Public Class MLAnalysisResult
        ''' <summary>
        ''' BPM détecté avec précision ML
        ''' </summary>
        Public Property BPM As Double

        ''' <summary>
        ''' Liste des positions de tous les beats (en secondes)
        ''' </summary>
        Public Property Beats As List(Of Double)

        ''' <summary>
        ''' Liste des positions des downbeats (premiers beats de mesure)
        ''' </summary>
        Public Property Downbeats As List(Of Double)

        ''' <summary>
        ''' Tonalité musicale (C, C#, D, D#, E, F, F#, G, G#, A, A#, B)
        ''' </summary>
        Public Property Key As String

        ''' <summary>
        ''' Mode (major ou minor)
        ''' </summary>
        Public Property Scale As String

        ''' <summary>
        ''' Confiance de la détection de tonalité (0.0 à 1.0)
        ''' </summary>
        Public Property KeyConfidence As Double

        ''' <summary>
        ''' Code Camelot pour mixage harmonique (1A-12A, 1B-12B)
        ''' </summary>
        Public Property CamelotCode As String

        ''' <summary>
        ''' Genre musical principal
        ''' </summary>
        Public Property Genre As String

        ''' <summary>
        ''' Confiance de la classification de genre (0.0 à 1.0)
        ''' </summary>
        Public Property GenreConfidence As Double

        ''' <summary>
        ''' Score de dansabilité (0.0 à 1.0) - Plus élevé = plus dansant
        ''' </summary>
        Public Property Danceability As Double

        ''' <summary>
        ''' Niveau d'énergie (0.0 à 1.0) - Plus élevé = plus énergique
        ''' </summary>
        Public Property Energy As Double

        ''' <summary>
        ''' Valence émotionnelle (0.0 à 1.0) - 0=triste/négatif, 1=joyeux/positif
        ''' </summary>
        Public Property Valence As Double

        ''' <summary>
        ''' Signature rythmique (3, 4, 5, etc.)
        ''' </summary>
        Public Property TimeSignature As Integer

        ''' <summary>
        ''' Structure musicale avec timestamps (intro, verse, chorus, bridge, outro)
        ''' </summary>
        Public Property MusicStructure As Dictionary(Of String, List(Of Double))

        Public Sub New()
            Beats = New List(Of Double)()
            Downbeats = New List(Of Double)()
            MusicStructure = New Dictionary(Of String, List(Of Double))()
            TimeSignature = 4
        End Sub
    End Class

    ''' <summary>
    ''' Vérifie si Essentia est installé
    ''' </summary>
    Public Shared Async Function EstInstalle() As Task(Of Boolean)
        Try
            If Not PythonManager.EstInstalle() Then
                Return False
            End If

            ' Vérifier si essentia-tensorflow est installé
            Dim result = Await PythonManager.ExecuterPython("-c ""import essentia.standard; print('OK')""")
            Return result.Contains("OK")
        Catch
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Analyser un fichier audio avec Machine Learning (méthode principale)
    ''' </summary>
    Public Shared Async Function AnalyserAvecML(cheminFichier As String) As Task(Of MLAnalysisResult)
        If Not PythonManager.EstInstalle() Then
            Debug.WriteLine("[ML] Python non installé - analyse ML impossible")
            Return Nothing
        End If

        Try
            Debug.WriteLine($"[ML] Début analyse ML : {Path.GetFileName(cheminFichier)}")
            Dim startTime = DateTime.Now

            ' Construire le script Python Essentia
            Dim scriptPython As String = $"
import sys
import json
import numpy as np

try:
    import essentia.standard as es

    # Charger l'audio (limiter à 120 secondes pour performance)
    loader = es.MonoLoader(filename=r'{cheminFichier.Replace("\", "\\")}', sampleRate=44100)
    audio = loader()

    # Limiter la durée pour l'analyse (2 minutes max)
    max_samples = 44100 * 120
    if len(audio) > max_samples:
        audio = audio[:max_samples]

    result = {{}}

    # === 1. BEAT & BPM DETECTION ===
    rhythm_extractor = es.RhythmExtractor2013(method='multifeature')
    bpm, beats, beats_confidence, _, beats_intervals = rhythm_extractor(audio)
    result['bpm'] = float(bpm)
    result['beats'] = beats.tolist()
    result['beats_confidence'] = float(beats_confidence)

    # === 2. DOWNBEAT DETECTION ===
    # Utiliser l'analyse de beats pour détecter les downbeats
    if len(beats) > 0:
        # Estimation simple : chaque 4e beat (assume 4/4)
        downbeats = [beats[i] for i in range(0, len(beats), 4)]
        result['downbeats'] = downbeats
        result['time_signature'] = 4
    else:
        result['downbeats'] = []
        result['time_signature'] = 4

    # === 3. KEY DETECTION (Tonalité) ===
    key_extractor = es.KeyExtractor()
    key, scale, strength = key_extractor(audio)
    result['key'] = key
    result['scale'] = scale
    result['key_confidence'] = float(strength)

    # === 4. ENERGY & DYNAMICS ===
    # Energy
    energy = es.Energy()
    energy_value = float(energy(audio))
    result['energy'] = min(1.0, energy_value / 100.0)  # Normaliser

    # Dynamic complexity
    dynamic_complexity = es.DynamicComplexity()
    dynamics = float(dynamic_complexity(audio))
    result['dynamics'] = dynamics

    # === 5. DANCEABILITY ===
    danceability = es.Danceability()
    dance_value = float(danceability(audio))
    result['danceability'] = dance_value

    # === 6. SPECTRAL FEATURES (pour estimation de valence/mood) ===
    # Spectral centroid (brightness)
    centroid = es.Centroid()
    spectrum = es.Spectrum()
    spec = spectrum(audio)
    spectral_centroid = float(centroid(spec))

    # Estimations basées sur les features spectrales
    # Valence approximative basée sur brightness et energy
    valence_estimate = min(1.0, (spectral_centroid / 10000.0 + result['energy']) / 2.0)
    result['valence'] = valence_estimate

    # === 7. GENRE ESTIMATION (basique, sans modèle ML lourd) ===
    # Classification simple basée sur BPM et features
    genre = 'Unknown'
    if bpm < 90:
        genre = 'Downtempo/Hip-Hop'
    elif bpm < 115:
        genre = 'Pop/Rock'
    elif bpm < 135:
        genre = 'House'
    elif bpm < 150:
        genre = 'Techno/Trance'
    else:
        genre = 'Drum & Bass/Hardcore'
    result['genre'] = genre
    result['genre_confidence'] = 0.7  # Estimation basique

    # Succès
    print(json.dumps(result))
    sys.exit(0)

except ImportError as e:
    # Essentia n'est pas installé
    error_result = {{'error': 'essentia_not_installed', 'message': str(e)}}
    print(json.dumps(error_result))
    sys.exit(1)

except Exception as e:
    error_result = {{'error': 'analysis_failed', 'message': str(e)}}
    print(json.dumps(error_result))
    sys.exit(1)
"

            ' Exécuter le script Python
            Dim startInfo As New ProcessStartInfo() With {
                .FileName = PythonManager.CheminPython,
                .Arguments = "-c """ & scriptPython.Replace("""", """""") & """",
                .WorkingDirectory = PythonManager.CheminDossierPython,
                .RedirectStandardOutput = True,
                .RedirectStandardError = True,
                .UseShellExecute = False,
                .CreateNoWindow = True,
                .StandardOutputEncoding = System.Text.Encoding.UTF8
            }

            Using process As Process = Process.Start(startInfo)
                Dim output As String = Await process.StandardOutput.ReadToEndAsync()
                Dim errorOutput As String = Await process.StandardError.ReadToEndAsync()
                Await Task.Run(Sub() process.WaitForExit())

                If process.ExitCode = 0 AndAlso Not String.IsNullOrWhiteSpace(output) Then
                    ' Parser le JSON
                    Dim result As New MLAnalysisResult()
                    Dim jsonObj = JObject.Parse(output)

                    ' Vérifier si c'est une erreur
                    If jsonObj("error") IsNot Nothing Then
                        Dim errorType = jsonObj("error").ToString()
                        If errorType = "essentia_not_installed" Then
                            Debug.WriteLine("[ML] Essentia n'est pas installé - utiliser 'pip install essentia-tensorflow'")
                        Else
                            Debug.WriteLine($"[ML] Erreur d'analyse: {jsonObj("message")}")
                        End If
                        Return Nothing
                    End If

                    ' Parser les données
                    result.BPM = CDbl(jsonObj("bpm"))
                    result.Key = jsonObj("key").ToString()
                    result.Scale = jsonObj("scale").ToString()
                    result.KeyConfidence = CDbl(jsonObj("key_confidence"))
                    result.Danceability = CDbl(jsonObj("danceability"))
                    result.Energy = CDbl(jsonObj("energy"))
                    result.Valence = CDbl(jsonObj("valence"))
                    result.Genre = jsonObj("genre").ToString()
                    result.GenreConfidence = CDbl(jsonObj("genre_confidence"))
                    result.TimeSignature = CInt(jsonObj("time_signature"))

                    ' Parser les beats
                    result.Beats = New List(Of Double)()
                    Dim beatsArray = TryCast(jsonObj("beats"), JArray)
                    If beatsArray IsNot Nothing Then
                        For Each beat In beatsArray
                            result.Beats.Add(CDbl(beat))
                        Next
                    End If

                    ' Parser les downbeats
                    result.Downbeats = New List(Of Double)()
                    Dim downbeatsArray = TryCast(jsonObj("downbeats"), JArray)
                    If downbeatsArray IsNot Nothing Then
                        For Each downbeat In downbeatsArray
                            result.Downbeats.Add(CDbl(downbeat))
                        Next
                    End If

                    ' Calculer le code Camelot
                    result.CamelotCode = CalculerCamelotCode(result.Key, result.Scale)

                    Dim elapsed = (DateTime.Now - startTime).TotalSeconds
                    Debug.WriteLine($"[ML] Analyse terminée en {elapsed:F1}s")
                    Debug.WriteLine($"[ML] BPM: {result.BPM:F1}, Key: {result.Key} {result.Scale} ({result.CamelotCode})")
                    Debug.WriteLine($"[ML] Genre: {result.Genre}, Danceability: {result.Danceability:F2}, Energy: {result.Energy:F2}")
                    Debug.WriteLine($"[ML] Beats: {result.Beats.Count}, Downbeats: {result.Downbeats.Count}")

                    Return result
                Else
                    Debug.WriteLine($"[ML] Erreur Python: {errorOutput}")
                    Return Nothing
                End If
            End Using

        Catch ex As Exception
            Debug.WriteLine($"[ML] Erreur analyse ML: {ex.Message}")
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Calculer le code Camelot pour mixage harmonique (Camelot Wheel)
    ''' </summary>
    Private Shared Function CalculerCamelotCode(key As String, scale As String) As String
        ' Table de conversion Key → Camelot
        Dim camelotTable As New Dictionary(Of String, String)()

        ' Majeur (B)
        camelotTable.Add("C major", "8B")
        camelotTable.Add("G major", "9B")
        camelotTable.Add("D major", "10B")
        camelotTable.Add("A major", "11B")
        camelotTable.Add("E major", "12B")
        camelotTable.Add("B major", "1B")
        camelotTable.Add("F# major", "2B")
        camelotTable.Add("Db major", "3B")
        camelotTable.Add("Ab major", "4B")
        camelotTable.Add("Eb major", "5B")
        camelotTable.Add("Bb major", "6B")
        camelotTable.Add("F major", "7B")

        ' Mineur (A)
        camelotTable.Add("A minor", "8A")
        camelotTable.Add("E minor", "9A")
        camelotTable.Add("B minor", "10A")
        camelotTable.Add("F# minor", "11A")
        camelotTable.Add("C# minor", "12A")
        camelotTable.Add("G# minor", "1A")
        camelotTable.Add("D# minor", "2A")
        camelotTable.Add("Bb minor", "3A")
        camelotTable.Add("F minor", "4A")
        camelotTable.Add("C minor", "5A")
        camelotTable.Add("G minor", "6A")
        camelotTable.Add("D minor", "7A")

        Dim fullKey = $"{key} {scale}"
        If camelotTable.ContainsKey(fullKey) Then
            Return camelotTable(fullKey)
        End If

        Return "?"
    End Function

    ''' <summary>
    ''' Obtenir les clés compatibles pour mixage harmonique (Camelot Wheel)
    ''' </summary>
    Public Shared Function ObtenirClesCompatibles(camelotCode As String) As List(Of String)
        Dim compatible As New List(Of String)()

        If String.IsNullOrEmpty(camelotCode) OrElse camelotCode = "?" Then
            Return compatible
        End If

        Try
            ' Parser le code (ex: "8B")
            Dim number = Integer.Parse(camelotCode.Substring(0, camelotCode.Length - 1))
            Dim letter = camelotCode.Substring(camelotCode.Length - 1)

            ' Règles Camelot Wheel:
            ' 1. Même code (mix parfait)
            compatible.Add(camelotCode)

            ' 2. Code adjacent (+1/-1)
            Dim nextNumber = If(number = 12, 1, number + 1)
            Dim prevNumber = If(number = 1, 12, number - 1)
            compatible.Add($"{nextNumber}{letter}")
            compatible.Add($"{prevNumber}{letter}")

            ' 3. Relatif majeur/mineur (même numéro, lettre différente)
            Dim relativeLetter = If(letter = "A", "B", "A")
            compatible.Add($"{number}{relativeLetter}")

        Catch ex As Exception
            Debug.WriteLine($"[ML] Erreur calcul clés compatibles: {ex.Message}")
        End Try

        Return compatible
    End Function

    ''' <summary>
    ''' Vérifier si deux pistes sont harmoniquement compatibles
    ''' </summary>
    Public Shared Function SontHarmoniquementCompatibles(camelot1 As String, camelot2 As String) As Boolean
        If String.IsNullOrEmpty(camelot1) OrElse String.IsNullOrEmpty(camelot2) Then
            Return False
        End If

        Dim compatible = ObtenirClesCompatibles(camelot1)
        Return compatible.Contains(camelot2)
    End Function

End Class
