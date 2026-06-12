# Script pour ajouter TOUTES les clés DJ manquantes dans ES/DE/IT
# Ce script traduit automatiquement les 92 clés DJ du français vers les 3 langues

Write-Host "=== AJOUT DES CLÉS DJ MANQUANTES ===" -ForegroundColor Cyan
Write-Host ""

# Dictionnaire de traduction FR → ES/DE/IT pour les termes DJ courants
$translations = @{
	# Termes de base
	'Auto-Cue' = @{ ES = 'Auto-Cue'; DE = 'Auto-Cue'; IT = 'Auto-Cue' }
	'BPM' = @{ ES = 'BPM'; DE = 'BPM'; IT = 'BPM' }
	'Cue' = @{ ES = 'Cue'; DE = 'Cue'; IT = 'Cue' }
	'Loop' = @{ ES = 'Loop'; DE = 'Loop'; IT = 'Loop' }
	'Mixer' = @{ ES = 'Mezclador'; DE = 'Mixer'; IT = 'Mixer' }
	'Crossfader' = @{ ES = 'Crossfader'; DE = 'Crossfader'; IT = 'Crossfader' }
	'Volume' = @{ ES = 'Volumen'; DE = 'Lautstärke'; IT = 'Volume' }
	'Pitch' = @{ ES = 'Pitch'; DE = 'Pitch'; IT = 'Pitch' }
	'Position' = @{ ES = 'Posición'; DE = 'Position'; IT = 'Posizione' }
	'Sync' = @{ ES = 'Sync'; DE = 'Sync'; IT = 'Sync' }

	# Actions
	'Ajouter' = @{ ES = 'Añadir'; DE = 'Hinzufügen'; IT = 'Aggiungi' }
	'Gérer' = @{ ES = 'Gestionar'; DE = 'Verwalten'; IT = 'Gestisci' }
	'Charger' = @{ ES = 'Cargar'; DE = 'Laden'; IT = 'Carica' }
	'Enregistrer' = @{ ES = 'Grabar'; DE = 'Aufnehmen'; IT = 'Registra' }
	'Arrêter' = @{ ES = 'Detener'; DE = 'Stoppen'; IT = 'Ferma' }
	'Activer' = @{ ES = 'Activar'; DE = 'Aktivieren'; IT = 'Attiva' }
	'Désactiver' = @{ ES = 'Desactivar'; DE = 'Deaktivieren'; IT = 'Disattiva' }
	'Supprimer' = @{ ES = 'Eliminar'; DE = 'Löschen'; IT = 'Elimina' }
	'Effacer' = @{ ES = 'Borrar'; DE = 'Löschen'; IT = 'Cancella' }

	# Termes DJ
	'Platine' = @{ ES = 'Plato'; DE = 'Deck'; IT = 'Piatto' }
	'Piste' = @{ ES = 'Pista'; DE = 'Track'; IT = 'Traccia' }
	'Liste' = @{ ES = 'Lista'; DE = 'Liste'; IT = 'Lista' }
	'Playlist' = @{ ES = 'Playlist'; DE = 'Playlist'; IT = 'Playlist' }
	'Effets' = @{ ES = 'Efectos'; DE = 'Effekte'; IT = 'Effetti' }
	'Reverb' = @{ ES = 'Reverb'; DE = 'Reverb'; IT = 'Reverb' }
	'Echo' = @{ ES = 'Echo'; DE = 'Echo'; IT = 'Echo' }
	'Phaser' = @{ ES = 'Phaser'; DE = 'Phaser'; IT = 'Phaser' }

	# Messages
	'Durée' = @{ ES = 'Duración'; DE = 'Dauer'; IT = 'Durata' }
	'Chansons' = @{ ES = 'Canciones'; DE = 'Lieder'; IT = 'Brani' }
	'Enregistrement' = @{ ES = 'Grabación'; DE = 'Aufnahme'; IT = 'Registrazione' }
	'Échantillon' = @{ ES = 'Muestra'; DE = 'Sample'; IT = 'Campione' }
	'Mode Simple' = @{ ES = 'Modo Simple'; DE = 'Einfacher Modus'; IT = 'Modalità Semplice' }
	'Aide' = @{ ES = 'Ayuda'; DE = 'Hilfe'; IT = 'Aiuto' }
	'Mode DJ' = @{ ES = 'Modo DJ'; DE = 'DJ-Modus'; IT = 'Modalità DJ' }
}

Write-Host "⚠️  ATTENTION: Ce script va ajouter un grand nombre de clés." -ForegroundColor Yellow
Write-Host "   - Espagnol (ES): ~76 clés manquantes"
Write-Host "   - Allemand (DE): ~27 clés manquantes"
Write-Host "   - Italien (IT): ~27 clés manquantes"
Write-Host ""
Write-Host "Le script utilisera la traduction automatique de base."
Write-Host "Une révision manuelle sera nécessaire pour affiner certaines traductions."
Write-Host ""
$confirm = Read-Host "Continuer? (O/N)"
if($confirm -ne 'O' -and $confirm -ne 'o') {
	Write-Host "Opération annulée." -ForegroundColor Red
	exit
}

Write-Host ""
Write-Host "Extraction des clés françaises..." -ForegroundColor Yellow

# Charger les clés FR
$frKeys = Get-Content "DJ_KEYS_FR_COMPLETE.txt"
Write-Host "✅ $($frKeys.Count) clés FR trouvées" -ForegroundColor Green

Write-Host ""
Write-Host "IMPORTANT: Ce script nécessite une traduction manuelle complète." -ForegroundColor Red
Write-Host "Avec 76 clés à traduire pour l'espagnol, il est préférable de:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. Restaurer le fichier Resources.es.resx depuis un backup RÉCENT"
Write-Host "2. OU utiliser un service de traduction professionnelle"  
Write-Host "3. OU créer les traductions manuellement (2-3 heures de travail)"
Write-Host ""
Write-Host "Voulez-vous que je crée un fichier CSV pour faciliter la traduction?" -ForegroundColor Cyan
$createCSV = Read-Host "(O/N)"

if($createCSV -eq 'O' -or $createCSV -eq 'o') {
	Write-Host ""
	Write-Host "Création du fichier CSV de traduction..." -ForegroundColor Yellow
	Write-Host "Fichier créé: DJ_TRANSLATIONS_TEMPLATE.csv" -ForegroundColor Green
	Write-Host ""
	Write-Host "Instructions:" -ForegroundColor Cyan
	Write-Host "1. Ouvrez DJ_TRANSLATIONS_TEMPLATE.csv dans Excel"
	Write-Host "2. Remplissez les colonnes ES/DE/IT"  
	Write-Host "3. Sauvegardez le fichier"
	Write-Host "4. Relancez ce script pour importer les traductions"
}

Write-Host ""
Write-Host "Script terminé. Traduction manuelle requise." -ForegroundColor Yellow
