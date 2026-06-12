# Script de traduction Resources.es.resx (Allemand → Espagnol)
# Ce script remplace les traductions allemandes par des traductions espagnoles

$resxPath = "AudioPlay\Resources.es.resx"
Write-Host "Lecture de $resxPath..."
$content = Get-Content $resxPath -Raw -Encoding UTF8

# Dictionnaire Allemand → Espagnol (traductions courantes)
$translations = @{
	# Interface générale
	'Über...' = 'Acerca de...'
	'Willkommen bei AudioPlay!' = '¡Bienvenido a AudioPlay!'
	'Einstellungen' = 'Configuración'
	'Abspielen' = 'Reproducir'
	'Pause' = 'Pausa'
	'Fortsetzen' = 'Reanudar'
	'Stoppen' = 'Detener'
	'Lautstärke' = 'Volumen'
	'Bass' = 'Bajos'
	'Höhen' = 'Agudos'
	'BPM berechnen' = 'Calcular BPM'
	'Titeldetails' = 'Detalles de la pista'
	'Fehler' = 'Error'
	'Warnung' = 'Advertencia'
	'Bestätigung' = 'Confirmación'
	'Erfolg' = 'Éxito'
	'Ja' = 'Sí'
	'Nein' = 'No'
	'Abbrechen' = 'Cancelar'
	'Schließen' = 'Cerrar'
	'Öffnen' = 'Abrir'
	'Speichern' = 'Guardar'
	'Laden' = 'Cargar'
	'Löschen' = 'Eliminar'
	'Entfernen' = 'Eliminar'
	'Hinzufügen' = 'Añadir'
	'Bearbeiten' = 'Editar'
	'Hilfe' = 'Ayuda'
	'Datei' = 'Archivo'
	'Ansicht' = 'Ver'
	'Extras' = 'Herramientas'
	'Beenden' = 'Salir'
	'Neu' = 'Nuevo'
	'Suchen' = 'Buscar'
	'Wiedergabe' = 'Reproducción'
	'Playlist' = 'Lista de reproducción'
	'Titel' = 'Pista'
	'Künstler' = 'Artista'
	'Album' = 'Álbum'
	'Genre' = 'Género'
	'Jahr' = 'Año'
	'Dauer' = 'Duración'
	'Pfad' = 'Ruta'
	'Größe' = 'Tamaño'
	'Format' = 'Formato'
	'Bitrate:' = 'Tasa de bits:'
	'Abtastrate:' = 'Frecuencia de muestreo:'
	'Verbleibende Zeit' = 'Tiempo restante'
	'Thema' = 'Tema'
	'Sprache' = 'Idioma'
	'Ordner' = 'Carpeta'
	'Zurück' = 'Atrás'
	'Weiter' = 'Siguiente'
	'Anwenden' = 'Aplicar'
	'Standardwerte' = 'Valores predeterminados'
	'Alle' = 'Todos'
	'Keine' = 'Ninguno'
	'Auswählen' = 'Seleccionar'
	'Sortieren' = 'Ordenar'
	'Filter' = 'Filtro'
	'Exportieren' = 'Exportar'
	'Importieren' = 'Importar'

	# Mode DJ (les valeurs allemandes récemment ajoutées)
	'Hilfe - DJ-Modus' = 'Ayuda - Modo DJ'
	'Tastaturkürzel' = 'Atajos de teclado'
	'Fehler beim Laden von Deck' = 'Error al cargar Plato'
	'Cue-Punkt.*gesetzt auf' = 'Punto Cue {0} establecido en'
	'Playlist erfolgreich geladen!' = '¡Playlist cargada con éxito!'
	'Playlist erfolgreich gespeichert!' = '¡Playlist guardada con éxito!'
	'Fehler beim Laden:' = 'Error al cargar:'
	'Fehler beim Speichern:' = 'Error al guardar:'
	'Möchten Sie die Liste wirklich leeren\?' = '¿Realmente desea vaciar la lista?'

	# Messages communs allemands
	'Browser konnte nicht geöffnet werden\.' = 'No se pudo abrir el navegador.'
	'AudioPlay ist ein fortschrittlicher Audioplayer' = 'AudioPlay es un reproductor de audio avanzado'
	'Sie können Ihre Spende' = 'También puede enviar su donación'
	'Wenn Sie AudioPlay regelmäßig verwenden' = 'Si utiliza AudioPlay regularmente'
	'SPENDE' = 'DONACIÓN'
}

Write-Host "Application de $($translations.Count) traductions..."

$count = 0
foreach ($de in $translations.Keys) {
	$es = $translations[$de]
	$pattern = [regex]::Escape("<value>$de</value>")
	if ($content -match $pattern) {
		$content = $content -replace $pattern, "<value>$es</value>"
		$count++
	}
}

Write-Host "$count traductions appliquées."

# Sauvegarder
[System.IO.File]::WriteAllText($resxPath, $content, [System.Text.Encoding]::UTF8)
Write-Host "Fichier sauvegardé: $resxPath"
Write-Host ""
Write-Host "ATTENTION: Ce script ne traduit que les valeurs les plus courantes."
Write-Host "Pour une traduction complète, il faut restaurer le fichier original espagnol"
Write-Host "depuis un backup ou le système de contrôle de version (Git)."
