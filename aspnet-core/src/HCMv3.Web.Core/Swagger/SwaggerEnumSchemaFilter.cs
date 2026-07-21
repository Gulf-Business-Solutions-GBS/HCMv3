using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace HCMv3.Web.Swagger;

public class SwaggerEnumSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        var type = Nullable.GetUnderlyingType(context.Type) ?? context.Type;
        if (!type.IsEnum || (schema.Extensions != null && schema.Extensions.ContainsKey("x-enumNames")))
        {
            return;
        }
        
        if (schema is not OpenApiSchema concreteSchema)
        {
            return;
        }
        
        var enumNames = new JsonArray();
        foreach (var name in Enum.GetNames(type))
        {
            enumNames.Add(name);
        }
        
        concreteSchema.Extensions ??= new ConcurrentDictionary<string, IOpenApiExtension>();
        concreteSchema.Extensions["x-enumNames"] = new JsonNodeExtension(enumNames);
    }
}

