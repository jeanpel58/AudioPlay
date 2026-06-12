#define MyAppName "AudioPlay"
#define MyAppVersion "1.26.06.11"
#define MyAppPublisher "Jean Pelletier"
#define MyAppExeName "AudioPlay.exe"

#ifndef PublishDir
  #define PublishDir "g:\Visual Studio Projects\Jean\AudioPlay 2026-06-11\AudioPlay\bin\Debug\net8.0-windows\"
#endif

#ifndef OutputDir
  #define OutputDir "G:\Visual Studio Projects\Jean\AudioPlay 2026-06-11\installer\EXE"
#endif

[Setup]
AppId={{A2E7F95E-58E4-4E53-8AFA-8B9AA9F7E1260611}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir={#OutputDir}
OutputBaseFilename=AudioPlay-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
SetupIconFile=g:\Visual Studio Projects\Jean\AudioPlay 2026-06-11\AudioPlay\Assets\AudioPlay.ico
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\\French.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\\Spanish.isl"
Name: "german"; MessagesFile: "compiler:Languages\\German.isl"
Name: "italian"; MessagesFile: "compiler:Languages\\Italian.isl"


[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "assocmp3"; Description: "Associer les fichiers MP3 à AudioPlay"; GroupDescription: "Associations de fichiers"
Name: "assocflac"; Description: "Associer les fichiers FLAC à AudioPlay"; GroupDescription: "Associations de fichiers"
Name: "assocwav"; Description: "Associer les fichiers WAV à AudioPlay"; GroupDescription: "Associations de fichiers"
Name: "assocwma"; Description: "Associer les fichiers WMA à AudioPlay"; GroupDescription: "Associations de fichiers"
Name: "assocaac"; Description: "Associer les fichiers AAC à AudioPlay"; GroupDescription: "Associations de fichiers"

[Files]
; Fichiers principaux de l'application (récursif)
Source: "{#PublishDir}\\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion; Excludes: "python_embedded\*"

; DLL native SoundTouch pour l'effet Time Stretch (qualité Audacity)
; NOTE: Cette DLL est copiée automatiquement par le Target MSBuild CopySoundTouchDll
; mais on la déclare explicitement ici pour documentation et vérification
Source: "{#PublishDir}\\SoundTouch.dll"; DestDir: "{app}"; Flags: ignoreversion; Check: FileExists(ExpandConstant('{#PublishDir}\\SoundTouch.dll'))

; Dossier Python embarqué (pour BPM detection avec Librosa)
Source: "python_embedded\\*"; DestDir: "{userappdata}\\AudioPlay\\python_embedded"; Flags: recursesubdirs createallsubdirs

[Icons]
Name: "{group}\\{#MyAppName}"; Filename: "{app}\\{#MyAppExeName}"
Name: "{group}\\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
//Name: "{autodesktop}\\{#MyAppName}"; Filename: "{app}\\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userdesktop}\AudioPlay"; Filename: "{app}\AudioPlay.exe"; WorkingDir: "{app}"

 [Registry]
; Associe .mp3 à AudioPlay
//Root: HKCU; Subkey: "Software\Classes\.mp3"; ValueType: string; ValueName: ""; ValueData: "AudioPlay.mp3"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.mp3"; ValueType: string; ValueName: ""; ValueData: "AudioPlay.mp3"; Flags: uninsdeletevalue; Tasks: assocmp3
Root: HKCU; Subkey: "Software\Classes\AudioPlay.mp3\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AudioPlay.exe"" ""%1"""
Root: HKCU; Subkey: "Software\Classes\AudioPlay.mp3"; ValueType: string; ValueName: ""; ValueData: "Fichier MP3 AudioPlay"

; Associe .wav à AudioPlay
Root: HKCU; Subkey: "Software\Classes\.wav"; ValueType: string; ValueName: ""; ValueData: "AudioPlay.wav"; Flags: uninsdeletevalue
//Root: HKCU; Subkey: "Software\Classes\.wav"; ValueType: string; ValueName: ""; ValueData: "AudioPlay.wav"; Flags: uninsdeletevalue; Tasks: assocwav
Root: HKCU; Subkey: "Software\Classes\AudioPlay.wav\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AudioPlay.exe"" ""%1"""
Root: HKCU; Subkey: "Software\Classes\AudioPlay.wav"; ValueType: string; ValueName: ""; ValueData: "Fichier WAV AudioPlay"

; Associe .flac à AudioPlay
Root: HKCU; Subkey: "Software\Classes\.flac"; ValueType: string; ValueName: ""; ValueData: "AudioPlay.flac"; Flags: uninsdeletevalue
//Root: HKCU; Subkey: "Software\Classes\.flac"; ValueType: string; ValueName: ""; ValueData: "AudioPlay.flac"; Flags: uninsdeletevalue; Tasks: assocflac
Root: HKCU; Subkey: "Software\Classes\AudioPlay.flac\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AudioPlay.exe"" ""%1"""
Root: HKCU; Subkey: "Software\Classes\AudioPlay.flac"; ValueType: string; ValueName: ""; ValueData: "Fichier FLAC AudioPlay"

; Associe .wma à AudioPlay
Root: HKCU; Subkey: "Software\Classes\.wma"; ValueType: string; ValueName: ""; ValueData: "AudioPlay.wma"; Flags: uninsdeletevalue
//Root: HKCU; Subkey: "Software\Classes\.wma"; ValueType: string; ValueName: ""; ValueData: "AudioPlay.wma"; Flags: uninsdeletevalue; Tasks: assocwma
Root: HKCU; Subkey: "Software\Classes\AudioPlay.wma\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\AudioPlay.exe"" ""%1"""
Root: HKCU; Subkey: "Software\Classes\AudioPlay.wma"; ValueType: string; ValueName: ""; ValueData: "Fichier WMA AudioPlay"

; Associe .aac à AudioPlay
Root: HKCU; Subkey: "Software\Classes\.aac"; ValueType: string; ValueName: ""; ValueData: "AudioPlay.aac"; Flags: uninsdeletevalue
//Root: HKCU; Subkey: "Software\Classes\.aac"; ValueType: string; ValueName: ""; ValueData: "AudioPlay.aac"; Flags: uninsdeletevalue; Tasks: assocaac
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
// Fonction pour vérifier si un fichier existe
function FileExists(const FileName: string): Boolean;
begin
  Result := FileOrDirExists(FileName);
  if not Result then
	Log('AVERTISSEMENT: Fichier non trouvé: ' + FileName);
end;
