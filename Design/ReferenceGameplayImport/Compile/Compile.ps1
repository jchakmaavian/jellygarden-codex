$ErrorActionPreference='Stop'
$project=(Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$destination=$PSScriptRoot
$dotnet='C:/Program Files/Unity/Hub/Editor/6000.5.10f1/Editor/Data/DotNetSdk/dotnet.exe'
$compiler='C:/Program Files/Unity/Hub/Editor/6000.5.10f1/Editor/Data/DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll'
Push-Location $project
try {
  foreach($assembly in @('Assembly-CSharp','Assembly-CSharp-Editor')) {
    $rsp=Get-Content -LiteralPath "Library/Bee/artifacts/1300b0aE.dag/$assembly.rsp"
    $rsp=@($rsp | Where-Object {$_ -notmatch '^(-out:|-refout:|/analyzer:|-analyzer:|/additionalfile:)'})
    if($assembly -eq 'Assembly-CSharp-Editor') {
      $rsp=@($rsp | ForEach-Object {$_ -replace 'Library/Bee/artifacts/1300b0aE.dag/Assembly-CSharp.ref.dll', "$destination/Assembly-CSharp.ref.dll"})
    }
    $rsp+=('-out:"'+$destination+'/'+$assembly+'.dll"')
    $rsp+=('-refout:"'+$destination+'/'+$assembly+'.ref.dll"')
    foreach($file in Get-ChildItem -LiteralPath 'Assets/ReferenceGameplay' -Recurse -Filter '*.cs') {
      $editor=$file.FullName -match '\\Editor\\'
      if(($assembly -eq 'Assembly-CSharp-Editor') -ne $editor){continue}
      $relative=$file.FullName.Substring($project.Length+1).Replace('\','/')
      if($rsp -notcontains ('"'+$relative+'"')){$rsp+=('"'+$relative+'"')}
    }
    $path=Join-Path $destination ($assembly+'.rsp')
    $rsp|Set-Content -LiteralPath $path -Encoding utf8
    & $dotnet $compiler ('@'+$path) 2>&1 | Tee-Object -FilePath (Join-Path $destination ($assembly+'.log'))
    if($LASTEXITCODE -ne 0){throw "$assembly compile failed"}
  }
} finally {Pop-Location}
