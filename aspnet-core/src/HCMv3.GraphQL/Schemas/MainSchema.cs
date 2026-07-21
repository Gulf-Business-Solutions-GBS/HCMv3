using Abp.Dependency;
using GraphQL.Types;
using HCMv3.Queries.Container;
using System;
using GraphQL.Conversion;
using Microsoft.Extensions.DependencyInjection;

namespace HCMv3.Schemas;

public class MainSchema : Schema, ITransientDependency
{
    public MainSchema(IServiceProvider provider) :
        base(provider)
    {
        Query = provider.GetRequiredService<QueryContainer>();
        NameConverter = new CamelCaseNameConverter();
    }
}

