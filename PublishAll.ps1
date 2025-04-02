$publishProfilesDir = "Website/Properties/PublishProfiles"
$pubxmlFiles = Get-ChildItem -Path $publishProfilesDir -Filter "*.pubxml"
foreach ($file in $pubxmlFiles) {
    $profilePath = $file.FullName
    Write-Host "Publishing profile: $profilePath"
    dotnet publish Website -p:PublishProfile=$profilePath --tl:off -v q
}
