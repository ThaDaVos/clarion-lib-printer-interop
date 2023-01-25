# this script needs https://www.nuget.org/packages/ilmerge

# set your target executable name (typically [projectname].exe)
$OUTPUT_NAME = "PrinterInteropClarion.dll"
$APP_NAME = "PrinterInterop.dll"
$DNNE_NAME = "PrinterInteropNE.dll"
$RUNTIME_IDENTIFIER = "win-x86"
$SDK_TARGET = "net7.0-windows"

# Set build, used for directory. Typically Release or Debug
$ILMERGE_BUILD = "Release"

# Set platform, typically x64
$ILMERGE_PLATFORM = "x86"

# set your NuGet ILMerge Version, this is the number from the package manager install, for example:
# PM> Install-Package ilmerge -Version 3.0.29
# to confirm it is installed for a given project, see the packages.config file
$ILMERGE_VERSION = "3.0.41"

# the full ILMerge should be found here:
$ILMERGE_PATH = "$HOME\.nuget\packages\ilmerge\$ILMERGE_VERSION\tools\net452"
# dir "$ILMERGE_PATH"\ILMerge.exe

$OUTPUT_PATH = "Bin\$ILMERGE_PLATFORM\$ILMERGE_BUILD\$SDK_TARGET\$RUNTIME_IDENTIFIER"

$LIBS = "$((Get-ChildItem -Path "$OUTPUT_PATH" -Include *.dll).FullName -join ' ')"

Write-Output "$LIBS";

Write-Output "Merging $OUTPUT_NAME ..."

# add project DLL's starting with replacing the FirstLib with this project's DLL
try {
    & "$ILMERGE_PATH\ILMerge.exe" /lib:"$OUTPUT_PATH\" /out:"$OUTPUT_PATH\$OUTPUT_NAME" "$APP_NAME" "$LIBS"

    if ($LASTEXITCODE -ne 0) {
        throw "Exception caught."
        return
    }

    $PUBLISH_PATH = "$OUTPUT_PATH\publish";

    If (!(Test-Path -PathType container $PUBLISH_PATH)) {
        New-Item -ItemType Directory -Path $PUBLISH_PATH
    }

    Copy-Item -Force "$OUTPUT_PATH\$OUTPUT_NAME" "$PUBLISH_PATH\$APP_NAME";
    Copy-Item -Force "$OUTPUT_PATH\$DNNE_NAME" "$PUBLISH_PATH\$DNNE_NAME";
    Copy-Item -Force "$OUTPUT_PATH\*runtimeconfig.json" "$PUBLISH_PATH\";
    Copy-Item -Force "$OUTPUT_PATH\*deps.json" "$PUBLISH_PATH\";
}
Catch {
    Write-Error $_.Exception;
}

pause