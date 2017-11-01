#addin "nuget:?package=Cake.Wyam"
#tool "nuget:?package=GitVersion.CommandLine"
#tool "nuget:?package=NUnit.ConsoleRunner"
#tool "nuget:?package=Wyam"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

private string SolutionFile() { return "./KVLite.sln"; }
private string ArtifactsDir() { return "./artifacts"; }
private string MSBuildLinuxPath() { return @"/usr/lib/mono/msbuild/15.0/bin/MSBuild.dll"; }

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectory(ArtifactsDir());
});

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    Restore();
});

Task("Version")
    .IsDependentOn("Restore")
    .Does(() =>
{
    Version();
});

Task("Build-Debug")
    .IsDependentOn("Restore")
    .Does(() => 
{
    Build("Debug");
});

Task("Build-Release")
    .IsDependentOn("Build-Debug")
    .Does(() => 
{
    Build("Release");
});

Task("Pack-Release")
    .IsDependentOn("Build-Release")
    .Does(() => 
{
    Pack("Release");
});

Task("Docs")
    .IsDependentOn("Pack-Release")
    .Does(() =>
{
    Docs();
});

Task("Test-Debug")
    .IsDependentOn("Docs")
    .Does(() =>
{
    Test("Debug");
});

Task("Test-Release")
    .IsDependentOn("Test-Debug")
    .Does(() =>
{
    Test("Release");
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Pack-Release");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);

//////////////////////////////////////////////////////////////////////
// HELPERS
//////////////////////////////////////////////////////////////////////

private void Restore()
{
    //DotNetCoreRestore();

    MSBuild(SolutionFile(), settings =>
    {
        settings.SetMaxCpuCount(0);
        settings.SetVerbosity(Verbosity.Quiet);
        settings.WithTarget("restore");
        if (!IsRunningOnWindows())
        { 
            // Hack for Linux bug - Missing MSBuild path.
            settings.ToolPath = new FilePath(MSBuildLinuxPath());
        }
    });
}

private void Version()
{
    var versionInfo = GitVersion();
    var buildVersion = EnvironmentVariable("APPVEYOR_BUILD_NUMBER") ?? "0";
    var assemblyVersion =  versionInfo.Major + ".0.0.0"; // Minor and Patch versions should work with base Major version
    var fileVersion = versionInfo.MajorMinorPatch + "." + buildVersion;
    var informationalVersion = versionInfo.FullSemVer;
    var nuGetVersion = versionInfo.NuGetVersion;

    Information("BuildVersion: " + buildVersion);
    Information("AssemblyVersion: " + assemblyVersion);
    Information("FileVersion: " + fileVersion);
    Information("InformationalVersion: " + informationalVersion);
    Information("NuGetVersion: " + nuGetVersion);
    
    if (AppVeyor.IsRunningOnAppVeyor)
    {
        AppVeyor.UpdateBuildVersion(informationalVersion + ".build." + buildVersion);
    }	
    
    Information("Updating Directory.build.props...");

    var dbp = File("./Directory.build.props");
    XmlPoke(dbp, "/Project/PropertyGroup/Version", nuGetVersion);
    XmlPoke(dbp, "/Project/PropertyGroup/AssemblyVersion", assemblyVersion);
    XmlPoke(dbp, "/Project/PropertyGroup/FileVersion", fileVersion);
    XmlPoke(dbp, "/Project/PropertyGroup/InformationalVersion", informationalVersion);
}

private void Build(string cfg)
{
    //DotNetCoreBuild(SolutionFile(), new DotNetCoreBuildSettings
    //{
    //    Configuration = cfg,
    //    NoIncremental = true
    //});

    MSBuild(SolutionFile(), settings =>
    {
        settings.SetConfiguration(cfg);
        settings.SetMaxCpuCount(0);
        settings.SetVerbosity(Verbosity.Quiet);
        if (!IsRunningOnWindows())
        { 
            // Hack for Linux bug - Missing MSBuild path.
            settings.ToolPath = new FilePath(MSBuildLinuxPath());
        }
    });
}

private void Test(string cfg)
{
    //NUnit3("./test/**/bin/{cfg}/*/*.UnitTests.dll".Replace("{cfg}", cfg), new NUnit3Settings 
    //{
    //    NoResults = true
    //});

    const string flags = "--noheader --noresult --stoponerror";
    const string errMsg = " - Unit test failure - ";

    Parallel.ForEach(GetFiles("./test/*.UnitTests/**/bin/{cfg}/*/*.UnitTests.exe".Replace("{cfg}", cfg)), netExe => 
    {
        if (StartProcess(netExe, flags) != 0)
        {
            throw new Exception(cfg + errMsg + netExe);
        }
    });

    Parallel.ForEach(GetFiles("./test/*.UnitTests/**/bin/{cfg}/*/*.UnitTests.dll".Replace("{cfg}", cfg)), netCoreDll =>
    {
        DotNetCoreExecute(netCoreDll, flags);
    });
}

private void Pack(string cfg)
{
    Parallel.ForEach(GetFiles("./src/**/*.csproj"), project =>
    {
        //DotNetCorePack(project.FullPath, new DotNetCorePackSettings
        //{
        //    Configuration = cfg,
        //    OutputDirectory = ArtifactsDir(),
        //    NoBuild = true,
        //    IncludeSource = true,
        //    IncludeSymbols = true
        //});

        MSBuild(project, settings =>
        {
            settings.SetConfiguration(cfg);
            settings.SetMaxCpuCount(0);
            settings.SetVerbosity(Verbosity.Quiet);
            settings.WithTarget("pack");
            settings.WithProperty("IncludeSource", new[] { "true" });
            settings.WithProperty("IncludeSymbols", new[] { "true" });
            if (!IsRunningOnWindows())
            { 
                // Hack for Linux bug - Missing MSBuild path.
                settings.ToolPath = new FilePath(MSBuildLinuxPath());
            }
        });

        var packDir = project.GetDirectory().Combine("bin").Combine(cfg);
        MoveFiles(GetFiles(packDir + "/*.nupkg"), ArtifactsDir());
    });
}

private void Docs()
{
    if (IsRunningOnWindows())
    {
        Wyam(new WyamSettings()
        {
            InputPaths = new DirectoryPath[] { Directory("./pages") },
            OutputPath = Directory("./docs")
        });
    }
}