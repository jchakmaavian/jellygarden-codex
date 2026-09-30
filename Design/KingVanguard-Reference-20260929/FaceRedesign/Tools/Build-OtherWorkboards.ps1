$ErrorActionPreference='Stop'
$referenceRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$projectRoot=[IO.Path]::GetFullPath((Join-Path $referenceRoot '..\..'))
Add-Type -TypeDefinition ([IO.File]::ReadAllText((Join-Path $PSScriptRoot 'FaceWorkboard.cs'),[Text.Encoding]::UTF8)) -ReferencedAssemblies System.Drawing,System.Web.Extensions,System.Core
$config=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'native-nightmare-face-regions.json') -Raw -Encoding UTF8 | ConvertFrom-Json
foreach($rig in $config.rigs){
    $regions=New-Object 'System.Collections.Generic.List[FaceWorkboard+Region]'
    $ordered=@($rig.regions)
    $boardName='Native'
    $columns=4
    if($rig.name -eq 'Nightmare Vanguard'){
        $boardName='Nightmare'
        $columns=4
        $priority=@('01200_A_Face copy 2','01300_A_nose','01400_A_moustache')
        $ordered=@(foreach($name in $priority){$rig.regions | Where-Object {$_.name -eq $name}})+@($rig.regions | Where-Object {$priority -notcontains $_.name})
    }else{
        $priority=@('01900_kafa','02500_biyik','02700_kas_sag','02900_kas_sol')
        $ordered=@(foreach($name in $priority){$rig.regions | Where-Object {$_.name -eq $name}})+@($rig.regions | Where-Object {$priority -notcontains $_.name})
    }
    foreach($inputRegion in $ordered){
        $r=New-Object 'FaceWorkboard+Region'
        $r.name=$inputRegion.name;$r.x=$inputRegion.x;$r.y=$inputRegion.y;$r.width=$inputRegion.logicalWidth;$r.height=$inputRegion.logicalHeight;$r.rotation=$inputRegion.rotation
        $r.original=@($r.width,$r.height);$r.offset=@(0,0);$regions.Add($r)
    }
    [FaceWorkboard]::BuildRegions((Join-Path $projectRoot $rig.sourceAtlasPath),(Join-Path $PSScriptRoot 'native-nightmare-face-regions.json'),$regions,$rig.atlasWidth,$rig.atlasHeight,(Join-Path $referenceRoot ('FaceRedesign/'+$boardName)),$boardName,$true,$columns,256)
}
