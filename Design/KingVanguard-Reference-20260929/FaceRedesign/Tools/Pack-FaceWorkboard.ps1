[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][ValidateSet('Portrait','Area','Native','Nightmare')][string]$Rig,
    [Parameter(Mandatory=$true)][string]$PaintedBoard,
    [switch]$PreserveTechnicalShadows,
    [switch]$PreserveSourceCoverage
)
$ErrorActionPreference='Stop'
$referenceRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$projectRoot=[IO.Path]::GetFullPath((Join-Path $referenceRoot '..\..'))
Add-Type -TypeDefinition ([IO.File]::ReadAllText((Join-Path $PSScriptRoot 'FaceWorkboard.cs'),[Text.Encoding]::UTF8)) -ReferencedAssemblies System.Drawing,System.Web.Extensions,System.Core
$layoutRig=$Rig
$targetAtlas=''
switch($Rig){
    'Portrait' {$base='Assets/Characters/KingVenguard/V2/Spine/King_Vanguard.png';$source='Original-rfv387/King.png';$output='King_Vanguard.png'}
    'Area' {$layoutRig='Portrait';$base='Assets/Characters/KingVenguard/V2/Spine/AreaCompleted_Vanguard.png';$source='Original-rfv387/AreaCompleted.png';$targetAtlas=Join-Path $referenceRoot 'Original-rfv387/AreaCompleted.atlas';$output='AreaCompleted_Vanguard.png'}
    'Native' {$base='Assets/Characters/KingVenguard/V2/Activities/KingDrill/Textures/KingDrill_Vanguard.png';$source='EmbeddedKingBundles/Recovered/kingdrill_v3/Textures/sactx-0-1024x2048-ETC2-KingDrillAtlas-89d7723c__4934485327719105542.png';$output='KingDrill_Vanguard.png'}
    'Nightmare' {$base='Assets/Characters/KingVenguard/V2/Spine/Nightmare_Vanguard.png';$source='Original-rfv387/nightmare_popup.png';$output='Nightmare_Vanguard.png'}
}
$layout=Join-Path $referenceRoot ('FaceRedesign/'+$layoutRig+'/'+$layoutRig+'-layout.json')
$outputPath=Join-Path $referenceRoot ('FaceRedesign/Packed/'+$output)
[FaceWorkboard]::Pack($layout,[IO.Path]::GetFullPath($PaintedBoard),(Join-Path $projectRoot $base),(Join-Path $referenceRoot $source),$targetAtlas,$outputPath,$PreserveTechnicalShadows.IsPresent,$PreserveSourceCoverage.IsPresent)
