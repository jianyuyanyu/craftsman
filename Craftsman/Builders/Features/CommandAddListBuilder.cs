﻿namespace Craftsman.Builders.Features;

using System;
using Domain;
using Domain.Enums;
using Helpers;
using Services;

public class CommandAddListBuilder(ICraftsmanUtilities utilities)
{
    public void CreateCommand(string srcDirectory, Entity entity, string projectBaseName, Feature feature, bool isProtected, string permissionName, string dbContextName)
    {
        var classPath = ClassPathHelper.FeaturesClassPath(srcDirectory, $"{feature.Name}.cs", entity.Plural, projectBaseName);
        var fileText = GetCommandFileText(classPath.ClassNamespace, entity, srcDirectory, feature, projectBaseName, isProtected, permissionName, dbContextName);
        utilities.CreateFile(classPath, fileText);
    }

    public static string GetCommandFileText(string classNamespace, Entity entity, string srcDirectory, Feature feature, string projectBaseName, bool isProtected, string permissionName, string dbContextName)
    {
        var className = feature.Name;
        var readDto = FileNames.GetDtoName(entity.Name, Dto.Read);
        var readDtoAsList = $"List<{readDto}>";
        var createDto = FileNames.GetDtoName(entity.Name, Dto.Creation);
        createDto = $"IEnumerable<{createDto}>";
        var featurePropNameLowerFirst = feature.BatchPropertyName.LowercaseFirstLetter();

        var entityName = entity.Name;
        var entityNameLowercase = entity.Name.LowercaseFirstLetter();
        var parentEntityNameLowercaseFirst = feature.ParentEntity.LowercaseFirstLetter();
        var entityNameLowercaseListVar = $"{entity.Name.LowercaseFirstLetter()}List";
        var primaryKeyPropName = Entity.PrimaryKeyProperty.Name;
        var commandProp = $"{entityName}ListToAdd";
        var newEntityProp = $"{entityNameLowercaseListVar}ListToAdd";
        var repoInterface = FileNames.EntityRepositoryInterface(entityName);
        var modelToCreateVariableName = $"{entityName.LowercaseFirstLetter()}ToAdd";

        var entityClassPath = ClassPathHelper.EntityClassPath(srcDirectory, "", entity.Plural, projectBaseName);
        var dtoClassPath = ClassPathHelper.DtoClassPath(srcDirectory, "", entity.Plural, projectBaseName);
        var exceptionsClassPath = ClassPathHelper.ExceptionsClassPath(srcDirectory, "", projectBaseName);
        var modelClassPath = ClassPathHelper.EntityModelClassPath(srcDirectory, entity.Name, entity.Plural, null, projectBaseName);
        var dbContextClassPath = ClassPathHelper.DbContextClassPath(srcDirectory, "", projectBaseName);
        
        FeatureBuilderHelpers.GetPermissionValuesForHandlers(srcDirectory, 
            projectBaseName, 
            isProtected, 
            permissionName, 
            out string heimGuardCtor, 
            out string permissionCheck, 
            out string permissionsUsing);

        var batchFkCheck = !string.IsNullOrEmpty(feature.BatchPropertyName)
            ? @$"
            var {parentEntityNameLowercaseFirst} = await dbContext.{feature.ParentEntityPlural}.GetById(command.{feature.BatchPropertyName}, cancellationToken);{Environment.NewLine}{Environment.NewLine}            "
            : "";

        return @$"namespace {classNamespace};

using {dbContextClassPath.ClassNamespace};
using {entityClassPath.ClassNamespace};
using {dtoClassPath.ClassNamespace};
using {modelClassPath.ClassNamespace};
using {exceptionsClassPath.ClassNamespace};{permissionsUsing}
using Mappings;
using MediatR;

public static class {className}
{{
    public sealed record Command({createDto} {commandProp}, {feature.BatchPropertyType} {feature.BatchPropertyName}) : IRequest<{readDtoAsList}>;

    public sealed class Handler({dbContextName} dbContext{heimGuardCtor})
        : IRequestHandler<Command, {readDtoAsList}>
    {{
        public async Task<{readDtoAsList}> Handle(Command command, CancellationToken cancellationToken)
        {{{permissionCheck}{batchFkCheck}var {entityNameLowercaseListVar}ToAdd = command.{commandProp}.ToList();
            var {entityNameLowercaseListVar} = new List<{entityName}>();
            foreach (var {entityNameLowercase} in {entityNameLowercaseListVar}ToAdd)
            {{
                var {entityNameLowercase}ForCreation = {entityNameLowercase}.To{EntityModel.Creation.GetClassName(entity.Name)}();
                var {entityNameLowercase}ToAdd = {entityName}.Create({entityNameLowercase}ForCreation);
                {entityNameLowercaseListVar}.Add({entityNameLowercase}ToAdd);
                {parentEntityNameLowercaseFirst}.Add{entityName}({entityNameLowercase}ToAdd);
            }}

            // if you have large datasets to add in bulk and have performance concerns, there 
            // are additional methods that could be leveraged in your repository instead (e.g. SqlBulkCopy)
            // https://timdeschryver.dev/blog/faster-sql-bulk-inserts-with-csharp#table-valued-parameter 
            await dbContext.{entity.Plural}.AddRangeAsync({entityNameLowercaseListVar}, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            return {entityNameLowercaseListVar}
                .Select({entity.Lambda} => {entity.Lambda}.To{readDto}())
                .ToList();
        }}
    }}
}}";
    }
}
