﻿namespace Craftsman.Builders.Tests.UnitTests;

using System.IO;
using Domain;
using Domain.Enums;
using Helpers;
using Services;

public class CreateEntityUnitTestBuilder(ICraftsmanUtilities utilities)
{
    public void CreateTests(string solutionDirectory, string testDirectory, string srcDirectory, string entityName, string entityPlural, List<EntityProperty> properties, string projectBaseName)
    {
        var classPath = ClassPathHelper.UnitTestEntityTestsClassPath(testDirectory, $"{FileNames.CreateEntityUnitTestName(entityName)}.cs", entityPlural, projectBaseName);
        var fileText = WriteTestFileText(solutionDirectory, srcDirectory, classPath, entityName, entityPlural, properties, projectBaseName);
        utilities.CreateFile(classPath, fileText);
    }

    private static string WriteTestFileText(string solutionDirectory, string srcDirectory, ClassPath classPath, string entityName, string entityPlural, List<EntityProperty> properties, string projectBaseName)
    {
        var entityClassPath = ClassPathHelper.EntityClassPath(srcDirectory, "", entityPlural, projectBaseName);
        var fakerClassPath = ClassPathHelper.TestFakesClassPath(solutionDirectory, "", entityName, projectBaseName);
        var domainEventsClassPath = ClassPathHelper.DomainEventsClassPath(srcDirectory, "", entityPlural, projectBaseName);
        var exceptionClassPath = ClassPathHelper.ExceptionsClassPath(srcDirectory, "", projectBaseName);

        var seedInfoVar = $"{entityName.LowercaseFirstLetter()}ToCreate";
        var creationModelName = EntityModel.Creation.GetClassName(entityName);
        var fakeCreationModelName = FileNames.FakerName(creationModelName);
        var createdEntityVar = $"{entityName.LowercaseFirstLetter()}";
        
        return @$"namespace {classPath.ClassNamespace};

using {fakerClassPath.ClassNamespace};
using {entityClassPath.ClassNamespace};
using {domainEventsClassPath.ClassNamespace};
using Bogus;
using FluentAssertions.Extensions;
using ValidationException = {exceptionClassPath.ClassNamespace}.ValidationException;

public class {Path.GetFileNameWithoutExtension(classPath.FullClassPath)}
{{
    private readonly Faker _faker;

    public {Path.GetFileNameWithoutExtension(classPath.FullClassPath)}()
    {{
        _faker = new Faker();
    }}
    
    [Fact]
    public void can_create_valid_{entityName.LowercaseFirstLetter()}()
    {{
        // Arrange
        var {seedInfoVar} = new {fakeCreationModelName}().Generate();
        
        // Act
        var {createdEntityVar} = {entityName}.Create({seedInfoVar});

        // Assert{GetAssertions(properties, createdEntityVar, seedInfoVar)}
    }}

    [Fact]
    public void queue_domain_event_on_create()
    {{
        // Arrange
        var {seedInfoVar} = new {fakeCreationModelName}().Generate();
        
        // Act
        var {createdEntityVar} = {entityName}.Create({seedInfoVar});

        // Assert
        {createdEntityVar}.DomainEvents.Count.Should().Be(1);
        {createdEntityVar}.DomainEvents.FirstOrDefault().Should().BeOfType(typeof({FileNames.EntityCreatedDomainMessage(entityName)}));
    }}
}}";
    }

    private static string GetAssertions(List<EntityProperty> properties, string createdEntityVar, string seedInfoVar)
    {
        var entityAssertions = "";
        foreach (var entityProperty in properties.Where(x => x.IsPrimitiveType && x.GetDbRelationship.IsNone && x.CanManipulate))
        {
            entityAssertions += entityProperty.Type switch
            {
                "DateTime" or "DateTimeOffset" or "TimeOnly" =>
                    $@"{Environment.NewLine}        {createdEntityVar}.{entityProperty.Name}.Should().BeCloseTo({seedInfoVar}.{entityProperty.Name}, 1.Seconds());",
                "DateTime?" =>
                    $@"{Environment.NewLine}        {createdEntityVar}.{entityProperty.Name}.Should().BeCloseTo((DateTime){seedInfoVar}.{entityProperty.Name}, 1.Seconds());",
                "DateTimeOffset?" =>
                    $@"{Environment.NewLine}        {createdEntityVar}.{entityProperty.Name}.Should().BeCloseTo((DateTimeOffset){seedInfoVar}.{entityProperty.Name}, 1.Seconds());",
                "TimeOnly?" =>
                    $@"{Environment.NewLine}        {createdEntityVar}.{entityProperty.Name}.Should().BeCloseTo((TimeOnly){seedInfoVar}.{entityProperty.Name}, 1.Seconds());",
                _ =>
                    $@"{Environment.NewLine}        {createdEntityVar}.{entityProperty.Name}.Should().Be({seedInfoVar}.{entityProperty.Name});"
            };
        }
        foreach (var entityProperty in properties.Where(x => x.IsStringArray && x.CanManipulate))
        {
            entityAssertions += $@"{Environment.NewLine}        {createdEntityVar}.{entityProperty.Name}.Should().BeEquivalentTo({seedInfoVar}.{entityProperty.Name});";
        }

        return entityAssertions;
    }
}
