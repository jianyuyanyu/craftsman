﻿namespace Craftsman.Builders.Projects;

using Domain;
using Helpers;
using Services;

public class IntegrationTestsCsProjBuilder(ICraftsmanUtilities utilities)
{
    public void CreateTestsCsProj(string solutionDirectory, string projectBaseName, DbProvider provider)
    {
        var classPath = ClassPathHelper.IntegrationTestProjectClassPath(solutionDirectory, projectBaseName);
        utilities.CreateFile(classPath, GetTestsCsProjFileText(solutionDirectory, projectBaseName, provider));
    }

    public static string GetTestsCsProjFileText(string solutionDirectory, string projectBaseName, DbProvider provider)
    {
        var webApiClassPath = ClassPathHelper.WebApiProjectClassPath(solutionDirectory, projectBaseName);
        var sharedTestClassPath = ClassPathHelper.SharedTestProjectClassPath(solutionDirectory, projectBaseName);

        return @$"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>

    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""AutoBogusLifesupport"" Version=""2.14.0"" />
    <PackageReference Include=""Bogus"" Version=""35.0.1"" />
    <PackageReference Include=""Ductus.FluentDocker"" Version=""2.10.59"" />
    <PackageReference Include=""FluentAssertions"" Version=""6.12.0"" />
    <PackageReference Include=""MediatR"" Version=""12.2.0"" />
    <PackageReference Include=""Microsoft.AspNetCore.Mvc.Testing"" Version=""8.0.0"" />
    <PackageReference Include=""NSubstitute"" Version=""5.1.0"" />
    <PackageReference Include=""NSubstitute.Analyzers.CSharp"" Version=""1.0.16"">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include=""Testcontainers"" Version=""3.6.0"" />
    <PackageReference Include=""Testcontainers.RabbitMq"" Version=""3.6.0"" />{provider.TestingCsProjNugetPackages()}
    <PackageReference Include=""xunit"" Version=""2.6.4"" />
    <PackageReference Include=""xunit.runner.visualstudio"" Version=""2.5.6"" />
    <PackageReference Include=""Microsoft.NET.Test.Sdk"" Version=""17.8.0"" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include=""..\..\src\{webApiClassPath.ClassNamespace}\{webApiClassPath.ClassName}"" />
    <ProjectReference Include=""..\{sharedTestClassPath.ClassNamespace}\{sharedTestClassPath.ClassName}"" />
  </ItemGroup>

</Project>";
    }
}
