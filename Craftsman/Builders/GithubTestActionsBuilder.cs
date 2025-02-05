﻿namespace Craftsman.Builders;

using Helpers;
using Humanizer;
using Services;

public class GithubTestActionsBuilder(ICraftsmanUtilities utilities)
{
    public void CreateUnitTestAction(string solutionDirectory, string projectBaseName)
    {
        var humanized = $"{projectBaseName}UnitTests".Kebaberize();
        var classPath = ClassPathHelper.GithubWorkflowsClassPath(solutionDirectory, $"{humanized}.yaml");
        var fileText = GetUnitTestFileText(projectBaseName);
        utilities.CreateFile(classPath, fileText);
    }
    
    public void CreateIntegrationTestAction(string solutionDirectory, string projectBaseName)
    {
        var humanized = $"{projectBaseName}IntegrationTests".Kebaberize();
        var classPath = ClassPathHelper.GithubWorkflowsClassPath(solutionDirectory, $"{humanized}.yaml");
        var fileText = GetIntegrationTestFileText(projectBaseName);
        utilities.CreateFile(classPath, fileText);
    }
    
    public void CreateFunctionalTestAction(string solutionDirectory, string projectBaseName)
    {
        var humanized = $"{projectBaseName}FunctionalTests".Kebaberize();
        var classPath = ClassPathHelper.GithubWorkflowsClassPath(solutionDirectory, $"{humanized}.yaml");
        var fileText = GetFunctionalTestFileText(projectBaseName);
        utilities.CreateFile(classPath, fileText);
    }

    public static string GetUnitTestFileText(string projectBaseName)
    {
        return @$"name: Unit Tests

on: [pull_request, workflow_dispatch]

jobs:
  test:
    runs-on: ubuntu-latest
    strategy:
      matrix:
        dotnet-version: ['8.0.x']
    steps:
      - uses: actions/checkout@v2
      - name: Setup .NET Core SDK ${{{{ matrix.dotnet-version }}}}
        uses: actions/setup-dotnet@v1.7.2
        with:
          dotnet-version: ${{{{ matrix.dotnet-version }}}}

      - name: Install dependencies
        run: dotnet restore
      - name: Build
        run: dotnet build --configuration Release --no-restore
      - name: Test
        working-directory: {projectBaseName}/tests/{projectBaseName}.UnitTests
        run: dotnet test --no-restore --verbosity minimal";
    }

    public static string GetIntegrationTestFileText(string projectBaseName)
    {
        return @$"name: Integration Tests

on: [pull_request, workflow_dispatch]

jobs:
  test:
    runs-on: ubuntu-latest
    strategy:
      matrix:
        dotnet-version: ['8.0.x']
    steps:
      - uses: actions/checkout@v2
      - name: Setup .NET Core SDK ${{{{ matrix.dotnet-version }}}}
        uses: actions/setup-dotnet@v1.7.2
        with:
          dotnet-version: ${{{{ matrix.dotnet-version }}}}

      - name: Install dependencies
        run: dotnet restore
      - name: Build
        run: dotnet build --configuration Release --no-restore
      - name: Test
        working-directory: {projectBaseName}/tests/{projectBaseName}.IntegrationTests
        run: dotnet test --no-restore --verbosity minimal";
    }

    public static string GetFunctionalTestFileText(string projectBaseName)
    {
        return @$"name: Functional Tests

on: [pull_request, workflow_dispatch]

jobs:
  test:
    runs-on: ubuntu-latest
    strategy:
      matrix:
        dotnet-version: ['8.0.x']
    steps:
      - uses: actions/checkout@v2
      - name: Setup .NET Core SDK ${{{{ matrix.dotnet-version }}}}
        uses: actions/setup-dotnet@v1.7.2
        with:
          dotnet-version: ${{{{ matrix.dotnet-version }}}}

      - name: Install dependencies
        run: dotnet restore
      - name: Build
        run: dotnet build --configuration Release --no-restore
      - name: Test
        working-directory: {projectBaseName}/tests/{projectBaseName}.FunctionalTests
        run: dotnet test --no-restore --verbosity minimal";
    }
}
