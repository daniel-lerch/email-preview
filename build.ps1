$sdkPath = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22000.0\x64"
$executableName = "EmailPreview.exe"
$executablePdbName = "EmailPreview.pdb"
$certificatePath = "C:\Users\Daniel\source\vocup\src\Vocup.Packaging\Vocup.Packaging_TemporaryKey.pfx"

$projectPath = $PSScriptRoot
$intermediateRootPath = Join-Path $projectPath "pkg\obj"
$intermediateAppxPath = Join-Path $intermediateRootPath "appx"
$intermediateBundlePath = Join-Path $intermediateRootPath "bundle"
$imagesPath = Join-Path $projectPath "pkg\Images"

$platformX64 = [PSCustomObject]@{
    Architecture = "x64"
    Runtime = "win-x64"
    PublishPath = Join-Path $projectPath "src\EmailPreview\bin\Release\net7.0-windows\win-x64\publish"
    AppxManifestFile = Join-Path $projectPath "pkg\AppxManifest.x64.xml"
}
$platformArm64 = [PSCustomObject]@{
    Architecture = "arm64"
    Runtime = "win-arm64"
    PublishPath = Join-Path $projectPath "src\EmailPreview\bin\Release\net7.0-windows\win-arm64\publish"
    AppxManifestFile = Join-Path $projectPath "pkg\AppxManifest.arm64.xml"
}

function BuildPlatform ($Platform) {
    & dotnet publish .\src\EmailPreview\EmailPreview.csproj -c Release -r $Platform.Runtime
    $intermediatePath = Join-Path $intermediateRootPath $Platform.Architecture
    New-Item -ItemType Directory -Path $intermediatePath -Force | Out-Null
    Copy-Item -Path (Join-Path $Platform.PublishPath $executableName) -Destination $intermediatePath -Force
    Copy-Item -Path $Platform.AppxManifestFile -Destination (Join-Path $intermediatePath "AppxManifest.xml") -Force
    $imagePublishPath = (Join-Path $intermediatePath "Images")
    New-Item -ItemType Directory -Path $imagePublishPath -Force | Out-Null
    Get-ChildItem -Path $imagesPath
        | Where-Object -FilterScript { $_.Name.Contains(".scale-200") }
        | ForEach-Object { Copy-Item -Path $_.FullName -Destination (Join-Path $imagePublishPath $_.Name.Replace(".scale-200", "")) }
    $appxPath = Join-Path $intermediateAppxPath "EmailPreview_$($Platform.Architecture).appx"
    & $sdkPath\makeappx.exe pack /d $intermediatePath /p $appxPath
    & $sdkPath\signtool.exe sign /fd SHA256 /f $certificatePath $appxPath
    Compress-Archive -Path (Join-Path $Platform.PublishPath $executablePdbName) -DestinationPath (Join-Path $intermediateBundlePath "EmailPreview_$($Platform.Architecture).appxsym") -Force
}

New-Item -ItemType Directory -Path $intermediateBundlePath -Force | Out-Null

BuildPlatform $platformX64
BuildPlatform $platformArm64

$bundleFilePath = Join-Path $intermediateBundlePath "EmailPreview.appxbundle"
& $sdkPath\makeappx.exe bundle /d $intermediateAppxPath /p $bundleFilePath
& $sdkPath\signtool.exe sign /fd SHA256 /f $certificatePath $bundleFilePath

Compress-Archive -Path (Get-ChildItem -Path $intermediateBundlePath) -DestinationPath .\EmailPreview.appxupload -Force
