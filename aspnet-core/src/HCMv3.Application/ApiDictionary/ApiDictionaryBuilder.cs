using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Localization;
using HCMv3.ApiDictionary.Dto;
using Microsoft.AspNetCore.Mvc.Routing;

namespace HCMv3.ApiDictionary;

/// <summary>
/// Reads one catalog entry's service, DTOs and entity, and writes the dictionary page for it.
///
/// <para>
/// <b>It mirrors how ABP publishes the service rather than asking MVC.</b> The alternative, ASP.NET Core's API
/// explorer, only exists inside the running web host, so a dictionary built on it could not be tested. ABP's rules
/// are few and fixed, and were checked against all 2,674 endpoints the running host serves: the route is
/// <c>/api/services/app/{Service}/{Method}</c>; an explicit <c>[HttpGet]</c>, <c>[HttpPost]</c> and so on wins;
/// otherwise the verb comes from the method name -- Get, Update/Put, Delete/Remove, and POST for everything else.
/// ApiDictionaryAppService_Tests pins the Employee results to what that host returned.
/// </para>
/// </summary>
public class ApiDictionaryBuilder : ITransientDependency
{
    public const string PlaceholderBaseUrl = "https://servername";

    private const string RoutePrefix = "/api/services/app/";

    private readonly ILocalizationManager _localizationManager;

    public ApiDictionaryBuilder(ILocalizationManager localizationManager)
    {
        _localizationManager = localizationManager;
    }

    public ApiDictionaryEntityDto Build(ApiDictionaryCatalogEntry entry)
    {
        var dto = new ApiDictionaryEntityDto
        {
            Name = entry.Name,
            DisplayName = Localize(entry.DisplayNameKey) ?? entry.Name,
            BaseUrl = PlaceholderBaseUrl
        };

        var query = FindMethod(entry, entry.QueryMethod);
        var read = FindMethod(entry, entry.ReadMethod);
        var upsert = FindMethod(entry, entry.UpsertMethod);
        var delete = FindMethod(entry, entry.DeleteMethod);

        AddOperation(dto, entry, "Query", query);
        AddOperation(dto, entry, "Read", read);
        AddOperation(dto, entry, "Upsert", upsert);
        AddOperation(dto, entry, "Delete", delete);

        dto.Fields = BuildFields(entry, query, read, upsert);

        return dto;
    }

    // ------------------------------------------------------------------ operations

    private void AddOperation(ApiDictionaryEntityDto dto, ApiDictionaryCatalogEntry entry, string operation, MethodInfo method)
    {
        // An operation the service does not have is simply absent, so the screen can say "not supported" rather
        // than printing an endpoint that answers 404.
        if (method == null)
        {
            return;
        }

        var httpMethod = HttpMethodOf(method);
        var url = PlaceholderBaseUrl + RoutePrefix + ServiceNameOf(entry.ServiceImplementation) + "/" + method.Name;
        var requestFields = new List<string>();

        var parameter = method.GetParameters().SingleOrDefault();

        if (parameter != null)
        {
            var sendsBody = httpMethod is "POST" or "PUT" or "PATCH";
            var names = IsSimple(parameter.ParameterType)
                ? new List<string> { CamelCase(parameter.Name) }
                : WritableProperties(parameter.ParameterType).Select(p => CamelCase(p.Name)).ToList();

            requestFields.AddRange(names);

            // GET and DELETE carry their input in the query string, which is where ABP binds it from. The body of
            // a POST is described by RequestFields instead of being crammed into the address.
            if (!sendsBody && names.Count > 0)
            {
                url += "?" + string.Join("&", names.Select(n => Capitalize(n) + "={" + n + "}"));
            }
        }

        dto.Operations.Add(new ApiDictionaryOperationDto
        {
            Operation = operation,
            HttpMethod = httpMethod,
            Url = url,
            Permissions = PermissionsOf(entry.ServiceImplementation, method, operation),
            RequestFields = requestFields,
            Response = DescribeResponse(method, entry.Name)
        });
    }

    /// <summary>
    /// An attribute that fixes the verb always wins -- 753 Get methods in this solution are POST because they
    /// take a filter object. Only when there is none does the name decide, the way ABP decides it.
    /// </summary>
    public static string HttpMethodOf(MethodInfo method)
    {
        var fixedVerb = method.GetCustomAttributes(true)
            .OfType<HttpMethodAttribute>()
            .SelectMany(a => a.HttpMethods)
            .FirstOrDefault();

        if (fixedVerb != null)
        {
            return fixedVerb.ToUpperInvariant();
        }

        var name = method.Name;

        if (name.StartsWith("Get", StringComparison.Ordinal))
        {
            return "GET";
        }

        if (name.StartsWith("Update", StringComparison.Ordinal) || name.StartsWith("Put", StringComparison.Ordinal))
        {
            return "PUT";
        }

        if (name.StartsWith("Delete", StringComparison.Ordinal) || name.StartsWith("Remove", StringComparison.Ordinal))
        {
            return "DELETE";
        }

        if (name.StartsWith("Patch", StringComparison.Ordinal))
        {
            return "PATCH";
        }

        return "POST";
    }

