param(
  [Parameter(Mandatory = $true)]
  [string]$Ref
)

$ErrorActionPreference = 'Stop'

$tagNameRegex = '^refs/tags/(?<TagName>\S+)$'
$versionRegex = '^v?(?<Major>\d+)\.(?<Minor>\d+)\.(?<Patch>\d+)(?<Suffix>-[A-Za-z0-9-]+)?$'

if ($Ref -notmatch $tagNameRegex) {
  Write-Error 'Ref is not a tag'
  return
}

$tagName = $Matches.TagName

if ($tagName -notmatch $versionRegex) {
  Write-Error 'Tag is not a version'
}

$major = $Matches.Major
$minor = $Matches.Minor
$patch = $Matches.Patch
$suffix = $Matches.Suffix
$suffixStripped = ''
if (![string]::IsNullOrWhiteSpace($suffix)) {
  $suffixStripped = $suffix.Substring(1)
}

Write-Host "::set-output name=tag_name::$tagName"
Write-Host "::set-output name=version_full::$major.$minor.$patch$suffix"
Write-Host "::set-output name=version_major::$major"
Write-Host "::set-output name=version_minor::$minor"
Write-Host "::set-output name=version_patch::$patch"
Write-Host "::set-output name=version_suffix::$suffixStripped"
Write-Host "::set-output name=version_2::$major.$minor"
Write-Host "::set-output name=version_3::$major.$minor.$patch"