# NUnit.Portable.Agent #

[![Build status](https://ci.appveyor.com/api/projects/status/unhanlr79vb8h9vw/branch/master?svg=true)](https://ci.appveyor.com/project/CharliePoole/nunit-portable-agent/branch/master) [![NuGet Version and Downloads count](https://buildstats.info/nuget/NUnit.Portable.Agent?includePreReleases=true)](https://www.nuget.org/packages/NUnit.Portable.Agent)

[![Follow NUnit](https://img.shields.io/twitter/follow/nunit.svg?style=social)](https://twitter.com/nunit)

NUnit is a unit-testing framework for all .Net languages. The NUnit Portable Agent provides an interface that allows test runners to load the NUnit Framework and run tests in an assembly without taking a dependency on a specific version of the framework.

## Building ##

Install [.NET Core](https://www.microsoft.com/net/core) and make sure it is working.

```
# Restore NuGet Packages
dotnet restore

# Build All
dotnet build -c Release test/NUnit.Portable.Agent.Tests

# Run Tests
dotnet run -c Release -p test/NUnit.Portable.Agent.Tests

# Package NuGet
dotnet pack -c Release src/NUnit.Portable.Agent
```

## License ##

NUnit is Open Source software released under the [MIT license](http://www.nunit.org/nuget/nunit3-license.txt).