    public static string ServiceNameOf(Type serviceImplementation)
    {
        var name = serviceImplementation.Name;

        foreach (var suffix in new[] { "ApplicationService", "AppService" })
        {
            if (name.EndsWith(suffix, StringComparison.Ordinal))
            {
                return name.Substring(0, name.Length - suffix.Length);
            }
        }

        return name;
    }

    /// <summary>
    /// The class-level permission, then the method's own. An upsert decides at run time whether it creates or
    /// updates and checks a different permission for each, on the protected CreateAsync / UpdateAsync that ABP's
    /// generated services use -- so those are read too and labelled, because an integration that can create but
    /// not edit needs to know before it tries.
    /// </summary>
    private static List<string> PermissionsOf(Type service, MethodInfo method, string operation)
    {
        var permissions = new List<string>();

        permissions.AddRange(PermissionNames(service.GetCustomAttributes<AbpAuthorizeAttribute>(true)));
        permissions.AddRange(PermissionNames(method.GetCustomAttributes<AbpAuthorizeAttribute>(true)));

        if (operation == "Upsert")
        {
            const BindingFlags anyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (var (label, names) in new[] { ("Create", new[] { "CreateAsync", "Create" }), ("Update", new[] { "UpdateAsync", "Update" }) })
            {
                var inner = names
                    .SelectMany(n => service.GetMethods(anyInstance).Where(m => m.Name == n))
                    .FirstOrDefault();

                if (inner == null)
                {
                    continue;
                }

                permissions.AddRange(PermissionNames(inner.GetCustomAttributes<AbpAuthorizeAttribute>(true))
                    .Select(p => label + ": " + p));
            }
        }

        return permissions.Distinct().ToList();
    }

    private static IEnumerable<string> PermissionNames(IEnumerable<AbpAuthorizeAttribute> attributes)
    {
        return attributes.SelectMany(a => a.Permissions ?? Array.Empty<string>());
    }

    /// <summary>
    /// The shape that comes back, named by the record type rather than the DTO class: a third party sees
    /// "Employee", not "CreateOrEditEmployeeDto", which is an implementation detail of this solution.
    /// </summary>
    private string DescribeResponse(MethodInfo method, string entityName)
    {
        var returned = UnwrapTask(method.ReturnType);

        if (returned == null)
        {
            return Localize("ApiResponseNoContent") ?? "No content";
        }

        var listItem = ListItemType(returned);

        if (listItem != null)
        {
            return typeof(IHasTotalCount).IsAssignableFrom(returned)
                ? "{ totalCount, items: [ " + entityName + " ] }"
                : "{ items: [ " + entityName + " ] }";
        }

        var wrapped = SingleComplexProperty(returned);

        return wrapped != null
            ? "{ " + CamelCase(wrapped.Name) + ": " + entityName + " }"
            : entityName;
    }

    // ------------------------------------------------------------------ fields

    private List<ApiDictionaryFieldDto> BuildFields(ApiDictionaryCatalogEntry entry, MethodInfo query, MethodInfo read, MethodInfo upsert)
    {
        var listType = query == null ? null : ListItemType(UnwrapTask(query.ReturnType));
        var readType = ReadFieldsType(read);
        var upsertType = upsert?.GetParameters().SingleOrDefault()?.ParameterType;

        var listProps = PropertyMap(listType);
        var readProps = PropertyMap(readType);
        var upsertProps = PropertyMap(upsertType);

        // Id first, then the order the upsert DTO declares its fields in -- the order the create screen shows them
        // -- then anything only the list returns, such as computed full names.
        var names = new List<string>();

        void Add(string name)
        {
            if (!names.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                names.Add(name);
            }
        }

        if (listProps.ContainsKey("Id") || upsertProps.ContainsKey("Id") || readProps.ContainsKey("Id"))
        {
            Add("Id");
        }

        foreach (var name in OrderedNames(upsertType)) Add(name);
        foreach (var name in OrderedNames(readType)) Add(name);
        foreach (var name in OrderedNames(listType)) Add(name);

        return names.Select(name => BuildField(entry, name, listProps, readProps, upsertProps)).ToList();
    }

