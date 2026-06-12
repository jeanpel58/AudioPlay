Imports NAudio.Wave
Imports NAudio.Wave.SampleProviders

''' <summary>
''' Générateur de métronome basé sur le BPM avec sons de click
''' </summary>
Public Class MetronomeProvider
    Implements ISampleProvider

    Private ReadOnly sampleRate As Integer
    Private position As Long = 0
    Private ReadOnly bpm As Double
    Private ReadOnly nombreBeats As Integer
    Private ReadOnly samplesParBeat As Long
    Private beatActuel As Integer = 0
    Private _estTermine As Boolean = False
    Private dernierBeatNotifie As Integer = -1

    ' Événement déclenché à chaque beat
    Public Event BeatPlayed(beatNumber As Integer, totalBeats As Integer)

    Public Sub New(sampleRate As Integer, bpm As Double, nombreBeats As Integer)
        Me.sampleRate = sampleRate
        Me.bpm = bpm
        Me.nombreBeats = nombreBeats

        ' Calculer le nombre de samples par beat
        Dim secondesParBeat As Double = 60.0 / bpm
        Me.samplesParBeat = CLng(secondesParBeat * sampleRate)
    End Sub

    Public ReadOnly Property WaveFormat As WaveFormat Implements ISampleProvider.WaveFormat
        Get
            Return WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1)
        End Get
    End Property

    Public ReadOnly Property EstTermine As Boolean
        Get
            Return _estTermine
        End Get
    End Property

    Public Function Read(buffer() As Single, offset As Integer, count As Integer) As Integer Implements ISampleProvider.Read
        Dim samplesEcrits As Integer = 0

        For i As Integer = 0 To count - 1
            If beatActuel >= nombreBeats Then
                ' Tous les beats ont été joués
                _estTermine = True
                Return samplesEcrits
            End If

            ' Position dans le beat actuel
            Dim positionDansBeat As Long = position Mod samplesParBeat

            ' Notifier le début d'un nouveau beat AVANT de générer le son (pour meilleure sync)
            If positionDansBeat = 0 AndAlso beatActuel <> dernierBeatNotifie Then
                dernierBeatNotifie = beatActuel
                Try
                    RaiseEvent BeatPlayed(beatActuel + 1, nombreBeats) ' +1 pour avoir 1-based index
                Catch
                    ' Ignorer les erreurs si les handlers sont en cours de modification
                End Try
            End If

            ' Générer un click au début de chaque beat (fréquence plus haute pour le premier beat)
            Dim frequence As Double = If(beatActuel = 0, 1000, 800) ' Premier beat plus aigu
            Dim dureeBip As Integer = CInt(sampleRate * 0.05) ' 50ms de durée

            If positionDansBeat < dureeBip Then
                ' Générer une onde sinusoïdale
                Dim angle As Double = 2.0 * Math.PI * frequence * positionDansBeat / sampleRate
                Dim envelope As Single = CSng(Math.Sin(Math.PI * positionDansBeat / dureeBip)) ' Envelope pour éviter les clics
                buffer(offset + i) = CSng(Math.Sin(angle) * 0.3 * envelope)
            Else
                ' Silence entre les beats
                buffer(offset + i) = 0.0F
            End If

            position += 1
            samplesEcrits += 1

            ' Passer au beat suivant
            If positionDansBeat = samplesParBeat - 1 Then
                beatActuel += 1
            End If
        Next

        Return samplesEcrits
    End Function
End Class

''' <summary>
''' Wrapper ISampleProvider qui ignore les échantillons silencieux au début
''' Alternative à la manipulation de Position qui cause des problèmes COM
''' </summary>
Public Class SkipSilenceSampleProvider
    Implements ISampleProvider

    Private source As ISampleProvider
    Private silenceSkipped As Boolean = False
    Private ReadOnly seuilSilence As Single
    Private ReadOnly format As WaveFormat

    Public Sub New(source As ISampleProvider, Optional seuilSilence As Single = 0.01F)
        Me.source = source
        Me.seuilSilence = seuilSilence
        Me.format = source.WaveFormat
    End Sub

    Public ReadOnly Property WaveFormat As WaveFormat Implements ISampleProvider.WaveFormat
        Get
            Return format
        End Get
    End Property

    Public Function Read(buffer() As Single, offset As Integer, count As Integer) As Integer Implements ISampleProvider.Read
        Dim lu = source.Read(buffer, offset, count)

        If Not silenceSkipped AndAlso lu > 0 Then
            ' Première lecture : chercher le premier échantillon non-silencieux
            Dim debutSon As Integer = 0
            For i As Integer = 0 To lu - 1
                If Math.Abs(buffer(offset + i)) > seuilSilence Then
                    debutSon = i
                    silenceSkipped = True
                    Exit For
                End If
            Next

            ' Si on a trouvé du son, décaler le buffer pour supprimer le silence
            If silenceSkipped AndAlso debutSon > 0 Then
                Dim samplesRestants = lu - debutSon
                ' Décaler les samples vers le début
                For i As Integer = 0 To samplesRestants - 1
                    buffer(offset + i) = buffer(offset + debutSon + i)
                Next

                ' Remplir le reste du buffer avec de nouveaux samples
                If samplesRestants < count Then
                    Dim luSupplementaire = source.Read(buffer, offset + samplesRestants, count - samplesRestants)
                    Return samplesRestants + luSupplementaire
                End If

                Return samplesRestants
            ElseIf Not silenceSkipped Then
                ' Tout le buffer est silencieux, continuer à lire
                Return Read(buffer, offset, count)
            End If
        End If

        Return lu
    End Function
End Class

''' <summary>
''' Wrapper ISampleProvider qui supprime le silence à la fin de l'audio
''' Utilise un buffer de retard pour détecter et supprimer les silences finaux
''' </summary>
Public Class TrimEndSilenceSampleProvider
    Implements ISampleProvider

    Private source As ISampleProvider
    Private ReadOnly seuilSilence As Single
    Private ReadOnly format As WaveFormat
    Private ReadOnly tailleBufferRetard As Integer
    Private bufferRetard As Queue(Of Single)
    Private sourceTerminee As Boolean = False
    Private toutRetourne As Boolean = False

    Public Sub New(source As ISampleProvider, Optional seuilSilence As Single = 0.01F, Optional dureeSilenceMin As Double = 0.5)
        Me.source = source
        Me.seuilSilence = seuilSilence
        Me.format = source.WaveFormat

        ' Buffer de retard : on garde en mémoire X secondes avant de les retourner
        Me.tailleBufferRetard = CInt(format.SampleRate * format.Channels * dureeSilenceMin)
        Me.bufferRetard = New Queue(Of Single)(tailleBufferRetard)
    End Sub

    Public ReadOnly Property WaveFormat As WaveFormat Implements ISampleProvider.WaveFormat
        Get
            Return format
        End Get
    End Property

    Public Function Read(buffer() As Single, offset As Integer, count As Integer) As Integer Implements ISampleProvider.Read
        If toutRetourne Then Return 0

        Dim samplesEcrits As Integer = 0
        Dim tempBuffer(count - 1) As Single

        While samplesEcrits < count
            ' Si la source n'est pas terminée, lire de nouvelles données
            If Not sourceTerminee Then
                Dim lu = source.Read(tempBuffer, 0, Math.Min(count, tempBuffer.Length))

                If lu = 0 Then
                    ' Source terminée
                    sourceTerminee = True
                Else
                    ' Ajouter au buffer de retard
                    For i As Integer = 0 To lu - 1
                        bufferRetard.Enqueue(tempBuffer(i))

                        ' Si le buffer de retard est plein, on peut retourner les anciennes données
                        If bufferRetard.Count > tailleBufferRetard Then
                            buffer(offset + samplesEcrits) = bufferRetard.Dequeue()
                            samplesEcrits += 1

                            If samplesEcrits >= count Then
                                Exit For
                            End If
                        End If
                    Next
                End If
            End If

            ' Si la source est terminée, analyser le buffer de retard pour supprimer le silence final
            If sourceTerminee Then
                If bufferRetard.Count = 0 Then
                    toutRetourne = True
                    Exit While
                End If

                ' Chercher le dernier échantillon non-silencieux dans le buffer
                Dim bufferArray = bufferRetard.ToArray()
                Dim dernierSonIndex As Integer = -1

                For i As Integer = bufferArray.Length - 1 To 0 Step -1
                    If Math.Abs(bufferArray(i)) > seuilSilence Then
                        dernierSonIndex = i
                        Exit For
                    End If
                Next

                ' Retourner seulement jusqu'au dernier son
                If dernierSonIndex >= 0 Then
                    Dim aRetourner = Math.Min(dernierSonIndex + 1, count - samplesEcrits)
                    For i As Integer = 0 To aRetourner - 1
                        buffer(offset + samplesEcrits) = bufferArray(i)
                        samplesEcrits += 1
                    Next
                End If

                toutRetourne = True
                Exit While
            End If

            ' Si on n'a pas rempli le buffer et qu'on ne peut pas lire plus, sortir
            If Not sourceTerminee AndAlso samplesEcrits = 0 Then
                Exit While
            End If
        End While

        Return samplesEcrits
    End Function
End Class

''' <summary>
''' Utilitaires pour détecter et supprimer les silences au début d'un fichier audio
''' </summary>
Public Class SilenceDetector
    ''' <summary>
    ''' Analyse un fichier pour trouver la durée réelle (sans le silence à la fin)
    ''' </summary>
    Public Shared Function TrouverDureeReelle(cheminFichier As String, Optional seuilSilence As Single = 0.01F) As TimeSpan
        Try
            Using reader As New AudioFileReader(cheminFichier)
                ' Lire le fichier à l'envers par blocs pour trouver le dernier son
                Dim tailleBloc As Integer = reader.WaveFormat.SampleRate * reader.WaveFormat.Channels ' 1 seconde
                Dim buffer(tailleBloc - 1) As Single

                ' Commencer à 1 seconde de la fin et remonter
                Dim positionTest As Long = Math.Max(0, reader.Length - tailleBloc * 4) ' 1 seconde en bytes (float = 4 bytes)
                Dim dernierSonTrouve As Long = reader.Length

                While positionTest >= 0
                    Try
                        ' Positionner le lecteur
                        reader.Position = positionTest
                        Dim lu = reader.Read(buffer, 0, buffer.Length)

                        If lu > 0 Then
                            ' Chercher du son dans ce bloc (de la fin vers le début)
                            For i As Integer = lu - 1 To 0 Step -1
                                If Math.Abs(buffer(i)) > seuilSilence Then
                                    ' Trouvé un son ! Calculer la position exacte
                                    Dim samplePosition As Long = (positionTest \ 4) + i
                                    Dim tempsEnSecondes As Double = samplePosition / (reader.WaveFormat.SampleRate * reader.WaveFormat.Channels)
                                    Return TimeSpan.FromSeconds(tempsEnSecondes)
                                End If
                            Next
                        End If

                        ' Reculer d'un bloc
                        positionTest -= tailleBloc * 4
                    Catch
                        ' Si erreur de positionnement, reculer moins
                        positionTest -= tailleBloc * 2
                    End Try
                End While

                ' Si aucun son trouvé, retourner la durée totale
                Return reader.TotalTime
            End Using
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"Erreur analyse durée réelle {cheminFichier}: {ex.Message}")
            ' En cas d'erreur, retourner une durée invalide
            Return TimeSpan.Zero
        End Try
    End Function

    ''' <summary>
    ''' Détecte la position du premier échantillon non-silencieux
    ''' ATTENTION : Cette méthode modifie la position du reader
    ''' </summary>
    Public Shared Function TrouverDebutAudio(reader As AudioFileReader, Optional seuilSilence As Single = 0.01F) As TimeSpan
        Try
            ' Revenir au début
            reader.CurrentTime = TimeSpan.Zero

            ' Buffer pour lire par blocs d'une seconde
            Dim samplesParSeconde = reader.WaveFormat.SampleRate * reader.WaveFormat.Channels
            Dim buffer(samplesParSeconde - 1) As Single
            Dim totalSamplesLus As Long = 0

            While reader.Position < reader.Length
                Dim posAvantLecture = reader.Position
                Dim lu = reader.Read(buffer, 0, buffer.Length)
                If lu = 0 Then Exit While

                ' Chercher le premier échantillon au-dessus du seuil
                For i As Integer = 0 To lu - 1
                    If Math.Abs(buffer(i)) > seuilSilence Then
                        ' Calculer le temps correspondant
                        Dim samplePosition As Long = totalSamplesLus + i
                        Dim tempsEnSecondes As Double = samplePosition / (reader.WaveFormat.SampleRate * reader.WaveFormat.Channels)
                        Dim debutAudio = TimeSpan.FromSeconds(tempsEnSecondes)

                        ' Repositionner le reader au début du son
                        reader.CurrentTime = debutAudio
                        Return debutAudio
                    End If
                Next

                totalSamplesLus += lu
            End While

            ' Si aucun son n'est détecté, remettre au début
            reader.CurrentTime = TimeSpan.Zero
            Return TimeSpan.Zero

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"Erreur détection silence: {ex.Message}")
            ' En cas d'erreur, remettre au début
            Try
                reader.CurrentTime = TimeSpan.Zero
            Catch
            End Try
            Return TimeSpan.Zero
        End Try
    End Function

    ''' <summary>
    ''' Applique un offset pour sauter le silence au début
    ''' À appeler AVANT ToSampleProvider()
    ''' </summary>
    Public Shared Function AppliquerOffsetSilence(reader As AudioFileReader) As TimeSpan
        Try
            Dim debutAudio = TrouverDebutAudio(reader)
            If debutAudio.TotalSeconds > 0.1 Then ' Seulement si plus de 100ms de silence
                System.Diagnostics.Debug.WriteLine($"Silence détecté et supprimé : {debutAudio.TotalSeconds:F2}s")
                Return debutAudio
            Else
                ' Moins de 100ms, on garde tout
                reader.CurrentTime = TimeSpan.Zero
                Return TimeSpan.Zero
            End If
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"Erreur application offset silence: {ex.Message}")
            ' En cas d'erreur, simplement remettre au début
            Try
                reader.CurrentTime = TimeSpan.Zero
            Catch
            End Try
            Return TimeSpan.Zero
        End Try
    End Function
End Class

''' <summary>
''' Combine le métronome et l'audio principal en séquence
''' </summary>
Public Class MetronomeAudioSequencer
    Implements ISampleProvider

    Private metronome As MetronomeProvider
    Private audioProvider As ISampleProvider
    Private phaseMetronome As Boolean = True
    Private ReadOnly format As WaveFormat

    Public Sub New(metronome As MetronomeProvider, audioProvider As ISampleProvider)
        Me.metronome = metronome
        Me.audioProvider = audioProvider

        ' Utiliser le format de l'audio (peut être stéréo)
        Me.format = audioProvider.WaveFormat
    End Sub

    Public ReadOnly Property WaveFormat As WaveFormat Implements ISampleProvider.WaveFormat
        Get
            Return format
        End Get
    End Property

    Public Function Read(buffer() As Single, offset As Integer, count As Integer) As Integer Implements ISampleProvider.Read
        If phaseMetronome Then
            ' Lire depuis le métronome (mono)
            ' On doit dupliquer sur tous les canaux si l'audio est stéréo
            Dim channelCount = format.Channels

            If channelCount = 1 Then
                ' Audio mono : lecture directe du métronome
                Dim lu = metronome.Read(buffer, offset, count)

                If metronome.EstTermine Then
                    phaseMetronome = False
                    System.Diagnostics.Debug.WriteLine("Métronome terminé, passage à l'audio principal")

                    ' Si le métronome n'a pas rempli tout le buffer, continuer avec l'audio
                    If lu < count Then
                        Dim luAudio = audioProvider.Read(buffer, offset + lu, count - lu)
                        Return lu + luAudio
                    End If
                End If

                Return lu
            Else
                ' Audio stéréo : dupliquer le métronome sur chaque canal
                Dim samplesMonoBuffer(count \ channelCount - 1) As Single
                Dim luMono = metronome.Read(samplesMonoBuffer, 0, samplesMonoBuffer.Length)

                ' Dupliquer chaque sample mono sur tous les canaux
                For i As Integer = 0 To luMono - 1
                    For ch As Integer = 0 To channelCount - 1
                        buffer(offset + i * channelCount + ch) = samplesMonoBuffer(i)
                    Next
                Next

                Dim luTotal = luMono * channelCount

                If metronome.EstTermine Then
                    phaseMetronome = False
                    System.Diagnostics.Debug.WriteLine("Métronome terminé, passage à l'audio principal")

                    ' Si le métronome n'a pas rempli tout le buffer, continuer avec l'audio
                    If luTotal < count Then
                        Dim luAudio = audioProvider.Read(buffer, offset + luTotal, count - luTotal)
                        Return luTotal + luAudio
                    End If
                End If

                Return luTotal
            End If
        Else
            ' Lire depuis l'audio principal
            Return audioProvider.Read(buffer, offset, count)
        End If
    End Function
End Class
