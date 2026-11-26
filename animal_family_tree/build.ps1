# Builds the mod assembly for every supported RimWorld version.
$ErrorActionPreference = "Stop"
foreach ($version in "1.6", "1.7") {
    Remove-Item -Recurse -Force "$PSScriptRoot\$version\Assemblies\*" -ErrorAction SilentlyContinue
    dotnet build "$PSScriptRoot\Source\AnimalFamilyTree.csproj" -c Release -p:GameVersion=$version
    if ($LASTEXITCODE -ne 0) { throw "Build for RimWorld $version failed" }
}
