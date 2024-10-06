﻿namespace Craftsman.Builders;

using Helpers;
using Services;

public class GithubDependabotBuilder(ICraftsmanUtilities utilities)
{
    public void CreateFile(string solutionDirectory)
    {
        var classPath = ClassPathHelper.GithubClassPath(solutionDirectory, $"dependabot.yaml");
        var fileText = GetFileText();
        utilities.CreateFile(classPath, fileText);
    }

    public static string GetFileText()
    {
        return @$"version: 2

updates:
  - package-ecosystem: ""nuget""
    # Targeted directory, it will look for any csProj file recursively.
    directory: ""/""
    schedule:
      interval: ""weekly""
      day: ""wednesday"" 
    groups:
      microsoft:
        patterns:
        - ""Microsoft*""
        update-types:
        - ""minor""
        - ""patch""
      hangfire:
        patterns:
        - ""Hangfire*""
        update-types:
        - ""minor""
        - ""patch""
      xunit:
        patterns:
        - ""xunit*""
        update-types:
        - ""minor""
        - ""patch""
      serilog:
        patterns:
        - ""Serilog*""
        update-types:
        - ""minor""
        - ""patch""
      otel:
        patterns:
        - ""OpenTelemetry*""
        update-types:
        - ""minor""
        - ""patch""
      testcontainers:
        patterns:
          - ""Testcontainers*""
        update-types:
          - ""minor""
          - ""patch""
    commit-message:      
      prefix: ""Package Dependencies""
    # Temporarily disable PR limit, till initial dependency update goes through
    open-pull-requests-limit: 1000";
    }
}
