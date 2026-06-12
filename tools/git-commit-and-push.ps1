Param(
	[string]$CommitMessage = "Normalize encodage: convert .resx to UTF-8 no BOM + add .editorconfig/.gitattributes and hooks",
	[string]$AuthorName = $null,
	[string]$AuthorEmail = $null,
	[string]$Remote = "origin",
	[string]$Branch = $null,
	[switch]$Push
)

function Run-Git {
	param([Parameter(Mandatory=$true)][string[]]$Args)
	& git @Args
	if ($LASTEXITCODE -ne 0) { throw "git $($Args -join ' ') a échoué avec code $LASTEXITCODE" }
}

Write-Host "Vérification git..."
try {
	$gitVer = & git --version 2>&1
	if ($LASTEXITCODE -ne 0) { throw "git non trouvé" }
	Write-Host "OK: $gitVer"
} catch {
	Write-Error "Erreur: git n'est pas disponible dans le PATH. Installez git et relancez le script."; exit 1
}

# Optionnel : configurer user.name et user.email si fournis
if ($AuthorName) {
	Run-Git -Args @('config','--global','user.name',$AuthorName)
	Write-Host "user.name défini à '$AuthorName'"
}
if ($AuthorEmail) {
	Run-Git -Args @('config','--global','user.email',$AuthorEmail)
	Write-Host "user.email défini à '$AuthorEmail'"
}

# Aller au répertoire racine du dépôt (parent du dossier tools)
# Permet d'exécuter ce script depuis n'importe où et de se placer dans la racine du repo
$scriptFolder = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptFolder "..")
Set-Location -Path $repoRoot
Write-Host "Répertoire utilisé: $(Get-Location)"

# Afficher statut
Run-Git -Args @('status','--porcelain') | Out-Null
$changes = & git status --porcelain
if ([string]::IsNullOrWhiteSpace($changes)) {
	Write-Host "Aucun changement à committer."; exit 0
}

Write-Host "Fichiers modifiés détectés :"; Write-Host $changes

# Ajouter tous
Run-Git -Args @('add','-A')

# Commit
try {
	Run-Git -Args @('commit','-m',$CommitMessage)
	Write-Host "Commit créé avec le message: $CommitMessage"
} catch {
	Write-Error "Echec du commit: $_"; exit 1
}

if ($Push) {
	# déterminer la branche courante si non fournie
	if (-not $Branch) {
		$Branch = (& git rev-parse --abbrev-ref HEAD).Trim()
		Write-Host "Branche actuelle détectée: $Branch"
	}

	# si remote n'existe pas, avertir
	$remotes = & git remote
	if (-not ($remotes -split "\r?\n" | Where-Object { $_ -eq $Remote })) {
		Write-Warning "Remote '$Remote' non trouvé dans le repo. Utilisez 'git remote add' manuellement si nécessaire.";
	}

	try {
		# push - si la branche n'a pas d'upstream, utiliser -u
		Write-Host "Pushing vers $Remote/$Branch ..."
		Run-Git -Args @('push',$Remote,$Branch)
		Write-Host "Push terminé.";
	} catch {
		Write-Warning "Push simple échoué : $_. Tentative avec -u ..."
		try { Run-Git -Args @('push','-u',$Remote,$Branch); Write-Host 'Push avec -u réussi.' } catch { Write-Error "Push échoué définitivement : $_"; exit 1 }
	}
} else {
	Write-Host "-- Push non demandé. Pour pousser, relancer avec -Push ou exécuter 'git push'."
}
