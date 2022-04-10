﻿namespace Craftsman.Builders.Tests.UnitTests
{
    using Craftsman.Exceptions;
    using Craftsman.Helpers;
    using Craftsman.Models;
    using System.IO;
    using System.IO.Abstractions;
    using System.Text;
    using Enums;

    public class CreateEntityUnitTestBuilder
    {
        public static void CreateTests(string solutionDirectory, string testDirectory, string srcDirectory, string entityName, string entityPlural, string projectBaseName, IFileSystem fileSystem)
        {
            var classPath = ClassPathHelper.UnitTestEntityTestsClassPath(testDirectory, $"{Utilities.CreateEntityUnitTestName(entityName)}.cs", entityPlural, projectBaseName);
            var fileText = WriteTestFileText(solutionDirectory, srcDirectory, classPath, entityName, entityPlural, projectBaseName);
            Utilities.CreateFile(classPath, fileText, fileSystem);
        }

        private static string WriteTestFileText(string solutionDirectory, string srcDirectory, ClassPath classPath, string entityName, string entityPlural, string projectBaseName)
        {
            var entityClassPath = ClassPathHelper.EntityClassPath(srcDirectory, "", entityPlural, projectBaseName);
            var fakerClassPath = ClassPathHelper.TestFakesClassPath(solutionDirectory, "", entityName, projectBaseName);
            var createDto = Utilities.GetDtoName(entityName, Dto.Creation);
            var fakeEntityForCreation = $"Fake{createDto}";

            return @$"namespace {classPath.ClassNamespace};

using {fakerClassPath.ClassNamespace};
using {entityClassPath.ClassNamespace};
using Bogus;
using FluentAssertions;
using NUnit.Framework;

public class {Path.GetFileNameWithoutExtension(classPath.FullClassPath)}
{{
    private readonly Faker _faker;

    public {Path.GetFileNameWithoutExtension(classPath.FullClassPath)}()
    {{
        _faker = new Faker();
    }}
    
    [Test]
    public void can_create_valid_{entityName.LowercaseFirstLetter()}()
    {{
        // Arrange + Act
        var fake{entityName} = {entityName}.Create(new {fakeEntityForCreation}().Generate());

        // Assert
        fake{entityName}.Should().NotBeNull();
    }}
}}";
        }
    }
}