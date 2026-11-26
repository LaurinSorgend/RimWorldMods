# Builds the mod assembly for every supported RimWorld version.
$ErrorActionPreference = "Stop"
$project = Join-Path $PSScriptRoot "Source/AnimalFamilyTree.csproj"
foreach ($version in "1.6", "1.7") {
    $assemblies = Join-Path $PSScriptRoot "$version/Assemblies"
    if (Test-Path $assemblies) { Remove-Item -Recurse -Force (Join-Path $assemblies "*") }
    dotnet build $project -c Release -p:GameVersion=$version
    if ($LASTEXITCODE -ne 0) { throw "Build for RimWorld $version failed" }
}
