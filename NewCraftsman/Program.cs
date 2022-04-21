﻿using System.IO.Abstractions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NewCraftsman;
using NewCraftsman.Commands;
using NewCraftsman.Exceptions;
using NewCraftsman.Helpers;
using NewCraftsman.Interceptors;
using NewCraftsman.Services;
using Spectre.Console;
using Spectre.Console.Cli;

var serviceCollection = new ServiceCollection();
serviceCollection.AddSingleton<IFileSystem, FileSystem>();
serviceCollection.AddSingleton<IConsoleWriter, ConsoleWriter>();
serviceCollection.AddSingleton<ICraftsmanUtilities, CraftsmanUtilities>();
serviceCollection.AddSingleton<IScaffoldingDirectoryStore, ScaffoldingDirectoryStore>();
serviceCollection.AddSingleton<IDbMigrator, DbMigrator>();
serviceCollection.AddSingleton<IGitService, GitService>();
serviceCollection.AddCraftsmanBuildersAndModifiers(typeof(Program));
serviceCollection.AddAutoMapper(Assembly.GetExecutingAssembly());

var registrar = new TypeRegistrar(serviceCollection);
var app = new CommandApp(registrar);

app.Configure(config =>
{
    config.SetApplicationName("craftsman");
    config.SetInterceptor(new OperatingSystemInterceptor());
    
    config.AddBranch("new", @new =>
    {
        @new.AddCommand<NewDomainCommand>("domain")
            .WithDescription("Scaffolds a project based on a given template file in a json or yaml format.")
            .WithExample(new [] { "domain", $"my{Path.DirectorySeparatorChar}file{Path.DirectorySeparatorChar}path.yaml" })
            .WithExample(new [] { "domain", $"my{Path.DirectorySeparatorChar}file{Path.DirectorySeparatorChar}path.yml" })
            .WithExample(new [] { "domain", $"my{Path.DirectorySeparatorChar}file{Path.DirectorySeparatorChar}path.json" });

        @new.AddCommand<NewExampleCommand>("example")
            .WithDescription("Scaffolds out an example project via CLI prompts into the current directory.")
            .WithExample(new[] { "example" })
            .WithExample(new[] { "example", "MyProjectName" });
    });
});


try
{
    app.Run(args);
}
catch (Exception e)
{
    // // TODO update to check if exception inherits from ICraftsmanException
    // if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Dev" &&
    //     e is FileAlreadyExistsException
    //     or DirectoryAlreadyExistsException
    //     or InvalidSolutionNameException
    //     or FileNotFoundException
    //     or InvalidDbProviderException
    //     or InvalidFileTypeException
    //     or SolutiuonNameEntityMatchException)
    // {
    //     AnsiConsole.MarkupLine($"{e.Message}");
    // }
    // else
    // {
        AnsiConsole.WriteException(e, new ExceptionSettings
        {
            Format = ExceptionFormats.ShortenEverything | ExceptionFormats.ShowLinks,
            Style = new ExceptionStyle
            {
                Exception = new Style().Foreground(Color.Grey),
                Message = new Style().Foreground(Color.White),
                NonEmphasized = new Style().Foreground(Color.Cornsilk1),
                Parenthesis = new Style().Foreground(Color.Cornsilk1),
                Method = new Style().Foreground(Color.Red),
                ParameterName = new Style().Foreground(Color.Cornsilk1),
                ParameterType = new Style().Foreground(Color.Red),
                Path = new Style().Foreground(Color.Red),
                LineNumber = new Style().Foreground(Color.Cornsilk1),
            }
        });
    // }
}
finally
{
    VersionChecker.CheckForLatestVersion();
}

