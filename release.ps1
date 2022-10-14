$releasePath = $PSScriptRoot + "\src\bin\Release"
$dist = $PSScriptRoot + "\dist"
$pluginPath = $dist + "\BepInEx\plugins"

# Create releases ---------
function CreateZip ($pluginFile)
{
    New-Item -ItemType Directory -Force -Path $pluginPath

    Copy-Item -Path $pluginFile.FullName -Destination $pluginPath -Recurse -Force 

    # the replace removes .0 from the end of version up until there are 3 version parts remaining (e.g. v1.0.0 v1.0.1)
    $ver = (Get-ChildItem -Path ($pluginPath) -Filter "*.dll" -Recurse -Force)[0].VersionInfo.FileVersion.ToString() -replace "^(\d+.\d+.\d+)[\.0]*$", '${1}'

    Compress-Archive -Path ($pluginPath + "\..\") -Force -CompressionLevel "Optimal" -DestinationPath ($dist + "\" + $pluginFile.BaseName + "-" + $ver + ".zip")
}

foreach ($pluginFile in Get-ChildItem -Path $releasePath)
{
    try
    {
        CreateZip ($pluginFile)
    }
    catch 
    {
        # retry
        CreateZip ($pluginFile)
    }
}

Remove-Item -Force -Path ($pluginPath + "\..\") -Recurse