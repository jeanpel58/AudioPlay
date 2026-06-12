Imports System.IO
Imports System.Drawing

''' <summary>
''' Lecteur de fichiers CDG (CD+Graphics) pour karaoke
''' Format CDG : 24 octets de données + 72 octets de sous-code par paquet
''' Synchronisé à 300 paquets par seconde (4 paquets par secteur CD, 75 secteurs/sec)
''' </summary>
Public Class CDGReader
    ' Constantes du format CDG
    Private Const CDG_COMMAND As Integer = &H9        ' Masque pour identifier une commande CDG
    Private Const CDG_MASK As Integer = &H3F          ' Masque pour extraire les 6 bits utiles
    Private Const CDG_PACKET_SIZE As Integer = 24     ' Taille d'un sous-paquet CDG

    ' Dimensions de l'écran CDG
    Private Const CDG_FULL_WIDTH As Integer = 300
    Private Const CDG_FULL_HEIGHT As Integer = 216
    Private Const CDG_DISPLAY_WIDTH As Integer = 294   ' Zone visible (300 - 2*3)
    Private Const CDG_DISPLAY_HEIGHT As Integer = 204  ' Zone visible (216 - 2*6)
    Private Const CDG_BORDER_WIDTH As Integer = 6
    Private Const CDG_BORDER_HEIGHT As Integer = 12

    ' Commandes CDG
    Private Const CDG_MEMORY_PRESET As Integer = 1
    Private Const CDG_BORDER_PRESET As Integer = 2
    Private Const CDG_TILE_BLOCK As Integer = 6
    Private Const CDG_SCROLL_PRESET As Integer = 20
    Private Const CDG_SCROLL_COPY As Integer = 24
    Private Const CDG_DEFINE_TRANSPARENT As Integer = 28
    Private Const CDG_LOAD_CLUT_LOW As Integer = 30   ' Color Look-Up Table
    Private Const CDG_LOAD_CLUT_HIGH As Integer = 31
    Private Const CDG_TILE_BLOCK_XOR As Integer = 38

    ' État du lecteur
    Private cdgData As Byte()
    Private totalPackets As Integer
    Private lastRenderedPacket As Integer = -1

    ' Buffers d'affichage
    Private pixelBuffer(CDG_FULL_WIDTH - 1, CDG_FULL_HEIGHT - 1) As Byte
    Private colorTable(15) As Color

    ''' <summary>
    ''' Charge un fichier CDG
    ''' </summary>
    Public Function LoadCDGFile(filePath As String) As Boolean
        Try
            If Not File.Exists(filePath) Then
                System.Diagnostics.Debug.WriteLine($"CDG: Fichier introuvable: {filePath}")
                Return False
            End If

            cdgData = File.ReadAllBytes(filePath)
            totalPackets = cdgData.Length \ CDG_PACKET_SIZE

            System.Diagnostics.Debug.WriteLine($"CDG: Chargé {totalPackets} paquets ({cdgData.Length} octets)")

            ' Réinitialiser l'état
            Reset()

            Return True
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"CDG: Erreur chargement: {ex.Message}")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Réinitialise le lecteur
    ''' </summary>
    Public Sub Reset()
        ' Initialiser la palette par défaut (noir)
        For i As Integer = 0 To 15
            colorTable(i) = Color.Black
        Next

        ' Initialiser le buffer de pixels (tout à 0 = noir)
        For x As Integer = 0 To CDG_FULL_WIDTH - 1
            For y As Integer = 0 To CDG_FULL_HEIGHT - 1
                pixelBuffer(x, y) = 0
            Next
        Next

        lastRenderedPacket = -1
    End Sub

    ''' <summary>
    ''' Rend l'image CDG à une position temporelle donnée
    ''' </summary>
    Public Function RenderAtTime(timeInSeconds As Double) As Bitmap
        If cdgData Is Nothing OrElse totalPackets = 0 Then
            Return CreateBlackBitmap()
        End If

        ' Calculer le paquet cible (300 paquets/seconde)
        Dim targetPacket As Integer = CInt(timeInSeconds * 300.0)

        ' Limiter au nombre de paquets disponibles
        If targetPacket >= totalPackets Then
            targetPacket = totalPackets - 1
        End If

        If targetPacket < 0 Then
            targetPacket = 0
        End If

        ' Si on recule dans le temps, réinitialiser
        If targetPacket < lastRenderedPacket Then
            Reset()
        End If

        ' Traiter les paquets jusqu'à la position cible
        For packetIndex As Integer = lastRenderedPacket + 1 To targetPacket
            If packetIndex >= 0 AndAlso packetIndex < totalPackets Then
                ProcessPacket(packetIndex)
            End If
        Next

        lastRenderedPacket = targetPacket

        ' Créer et retourner le bitmap
        Return CreateBitmap()
    End Function

    ''' <summary>
    ''' Traite un paquet CDG
    ''' </summary>
    Private Sub ProcessPacket(packetIndex As Integer)
        Dim offset As Integer = packetIndex * CDG_PACKET_SIZE

        ' Vérifier que le paquet est valide
        If offset + CDG_PACKET_SIZE > cdgData.Length Then
            Return
        End If

        Dim command As Byte = cdgData(offset) And CDG_MASK
        Dim instruction As Byte = cdgData(offset + 1) And CDG_MASK

        ' Seules les commandes CDG_COMMAND (&H9) sont valides
        If command <> CDG_COMMAND Then
            Return
        End If

        ' Traiter l'instruction
        Select Case instruction
            Case CDG_MEMORY_PRESET
                ProcessMemoryPreset(offset)
            Case CDG_BORDER_PRESET
                ProcessBorderPreset(offset)
            Case CDG_TILE_BLOCK
                ProcessTileBlock(offset, False)
            Case CDG_TILE_BLOCK_XOR
                ProcessTileBlock(offset, True)
            Case CDG_LOAD_CLUT_LOW
                ProcessLoadCLUT(offset, 0)
            Case CDG_LOAD_CLUT_HIGH
                ProcessLoadCLUT(offset, 8)
            Case CDG_SCROLL_PRESET
                ProcessScroll(offset, False)
            Case CDG_SCROLL_COPY
                ProcessScroll(offset, True)
        End Select
    End Sub

    ''' <summary>
    ''' Memory Preset : efface l'écran avec une couleur
    ''' </summary>
    Private Sub ProcessMemoryPreset(offset As Integer)
        Dim color As Byte = cdgData(offset + 4) And &HF
        Dim repeat As Byte = cdgData(offset + 5) And &HF

        ' Remplir tout l'écran
        For y As Integer = 0 To CDG_FULL_HEIGHT - 1
            For x As Integer = 0 To CDG_FULL_WIDTH - 1
                pixelBuffer(x, y) = color
            Next
        Next
    End Sub

    ''' <summary>
    ''' Border Preset : définit la couleur de la bordure
    ''' </summary>
    Private Sub ProcessBorderPreset(offset As Integer)
        Dim color As Byte = cdgData(offset + 4) And &HF

        ' Bordures horizontales (haut et bas)
        For y As Integer = 0 To CDG_BORDER_HEIGHT - 1
            For x As Integer = 0 To CDG_FULL_WIDTH - 1
                pixelBuffer(x, y) = color
                pixelBuffer(x, CDG_FULL_HEIGHT - y - 1) = color
            Next
        Next

        ' Bordures verticales (gauche et droite)
        For y As Integer = 0 To CDG_FULL_HEIGHT - 1
            For x As Integer = 0 To CDG_BORDER_WIDTH - 1
                pixelBuffer(x, y) = color
                pixelBuffer(CDG_FULL_WIDTH - x - 1, y) = color
            Next
        Next
    End Sub

    ''' <summary>
    ''' Tile Block : dessine un bloc 6×12 pixels
    ''' </summary>
    Private Sub ProcessTileBlock(offset As Integer, xorMode As Boolean)
        Dim color0 As Byte = cdgData(offset + 4) And &HF
        Dim color1 As Byte = cdgData(offset + 5) And &HF
        Dim row As Integer = (cdgData(offset + 6) And &H1F) * 12
        Dim column As Integer = (cdgData(offset + 7) And &H3F) * 6

        ' Lire les 12 lignes de pixels (6 bits par ligne)
        For y As Integer = 0 To 11
            Dim rowByte As Byte = cdgData(offset + 8 + y) And &H3F

            For x As Integer = 0 To 5
                Dim bitSet As Boolean = ((rowByte >> (5 - x)) And 1) = 1
                Dim pixelColor As Byte = If(bitSet, color1, color0)

                Dim px As Integer = column + x
                Dim py As Integer = row + y

                ' Vérifier les limites
                If px >= 0 AndAlso px < CDG_FULL_WIDTH AndAlso py >= 0 AndAlso py < CDG_FULL_HEIGHT Then
                    If xorMode Then
                        pixelBuffer(px, py) = pixelBuffer(px, py) Xor pixelColor
                    Else
                        pixelBuffer(px, py) = pixelColor
                    End If
                End If
            Next
        Next
    End Sub

    ''' <summary>
    ''' Load CLUT : charge la palette de couleurs
    ''' </summary>
    Private Sub ProcessLoadCLUT(offset As Integer, colorOffset As Integer)
        ' Charger 8 couleurs (2 octets par couleur)
        For i As Integer = 0 To 7
            Dim high As Byte = cdgData(offset + 4 + (i * 2)) And &H3F
            Dim low As Byte = cdgData(offset + 5 + (i * 2)) And &H3F

            ' Combiner en un mot de 12 bits (4 bits RGB)
            Dim colorValue As Integer = (high << 6) Or low

            ' Extraire les composantes RGB (4 bits chacune)
            Dim r As Integer = ((colorValue >> 8) And &HF) * 17  ' 0-15 -> 0-255
            Dim g As Integer = ((colorValue >> 4) And &HF) * 17
            Dim b As Integer = (colorValue And &HF) * 17

            colorTable(colorOffset + i) = Color.FromArgb(r, g, b)
        Next
    End Sub

    ''' <summary>
    ''' Scroll : fait défiler l'écran
    ''' </summary>
    Private Sub ProcessScroll(offset As Integer, copy As Boolean)
        ' Pour l'instant, ignorer le défilement (rarement utilisé et complexe)
        ' Une implémentation complète nécessiterait de gérer les 4 directions
    End Sub

    ''' <summary>
    ''' Crée un bitmap noir
    ''' </summary>
    Private Function CreateBlackBitmap() As Bitmap
        Dim bmp As New Bitmap(CDG_DISPLAY_WIDTH, CDG_DISPLAY_HEIGHT)
        Using g As Graphics = Graphics.FromImage(bmp)
            g.Clear(Color.Black)
        End Using
        Return bmp
    End Function

    ''' <summary>
    ''' Crée le bitmap à partir du buffer de pixels
    ''' </summary>
    Private Function CreateBitmap() As Bitmap
        Dim bmp As New Bitmap(CDG_DISPLAY_WIDTH, CDG_DISPLAY_HEIGHT)

        Try
            ' Dessiner uniquement la zone d'affichage (sans les bordures)
            For y As Integer = 0 To CDG_DISPLAY_HEIGHT - 1
                For x As Integer = 0 To CDG_DISPLAY_WIDTH - 1
                    ' Ajouter l'offset de bordure
                    Dim sourceX As Integer = x + 3
                    Dim sourceY As Integer = y + 6

                    If sourceX < CDG_FULL_WIDTH AndAlso sourceY < CDG_FULL_HEIGHT Then
                        Dim colorIndex As Byte = pixelBuffer(sourceX, sourceY) And &HF
                        bmp.SetPixel(x, y, colorTable(colorIndex))
                    End If
                Next
            Next
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"CDG: Erreur création bitmap: {ex.Message}")
        End Try

        Return bmp
    End Function
End Class
