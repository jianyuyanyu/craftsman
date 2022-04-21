namespace NewCraftsman.Services;

using System.IO.Abstractions;
using Builders;
using Builders.Auth;
using Builders.Docker;
using Builders.Tests.FunctionalTests;
using Builders.Tests.UnitTests;
using Builders.Tests.Utilities;
using Domain;
using FluentAssertions.Common;
using Helpers;
using Spectre.Console;

public class ApiScaffoldingService
{
    private IAnsiConsole _console;
    private readonly IConsoleWriter _consoleWriter;
    private readonly ICraftsmanUtilities _utilities;
    private readonly IFileSystem _fileSystem;
    private readonly IScaffoldingDirectoryStore _scaffoldingDirectoryStore;
    
    public ApiScaffoldingService(IAnsiConsole console, IConsoleWriter consoleWriter, ICraftsmanUtilities utilities, IScaffoldingDirectoryStore scaffoldingDirectoryStore, IFileSystem fileSystem)
    {
        _console = console;
        _consoleWriter = consoleWriter;
        _utilities = utilities;
        _scaffoldingDirectoryStore = scaffoldingDirectoryStore;
        _fileSystem = fileSystem;
    }
    
    public void ScaffoldApi(string buildSolutionDirectory, ApiTemplate template)
    {
        var projectName = template.ProjectName;
        _console.Status()
            .AutoRefresh(true)
            .Spinner(Spinner.Known.Dots2)
            .Start($"[yellow]Creating {template.ProjectName} [/]", ctx =>
            {
                FileParsingHelper.RunPrimaryKeyGuard(template.Entities);
                FileParsingHelper.RunSolutionNameAssignedGuard(projectName);
                FileParsingHelper.SolutionNameDoesNotEqualEntityGuard(projectName, template.Entities);

                // add an accelerate.config.yaml file to the root?
                var bcDirectory = $"{buildSolutionDirectory}{Path.DirectorySeparatorChar}{projectName}";
                var srcDirectory = Path.Combine(bcDirectory, "src");
                var testDirectory = Path.Combine(bcDirectory, "tests");
                _fileSystem.Directory.CreateDirectory(srcDirectory);
                _fileSystem.Directory.CreateDirectory(testDirectory);

                ctx.Spinner(Spinner.Known.BouncingBar);
                ctx.Status($"[bold blue]Building {projectName} Projects [/]");
                new SolutionBuilder(_fileSystem, _utilities, _consoleWriter)
                    .AddProjects(buildSolutionDirectory,
                        srcDirectory,
                        testDirectory,
                        template.DbContext.ProviderEnum,
                        template.DbContext.DatabaseName,
                        projectName, template.AddJwtAuthentication);

                // add all files based on the given template config
                ctx.Status($"[bold blue]Scaffolding Files for {projectName} [/]");
                RunTemplateBuilders(bcDirectory, srcDirectory, testDirectory, template);
                _consoleWriter.WriteLogMessage($"File scaffolding for {template.ProjectName} was successful");
            });
    }

