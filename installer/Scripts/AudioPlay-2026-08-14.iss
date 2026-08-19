#define MyAppName "AudioPlay"
#define MyAppVersion "1.26.08.14"
#define MyAppPublisher "Jean Pelletier"
#define MyAppExeName "AudioPlay.exe"

#ifndef PublishDir
  #define PublishDir "g:\Visual Studio Projects\Jean\AudioPlay 2026-08-14\AudioPlay\bin\Debug\net8.0-windows\"
#endif

#ifndef OutputDir
  #define OutputDir "G:\Visual Studio Projects\Jean\AudioPlay 2026-08-14\installer\EXE"
#endif

[Setup]
AppId={{A2E7F95E-58E4-4E53-8AFA-8B9AA9F7E1260814}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName} {#MyAppVersion}
DefaultGroupName={#MyAppName}
OutputDir={#OutputDir}
OutputBaseFilename=AudioPlay-Setup {#MyAppVersion}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
UsedUserAreasWarning=no
ArchitecturesInstallIn64BitMode=x64compatible
SetupIconFile=g:\Visual Studio Projects\Jean\AudioPlay 2026-08-14\AudioPlay\Assets\AudioPlay.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
CloseApplications=yes
CloseApplicationsFilter=*.exe,*.dll
RestartApplications=no

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\\French.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\\Spanish.isl"
Name: "german"; MessagesFile: "compiler:Languages\\German.isl"
Name: "italian"; MessagesFile: "compiler:Languages\\Italian.isl"


[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "assocmp3"; Description: "{cm:AssociateMP3}"; GroupDescription: "{cm:FileAssociations}"
Name: "assocflac"; Description: "{cm:AssociateFLAC}"; GroupDescription: "{cm:FileAssociations}"
Name: "assocwav"; Description: "{cm:AssociateWAV}"; GroupDescription: "{cm:FileAssociations}"
Name: "assocwma"; Description: "{cm:AssociateWMA}"; GroupDescription: "{cm:FileAssociations}"
Name: "assocaac"; Description: "{cm:AssociateAAC}"; GroupDescription: "{cm:FileAssociations}"

[CustomMessages]
; Français
french.FileAssociations=Associations de fichiers
french.AssociateMP3=Associer les fichiers MP3 à AudioPlay
french.AssociateFLAC=Associer les fichiers FLAC à AudioPlay
french.AssociateWAV=Associer les fichiers WAV à AudioPlay
french.AssociateWMA=Associer les fichiers WMA à AudioPlay
french.AssociateAAC=Associer les fichiers AAC à AudioPlay

; English
english.FileAssociations=File Associations
english.AssociateMP3=Associate MP3 files with AudioPlay
english.AssociateFLAC=Associate FLAC files with AudioPlay
english.AssociateWAV=Associate WAV files with AudioPlay
english.AssociateWMA=Associate WMA files with AudioPlay
english.AssociateAAC=Associate AAC files with AudioPlay

; Español
spanish.FileAssociations=Asociaciones de archivos
spanish.AssociateMP3=Asociar archivos MP3 con AudioPlay
spanish.AssociateFLAC=Asociar archivos FLAC con AudioPlay
spanish.AssociateWAV=Asociar archivos WAV con AudioPlay
spanish.AssociateWMA=Asociar archivos WMA con AudioPlay
spanish.AssociateAAC=Asociar archivos AAC con AudioPlay

; Deutsch
german.FileAssociations=Dateiverknüpfungen
german.AssociateMP3=MP3-Dateien mit AudioPlay verknüpfen
german.AssociateFLAC=FLAC-Dateien mit AudioPlay verknüpfen
german.AssociateWAV=WAV-Dateien mit AudioPlay verknüpfen
german.AssociateWMA=WMA-Dateien mit AudioPlay verknüpfen
german.AssociateAAC=AAC-Dateien mit AudioPlay verknüpfen

; Italiano
italian.FileAssociations=Associazioni file
italian.AssociateMP3=Associa file MP3 ad AudioPlay
italian.AssociateFLAC=Associa file FLAC ad AudioPlay
italian.AssociateWAV=Associa file WAV ad AudioPlay
italian.AssociateWMA=Associa file WMA ad AudioPlay
italian.AssociateAAC=Associa file AAC ad AudioPlay

[Files]
; Fichiers principaux de l'application (récursif)
Source: "{#PublishDir}\\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion; Excludes: "python_embedded\*"

; DLL native SoundTouch pour l'effet Time Stretch (qualité Audacity)
Source: "{#PublishDir}\\SoundTouch.dll"; DestDir: "{app}"; Flags: ignoreversion; Check: FileExists(ExpandConstant('{#PublishDir}\\SoundTouch.dll'))

; Dossier Python embarqué (pour BPM detection avec Librosa)
Source: "python_embedded\\*"; DestDir: "{userappdata}\\AudioPlay\\python_embedded"; Flags: recursesubdirs createallsubdirs ignoreversion uninsneveruninstall

[Icons]
Name: "{group}\\{#MyAppName}"; Filename: "{app}\\{#MyAppExeName}"
Name: "{group}\\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{userdesktop}\AudioPlay"; Filename: "{app}\AudioPlay.exe"; WorkingDir: "{app}"

[Registry]
; Associe .mp3 à AudioPlay
Root: HKCU; Subkey: "Software\Classes\.mp3"; ValueType: string; ValueName: ""; ValueData: "AudioPlay.mp3"; Flags: uninsdeletevalue; Tasks: assocmp3
Root: HKCU; Subkey: "Software\Classes\AudioPlay.mp3\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AudioPlay.exe"" ""%1"""
Root: HKCU; Subkey: "Software\Classes\AudioPlay.mp3"; ValueType: string; ValueName: ""; ValueData: "Fichier MP3 AudioPlay"

; Associe .wav à AudioPlay
Root: HKCU; Subkey: "Software\Classes\.wav"; ValueType: string; ValueName: ""; ValueData: "AudioPlay.wav"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\AudioPlay.wav\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AudioPlay.exe"" ""%1"""
Root: HKCU; Subkey: "Software\Classes\AudioPlay.wav"; ValueType: string; ValueName: ""; ValueData: "Fichier WAV AudioPlay"

; Associe .flac à AudioPlay
Root: HKCU; Subkey: "Software\Classes\.flac"; ValueType: string; ValueName: ""; ValueData: "AudioPlay.flac"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\AudioPlay.flac\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AudioPlay.exe"" ""%1"""
Root: HKCU; Subkey: "Software\Classes\AudioPlay.flac"; ValueType: string; ValueName: ""; ValueData: "Fichier FLAC AudioPlay"

; Associe .wma à AudioPlay
Root: HKCU; Subkey: "Software\Classes\.wma"; ValueType: string; ValueName: ""; ValueData: "AudioPlay.wma"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\AudioPlay.wma\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AudioPlay.exe"" ""%1"""
Root: HKCU; Subkey: "Software\Classes\AudioPlay.wma"; ValueType: string; ValueName: ""; ValueData: "Fichier WMA AudioPlay"

; Associe .aac à AudioPlay
Root: HKCU; Subkey: "Software\Classes\.aac"; ValueType: string; ValueName: ""; ValueData: "AudioPlay.aac"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\AudioPlay.aac\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AudioPlay.exe"" ""%1"""
Root: HKCU; Subkey: "Software\Classes\AudioPlay.aac"; ValueType: string; ValueName: ""; ValueData: "Fichier AAC AudioPlay"

Root: HKCU; Subkey: "Software\Classes\AudioPlay.mp3\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: """{app}\AudioPlay.exe"",0"
Root: HKCU; Subkey: "Software\Classes\AudioPlay.wav\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: """{app}\AudioPlay.exe"",0"
Root: HKCU; Subkey: "Software\Classes\AudioPlay.flac\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: """{app}\AudioPlay.exe"",0"
Root: HKCU; Subkey: "Software\Classes\AudioPlay.wma\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: """{app}\AudioPlay.exe"",0"
Root: HKCU; Subkey: "Software\Classes\AudioPlay.aac\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: """{app}\AudioPlay.exe"",0"

[Run]
Filename: "{app}\\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[Code]
const
  // URL de téléchargement du .NET 8.0 Desktop Runtime (x64)
  DotNetRuntimeURL_x64 = 'https://download.visualstudio.microsoft.com/download/pr/6224f00f-08da-4e7f-85b1-00d42c2bb3d3/b775de636b91e023574a0bbc291f705a/windowsdesktop-runtime-8.0.12-win-x64.exe';
  // URL de téléchargement du .NET 8.0 Desktop Runtime (x86)
  DotNetRuntimeURL_x86 = 'https://download.visualstudio.microsoft.com/download/pr/f8bcc7f3-6db9-4d05-bb3d-f9f5bc7c6dc7/f5f5bbf3cf2ea5c45ea7c1233b6e4878/windowsdesktop-runtime-8.0.12-win-x86.exe';

  // Version minimale requise de .NET
  DotNetMinVersion = '8.0.0';

var
  DotNetRuntimeNeeded: Boolean;
  DotNetInstallerPath: String;

// Fonction pour fermer AudioPlay s'il est en cours d'exécution
function CloseAudioPlayIfRunning(): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;

  // Vérifier si AudioPlay.exe est vraiment en cours d'exécution
  // taskkill retourne 0 si le processus existe, 128 si non trouvé
  if Exec('taskkill.exe', '/F /IM AudioPlay.exe /T', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    // ResultCode = 0 signifie que le processus a été trouvé et tué
    if ResultCode = 0 then
    begin
      Log('AudioPlay était en cours d''exécution et a été fermé.');
      Sleep(2000); // Attendre 2 secondes pour que le processus se termine complètement
    end
    else
    begin
      // Le processus n'existe pas (code 128 ou autre)
      Log('AudioPlay n''est pas en cours d''exécution (code: ' + IntToStr(ResultCode) + ').');
    end;
  end;
end;

// Fonction pour vérifier si un fichier existe
function FileExists(const FileName: string): Boolean;
begin
  Result := FileOrDirExists(FileName);
  if not Result then
	Log('AVERTISSEMENT: Fichier non trouvé: ' + FileName);
end;

// Fonction pour comparer les versions
function CompareVersion(V1, V2: string): Integer;
var
  P, N1, N2: Integer;
begin
  Result := 0;
  while (Result = 0) and ((V1 <> '') or (V2 <> '')) do
  begin
	P := Pos('.', V1);
	if P > 0 then
	begin
	  N1 := StrToIntDef(Copy(V1, 1, P - 1), 0);
	  Delete(V1, 1, P);
	end
	  else
	if V1 <> '' then
	begin
	  N1 := StrToIntDef(V1, 0);
	  V1 := '';
	end
	  else
	  N1 := 0;

	P := Pos('.', V2);
	if P > 0 then
	begin
	  N2 := StrToIntDef(Copy(V2, 1, P - 1), 0);
	  Delete(V2, 1, P);
	end
	  else
	if V2 <> '' then
	begin
	  N2 := StrToIntDef(V2, 0);
	  V2 := '';
	end
	  else
	  N2 := 0;

	if N1 < N2 then
	  Result := -1
	else
	  if N1 > N2 then
		Result := 1;
  end;
end;

// Fonction pour vérifier si .NET Desktop Runtime 8.0+ est installé
function IsDotNetInstalled: Boolean;
var
  InstalledVersions: TArrayOfString;
  I: Integer;
  DotNetKey: String;
  Version: String;
begin
  Result := False;

  // Vérifier dans le registre pour .NET Desktop Runtime
  DotNetKey := 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost';

  // Méthode 1: vérifier l'existence du dossier .NET 8
  if DirExists(ExpandConstant('{commonpf}\dotnet\shared\Microsoft.WindowsDesktop.App')) then
  begin
	Log('.NET Desktop Runtime détecté via le système de fichiers.');
	Result := True;
	Exit;
  end;

  // Méthode 2: Vérifier via le registre
  if RegGetSubkeyNames(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost', InstalledVersions) then
  begin
	for I := 0 to GetArrayLength(InstalledVersions) - 1 do
	begin
	  if RegQueryStringValue(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost\' + InstalledVersions[I], 'Version', Version) then
	  begin
		Log('Version .NET trouvée: ' + Version);
		if CompareVersion(Version, DotNetMinVersion) >= 0 then
		begin
		  Log('.NET Desktop Runtime 8.0+ est installé (version: ' + Version + ')');
		  Result := True;
		  Exit;
		end;
	  end;
	end;
  end;

  // Méthode 3: Vérifier via dotnet.exe
  if FileExists(ExpandConstant('{commonpf}\dotnet\dotnet.exe')) then
  begin
	Log('dotnet.exe trouvé, .NET est probablement installé.');
	Result := True;
  end
  else
  begin
	Log('.NET Desktop Runtime 8.0+ n''est pas installé.');
  end;
end;

// Fonction pour vérifier si AudioPlay est déjà installé et obtenir sa version
function GetInstalledVersion(var InstalledVersion: String): Boolean;
var
  UninstallKey: String;
begin
  Result := False;
  UninstallKey := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{A2E7F95E-58E4-4E53-8AFA-8B9AA9F7E1260602}_is1';

  if RegQueryStringValue(HKLM, UninstallKey, 'DisplayVersion', InstalledVersion) or
	 RegQueryStringValue(HKCU, UninstallKey, 'DisplayVersion', InstalledVersion) then
  begin
	Result := True;
	Log('AudioPlay version ' + InstalledVersion + ' est déjà installé.');
  end
  else
  begin
	Log('AudioPlay n''est pas installé.');
  end;
end;

// Initialisation avant l'installation
function InitializeSetup(): Boolean;
var
  InstalledVersion: String;
  MsgText: String;
  MsgResult: Integer;
begin
  Result := True;

  // Fermer AudioPlay s'il est en cours d'exécution
  if not CloseAudioPlayIfRunning() then
  begin
    Result := False;
    Exit;
  end;

  // Vérifier si AudioPlay est déjà installé
  if GetInstalledVersion(InstalledVersion) then
  begin
	// Comparer les versions
	if CompareVersion(InstalledVersion, '{#MyAppVersion}') = 0 then
	begin
	  // Même version déjà installée
	  if ActiveLanguage = 'french' then
		MsgText := 'AudioPlay version ' + InstalledVersion + ' est déjà installé.' + #13#10#13#10 +
				   'Voulez-vous continuer l''installation et réinstaller par-dessus la version existante ?'
	  else if ActiveLanguage = 'spanish' then
		MsgText := 'AudioPlay versión ' + InstalledVersion + ' ya está instalado.' + #13#10#13#10 +
				   '¿Desea continuar con la instalación y reinstalar sobre la versión existente?'
	  else if ActiveLanguage = 'german' then
		MsgText := 'AudioPlay Version ' + InstalledVersion + ' ist bereits installiert.' + #13#10#13#10 +
				   'Möchten Sie die Installation fortsetzen und über die vorhandene Version neu installieren?'
	  else if ActiveLanguage = 'italian' then
		MsgText := 'AudioPlay versione ' + InstalledVersion + ' è già installato.' + #13#10#13#10 +
				   'Vuoi continuare l''installazione e reinstallare sulla versione esistente?'
	  else
		MsgText := 'AudioPlay version ' + InstalledVersion + ' is already installed.' + #13#10#13#10 +
				   'Do you want to continue the installation and reinstall over the existing version?';

	  MsgResult := MsgBox(MsgText, mbConfirmation, MB_YESNO);

	  if MsgResult = IDNO then
	  begin
		Result := False;
		Exit;
	  end;
	end
	else if CompareVersion(InstalledVersion, '{#MyAppVersion}') > 0 then
	begin
	  // Version plus récente déjà installée
	  if ActiveLanguage = 'french' then
		MsgText := 'Une version plus récente d''AudioPlay (' + InstalledVersion + ') est déjà installée.' + #13#10#13#10 +
				   'Vous tentez d''installer la version {#MyAppVersion}.' + #13#10#13#10 +
				   'Voulez-vous continuer et revenir à cette version antérieure ?'
	  else if ActiveLanguage = 'spanish' then
		MsgText := 'Ya está instalada una versión más reciente de AudioPlay (' + InstalledVersion + ').' + #13#10#13#10 +
				   'Está intentando instalar la versión {#MyAppVersion}.' + #13#10#13#10 +
				   '¿Desea continuar y volver a esta versión anterior?'
	  else if ActiveLanguage = 'german' then
		MsgText := 'Eine neuere Version von AudioPlay (' + InstalledVersion + ') ist bereits installiert.' + #13#10#13#10 +
				   'Sie versuchen, Version {#MyAppVersion} zu installieren.' + #13#10#13#10 +
				   'Möchten Sie fortfahren und auf diese frühere Version zurückkehren?'
	  else if ActiveLanguage = 'italian' then
		MsgText := 'È già installata una versione più recente di AudioPlay (' + InstalledVersion + ').' + #13#10#13#10 +
				   'Si sta tentando di installare la versione {#MyAppVersion}.' + #13#10#13#10 +
				   'Vuoi continuare e tornare a questa versione precedente?'
	  else
		MsgText := 'A newer version of AudioPlay (' + InstalledVersion + ') is already installed.' + #13#10#13#10 +
				   'You are attempting to install version {#MyAppVersion}.' + #13#10#13#10 +
				   'Do you want to continue and downgrade to this earlier version?';

	  MsgResult := MsgBox(MsgText, mbConfirmation, MB_YESNO);

	  if MsgResult = IDNO then
	  begin
		Result := False;
		Exit;
	  end;
	end
	else
	begin
	  // Version plus ancienne installée - mise à jour normale
	  if ActiveLanguage = 'french' then
		MsgText := 'AudioPlay version ' + InstalledVersion + ' est installé.' + #13#10#13#10 +
				   'Vous allez installer la version {#MyAppVersion}.' + #13#10#13#10 +
				   'Voulez-vous continuer la mise à jour ?'
	  else if ActiveLanguage = 'spanish' then
		MsgText := 'AudioPlay versión ' + InstalledVersion + ' está instalado.' + #13#10#13#10 +
				   'Va a instalar la versión {#MyAppVersion}.' + #13#10#13#10 +
				   '¿Desea continuar con la actualización?'
	  else if ActiveLanguage = 'german' then
		MsgText := 'AudioPlay Version ' + InstalledVersion + ' ist installiert.' + #13#10#13#10 +
				   'Sie werden Version {#MyAppVersion} installieren.' + #13#10#13#10 +
				   'Möchten Sie mit dem Update fortfahren?'
	  else if ActiveLanguage = 'italian' then
		MsgText := 'AudioPlay versione ' + InstalledVersion + ' è installato.' + #13#10#13#10 +
				   'Stai per installare la versione {#MyAppVersion}.' + #13#10#13#10 +
				   'Vuoi continuare con l''aggiornamento?'
	  else
		MsgText := 'AudioPlay version ' + InstalledVersion + ' is installed.' + #13#10#13#10 +
				   'You are about to install version {#MyAppVersion}.' + #13#10#13#10 +
				   'Do you want to continue with the update?';

	  MsgResult := MsgBox(MsgText, mbConfirmation, MB_YESNO);

	  if MsgResult = IDNO then
	  begin
		Result := False;
		Exit;
	  end;
	end;
  end;

  // Vérifier .NET
  DotNetRuntimeNeeded := not IsDotNetInstalled;

  if DotNetRuntimeNeeded then
  begin
	Log('.NET Desktop Runtime 8.0 n''est pas installé. Il sera téléchargé et installé automatiquement.');
  end
  else
  begin
	Log('.NET Desktop Runtime 8.0+ est déjà installé.');
  end;
end;

// Télécharger et installer .NET Runtime si nécessaire
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
  DownloadPage: TDownloadWizardPage;
  DotNetURL: String;
  StatusText: String;
begin
  Result := '';

  if not DotNetRuntimeNeeded then
	Exit;

  // Déterminer l'URL appropriée selon l'architecture
  if Is64BitInstallMode then
	DotNetURL := DotNetRuntimeURL_x64
  else
	DotNetURL := DotNetRuntimeURL_x86;

  // Définir le chemin de téléchargement
  DotNetInstallerPath := ExpandConstant('{tmp}\windowsdesktop-runtime-8.0-installer.exe');

  // Message à l'utilisateur selon la langue
  if ActiveLanguage = 'french' then
	StatusText := 'Téléchargement et installation de .NET 8.0 Desktop Runtime...' + #13#10 + 
				  'Cette opération peut prendre quelques minutes.' + #13#10 +
				  'Veuillez patienter...'
  else if ActiveLanguage = 'spanish' then
	StatusText := 'Descargando e instalando .NET 8.0 Desktop Runtime...' + #13#10 + 
				  'Esta operación puede tardar unos minutos.' + #13#10 +
				  'Por favor espere...'
  else if ActiveLanguage = 'german' then
	StatusText := 'Herunterladen und Installieren von .NET 8.0 Desktop Runtime...' + #13#10 + 
				  'Dieser Vorgang kann einige Minuten dauern.' + #13#10 +
				  'Bitte warten Sie...'
  else if ActiveLanguage = 'italian' then
	StatusText := 'Download e installazione di .NET 8.0 Desktop Runtime...' + #13#10 + 
				  'Questa operazione può richiedere alcuni minuti.' + #13#10 +
				  'Attendere prego...'
  else
	StatusText := 'Downloading and installing .NET 8.0 Desktop Runtime...' + #13#10 + 
				  'This operation may take a few minutes.' + #13#10 +
				  'Please wait...';

  try
	// Créer une page de téléchargement
	DownloadPage := CreateDownloadPage(StatusText, '', nil);
	DownloadPage.Clear;
	DownloadPage.Add(DotNetURL, 'windowsdesktop-runtime-8.0-installer.exe', '');
	DownloadPage.Show;

	try
	  // Télécharger le runtime
	  DownloadPage.Download;

	  // Installer le runtime
	  if FileExists(DotNetInstallerPath) then
	  begin
		Log('Installation de .NET Desktop Runtime depuis: ' + DotNetInstallerPath);

		// Exécuter l'installateur en mode silencieux
		if not Exec(DotNetInstallerPath, '/install /quiet /norestart', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
		begin
		  if ActiveLanguage = 'french' then
			Result := 'Échec du lancement de l''installateur .NET Desktop Runtime.'
		  else if ActiveLanguage = 'spanish' then
			Result := 'Error al iniciar el instalador de .NET Desktop Runtime.'
		  else if ActiveLanguage = 'german' then
			Result := 'Fehler beim Starten des .NET Desktop Runtime-Installationsprogramms.'
		  else if ActiveLanguage = 'italian' then
			Result := 'Errore nell''avvio del programma di installazione di .NET Desktop Runtime.'
		  else
			Result := 'Failed to launch .NET Desktop Runtime installer.';
		  Exit;
		end;

		// Vérifier le code de sortie
		if ResultCode <> 0 then
		begin
		  Log('L''installateur .NET a retourné le code: ' + IntToStr(ResultCode));
		  if ResultCode = 1638 then
		  begin
			Log('.NET Desktop Runtime est déjà installé (code 1638).');
			// Ce n'est pas une erreur, continuer
		  end
		  else if ResultCode = 3010 then
		  begin
			Log('L''installation de .NET nécessite un redémarrage (code 3010).');
			NeedsRestart := True;
		  end
		  else
		  begin
			if ActiveLanguage = 'french' then
			  Result := 'L''installation de .NET Desktop Runtime a échoué avec le code: ' + IntToStr(ResultCode)
			else if ActiveLanguage = 'spanish' then
			  Result := 'La instalación de .NET Desktop Runtime falló con el código: ' + IntToStr(ResultCode)
			else if ActiveLanguage = 'german' then
			  Result := 'Die Installation von .NET Desktop Runtime ist mit Code fehlgeschlagen: ' + IntToStr(ResultCode)
			else if ActiveLanguage = 'italian' then
			  Result := 'L''installazione di .NET Desktop Runtime non è riuscita con il codice: ' + IntToStr(ResultCode)
			else
			  Result := '.NET Desktop Runtime installation failed with code: ' + IntToStr(ResultCode);
			Exit;
		  end;
		end
		else
		begin
		  Log('.NET Desktop Runtime installé avec succès.');
		end;
	  end
	  else
	  begin
		if ActiveLanguage = 'french' then
		  Result := 'Le téléchargement de .NET Desktop Runtime a échoué.'
		else if ActiveLanguage = 'spanish' then
		  Result := 'La descarga de .NET Desktop Runtime falló.'
		else if ActiveLanguage = 'german' then
		  Result := 'Der Download von .NET Desktop Runtime ist fehlgeschlagen.'
		else if ActiveLanguage = 'italian' then
		  Result := 'Il download di .NET Desktop Runtime non è riuscito.'
		else
		  Result := 'Failed to download .NET Desktop Runtime.';
	  end;
	finally
	  DownloadPage.Hide;
	end;
  except
	if ActiveLanguage = 'french' then
	  Result := 'Une erreur s''est produite lors de l''installation de .NET Desktop Runtime: ' + GetExceptionMessage
	else if ActiveLanguage = 'spanish' then
	  Result := 'Se produjo un error al instalar .NET Desktop Runtime: ' + GetExceptionMessage
	else if ActiveLanguage = 'german' then
	  Result := 'Beim Installieren von .NET Desktop Runtime ist ein Fehler aufgetreten: ' + GetExceptionMessage
	else if ActiveLanguage = 'italian' then
	  Result := 'Si è verificato un errore durante l''installazione di .NET Desktop Runtime: ' + GetExceptionMessage
	else
	  Result := 'An error occurred while installing .NET Desktop Runtime: ' + GetExceptionMessage;
  end;
end;
