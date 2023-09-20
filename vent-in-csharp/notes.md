Project Notes
-------------

## Code Coverage

* Install code coverage in project: `dotnet add package coverlet.collector`
* Install reporting tool: `dotnet tool install -g dotnet-reportgenerator-globaltool`
* Run code coverage in test project directory: `dotnet test --collect:"XPlat Code Coverage"`
* This will generate a report called `coverage.cobertura.xml`
* Run Reporting: `reportgenerator -reports:"Path_to_\coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html`
* View generated html report :)

More information:
* https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-code-coverage?tabs=windows
* https://github.com/coverlet-coverage/coverlet