    private void RunTemplateBuilders(string boundedContextDirectory, string srcDirectory, string testDirectory, ApiTemplate template)
    {
        var projectBaseName = template.ProjectName;
        
        // docker config data transform
        template.DockerConfig.ProjectName = template.ProjectName;
        template.DockerConfig.Provider = template.DbContext.Provider;

        // get solution dir from bcDir
        var solutionDirectory = Directory.GetParent(boundedContextDirectory)?.FullName;
        _utilities.IsSolutionDirectoryGuard(solutionDirectory);

        // base files needed before below is ran
        template.DockerConfig.ApiPort ??= template.Port; // set to the launch settings port if needed... really need to refactor to a domain layer and dto layer 😪
        if(template.AddJwtAuthentication)
            template.DockerConfig.AuthServerPort ??= template?.Environment?.AuthSettings?.AuthorizationUrl
                .Replace("localhost", "")
                .Replace("https://", "")
                .Replace("http://", "")
                .Replace(":", ""); // this is fragile and i hate it. also not in domain...
        new DbContextBuilder(_utilities, _fileSystem).CreateDbContext(srcDirectory,
            template.Entities,
            template.DbContext.ContextName,
            template.DbContext.ProviderEnum,
            template.DbContext.DatabaseName,
            template.DockerConfig.DbConnectionString,
            template.DbContext.NamingConventionEnum,
            template.UseSoftDelete,
            projectBaseName
        );
        new ApiRoutesBuilder(_utilities).CreateClass(testDirectory, projectBaseName);
        
        if (template.AddJwtAuthentication)
        {
            new PermissionsBuilder(_utilities).GetPermissions(srcDirectory, projectBaseName); // <-- needs to run before entity features
            new RolesBuilder(_utilities).GetRoles(solutionDirectory);
            new UserPolicyHandlerBuilder(_utilities).CreatePolicyBuilder(solutionDirectory, srcDirectory, projectBaseName, template.DbContext.ContextName);
            new InfrastructureServiceRegistrationModifier(_fileSystem).InitializeAuthServices(srcDirectory, projectBaseName);
            new EntityScaffoldingService(_utilities, _fileSystem).ScaffoldRolePermissions(solutionDirectory,
                srcDirectory,
                testDirectory,
                projectBaseName,
                template.DbContext.ContextName,
                template.SwaggerConfig.AddSwaggerComments,
                template.UseSoftDelete);
        }
        
        //entities
        new EntityScaffoldingService(_utilities, _fileSystem).ScaffoldEntities(solutionDirectory,
            srcDirectory,
            testDirectory,
            projectBaseName,
            template.Entities,
            template.DbContext.ContextName,
            template.SwaggerConfig.AddSwaggerComments,
            template.UseSoftDelete);

        // environments
        AddStartupEnvironmentsWithServices(
            srcDirectory,
            template.DbContext.DatabaseName,
            template.Environment,
            template.SwaggerConfig,
            template.Port,
            projectBaseName,
            template.DockerConfig
        );

        // unit tests, test utils, and one offs∂
        new PagedListTestBuilder(_utilities).CreateTests(srcDirectory, testDirectory, projectBaseName);
        new IntegrationTestFixtureBuilder(_utilities).CreateFixture(testDirectory, 
            projectBaseName, 
            template.DbContext.ContextName, 
            template.DbContext.ProviderEnum);
        new IntegrationTestBaseBuilder(_utilities).CreateBase(testDirectory, projectBaseName, template.DbContext.ProviderEnum);
        new DockerUtilitiesBuilder(_utilities).CreateGeneralUtilityClass(testDirectory, projectBaseName, template.DbContext.ProviderEnum);
        new DockerUtilitiesBuilder(_utilities).CreateDockerDatabaseUtilityClass(testDirectory, projectBaseName, template.DbContext.ProviderEnum);
        new WebAppFactoryBuilder(_utilities).CreateWebAppFactory(testDirectory, projectBaseName, template.DbContext.ContextName, template.AddJwtAuthentication);
        new FunctionalTestBaseBuilder(_utilities).CreateBase(testDirectory, projectBaseName, template.DbContext.ContextName);
        new HealthTestBuilder(_utilities).CreateTests(testDirectory, projectBaseName);
        new HttpClientExtensionsBuilder(_utilities).Create(testDirectory, projectBaseName);
        new EntityBuilder(_utilities).CreateBaseEntity(srcDirectory, projectBaseName, template.UseSoftDelete);
        new CurrentUserServiceTestBuilder(_utilities).CreateTests(testDirectory, projectBaseName);

        //services
        new CurrentUserServiceBuilder(_utilities).GetCurrentUserService(srcDirectory, projectBaseName);
        new SwaggerBuilder(_utilities, _fileSystem).AddSwagger(srcDirectory, template.SwaggerConfig, template.ProjectName, template.AddJwtAuthentication, template.PolicyName, projectBaseName);

        // if (template.Bus.AddBus)
        //     AddBusCommand.AddBus(template.Bus, srcDirectory, testDirectory, projectBaseName, solutionDirectory);
        //
        // if (template.Consumers.Count > 0)
        //     AddConsumerCommand.AddConsumers(template.Consumers, projectBaseName, solutionDirectory, srcDirectory, testDirectory);
        //
        // if (template.Producers.Count > 0)
        //     AddProducerCommand.AddProducers(template.Producers, projectBaseName, solutionDirectory, srcDirectory, testDirectory);
        
        new WebApiDockerfileBuilder(_utilities).CreateStandardDotNetDockerfile(srcDirectory, projectBaseName);
        new DockerIgnoreBuilder(_utilities).CreateDockerIgnore(srcDirectory, projectBaseName);
        // DockerBuilders.AddBoundaryToDockerCompose(solutionDirectory,
        //     template.DockerConfig,
        //     template.Environment.AuthSettings.ClientId,
        //     template.Environment.AuthSettings.ClientSecret,
        //     template.Environment.AuthSettings.Audience);
        new DockerComposeBuilders(_utilities, _fileSystem).AddVolumeToDockerComposeDb(solutionDirectory, template.DockerConfig);
    }
    
    private void AddStartupEnvironmentsWithServices(
        string srcDirectory,
        string dbName,
        ApiEnvironment environment,
        SwaggerConfig swaggerConfig,
        int port,
        string projectBaseName,
        DockerConfig dockerConfig)
    {
        new AppSettingsBuilder(_utilities).CreateWebApiAppSettings(srcDirectory, dbName, projectBaseName);

        new WebApiLaunchSettingsModifier(_fileSystem).AddProfile(srcDirectory, environment, port, dockerConfig, projectBaseName);
        if (!swaggerConfig.IsSameOrEqualTo(new SwaggerConfig()))
            new SwaggerBuilder(_utilities, _fileSystem).RegisterSwaggerInStartup(srcDirectory, projectBaseName);
    }
}