    private ApiDictionaryFieldDto BuildField(
        ApiDictionaryCatalogEntry entry,
        string name,
        IReadOnlyDictionary<string, PropertyInfo> listProps,
        IReadOnlyDictionary<string, PropertyInfo> readProps,
        IReadOnlyDictionary<string, PropertyInfo> upsertProps)
    {
        listProps.TryGetValue(name, out var listProp);
        readProps.TryGetValue(name, out var readProp);
        upsertProps.TryGetValue(name, out var upsertProp);

        var source = upsertProp ?? readProp ?? listProp;
        var clrType = source.PropertyType;
        var isKey = string.Equals(name, "Id", StringComparison.Ordinal);
        var isRequired = upsertProp?.GetCustomAttribute<RequiredAttribute>(true) != null;
        var inUpsert = upsertProp != null && !isKey;

        var field = new ApiDictionaryFieldDto
        {
            PropertyName = CamelCase(name),
            Label = LabelFor(name),
            Type = TypeName(clrType),
            EnumValues = EnumValues(clrType),
            IsKey = isKey,
            IsRequired = isRequired,
            MaxLength = MaxLengthOf(upsertProp ?? source),
            IsNullable = IsNullable(clrType, isRequired),
            InQueryResults = listProp != null,
            InReadResult = readProp != null,
            IsCreatable = inUpsert && !entry.IgnoredOnCreate.Contains(name, StringComparer.OrdinalIgnoreCase),
            IsUpdatable = inUpsert,
            LinksTo = LinkedEntityName(entry.EntityType, name),
            SensitiveCategory = ApiDictionarySensitiveFields.Categorize(name)
        };

        if (entry.FieldNotes.TryGetValue(name, out var noteKey))
        {
            field.Note = Localize(noteKey) ?? noteKey;
        }

        return field;
    }

    /// <summary>
    /// The Read operation's fields. A ForEdit output that only wraps the upsert DTO (<c>{ employee: {...} }</c>)
    /// is unwrapped, so the dictionary lists the fields a caller actually gets rather than one field called
    /// "employee".
    /// </summary>
    private static Type ReadFieldsType(MethodInfo read)
    {
        var returned = read == null ? null : UnwrapTask(read.ReturnType);

        if (returned == null)
        {
            return null;
        }

        return SingleComplexProperty(returned)?.PropertyType ?? returned;
    }

    /// <summary>
    /// Which record a foreign key points to, read from the ENTITY rather than guessed from the name: BirthPlaceId
    /// is a City, which no amount of trimming "Id" off the name would say.
    /// </summary>
    public static string LinkedEntityName(Type entityType, string fieldName)
    {
        if (entityType == null || fieldName.Length <= 2 || !fieldName.EndsWith("Id", StringComparison.Ordinal))
        {
            return null;
        }

        var navigation = entityType.GetProperty(fieldName.Substring(0, fieldName.Length - 2), BindingFlags.Instance | BindingFlags.Public);

        return navigation != null && IsEntity(navigation.PropertyType) ? navigation.PropertyType.Name : null;
    }

