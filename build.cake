//////////////////////////////////////////////////////////////////////
// TOOLS
//////////////////////////////////////////////////////////////////////
#tool "nuget:?package=GitVersion.CommandLine.DotNetCore&version=4.0.0"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// GitVersion
var gitVersion = GitVersion();

// Build Configuration
var configuration = EnvironmentVariable("CONFIGURATION") ?? "Release";
var isAppVeyorBuild = EnvironmentVariable("APPVEYOR") == "True";
var isPullRequest = EnvironmentVariable("APPVEYOR_PULL_REQUEST_NUMBER") != null;

// File/Directory paths
var artifactDirectory = MakeAbsolute(Directory("./artifacts")).FullPath;
var solutionFile = "./src/CacheManager.FileCaching.sln";

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .WithCriteria(!isAppVeyorBuild)
    .Does(() =>
{
    CleanDirectories($"./src/**/obj/{configuration}");
    CleanDirectories($"./src/**/bin/{configuration}");
    CleanDirectories("./artifacts");
});

Task("CleanAll")
    .WithCriteria(!isAppVeyorBuild)
    .Does(() =>
{
    CleanDirectories("./src/**/obj");
    CleanDirectories("./src/**/bin");
    CleanDirectories("./artifacts");
});

Task("Restore-NuGet-Packages")
    .Does(() =>
{
    var settings = new DotNetCoreRestoreSettings();

    DotNetCoreRestore(solutionFile, settings);
});

Task("Build")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
	var settings = new DotNetCoreBuildSettings
    {
        NoRestore = true,
        Configuration = configuration,
        MSBuildSettings = new DotNetCoreMSBuildSettings().WithProperty("Version", gitVersion.FullSemVer)
    };

	DotNetCoreBuild(solutionFile, settings);
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    DotNetCoreTest("./src/CacheManager.FileCaching.UnitTests/", new DotNetCoreTestSettings{
        NoRestore = true,
        NoBuild = true, // Build will fail for the gitversion task, dotnet build does not yet play nicely with gitverion, until gitversion adds support
        Configuration = configuration,
        DiagnosticOutput = true
    });
});

Task("Create-NuGet-Packages")
    .WithCriteria(!isPullRequest)
    .IsDependentOn("Build")
    .Does(() =>
{
    var settings = new DotNetCorePackSettings{
        NoRestore = true,
        NoBuild = true,
        Configuration = configuration,
        OutputDirectory = artifactDirectory,
        MSBuildSettings = new DotNetCoreMSBuildSettings().WithProperty("Version", gitVersion.FullSemVer)
    };
    DotNetCorePack(solutionFile, settings);
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore-NuGet-Packages")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Create-NuGet-Packages");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);