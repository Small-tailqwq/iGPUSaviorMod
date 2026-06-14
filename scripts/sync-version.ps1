param(
  [string]$Version,
  [switch]$CheckOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$versionFilePath = Join-Path $repoRoot 'version.json'

if (-not (Test-Path -LiteralPath $versionFilePath))
{
  throw "version.json not found at: $versionFilePath"
}

if ($Version)
{
  [ordered]@{ version = $Version } |
    ConvertTo-Json |
    Set-Content -LiteralPath $versionFilePath -Encoding utf8NoBOM
}

$versionData = Get-Content -LiteralPath $versionFilePath -Raw | ConvertFrom-Json
$resolvedVersion = [string]$versionData.version

if ([string]::IsNullOrWhiteSpace($resolvedVersion))
{
  throw "version.json does not contain a valid 'version' value."
}

function Join-RepoPath {
  param([Parameter(Mandatory = $true)][string]$RelativePath)
  Join-Path $repoRoot $RelativePath
}

function Assert-TargetVersion {
  param(
    [Parameter(Mandatory = $true)][string]$RelativePath,
    [Parameter(Mandatory = $true)][string]$Pattern,
    [Parameter(Mandatory = $true)][string]$Label
  )

  $filePath = Join-RepoPath $RelativePath
  if (-not (Test-Path -LiteralPath $filePath))
  {
    throw "Target file not found: $RelativePath"
  }

  $content = Get-Content -LiteralPath $filePath -Raw
  $match = [regex]::Match($content, $Pattern)
  if (-not $match.Success)
  {
    throw "Pattern '$Label' not found in $RelativePath"
  }

  $currentVersion = $match.Groups[1].Value
  if ($currentVersion -ne $resolvedVersion)
  {
    throw "Version mismatch in $RelativePath ($Label): found '$currentVersion', expected '$resolvedVersion'"
  }
}

function Update-TargetVersion {
  param(
    [Parameter(Mandatory = $true)][string]$RelativePath,
    [Parameter(Mandatory = $true)][string]$Pattern,
    [Parameter(Mandatory = $true)][scriptblock]$Replacer,
    [Parameter(Mandatory = $true)][string]$Label
  )

  $filePath = Join-RepoPath $RelativePath
  if (-not (Test-Path -LiteralPath $filePath))
  {
    throw "Target file not found: $RelativePath"
  }

  $content = Get-Content -LiteralPath $filePath -Raw
  $regex = [regex]::new($Pattern)
  if (-not $regex.IsMatch($content))
  {
    throw "Pattern '$Label' not found in $RelativePath"
  }

  $newContent = $regex.Replace(
    $content,
    [System.Text.RegularExpressions.MatchEvaluator]{
      param($m)
      & $Replacer $m
    })

  Set-Content -LiteralPath $filePath -Value $newContent -Encoding utf8NoBOM
}

$versionChecks = @(
  @{ Path = 'iGPU Savior/Core/Constants.cs'; Pattern = 'public const string PluginVersion = "([^"]+)";'; Label = 'Constants.PluginVersion' },
  @{ Path = 'thunderstore/manifest.json'; Pattern = '"version_number"\s*:\s*"([^"]+)"'; Label = 'manifest.version_number' },
  @{ Path = 'CHANGELOG.md'; Pattern = '(?m)^### v(\d+\.\d+\.\d+)（最新版本）- '; Label = 'CHANGELOG latest heading' },
  @{ Path = 'thunderstore/README.md'; Pattern = '(?m)^### v(\d+\.\d+\.\d+)（最新版本）- '; Label = 'thunderstore README latest heading' }
)

if ($CheckOnly)
{
  foreach ($check in $versionChecks)
  {
    Assert-TargetVersion -RelativePath $check.Path -Pattern $check.Pattern -Label $check.Label
  }

  "Version check OK: $resolvedVersion"
  exit 0
}

Update-TargetVersion -RelativePath 'iGPU Savior/Core/Constants.cs' `
  -Pattern 'public const string PluginVersion = "([^"]+)";' `
  -Label 'Constants.PluginVersion' `
  -Replacer { param($m) 'public const string PluginVersion = "' + $resolvedVersion + '";' }

Update-TargetVersion -RelativePath 'thunderstore/manifest.json' `
  -Pattern '"version_number"\s*:\s*"([^"]+)"' `
  -Label 'manifest.version_number' `
  -Replacer { param($m) '"version_number": "' + $resolvedVersion + '"' }

$headingReplacer = {
  param($m)
  "### v$resolvedVersion（最新版本）- $($m.Groups[2].Value)"
}

Update-TargetVersion -RelativePath 'CHANGELOG.md' `
  -Pattern '(?m)^### v(\d+\.\d+\.\d+)（最新版本）- (.+)$' `
  -Label 'CHANGELOG latest heading' `
  -Replacer $headingReplacer

Update-TargetVersion -RelativePath 'thunderstore/README.md' `
  -Pattern '(?m)^### v(\d+\.\d+\.\d+)（最新版本）- (.+)$' `
  -Label 'thunderstore README latest heading' `
  -Replacer $headingReplacer

"Version sync applied: $resolvedVersion"
