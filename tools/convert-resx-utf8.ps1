Param(
	[string]$Root = "$(pwd)"
)
Write-Host "Converting .resx files to UTF8 (no BOM) under: $Root"

Get-ChildItem -Path $Root -Recurse -Filter *.resx | ForEach-Object {
	$path = $_.FullName
	try {
		$bytes = [System.IO.File]::ReadAllBytes($path)
		# Detect encoding
		$enc = [System.Text.Encoding]::UTF8
		$pre = [System.Text.Encoding]::UTF8.GetPreamble()
		$hasBom = $false
		if ($bytes.Length -ge $pre.Length) {
			$hasBom = $true
			for ($i = 0; $i -lt $pre.Length; $i++) {
				if ($bytes[$i] -ne $pre[$i]) { $hasBom = $false; break }
			}
		}

		$text = Get-Content -Path $path -Raw
		# Re-write as UTF8 without BOM
		$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
		[System.IO.File]::WriteAllText($path, $text, $utf8NoBom)
		Write-Host "Converted: $path (BOM removed: $hasBom)"
	} catch {
		Write-Warning "Failed to convert $path : $_"
	}
}
