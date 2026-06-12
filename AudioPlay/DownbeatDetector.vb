Imports System.IO
Imports System.Threading.Tasks

''' <summary>
''' Classe pour détecter les downbeats (premiers beats de chaque mesure) dans une piste audio
''' Utilise librosa pour une détection précise style Virtual DJ / Serato
''' </summary>
Public Class DownbeatDetector

    ''' <summary>
    ''' Résultat de la détection de downbeats
    ''' </summary>
    Public Class DownbeatResult
        ''' <summary>
        ''' BPM détecté
        ''' </summary>
        Public Property BPM As Double

        ''' <summary>
        ''' Liste des positions de tous les beats (en secondes)
        ''' </summary>
        Public Property Beats As List(Of Double)

        ''' <summary>
        ''' Liste des positions des downbeats seulement (premiers beats de chaque mesure)
        ''' </summary>
        Public Property Downbeats As List(Of Double)

        ''' <summary>
        ''' Signature rythmique (4 = 4/4, 3 = 3/4, etc.)
        ''' </summary>
        Public Property TimeSignature As Integer

        ''' <summary>
        ''' Confiance de la détection (0.0 à 1.0)
        ''' </summary>
        Public Property Confidence As Double

        Public Sub New()
            Beats = New List(Of Double)()
            Downbeats = New List(Of Double)()
            TimeSignature = 4 ' Par défaut 4/4
            Confidence = 0.0
        End Sub
    End Class

    ''' <summary>
    ''' Détecter les downbeats avec Librosa (méthode principale)
    ''' </summary>
    Public Shared Async Function DetecterDownbeats(cheminFichier As String) As Task(Of DownbeatResult)
        ' Vérifier si Python/Librosa est installé
        If Not PythonManager.EstInstalle() Then
            ' Fallback : utiliser détection basique sans downbeats
            Return Await DetecterDownbeatsBasique(cheminFichier)
        End If

        Try
            ' Détecter avec Librosa
            Dim result = Await DetecterDownbeatsAvecLibrosa(cheminFichier)

            If result IsNot Nothing AndAlso result.BPM > 0 Then
                Return result
            End If

        Catch ex As Exception
            Debug.WriteLine($"Erreur détection downbeats Librosa: {ex.Message}")
        End Try

        ' Fallback si Librosa échoue
        Return Await DetecterDownbeatsBasique(cheminFichier)
    End Function

    ''' <summary>
    ''' Détection downbeats avec Librosa (précis, style Virtual DJ / Serato)
    ''' </summary>
    Private Shared Async Function DetecterDownbeatsAvecLibrosa(cheminFichier As String) As Task(Of DownbeatResult)
        Return Await Task.Run(Function()
            Try
                ' Créer le script Python pour la détection avancée
                Dim scriptPython As String = $"
import sys
import json
import librosa
import numpy as np

# Charger le fichier audio
y, sr = librosa.load(r'{cheminFichier}', sr=None)

# === ÉTAPE 1 : DÉTECTION DU BPM ===
tempo, beat_frames = librosa.beat.beat_track(y=y, sr=sr)

# Convertir les frames en secondes
beat_times = librosa.frames_to_time(beat_frames, sr=sr)

# === ÉTAPE 2 : DÉTECTION DES DOWNBEATS ===
# Utiliser l'enveloppe d'onset pour détecter les beats forts
onset_env = librosa.onset.onset_strength(y=y, sr=sr)

# Calculer le tempogram (représentation temps-fréquence du tempo)
tempogram = librosa.feature.tempogram(onset_envelope=onset_env, sr=sr)

# Détecter les downbeats avec autocorrélation locale
# On cherche les pics qui se répètent selon la signature rythmique (4/4)
hop_length = 512
ac_global = librosa.autocorrelate(onset_env, max_size=tempogram.shape[0])
ac_global = librosa.util.normalize(ac_global)

# Trouver le pic dominant pour déterminer la signature rythmique
beats_per_bar = 4  # Par défaut 4/4

# Calculer la périodicité des beats
if len(beat_times) > 8:
    # Analyser l'énergie de chaque beat
    beat_energies = []
    for beat_time in beat_times:
        # Extraire un segment autour du beat
        start_sample = max(0, int((beat_time - 0.05) * sr))
        end_sample = min(len(y), int((beat_time + 0.05) * sr))
        segment = y[start_sample:end_sample]

        # Calculer l'énergie RMS (Root Mean Square)
        energy = np.sqrt(np.mean(segment**2))
        beat_energies.append(energy)

    # Normaliser les énergies
    beat_energies = np.array(beat_energies)
    if np.max(beat_energies) > 0:
        beat_energies = beat_energies / np.max(beat_energies)

    # Les downbeats sont généralement les beats avec l'énergie la plus forte
    # On cherche un pattern répétitif tous les N beats

    # Essayer de détecter la signature rythmique (3/4, 4/4, 5/4, etc.)
    best_confidence = 0
    best_signature = 4

    for signature in [3, 4, 5, 6]:
        # Calculer la corrélation entre le pattern d'énergie et la signature
        if len(beat_energies) >= signature * 2:
            # Regrouper les beats par mesures
            num_bars = len(beat_energies) // signature
            reshaped = beat_energies[:num_bars * signature].reshape(num_bars, signature)

            # Calculer la moyenne d'énergie par position dans la mesure
            avg_energy_per_position = np.mean(reshaped, axis=0)

            # Le downbeat devrait avoir l'énergie la plus forte (position 0)
            if len(avg_energy_per_position) > 0:
                confidence = avg_energy_per_position[0] / np.mean(avg_energy_per_position)

                if confidence > best_confidence:
                    best_confidence = confidence
                    best_signature = signature

    beats_per_bar = best_signature

    # Identifier les downbeats (premier beat de chaque mesure)
    downbeat_indices = list(range(0, len(beat_times), beats_per_bar))
    downbeat_times = beat_times[downbeat_indices]
else:
    # Pas assez de beats, prendre tous les beats comme downbeats
    downbeat_times = beat_times

# === ÉTAPE 3 : CALCUL DE LA CONFIANCE ===
confidence = 0.8  # Confiance de base
if len(beat_times) > 8 and best_confidence > 1.2:
    confidence = min(0.95, 0.5 + (best_confidence - 1.0) * 0.3)

# === RÉSULTAT JSON ===
result = {{
    'bpm': float(tempo),
    'beats': beat_times.tolist(),
    'downbeats': downbeat_times.tolist(),
    'time_signature': int(beats_per_bar),
    'confidence': float(confidence)
}}

print(json.dumps(result))
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
                    Dim output As String = process.StandardOutput.ReadToEnd()
                    Dim errorOutput As String = process.StandardError.ReadToEnd()
                    process.WaitForExit()

                    If process.ExitCode = 0 AndAlso Not String.IsNullOrWhiteSpace(output) Then
                        ' Parser le JSON
                        Dim result As New DownbeatResult()

                        ' Parse manuel du JSON (simple)
                        output = output.Trim()

                        ' Extraire BPM
                        Dim bpmMatch = System.Text.RegularExpressions.Regex.Match(output, """bpm"":\s*([0-9.]+)")
                        If bpmMatch.Success Then
                            result.BPM = Double.Parse(bpmMatch.Groups(1).Value, System.Globalization.CultureInfo.InvariantCulture)
                        End If

                        ' Extraire beats
                        Dim beatsMatch = System.Text.RegularExpressions.Regex.Match(output, """beats"":\s*\[([\d.,\s]+)\]")
                        If beatsMatch.Success Then
                            Dim beatsList = beatsMatch.Groups(1).Value.Split(","c)
                            For Each beatStr In beatsList
                                Dim beatVal As Double
                                If Double.TryParse(beatStr.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, beatVal) Then
                                    result.Beats.Add(beatVal)
                                End If
                            Next
                        End If

                        ' Extraire downbeats
                        Dim downbeatsMatch = System.Text.RegularExpressions.Regex.Match(output, """downbeats"":\s*\[([\d.,\s]+)\]")
                        If downbeatsMatch.Success Then
                            Dim downbeatsList = downbeatsMatch.Groups(1).Value.Split(","c)
                            For Each downbeatStr In downbeatsList
                                Dim downbeatVal As Double
                                If Double.TryParse(downbeatStr.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, downbeatVal) Then
                                    result.Downbeats.Add(downbeatVal)
                                End If
                            Next
                        End If

                        ' Extraire signature rythmique
                        Dim signatureMatch = System.Text.RegularExpressions.Regex.Match(output, """time_signature"":\s*([0-9]+)")
                        If signatureMatch.Success Then
                            result.TimeSignature = Integer.Parse(signatureMatch.Groups(1).Value)
                        End If

                        ' Extraire confiance
                        Dim confidenceMatch = System.Text.RegularExpressions.Regex.Match(output, """confidence"":\s*([0-9.]+)")
                        If confidenceMatch.Success Then
                            result.Confidence = Double.Parse(confidenceMatch.Groups(1).Value, System.Globalization.CultureInfo.InvariantCulture)
                        End If

                        Debug.WriteLine($"[Downbeat] Détecté: BPM={result.BPM:F2}, Signature={result.TimeSignature}/4, Beats={result.Beats.Count}, Downbeats={result.Downbeats.Count}, Confiance={result.Confidence:F2}")

                        Return result
                    Else
                        Debug.WriteLine($"Erreur Python downbeat: {errorOutput}")
                        Return Nothing
                    End If
                End Using

            Catch ex As Exception
                Debug.WriteLine($"Erreur détection downbeats Librosa: {ex.Message}")
                Return Nothing
            End Try
        End Function)
    End Function

    ''' <summary>
    ''' Détection basique sans downbeats (fallback si Librosa indisponible)
    ''' </summary>
    Private Shared Async Function DetecterDownbeatsBasique(cheminFichier As String) As Task(Of DownbeatResult)
        Dim result As New DownbeatResult()

        ' Détecter le BPM basique
        Dim bpm = Await BPMDetector.DetecterBPM(cheminFichier)
        result.BPM = bpm

        If bpm > 0 Then
            ' Générer une grille de beats basique (tous les beats sont considérés comme downbeats)
            ' C'est moins précis mais fonctionnel
            Using reader As New NAudio.Wave.AudioFileReader(cheminFichier)
                Dim dureeTotale = reader.TotalTime.TotalSeconds
                Dim beatDuration = 60.0 / bpm

                Dim position As Double = 0
                While position < dureeTotale
                    result.Beats.Add(position)
                    result.Downbeats.Add(position) ' Tous les beats = downbeats (approximation)
                    position += beatDuration
                End While
            End Using

            result.TimeSignature = 4 ' Supposer 4/4 par défaut
            result.Confidence = 0.5 ' Confiance moyenne (pas de vraie détection)
        End If

        Return result
    End Function

End Class
