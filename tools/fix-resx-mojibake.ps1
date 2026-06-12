Param(
	[string]$Root = "$(pwd)"
)

Write-Host "Recherche et réparation des .resx contenant du mojibake sous: $Root"

# motifs fréquents de mojibake après mauvaise interprétation d'encodage
# Utiliser des caractères via leur codepoint pour éviter les problèmes d'encodage du script lui-même
$patterns = @(
	[char]0x00C3, # Ã
	[char]0x00C2, # Â
	[char]0x00E2, # â
	[char]0x00EF, # ï
	[char]0xFFFD  # � (replacement char)
)

Get-ChildItem -Path $Root -Recurse -Filter *.resx | ForEach-Object {
	$path = $_.FullName
	try {
		$bytes = [System.IO.File]::ReadAllBytes($path)
		$utf8Text = [System.Text.Encoding]::UTF8.GetString($bytes)

		$needFix = $false
		foreach ($p in $patterns) {
			if ($utf8Text.Contains($p)) { $needFix = $true; break }
		}

		if ($needFix) {
			$backup = "$path.bak"
			Copy-Item -Path $path -Destination $backup -Force
			Write-Host "Mojibake détecté -> backup créé: $backup"

			$cp1252 = [System.Text.Encoding]::GetEncoding(1252)
			$decoded = $cp1252.GetString($bytes)
			$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
			[System.IO.File]::WriteAllText($path, $decoded, $utf8NoBom)
			Write-Host "Réparé (ré-encodé en UTF-8 sans BOM): $path"
		} else {
			Write-Host "OK: $path (aucun signe de mojibake)"
		}
	} catch {
		Write-Warning "Erreur traitement $path : $_"
	}
}
