param(
  [Parameter(Mandatory = $true)]
  [string]$Ref
)

$ErrorActionPreference = 'Stop'

function Write-GitHubOutput([string]$Name, [string]$Value) {
  Write-Output "$Name=$Value" | Out-File -FilePath $env:GITHUB_ENV -Encoding utf8 -Append
}

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

Write-GitHubOutput -Name 'tag_name' -Value $tagName
Write-GitHubOutput -Name 'version_full' -Value "$major.$minor.$patch$suffix"
Write-GitHubOutput -Name 'version_major' -Value $major
Write-GitHubOutput -Name 'version_minor' -Value $minor
Write-GitHubOutput -Name 'version_patch' -Value $patch
Write-GitHubOutput -Name 'version_suffix' -Value $suffixStripped
Write-GitHubOutput -Name 'version_2' -Value "$major.$minor"
Write-GitHubOutput -Name 'version_3' -Value "$major.$minor.$patch"
