$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'NullTweaker/NullTweaker.csproj'
$artifacts = Join-Path $PSScriptRoot 'artifacts/publish'
$output = Join-Path $PSScriptRoot 'dist/NullTweaker'
dotnet publish $project -c Release -r win-x64 --self-contained true -p:RuntimeFrameworkVersion=10.0.11 -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:UseArtifactsOutput=true "-p:ArtifactsPath=$artifacts" -o $output
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.md') -Destination $output
