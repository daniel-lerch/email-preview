$sdkPath = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22000.0\x64"
$executableName = "EmailPreview.exe"
$executablePdbName = "EmailPreview.pdb"
$targetFramework = "net7.0-windows"
$version = "1.0.0.0"

$projectPath = $PSScriptRoot
$intermediateRootPath = Join-Path $projectPath "pkg\obj"
$intermediateAppxPath = Join-Path $intermediateRootPath "appx"
$intermediateBundlePath = Join-Path $intermediateRootPath "bundle"
$imagesPath = Join-Path $projectPath "pkg\Images"
$certificatePath = Join-Path $projectPath ".\pkg\EmailPreview_TemporaryKey.pfx"

<#
Self signed certificate can be created with
New-SelfSignedCertificate -Type Custom -Subject "CN=FDFCA12E-D0B5-4F1F-869D-7F97CD35B39E" -KeyUsage DigitalSignature -FriendlyName "Daniel Lerch" -CertStoreLocation "Cert:\CurrentUser\My" -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")
Export-PfxCertificate -Cert "Cert:\CurrentUser\My\EF3EC4EA215E87BA449B9F6A46DC03AEC7B365CB" -FilePath .\pkg\EmailPreview_TemporaryKey.pfx -Password (New-Object System.Security.SecureString)
#>

$platformX64 = [PSCustomObject]@{
    Architecture = "x64"
    Runtime = "win-x64"
    AppxManifestFile = Join-Path $projectPath "pkg\AppxManifest.x64.xml"
}
$platformArm64 = [PSCustomObject]@{
    Architecture = "arm64"
    Runtime = "win-arm64"
    AppxManifestFile = Join-Path $projectPath "pkg\AppxManifest.arm64.xml"
}

function BuildPlatform ($Platform) {
    # .NET build
    & dotnet publish .\src\EmailPreview\EmailPreview.csproj -c Release -r $Platform.Runtime
    
    # Copy built executable and image resources to intermediate directory
    $publishPath = Join-Path $projectPath "src\EmailPreview\bin\Release\$targetFramework\$($Platform.Runtime)\publish"
    $intermediatePath = Join-Path $intermediateRootPath $Platform.Architecture
    New-Item -ItemType Directory -Path $intermediatePath -Force | Out-Null
    Copy-Item -Path (Join-Path $publishPath $executableName) -Destination $intermediatePath -Force
    Copy-Item -Path $Platform.AppxManifestFile -Destination (Join-Path $intermediatePath "AppxManifest.xml") -Force
    $imagePublishPath = (Join-Path $intermediatePath "Images")
    New-Item -ItemType Directory -Path $imagePublishPath -Force | Out-Null
    Get-ChildItem -Path $imagesPath
        | Where-Object -FilterScript { $_.Name.Contains(".scale-200") }
        | ForEach-Object { Copy-Item -Path $_.FullName -Destination (Join-Path $imagePublishPath $_.Name.Replace(".scale-200", "")) }
    Get-ChildItem -Path $imagesPath
        | Where-Object -FilterScript { $_.Name.Contains(".targetsize-") -or $_.Name.Contains(".altform-") }
        | ForEach-Object { Copy-Item -Path $_.FullName -Destination (Join-Path $imagePublishPath $_.Name) }

    # Build package resources
    & $sdkPath\makepri.exe new /pr .\pkg\obj\$($Platform.Architecture) /cf .\pkg\priconfig.xml /of .\pkg\obj\$($Platform.Architecture)\resources.pri /o
    
    # Build and sign APPX package
    $appxPath = Join-Path $intermediateAppxPath "EmailPreview_$($Platform.Architecture).appx"
    & $sdkPath\makeappx.exe pack /d $intermediatePath /p $appxPath /o
    & $sdkPath\signtool.exe sign /fd SHA256 /f $certificatePath $appxPath

    # Create debug symbols file
    Compress-Archive -Path (Join-Path $publishPath $executablePdbName) -DestinationPath (Join-Path $intermediateBundlePath "EmailPreview_$($Platform.Architecture).appxsym") -Force
}

New-Item -ItemType Directory -Path $intermediateBundlePath -Force | Out-Null

BuildPlatform $platformX64
BuildPlatform $platformArm64

# Create APPX bundle
$bundleFilePath = Join-Path $intermediateBundlePath "EmailPreview.appxbundle"
& $sdkPath\makeappx.exe bundle /d $intermediateAppxPath /bv $version /p $bundleFilePath /o
& $sdkPath\signtool.exe sign /fd SHA256 /f $certificatePath $bundleFilePath

# Create APPX upload file for Microsoft Store
Compress-Archive -Path (Get-ChildItem -Path $intermediateBundlePath) -DestinationPath .\EmailPreview.appxupload -Force
