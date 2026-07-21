using System;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace HCMv3.Web.Swagger;

public class SwaggerNullableParameterFilter : IParameterFilter
{
    public void Apply(IOpenApiParameter parameter, ParameterFilterContext context)
    {
        if (parameter is not OpenApiParameter concreteParameter)
        {
            return;
        }
        
        if (concreteParameter.Schema is not OpenApiSchema concreteSchema)
        {
            return;
        }
        
        if (!concreteSchema.Type.HasFlag(JsonSchemaType.Null) &&
            (context.ApiParameterDescription.Type.IsNullable() || !context.ApiParameterDescription.Type.IsValueType))
        {
            concreteSchema.Type |= JsonSchemaType.Null;
        }
    }
}

public static class TypeExtensions
{
    public static bool IsNullable(this Type type)
    {
        return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
    }
}

public static class JsonSchemaTypeExtensions
{
    public static bool HasFlag(this JsonSchemaType? t, JsonSchemaType flag) =>
        (t & flag) != 0;
}