    private static bool IsEntity(Type type)
    {
        return type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntity<>));
    }

    /// <summary>
    /// The label the product already shows for the field, in the user's language: the field's own key, then the
    /// record it names (NationalityId and NationalityName are both "Nationality"). Only when there is none is the
    /// name spaced out, so a label never reads as a bare identifier if a translation exists.
    /// </summary>
    private string LabelFor(string name)
    {
        var candidates = new List<string> { name };

        foreach (var suffix in new[] { "Id", "Name" })
        {
            if (name.Length > suffix.Length && name.EndsWith(suffix, StringComparison.Ordinal))
            {
                candidates.Add(name.Substring(0, name.Length - suffix.Length));
            }
        }

        foreach (var candidate in candidates)
        {
            var text = Localize(candidate);

            if (text != null)
            {
                return text;
            }
        }

        return Humanize(name);
    }

    public static string TypeName(Type clrType)
    {
        var type = Nullable.GetUnderlyingType(clrType) ?? clrType;

        if (type.IsEnum) return "enum";
        if (type == typeof(string)) return "string";
        if (type == typeof(bool)) return "boolean";
        if (type == typeof(int) || type == typeof(short) || type == typeof(byte)) return "integer (int32)";
        if (type == typeof(long)) return "integer (int64)";
        if (type == typeof(decimal) || type == typeof(double) || type == typeof(float)) return "number";
        if (type == typeof(DateTime) || type == typeof(DateTimeOffset)) return "date-time";
        if (type == typeof(Guid)) return "string (uuid)";
        if (type == typeof(byte[])) return "binary (base64)";

        return type.Name;
    }

    private static List<string> EnumValues(Type clrType)
    {
        var type = Nullable.GetUnderlyingType(clrType) ?? clrType;

        if (!type.IsEnum)
        {
            return new List<string>();
        }

        return Enum.GetValues(type)
            .Cast<object>()
            .Select(value => Enum.GetName(type, value) + " = " + Convert.ToInt64(value))
            .ToList();
    }

    private static int? MaxLengthOf(PropertyInfo property)
    {
        return property?.GetCustomAttribute<StringLengthAttribute>(true)?.MaximumLength
               ?? property?.GetCustomAttribute<MaxLengthAttribute>(true)?.Length;
    }

    /// <summary>
    /// A nullable value type may be omitted; a plain one always has a value. Text and binary may be empty unless the
    /// field is required -- this solution does not use nullable reference annotations, so the attribute is the
    /// only signal.
    /// </summary>
    private static bool IsNullable(Type clrType, bool isRequired)
    {
        if (Nullable.GetUnderlyingType(clrType) != null)
        {
            return true;
        }

        return !clrType.IsValueType && !isRequired;
    }

    // ------------------------------------------------------------------ reflection helpers

    private static MethodInfo FindMethod(ApiDictionaryCatalogEntry entry, string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        return entry.ServiceImplementation.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                   .SingleOrDefault(m => m.Name == name)
               ?? throw new InvalidOperationException(
                   "The API dictionary entry '" + entry.Name + "' names the method " + entry.ServiceImplementation.Name
                   + "." + name + ", which is not a public method of that service.");
    }

    private static Type UnwrapTask(Type type)
    {
        if (type == typeof(System.Threading.Tasks.Task) || type == typeof(void))
        {
            return null;
        }

        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Threading.Tasks.Task<>)
            ? type.GetGenericArguments()[0]
            : type;
    }

    /// <summary>The item type of a PagedResultDto / ListResultDto, or null when the type is not a list result.</summary>
    private static Type ListItemType(Type type)
    {
        for (var current = type; current != null && current != typeof(object); current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(ListResultDto<>))
            {
                return current.GetGenericArguments()[0];
            }
        }

        return null;
    }

    private static PropertyInfo SingleComplexProperty(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

        return properties.Length == 1 && !IsSimple(properties[0].PropertyType) ? properties[0] : null;
    }

    private static IReadOnlyDictionary<string, PropertyInfo> PropertyMap(Type type)
    {
        return type == null
            ? new Dictionary<string, PropertyInfo>()
            : type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .GroupBy(p => p.Name)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Declaration order, with inherited properties -- EntityDto's Id -- ahead of the type's own. GetProperties
    /// does not promise an order, so it is rebuilt from the inheritance chain.
    /// </summary>
    private static IEnumerable<string> OrderedNames(Type type)
    {
        if (type == null)
        {
            return Enumerable.Empty<string>();
        }

        var chain = new List<Type>();

        for (var current = type; current != null && current != typeof(object); current = current.BaseType)
        {
            chain.Insert(0, current);
        }

        return chain
            .SelectMany(t => t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .OrderBy(p => p.MetadataToken))
            .Select(p => p.Name);
    }

    private static IEnumerable<PropertyInfo> WritableProperties(Type type)
    {
        return OrderedNames(type)
            .Distinct()
            .Select(name => type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public))
            .Where(p => p != null && p.CanWrite && p.GetSetMethod() != null);
    }

    private static bool IsSimple(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;

        return t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(decimal) || t == typeof(DateTime)
               || t == typeof(DateTimeOffset) || t == typeof(Guid) || t == typeof(byte[]);
    }

    /// <summary>The same conversion the server's JSON uses: <c>PACINumber</c> is <c>paciNumber</c> on the wire.</summary>
    public static string CamelCase(string name)
    {
        return JsonNamingPolicy.CamelCase.ConvertName(name);
    }

    private static string Capitalize(string name)
    {
        return string.IsNullOrEmpty(name) ? name : char.ToUpperInvariant(name[0]) + name.Substring(1);
    }

    /// <summary>"FirstNameEn" to "First Name En", keeping acronyms together: "PACINumber" to "PACI Number".</summary>
    public static string Humanize(string name)
    {
        var builder = new StringBuilder();

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            var startsWord = i > 0 && char.IsUpper(c)
                             && (char.IsLower(name[i - 1]) || (i + 1 < name.Length && char.IsLower(name[i + 1])));

            if (startsWord)
            {
                builder.Append(' ');
            }

            builder.Append(c);
        }

        return builder.ToString();
    }

    private string Localize(string key)
    {
        return string.IsNullOrEmpty(key)
            ? null
            : _localizationManager.GetSource(HCMv3Consts.LocalizationSourceName).GetStringOrNull(key);
    }
}
