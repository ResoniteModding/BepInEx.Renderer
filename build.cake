var target = Argument("target", "Default");

var _http = CreateHttpClient();

System.Net.Http.HttpClient CreateHttpClient()
{
	var handler = new System.Net.Http.HttpClientHandler { AllowAutoRedirect = true };
	var client = new System.Net.Http.HttpClient(handler);
	client.DefaultRequestHeaders.UserAgent.ParseAdd("BepInEx.Renderer-Build");
	return client;
}

string FetchUrl(string url) =>
	_http.GetStringAsync(url).GetAwaiter().GetResult();

var packages = new PackageConfig[]
{
	new PackageConfig
	{
		Name        = "BepInExRenderer",
		DisplayName = "Mono",
		PackageDir  = Directory("./packages/BepInExRenderer"),
		DistDir     = Directory("./packages/BepInExRenderer/dist")
	},
	new PackageConfig
	{
		Name        = "BepInExRenderer.IL2CPP",
		DisplayName = "IL2CPP",
		PackageDir  = Directory("./packages/BepInExRenderer.IL2CPP"),
		DistDir     = Directory("./packages/BepInExRenderer.IL2CPP/dist")
	}
};

Task("Resolve")
	.Does(() =>
{
	ResolveMono();
	ResolveIL2CPP();
});

void ResolveMono()
{
	var monoJson = FetchUrl("https://api.github.com/repos/BepInEx/BepInEx/releases/latest");
	var monoMatch = System.Text.RegularExpressions.Regex.Match(
		monoJson, @"""tag_name""\s*:\s*""v?(\d+\.\d+\.\d+\.\d+)""");
	if (!monoMatch.Success)
		throw new Exception("Failed to parse BepInEx 5 version from GitHub API response.");
	var monoVer = monoMatch.Groups[1].Value;
	var monoParts = monoVer.Split('.');
	if (monoParts.Length < 4)
		throw new Exception($"Unexpected BepInEx version format: '{monoVer}' (expected A.B.C.D).");
	var monoPkgVer = $"{monoParts[0]}.{monoParts[1]}.{monoParts[2]}{monoParts[3]}001";
	Information($"Mono  : BepInEx {monoVer} -> package v{monoPkgVer}");

	packages[0].PackageVersion = monoPkgVer;
	packages[0].DownloadUrl = $"https://github.com/BepInEx/BepInEx/releases/download/v{monoVer}/BepInEx_win_x64_{monoVer}.zip";
	packages[0].ZipFileName = $"BepInEx_win_x64_{monoVer}.zip";
}

void ResolveIL2CPP()
{
	var il2cppHtml = FetchUrl("https://builds.bepinex.dev/projects/bepinex_be");
	var il2cppMatch = System.Text.RegularExpressions.Regex.Match(
		il2cppHtml,
		@"href=""[^""]*?/(\d+)/BepInEx-Unity\.IL2CPP-win-x64-6\.0\.0-be\.\d+%2B([a-f0-9]+)\.zip""");
	if (!il2cppMatch.Success)
		throw new Exception("Failed to parse BepInEx 6 BE version from builds.bepinex.dev.");
	var il2cppBuild = il2cppMatch.Groups[1].Value;
	var il2cppCommit = il2cppMatch.Groups[2].Value;
	var il2cppPkgVer = $"6.0.{il2cppBuild}";
	Information($"IL2CPP: build {il2cppBuild} ({il2cppCommit}) -> package v{il2cppPkgVer}");

	packages[1].PackageVersion = il2cppPkgVer;
	packages[1].DownloadUrl = $"https://builds.bepinex.dev/projects/bepinex_be/{il2cppBuild}/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.{il2cppBuild}%2B{il2cppCommit}.zip";
	packages[1].ZipFileName = $"BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.{il2cppBuild}+{il2cppCommit}.zip";
}

foreach (var pkg in packages)
{
	Task($"Clean{pkg.Name}")
		.Does(() =>
	{
		Information($"Cleaning {pkg.DisplayName} dist directory...");
		EnsureDirectoryExists(pkg.DistDir);
		CleanDirectory(pkg.DistDir);
	});

	Task($"Download{pkg.Name}")
		.IsDependentOn("Resolve")
		.Does(() =>
	{
		EnsureDirectoryExists(pkg.DistDir);
		var zipFile = pkg.DistDir.CombineWithFilePath(pkg.ZipFileName);
		if (FileExists(zipFile))
		{
			Information($"{pkg.DisplayName}: zip cached, skipping download.");
			return;
		}
		Information($"Downloading {pkg.DisplayName} ...");
		DownloadFile(pkg.DownloadUrl, zipFile);
		Information($"Saved to: {zipFile}");
	});

	Task($"Extract{pkg.Name}")
		.IsDependentOn($"Download{pkg.Name}")
		.Does(() =>
	{
		var extractDir = pkg.DistDir.Combine("BepInEx");
		Information($"Extracting {pkg.DisplayName} to {extractDir} ...");
		if (DirectoryExists(extractDir))
			CleanDirectory(extractDir);
		else
			EnsureDirectoryExists(extractDir);
		Unzip(pkg.DistDir.CombineWithFilePath(pkg.ZipFileName), extractDir);
		Information($"{pkg.DisplayName} extraction complete.");
	});

	Task($"Build{pkg.Name}")
		.IsDependentOn($"Extract{pkg.Name}")
		.Does(() =>
	{
		Information($"Building {pkg.DisplayName} v{pkg.PackageVersion} ...");
		var exitCode = StartProcess("dotnet", new ProcessSettings
		{
			Arguments = $"tcli build --package-version {pkg.PackageVersion}",
			WorkingDirectory = pkg.PackageDir
		});
		if (exitCode != 0)
			throw new Exception($"tcli build failed for {pkg.DisplayName} (exit {exitCode}).");
		Information($"{pkg.DisplayName} build complete.");
	});
}

Task("Clean")
	.IsDependentOn("CleanBepInExRenderer")
	.IsDependentOn("CleanBepInExRenderer.IL2CPP");

Task("Build")
	.IsDependentOn("Resolve")
	.IsDependentOn("BuildBepInExRenderer")
	.IsDependentOn("BuildBepInExRenderer.IL2CPP");

Task("Default")
	.IsDependentOn("Build");

RunTarget(target);

public class PackageConfig
{
	public string Name { get; set; }
	public string DisplayName { get; set; }
	public DirectoryPath PackageDir { get; set; }
	public DirectoryPath DistDir { get; set; }
	public string PackageVersion { get; set; }
	public string DownloadUrl { get; set; }
	public string ZipFileName { get; set; }
}
