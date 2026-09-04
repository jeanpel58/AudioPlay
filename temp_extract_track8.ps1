$ErrorActionPreference = 'Stop'
$dllPath = "G:\Visual Studio Projects\Jean\AudioPlay 2026-08-19\AudioPlay\bin\Debug\net8.0-windows\AudioPlay.dll"
$destDir = "G:\Conversion CD 2 MP3\(Extracted Albums)"
$drive = "D:"
$trackNumber = 8

Write-Host "Loading assembly: $dllPath"
$asm = [Reflection.Assembly]::LoadFrom($dllPath)
# Find CDAudioManager type
# Essayer d'obtenir CDAudioManager via GetType avant GetTypes (réduit les risques de ReflectionTypeLoadException)
$possibleNames = @(
	'CDAudioManager',
	'AudioPlay.CDAudioManager'
)
$cdType = $null
foreach($n in $possibleNames){
	$cdType = $asm.GetType($n, $false, $true)
	if($cdType){ break }
}

if(-not $cdType){
	Write-Host "WARNING: CDAudioManager non trouvé par GetType; tentative via GetTypes()..."
	try{
		$types = $asm.GetTypes()
	}catch{
		Write-Host "ERROR: Unable to load types from assembly. LoaderExceptions:"
		$le = $_.Exception.LoaderExceptions
		if($le){ $le | ForEach-Object { Write-Host " - " $_.Message } }
		else{ Write-Host $_.Exception.Message }
		exit 10
	}
	$cdType = $types | Where-Object { $_.Name -eq 'CDAudioManager' }
}

if(-not $cdType) { Write-Error "CDAudioManager type not found"; exit 1 }

Write-Host "Calling LirePistesCD($drive)"
$methodLire = $cdType.GetMethod('LirePistesCD')
$tracks = $methodLire.Invoke($null, @($drive))
Write-Host "Tracks.Count = $($tracks.Count)"
for($i=0;$i -lt $tracks.Count;$i++){
	$t = $tracks[$i]
	Write-Host "Track $($t.TrackNumber): StartFrame=$($t.StartFrame) EndFrame=$($t.EndFrame) Duration=$($t.Duration) Title=$($t.Title)"
}

if($tracks.Count -lt $trackNumber){ Write-Error "Track $trackNumber not found in TOC"; exit 2 }
$track = $tracks[$trackNumber-1]

# Create destination folder
if(-not (Test-Path $destDir)){
	Write-Host "Creating destination folder: $destDir"
	New-Item -ItemType Directory -Path $destDir | Out-Null
}

# Create reader via CreerLecteurCDAudio
$methodCreer = $cdType.GetMethod('CreerLecteurCDAudio')
if(-not $methodCreer){ Write-Error "CreerLecteurCDAudio not found"; exit 3 }
$reader = $methodCreer.Invoke($null, @($track))
if(-not $reader){ Write-Error "Failed to create CD reader"; exit 4 }

# Try to load NAudio's WaveFileWriter type from loaded assemblies
$waveFileWriterType = [Type]::GetType('NAudio.Wave.WaveFileWriter, NAudio')
if(-not $waveFileWriterType){
	# try to load NAudio dll from same folder
	$binDir = Split-Path $dllPath
	$naudioDll = Get-ChildItem -Path $binDir -Filter 'NAudio*.dll' -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
	if($naudioDll){
		[Reflection.Assembly]::LoadFrom($naudioDll.FullName) | Out-Null
		$waveFileWriterType = [Type]::GetType('NAudio.Wave.WaveFileWriter, NAudio')
	}
}

if(-not $waveFileWriterType){ Write-Error "WaveFileWriter type not found (NAudio)"; exit 5 }

$destFile = Join-Path $destDir ("Track{0:00}.wav" -f $trackNumber)
Write-Host "Writing WAV to $destFile"

# Use WaveFileWriter.CreateWaveFile using reflection: ctor (string, WaveFormat) or static CreateWaveFile
$waveFormat = $reader.WaveFormat
$ctor = $waveFileWriterType.GetConstructor([Type[]]@([string],[Type]::GetType('NAudio.Wave.WaveFormat, NAudio')))
$writer = $null
if($ctor){
	$writer = $ctor.Invoke(@($destFile,$waveFormat))
}else{
	# fallback to CreateWaveFile (static method)
	$createMethod = $waveFileWriterType.GetMethod('CreateWaveFile', [Type[]]@([string],[Type]::GetType('NAudio.Wave.IWavePlayer, NAudio'))) 
}

if(-not $writer){
	Write-Host "Using manual copy: read from reader and write bytes"
	$fs = [System.IO.File]::OpenWrite($destFile)
	try{
		# write a simple WAV header: use WaveFileWriter via reflection is safer, but if not available we'll just dump raw PCM to .wav via NAudio's WaveFileWriter; so abort if not available
		# Actually rely on WaveFileWriter.WriteSamples via reflection: use method 'Write' (byte[], int, int)
		$writeMethod = $waveFileWriterType.GetMethod('Write',[Type[]]@([byte[]],[int],[int]))
		if($writeMethod){
			# create instance properly
			$writer = $ctor.Invoke(@($destFile,$waveFormat))
			$buffer = New-Object byte[](2352*10)
			while($true){
				$bytesRead = $reader.Read($buffer,0,$buffer.Length)
				if($bytesRead -le 0){ break }
				$writeMethod.Invoke($writer,@($buffer,0,$bytesRead)) | Out-Null
			}
			# dispose writer if it has Dispose
			$disp = $writer.GetType().GetMethod('Dispose')
			if($disp){ $disp.Invoke($writer, @()) | Out-Null }
		}else{
			Write-Error "Unable to find appropriate write method on WaveFileWriter"; exit 6
		}
	}finally{
		$fs.Close()
	}
}

Write-Host "Extraction complete (fichier: $destFile)"

# Dispose reader if has Dispose
$dispReader = $reader.GetType().GetMethod('Dispose')
if($dispReader){ $dispReader.Invoke($reader, @()) | Out-Null }

Write-Host "Done"
