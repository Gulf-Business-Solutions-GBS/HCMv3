using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json.Nodes;
using Abp.Collections.Extensions;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace HCMv3.Web.Swagger;

public class SwaggerEnumParameterFilter : IParameterFilter
{
    public void Apply(IOpenApiParameter parameter, ParameterFilterContext context)
    {
        var type = Nullable.GetUnderlyingType(context.ApiParameterDescription.Type) ?? context.ApiParameterDescription.Type;
        if (type.IsEnum)
        {
            AddEnumParamSpec(parameter, type, context);
            if (parameter is OpenApiParameter concrete)
            {
                concrete.Required = type == context.ApiParameterDescription.Type;
            }
        }
        else if (type.IsArray || (type.IsGenericType && type.GetInterfaces().Contains(typeof(IEnumerable))))
        {
            var itemType = type.GetElementType() ?? type.GenericTypeArguments.First();
            AddEnumSpec(itemType, context);
        }
    }

    private static void AddEnumSpec(Type type, ParameterFilterContext context)
    {
        var schema = context.SchemaRepository.Schemas.GetOrAdd($"{type.Name}", () =>
            context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository)
        );

        if (schema is not OpenApiSchema concreteSchema)
        {
            return;
        }
        
        if (!type.IsEnum)
        {
            return;
        }

        var enumNames = new JsonArray();
        foreach (var name in Enum.GetNames(type))
        {
            enumNames.Add(name);
        }

        concreteSchema.Extensions ??= new ConcurrentDictionary<string, IOpenApiExtension>();
        
        if (concreteSchema.Extensions.TryGetValue("x-enumNames", out var ext)
            && ext is JsonNodeExtension nodeExt
            && nodeExt.Node is JsonObject obj
            && obj["x-enumNames"] is JsonArray existingEnums)
        {
            foreach (var enumName in enumNames)
            {
                existingEnums.AddIfNotContains(enumName);
            }
        }
        else
        {
            concreteSchema.Extensions.Add("x-enumNames", new JsonNodeExtension(enumNames));
        }
    }

    private static void AddEnumParamSpec(IOpenApiParameter parameter, Type type, ParameterFilterContext context)
    {
        var schema = context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);

        if (parameter is not OpenApiParameter concreteParameter)
        {
            return;
        }
        
        concreteParameter.Schema = schema;
        
        var enumNames = new JsonArray();
        foreach (var name in Enum.GetNames(type))
        {
            enumNames.Add(name);
        }
        
        concreteParameter.Extensions ??= new ConcurrentDictionary<string, IOpenApiExtension>();
        concreteParameter.Extensions["x-enumNames"] = new JsonNodeExtension(enumNames);
    }
}

