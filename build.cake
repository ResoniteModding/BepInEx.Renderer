// Version constants
var bepInExVersion = "5.4.23.3";
var packagePatchNumber = "001"; // Reserved patch number for package updates (000-999)

// Convert to 3-part version with 3-digit patch suffix (Major.Minor.Patch###)
var versionParts = bepInExVersion.Split('.');
var packageVersion = $"{versionParts[0]}.{versionParts[1]}.{versionParts[2]}{versionParts[3]}{packagePatchNumber}";

// Build configuration
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

// Paths
var distDir = Directory("./dist");
var extractDir = distDir + Directory("BepInEx");
var downloadUrl = $"https://github.com/BepInEx/BepInEx/releases/download/v{bepInExVersion}/BepInEx_win_x64_{bepInExVersion}.zip";
var zipFile = distDir + File($"BepInEx_win_x64_{bepInExVersion}.zip");

// Tasks
Task("Clean")
    .Does(() =>
{
    if (DirectoryExists(distDir))
    {
        CleanDirectory(distDir);
    }
    else
    {
        CreateDirectory(distDir);
    }
});

Task("DownloadBepInEx")
    .IsDependentOn("Clean")
    .Does(() =>
{
    Information($"Downloading BepInEx v{bepInExVersion}...");

    if (!FileExists(zipFile))
    {
        DownloadFile(downloadUrl, zipFile);
        Information($"Downloaded to: {zipFile}");
    }
    else
    {
        Information("File already exists, skipping download.");
    }
});

Task("ExtractBepInEx")
    .IsDependentOn("DownloadBepInEx")
    .Does(() =>
{
    Information($"Extracting BepInEx to {extractDir}...");

    if (!DirectoryExists(extractDir))
    {
        CreateDirectory(extractDir);
    }

    Unzip(zipFile, extractDir);
    Information("Extraction completed.");
});

Task("Build")
    .IsDependentOn("ExtractBepInEx")
    .Does(() =>
{
    Information($"Building with package version {packageVersion}...");

    // Run dotnet tcli build command
    var exitCode = StartProcess("dotnet", new ProcessSettings
    {
        Arguments = $"tcli build --package-version {packageVersion}",
        WorkingDirectory = Directory(".")
    });

    if (exitCode != 0)
    {
        throw new Exception($"dotnet tcli build failed with exit code {exitCode}");
    }

    Information("Build completed successfully.");
});

Task("Default")
    .IsDependentOn("Build");

// Execute
RunTarget(target);