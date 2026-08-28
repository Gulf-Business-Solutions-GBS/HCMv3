using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HCMv3.Dto;
using Abp.Dependency;
using HCMv3.GridReports;
using HCMv3.ReportManagement.Dto;

namespace HCMv3.ReportManagement;

/// <summary>
/// One discovered report, with the reflection handles the renderer needs to execute it.
/// </summary>
public class SchedulableReportDescriptor
{
    public string ReportKey { get; set; }

    public string DisplayName { get; set; }

    public string Category { get; set; }

    /// <summary>The app-service interface to resolve from the container.</summary>
    public Type ServiceInterface { get; set; }

    /// <summary>The <c>Get{X}ToExcel</c> method to invoke.</summary>
    public MethodInfo ExportMethod { get; set; }

    /// <summary>Its single input DTO type.</summary>
    public Type InputType { get; set; }
}

/// <summary>
/// DISCOVERS schedulable reports instead of requiring each one to be registered.
///
/// WHY DISCOVERY AND NOT A REGISTRY. A registry means every new report has to be remembered and added
/// somewhere else before it can be scheduled -- which is the same hardcoding problem in a different
/// file, and it fails quietly: the report simply never appears in the wizard and nobody knows why.
/// Discovery inverts that. A new grid report is schedulable the moment it exists, because it already
/// does the two things this scans for.
///
/// THE CONVENTION, verified against the codebase rather than assumed: every grid report app service
/// derives from <c>GridReportAppServiceBase</c> and exposes a single
/// <c>public Task&lt;FileDto&gt; Get{Something}ToExcel(TInput input)</c>. All 138 services present when
/// this was written satisfy that. Note the method and input names are NOT required to correspond --
/// the PayrollJVCustom016 family shares one input DTO across seven services, so the discovery keys off
/// the SHAPE (one export method, one input parameter) rather than matching names.
///
/// A report that does not fit simply is not discovered; it does not break anything, and it shows up as
/// absent from the wizard's list rather than as a runtime failure.
/// </summary>
public interface ISchedulableReportCatalog
{
    IReadOnlyList<SchedulableReportDescriptor> GetAll();

    SchedulableReportDescriptor Find(string reportKey);

    IReadOnlyList<SchedulableReportDto> GetForClient();
}

public class SchedulableReportCatalog : ISchedulableReportCatalog, ISingletonDependency
{
    private readonly Lazy<List<SchedulableReportDescriptor>> _descriptors;

    public SchedulableReportCatalog()
    {
        // Reflection over one assembly, computed once. Lazy rather than in the constructor so a
        // malformed report cannot take down application start-up.
        _descriptors = new Lazy<List<SchedulableReportDescriptor>>(Discover, isThreadSafe: true);
    }

    public IReadOnlyList<SchedulableReportDescriptor> GetAll() => _descriptors.Value;

    public SchedulableReportDescriptor Find(string reportKey) =>
        _descriptors.Value.FirstOrDefault(d =>
            string.Equals(d.ReportKey, reportKey, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<SchedulableReportDto> GetForClient() =>
        _descriptors.Value
            .OrderBy(d => d.Category)
            .ThenBy(d => d.DisplayName)
            .Select(d => new SchedulableReportDto
            {
                ReportKey = d.ReportKey,
                ReportKind = ScheduledReportKind.GridReport,
                DisplayName = d.DisplayName,
                Category = d.Category,
                Parameters = DescribeParameters(d.InputType),
            })
            .ToList();

    private static List<SchedulableReportDescriptor> Discover()
    {
        var assembly = typeof(GridReportAppServiceBase).Assembly;
        var result = new List<SchedulableReportDescriptor>();

        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || !type.IsClass || !typeof(GridReportAppServiceBase).IsAssignableFrom(type))
            {
                continue;
            }

            var exportMethod = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m =>
                    m.Name.EndsWith("ToExcel", StringComparison.Ordinal) &&
                    m.GetParameters().Length == 1 &&
                    IsFileDtoTask(m.ReturnType));

            if (exportMethod == null)
            {
                continue;
            }

            // Prefer the app-service INTERFACE for resolution: ABP registers services by interface, and
            // resolving the concrete type would bypass its interceptors (unit of work, authorization,
            // auditing) -- exactly the cross-cutting behaviour a scheduled run still needs.
            var serviceInterface = type.GetInterfaces()
                .FirstOrDefault(i => i.Name == "I" + type.Name);

            result.Add(new SchedulableReportDescriptor
            {
                ReportKey = type.Name,
                DisplayName = Humanize(type.Name),
                Category = type.Namespace?.Split('.').LastOrDefault() ?? "Reports",
                ServiceInterface = serviceInterface ?? type,
                ExportMethod = exportMethod,
                InputType = exportMethod.GetParameters()[0].ParameterType,
            });
        }

        return result.OrderBy(d => d.DisplayName).ToList();
    }

    private static bool IsFileDtoTask(Type returnType)
    {
        return returnType.IsGenericType &&
               returnType.GetGenericArguments().Length == 1 &&
               returnType.GetGenericArguments()[0] == typeof(FileDto);
    }

    /// <summary>"EmployeeAttendanceReportAppService" -> "Employee Attendance Report".</summary>
    private static string Humanize(string typeName)
    {
        var name = typeName;

        if (name.EndsWith("AppService", StringComparison.Ordinal))
        {
            name = name.Substring(0, name.Length - "AppService".Length);
        }

        var chars = new List<char>();
        for (var i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
            {
                chars.Add(' ');
            }

            chars.Add(name[i]);
        }

        return new string(chars.ToArray());
    }

    /// <summary>
    /// Describes the report's own filter properties so the wizard can offer them. Read off the input
    /// DTO, so a new parameter on a report shows up in the wizard with no other change anywhere.
    /// </summary>
    private static List<SchedulableReportParameterDto> DescribeParameters(Type inputType)
    {
        var result = new List<SchedulableReportParameterDto>();

        if (inputType == null)
        {
            return result;
        }

        // Paging and sorting are the grid's concern, not a schedule's -- a scheduled export always
        // covers every matching row.
        var ignored = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SkipCount", "MaxResultCount", "Sorting",
        };

        foreach (var property in inputType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanWrite || ignored.Contains(property.Name))
            {
                continue;
            }

            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            result.Add(new SchedulableReportParameterDto
            {
                Name = property.Name,
                Type = type.IsEnum ? type.Name : SimpleTypeName(type),
                IsRequired = Nullable.GetUnderlyingType(property.PropertyType) == null && type.IsValueType,
                AllowedValues = type.IsEnum ? Enum.GetNames(type).ToList() : null,
            });
        }

        return result;
    }

    private static string SimpleTypeName(Type type)
    {
        if (type == typeof(string)) return "string";
        if (type == typeof(int) || type == typeof(long)) return "int";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(DateTime)) return "DateTime";
        if (type == typeof(decimal) || type == typeof(double)) return "decimal";
        return type.Name;
    }
}
