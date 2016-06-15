//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Debug");

#tool "nuget:?package=NUnit.ConsoleRunner"

//////////////////////////////////////////////////////////////////////
// SET PACKAGE VERSION
//////////////////////////////////////////////////////////////////////

var version = "3.3.0";
var modifier = "";

var isAppveyor = BuildSystem.IsRunningOnAppVeyor;
var dbgSuffix = configuration == "Debug" ? "-dbg" : "";
var packageVersion = version + modifier + dbgSuffix;

//////////////////////////////////////////////////////////////////////
// DEFINE RUN CONSTANTS
//////////////////////////////////////////////////////////////////////

var PROJECT_DIR = Context.Environment.WorkingDirectory.FullPath + "/";
var PACKAGE_DIR = PROJECT_DIR + "package/";
var BIN_DIR = PROJECT_DIR + "bin/" + configuration + "/";
var IMAGE_DIR = PROJECT_DIR + "images/";

var SOLUTION_FILE = "./nunit.portable.agent.sln";

// Package sources for nuget restore
var PACKAGE_SOURCE = new string[]
	{
		"https://www.nuget.org/api/v2",
		"https://www.myget.org/F/nunit/api/v2"
	};

// Test Assemblies
var PORTABLE_AGENT_TESTS = BIN_DIR + "agents/nunit.portable.agent.tests.dll";


//////////////////////////////////////////////////////////////////////
// CLEAN
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
    {
        CleanDirectory(BIN_DIR);
    });


//////////////////////////////////////////////////////////////////////
// INITIALIZE FOR BUILD
//////////////////////////////////////////////////////////////////////

Task("InitializeBuild")
    .Does(() =>
    {
		NuGetRestore(SOLUTION_FILE, new NuGetRestoreSettings()
		{
			Source = PACKAGE_SOURCE
		});

		if (BuildSystem.IsRunningOnAppVeyor)
		{
			var tag = AppVeyor.Environment.Repository.Tag;

			if (tag.IsTag)
			{
				packageVersion = tag.Name;
			}
			else
			{
				var buildNumber = AppVeyor.Environment.Build.Number;
				packageVersion = version + "-CI-" + buildNumber + dbgSuffix;
				if (AppVeyor.Environment.PullRequest.IsPullRequest)
					packageVersion += "-PR-" + AppVeyor.Environment.PullRequest.Number;
				else if (AppVeyor.Environment.Repository.Branch.StartsWith("release", StringComparison.OrdinalIgnoreCase))
					packageVersion += "-PRE-" + buildNumber;
				else
					packageVersion += "-" + AppVeyor.Environment.Repository.Branch;
			}

			AppVeyor.UpdateBuildVersion(packageVersion);
		}
	});

//////////////////////////////////////////////////////////////////////
// BUILD
//////////////////////////////////////////////////////////////////////

Task("Build")
    .IsDependentOn("InitializeBuild")
    .WithCriteria(IsRunningOnWindows)
    .Does(() =>
    {
        // Driver and tests
        BuildProject("src/NUnitFramework/mock-assembly/mock-assembly-4.5.csproj", configuration);
        BuildProject("./src/NUnitEngine/Portable/nunit.portable.agent/nunit.portable.agent.csproj", configuration);
        BuildProject("./src/NUnitEngine/Portable/nunit.portable.agent.tests/nunit.portable.agent.tests.csproj", configuration);
    });

//////////////////////////////////////////////////////////////////////
// TEST
//////////////////////////////////////////////////////////////////////

Task("Test")
    .IsDependentOn("Build")
    .WithCriteria(IsRunningOnWindows)
    .Does(() =>
    {
        NUnit3(PORTABLE_AGENT_TESTS);
    });

//////////////////////////////////////////////////////////////////////
// PACKAGE
//////////////////////////////////////////////////////////////////////

var RootFiles = new FilePath[]
{
    "LICENSE.txt",
    "NOTICES.txt",
    "CHANGES.txt",
    "nunit.ico"
};

var BinFiles = new FilePath[]
{
    "agents/nunit.portable.agent.dll",
    "agents/nunit.portable.agent.xml"
};

Task("CreateImage")
    .IsDependentOn("Test")
    .Does(() =>
    {
        var currentImageDir = IMAGE_DIR + "nunit.portable.agent-" + packageVersion + "/";
        var imageBinDir = currentImageDir + "bin/";

        CleanDirectory(currentImageDir);

        CopyFiles(RootFiles, currentImageDir);

        CreateDirectory(imageBinDir);
        Information("Created directory " + imageBinDir);

        foreach(FilePath file in BinFiles)
        {
          if (FileExists(BIN_DIR + file))
          {
              CreateDirectory(imageBinDir + file.GetDirectory());
              CopyFile(BIN_DIR + file, imageBinDir + file);
            }
        }
    });

Task("Package")
    .IsDependentOn("CreateImage")
    .Does(() =>
    {
        var currentImageDir = IMAGE_DIR + "nunit.portable.agent-" + packageVersion + "/";

        CreateDirectory(PACKAGE_DIR);

        // Package the portable agent
        NuGetPack("nuget/engine/nunit.portable.agent.nuspec", new NuGetPackSettings()
        {
            Version = packageVersion,
            BasePath = currentImageDir,
            OutputDirectory = PACKAGE_DIR,
            NoPackageAnalysis = true
        });
    });

//////////////////////////////////////////////////////////////////////
// SETUP AND TEARDOWN TASKS
//////////////////////////////////////////////////////////////////////
Setup(() =>
{
    // Executed BEFORE the first task.
});

Teardown(() =>
{
    // Executed AFTER the last task.
});

//////////////////////////////////////////////////////////////////////
// HELPER METHODS - BUILD
//////////////////////////////////////////////////////////////////////

void BuildProject(string projectPath, string configuration)
{
    BuildProject(projectPath, configuration, MSBuildPlatform.Automatic);
}

void BuildProject(string projectPath, string configuration, MSBuildPlatform buildPlatform)
{
    // Use MSBuild
    MSBuild(projectPath, new MSBuildSettings()
        .SetConfiguration(configuration)
        .SetMSBuildPlatform(buildPlatform)
        .SetVerbosity(Verbosity.Minimal)
        .SetNodeReuse(false)
    );
}

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Rebuild")
    .IsDependentOn("Clean")
    .IsDependentOn("Build");

Task("Appveyor")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Package");

Task("Default")
    .IsDependentOn("Build"); // Rebuild?

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
