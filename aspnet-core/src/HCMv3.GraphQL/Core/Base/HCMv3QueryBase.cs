using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.ObjectMapping;
using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace HCMv3.Core.Base;

public abstract class HCMv3QueryBase<TField, TResult> : ITransientDependency
{
    public IPermissionChecker PermissionChecker { protected get; set; }

    public Dictionary<string, Type> Arguments { get; set; }

    public string FieldName { get; set; }

    public ResolveFieldContext<object> Context { get; set; }

    public IObjectMapper ObjectMapper { protected get; set; }

    protected HCMv3QueryBase(string fieldName,
        Dictionary<string, Type> arguments = null)
    {
        PermissionChecker = NullPermissionChecker.Instance;
        FieldName = fieldName;
        Arguments = arguments ?? new Dictionary<string, Type>();
    }

    public List<QueryArgument> GetQueryArguments()
    {
        return Arguments.Select(arg => new QueryArgument(arg.Value)
        {
            Name = arg.Key
        }).ToList();
    }

    [UnitOfWork]
    protected virtual async Task<TResult> InternalResolve(IResolveFieldContext context)
    {
        // you can add your custom logic here before the original Resolve method.
        return await Resolve(context);
    }

    public abstract Task<TResult> Resolve(IResolveFieldContext context);

    public FieldType GetFieldType()
    {
        return new FieldType
        {
            Name = FieldName,
            Type = typeof(TField),
            Resolver = new FuncFieldResolver<TResult>(async context => await InternalResolve(context)),
            Arguments = new QueryArguments(GetQueryArguments())
        };
    }
}

