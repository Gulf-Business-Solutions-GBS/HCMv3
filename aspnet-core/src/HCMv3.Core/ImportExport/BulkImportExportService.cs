using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Transactions;
using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Timing;
using ClosedXML.Excel;
using HCMv3.SystemSecurity;

namespace HCMv3.ImportExport;

/// <inheritdoc cref="IBulkImportExportService"/>
public class BulkImportExportService : HCMv3DomainServiceBase, IBulkImportExportService
{
    /// <summary>
    /// Legacy <c>uscBulkImportExport.ExcludeList</c> was the XPO/DevExpress infrastructure column set
    /// (<c>oid, locked, syscreator, syscreatedon, sysmodifier, sysmodifiedon, systenant,
    /// optimisticlockfield, gcrecord</c>). This is the HCMv3 equivalent: the ABP primary key, the tenant
    /// discriminator and the <c>FullAuditedEntity</c> audit columns -- i.e. everything the framework owns
    /// and an operator must never supply in a spreadsheet.
    /// </summary>
    private static readonly HashSet<string> ExcludedPropertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Id",
        "TenantId",
        "IsDeleted",
        "DeleterUserId",
        "DeletionTime",
        "CreationTime",
        "CreatorUserId",
        "LastModificationTime",
        "LastModifierUserId"
    };

    /// <summary>Legacy <c>GetClassSelect</c> tried <c>Code</c>, then <c>NameEn</c>, then fell back to the
    /// primary key; <c>GetClassObject</c> tried <c>Code</c>, then <c>NameEn</c>, then <c>Name</c>. This is
    /// the union, in legacy's precedence order, used for both directions.</summary>
    private static readonly string[] ReferenceDisplayProperties = { "Code", "NameEn", "Name" };

    private readonly IIocResolver _iocResolver;
    private readonly IRepository<BulkUploadLog> _bulkUploadLogRepository;
    private readonly IRepository<ExecutionLog> _executionLogRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    private static IReadOnlyList<BulkEntityDescriptor> _descriptorCache;
    private static readonly object DescriptorCacheLock = new object();

    public BulkImportExportService(
        IIocResolver iocResolver,
        IRepository<BulkUploadLog> bulkUploadLogRepository,
        IRepository<ExecutionLog> executionLogRepository,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _iocResolver = iocResolver;
        _bulkUploadLogRepository = bulkUploadLogRepository;
        _executionLogRepository = executionLogRepository;
        _unitOfWorkManager = unitOfWorkManager;
    }

    #region Entity discovery (legacy BindModuleName)

    public IReadOnlyList<BulkEntityDescriptor> GetImportableEntities()
    {
        if (_descriptorCache != null)
        {
            return _descriptorCache;
        }

        lock (DescriptorCacheLock)
        {
            if (_descriptorCache == null)
            {
                _descriptorCache = GetImportableEntityTypes()
                    .Select(BuildDescriptor)
                    .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
        }

        return _descriptorCache;
    }

    public BulkEntityDescriptor GetEntityDescriptor(string entityFullName)
    {
        if (string.IsNullOrWhiteSpace(entityFullName))
        {
            // Legacy Main(): `if (ModuleName == "") throw new Exception("Entity not selected.");`
            throw new BulkImportExportException(L("BulkImportEntityNotSelected"));
        }

        var descriptor = GetImportableEntities()
            .FirstOrDefault(d => string.Equals(d.FullName, entityFullName, StringComparison.OrdinalIgnoreCase));

        if (descriptor == null)
        {
            throw new BulkImportExportException(L("BulkImportEntityNotFound", entityFullName));
        }

        return descriptor;
    }

    /// <summary>
    /// Legacy walked <c>AppDomain.CurrentDomain.GetAssemblies()</c> for anything named <c>HCM10*</c> and
    /// kept types whose <c>BaseType == typeof(MyBaseObject)</c>. Here the domain assembly is known
    /// statically (HCMv3.Core), so only that one is scanned and the test is "concrete public
    /// <c>IEntity&lt;int&gt;</c> with a public parameterless constructor".
    /// </summary>
    private static IEnumerable<Type> GetImportableEntityTypes()
    {
        return typeof(BulkUploadLog).Assembly
            .GetTypes()
            .Where(t => t.IsClass
                && t.IsPublic
                && !t.IsAbstract
                && !t.IsGenericTypeDefinition
                && typeof(IEntity<int>).IsAssignableFrom(t)
                && t.GetConstructor(Type.EmptyTypes) != null);
    }

    private static BulkEntityDescriptor BuildDescriptor(Type type)
    {
        var ns = type.Namespace ?? "";
        var area = ns.StartsWith("HCMv3.", StringComparison.Ordinal)
            ? ns.Substring("HCMv3.".Length).Split('.')[0]
            : ns;

        return new BulkEntityDescriptor
        {
            Name = type.Name,
            FullName = type.FullName,
            Area = area,
            Columns = GetImportableProperties(type).Select(p => p.Name).ToList()
        };
    }

    /// <summary>
    /// The column set, in declaration order. Mirrors legacy's loop over
    /// <c>XPClassInfo.PersistentProperties</c> minus <c>ExcludeList</c>.
    ///
    /// <para>Additional exclusions with no legacy counterpart, and why:
    /// <list type="bullet">
    /// <item><b>Collection properties</b> -- legacy's <c>PersistentProperties</c> never contained XPO
    /// association collections, so there is nothing to drop; here they must be filtered explicitly.</item>
    /// <item><b><c>byte[]</c> properties</b> (e.g. <c>Employee.Photo</c>) -- legacy had a stubbed-out,
    /// commented `else if (memberinfo.MemberType == typeof(Byte[]))` branch and would have emitted a raw
    /// varbinary column into the spreadsheet. Binary payloads do not round-trip through a text template;
    /// photos have their own dedicated importer (<see cref="IProfilePhotoImportService"/>, legacy
    /// <c>uscBulkImport</c>).</item>
    /// <item><b>The <c>{Nav}Id</c> shadow scalar</b> when a matching reference navigation property exists
    /// (e.g. <c>EmployeeId</c> is dropped because <c>Employee</c> is present). Legacy XPO had exactly one
    /// member per reference, so emitting both would produce two columns for one relationship that could
    /// disagree. The navigation column (matched by <c>Code</c>/<c>NameEn</c>/<c>Name</c>) is kept, matching
    /// legacy's behaviour; the FK id is written through from it on import.</item>
    /// </list></para>
    /// </summary>
    internal static List<PropertyInfo> GetImportableProperties(Type type)
    {
        var all = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0)
            .ToList();

        var navigationNames = new HashSet<string>(
            all.Where(p => IsReferenceProperty(p.PropertyType)).Select(p => p.Name),
            StringComparer.OrdinalIgnoreCase);

        return all
            .Where(p => !ExcludedPropertyNames.Contains(p.Name))
            .Where(p => !IsCollectionProperty(p.PropertyType))
            .Where(p => p.PropertyType != typeof(byte[]))
            .Where(p => IsReferenceProperty(p.PropertyType) || IsSupportedScalar(p.PropertyType))
            .Where(p => !(p.Name.EndsWith("Id", StringComparison.Ordinal)
                          && p.Name.Length > 2
                          && navigationNames.Contains(p.Name.Substring(0, p.Name.Length - 2))))
            .ToList();
    }

    private static bool IsReferenceProperty(Type t)
    {
        return t.IsClass && t != typeof(string) && typeof(IEntity<int>).IsAssignableFrom(t);
    }

    private static bool IsCollectionProperty(Type t)
    {
        return t != typeof(string) && typeof(IEnumerable).IsAssignableFrom(t);
    }

    private static bool IsSupportedScalar(Type t)
    {
        var u = Nullable.GetUnderlyingType(t) ?? t;
        return u == typeof(string)
            || u == typeof(int)
            || u == typeof(long)
            || u == typeof(short)
            || u == typeof(double)
            || u == typeof(decimal)
            || u == typeof(float)
            || u == typeof(bool)
            || u == typeof(DateTime)
            || u == typeof(TimeSpan)
            || u == typeof(Guid)
            || u.IsEnum;
    }

    #endregion

    #region Export (legacy DownloadGrid / GetDataToExport)

    public byte[] BuildWorkbook(string entityFullName, bool withData)
    {
        var descriptor = GetEntityDescriptor(entityFullName);
        var type = ResolveEntityType(entityFullName);
        var properties = GetImportableProperties(type);

        using (var workbook = new XLWorkbook())
        {
            // Legacy named the exported sheet nothing in particular (ASPxGridView default) but named the
            // *file* "{EntityName}_{yyyy_MM_dd_HHmm}". The file name is set by the app service; the sheet is
            // named after the entity so an operator can see what a stray template belongs to. Excel caps
            // sheet names at 31 chars.
            var sheetName = descriptor.Name.Length > 31 ? descriptor.Name.Substring(0, 31) : descriptor.Name;
            var sheet = workbook.Worksheets.Add(sheetName);

            for (var c = 0; c < properties.Count; c++)
            {
                sheet.Cell(1, c + 1).Value = properties[c].Name;
            }

            sheet.Row(1).Style.Font.Bold = true;

            if (withData)
            {
                var rows = GetAllEntities(type);
                var displayCaches = new Dictionary<Type, Dictionary<int, string>>();

                var r = 2;
                foreach (var entity in rows)
                {
                    for (var c = 0; c < properties.Count; c++)
                    {
                        var text = FormatForExport(properties[c], entity, displayCaches);
                        if (text != null)
                        {
                            sheet.Cell(r, c + 1).SetValue(text);
                        }
                    }

                    r++;
                }
            }

            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                return stream.ToArray();
            }
        }
    }

    /// <summary>
    /// Value formatting, matching legacy's SQL <c>CASE</c> projections in <c>GetDataToExport</c> one for one:
    /// bool -&gt; <c>YES</c>/<c>NO</c>; a <c>DateTime</c> column whose name contains "time" -&gt;
    /// <c>CONVERT(VARCHAR(5), x, 108)</c> i.e. <c>HH:mm</c>; enum -&gt; <c>Enum.GetName</c>; reference -&gt;
    /// the target row's <c>Code</c>/<c>NameEn</c> (legacy fell back to the raw key, this port falls back to
    /// <c>Name</c> first -- see <see cref="ReferenceDisplayProperties"/> -- then to the key).
    ///
    /// <para>DEVIATION: dates are emitted as <c>dd-MM-yyyy</c> text rather than a native Excel date, because
    /// that is exactly one of the two formats legacy's importer (<c>GetDateTimeValue</c>) parses, so an
    /// exported file re-imports cleanly. Legacy relied on OLE DB handing back typed values.</para>
    /// </summary>
    private string FormatForExport(PropertyInfo property, object entity, Dictionary<Type, Dictionary<int, string>> displayCaches)
    {
        if (IsReferenceProperty(property.PropertyType))
        {
            // Read the FK scalar, NOT the navigation instance: nothing eager-loads these navigations and
            // lazy loading is off, so property.GetValue would return null for every row and the whole
            // reference column would export blank. Legacy sidestepped this by LEFT JOINing in raw SQL.
            var fk = entity.GetType().GetProperty(property.Name + "Id", BindingFlags.Public | BindingFlags.Instance);
            var fkValue = fk?.GetValue(entity);
            if (fkValue == null)
            {
                return null;
            }

            var key = Convert.ToInt32(fkValue, CultureInfo.InvariantCulture);
            if (key == 0)
            {
                return null;
            }

            return GetDisplayCache(property.PropertyType, displayCaches).TryGetValue(key, out var display)
                ? display
                : key.ToString(CultureInfo.InvariantCulture);
        }

        var value = property.GetValue(entity);
        if (value == null)
        {
            return null;
        }

        var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        if (propertyType == typeof(bool))
        {
            return (bool)value ? "YES" : "NO";
        }

        if (propertyType.IsEnum)
        {
            return Enum.GetName(propertyType, value);
        }

        if (propertyType == typeof(DateTime))
        {
            var dt = (DateTime)value;
            return property.Name.IndexOf("time", StringComparison.OrdinalIgnoreCase) >= 0
                ? dt.ToString("HH:mm", CultureInfo.InvariantCulture)
                : dt.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
        }

        if (propertyType == typeof(TimeSpan))
        {
            return ((TimeSpan)value).ToString(@"hh\:mm", CultureInfo.InvariantCulture);
        }

        return Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    private Dictionary<int, string> GetDisplayCache(Type referencedType, Dictionary<Type, Dictionary<int, string>> caches)
    {
        if (caches.TryGetValue(referencedType, out var existing))
        {
            return existing;
        }

        var displayProperty = FindDisplayProperty(referencedType);
        var map = new Dictionary<int, string>();

        foreach (var row in GetAllEntities(referencedType))
        {
            var id = ((IEntity<int>)row).Id;
            map[id] = displayProperty == null
                ? id.ToString(CultureInfo.InvariantCulture)
                : Convert.ToString(displayProperty.GetValue(row), CultureInfo.InvariantCulture);
        }

        caches[referencedType] = map;
        return map;
    }

    private static PropertyInfo FindDisplayProperty(Type type)
    {
        foreach (var name in ReferenceDisplayProperties)
        {
            var p = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (p != null && p.PropertyType == typeof(string))
            {
                return p;
            }
        }

        return null;
    }

    #endregion

    #region Import (legacy Main / UploadData / SaveLog)

    public async Task<BulkImportRunResult> ImportAsync(BulkImportRunInput input)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        var startedAt = Clock.Now;
        var descriptor = GetEntityDescriptor(input.EntityFullName);
        var type = ResolveEntityType(input.EntityFullName);

        // Legacy Main(): `if (file == "") throw new Exception("File not selected.");`
        if (input.FileContent == null || input.FileContent.Length == 0)
        {
            throw new BulkImportExportException(L("BulkImportFileNotSelected"));
        }

        var properties = GetImportableProperties(type).ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        var sheet = ReadSheet(input.FileContent);
        var headers = ReadHeaders(sheet);

        // Legacy validated the header row inside the per-row try/catch, which logged the same
        // "Column Name : X not found in Y" message once per data row. Validating up front instead produces
        // one clear failure -- the operator gets the identical message text, not N copies of it.
        var unknown = headers.Where(h => !properties.ContainsKey(h)).ToList();
        if (unknown.Any())
        {
            throw new BulkImportExportException(string.Join(
                Environment.NewLine,
                unknown.Select(h => L("BulkImportColumnNotFound", h, descriptor.Name))));
        }

        var dataRows = ReadDataRows(sheet, headers);
        var totalRows = dataRows.Count;

        var executionLogId = await CreateExecutionLogAsync(input, descriptor, totalRows, startedAt);

        // Legacy purged inside UploadData, before the row loop, and only when not validating.
        if (input.ImportType == BulkImportType.Purge && !input.ValidateOnly)
        {
            await PurgeAsync(input.TenantId, type);
        }

        var lookupCaches = new Dictionary<Type, Dictionary<string, int>>();
        var succeeded = 0;
        var failed = 0;
        var aborted = false;

        foreach (var row in dataRows)
        {
            // Legacy checked `objSpace.GetObjectsQuery<BulkUploadLog>().Where(x => x.StopFlag).Any()` at the
            // top of every iteration and `break`s.
            if (await IsStopRequestedAsync(input.TenantId))
            {
                aborted = true;
                break;
            }

            string errorMessage = null;

            try
            {
                await RunInOwnUnitOfWorkAsync(input.TenantId, async () =>
                {
                    var errors = new List<string>();
                    var values = new Dictionary<PropertyInfo, object>();

                    foreach (var header in headers)
                    {
                        var property = properties[header];
                        values[property] = ConvertCell(property, row.Cells[header], lookupCaches, errors);
                    }

                    if (errors.Any())
                    {
                        // Legacy: `msg = string.Format("Excel RowNo : {0} - " + msg, rowNumner)`.
                        throw new BulkImportExportException(
                            L("BulkImportRowError", row.RowNumber, string.Join(", ", errors)));
                    }

                    if (input.ValidateOnly)
                    {
                        return;
                    }

                    var entity = Activator.CreateInstance(type);
                    foreach (var pair in values)
                    {
                        ApplyValue(entity, pair.Key, pair.Value);
                    }

                    if (entity is IMustHaveTenant mustHaveTenant)
                    {
                        mustHaveTenant.TenantId = input.TenantId;
                    }
                    else if (entity is IMayHaveTenant mayHaveTenant)
                    {
                        mayHaveTenant.TenantId = input.TenantId;
                    }

                    await InsertAsync(type, entity);
                });
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }

            if (errorMessage == null)
            {
                succeeded++;
                // Legacy's success message is the literal string "Successfull".
                await SaveLogAsync(input.TenantId, "Successfull", 0, totalRows, row.RowNumber);
            }
            else
            {
                failed++;
                await SaveLogAsync(input.TenantId, errorMessage, 1, totalRows, row.RowNumber);
            }
        }

        var result = new BulkImportRunResult
        {
            ExecutionLogId = executionLogId,
            TotalRecords = totalRows,
            Successfull = succeeded,
            Failed = failed,
            Aborted = aborted,
            ValidateOnly = input.ValidateOnly,
            StartedAt = startedAt,
            FinishedAt = Clock.Now
        };

        await CompleteExecutionLogAsync(input.TenantId, executionLogId, result);

        return result;
    }

    public async Task RequestStopAsync()
    {
        // Legacy took the FIRST log row and flipped its StopFlag. If the log is empty (stop pressed before
        // the first row was processed) legacy silently did nothing and the run continued; this port inserts
        // a flag-only marker row so the request is never lost.
        var existing = await _bulkUploadLogRepository.FirstOrDefaultAsync(x => true);
        if (existing != null)
        {
            existing.StopFlag = true;
            await _bulkUploadLogRepository.UpdateAsync(existing);
            return;
        }

        await _bulkUploadLogRepository.InsertAsync(new BulkUploadLog
        {
            LogType = BulkUploadLogType.BulkDataImport,
            Message = L("BulkImportStopRequested"),
            RunTimeStamp = Clock.Now,
            InPeriod = Clock.Now.Date,
            Status = 0,
            StopFlag = true
        });
    }

    public async Task ClearLogAsync()
    {
        // Legacy: TRUNCATE TABLE [dbo].[BulkUploadLog].
        await _bulkUploadLogRepository.DeleteAsync(x => true);
    }

    #endregion

    #region Cell conversion (legacy validation region + GetDateTimeValue / GetClassObject / GetEnumObject)

    /// <summary>
    /// Reproduces legacy's per-property type switch inside <c>UploadData</c>'s <c>&lt;&lt; Validation &gt;&gt;</c>
    /// region. Legacy's exact quirks that are kept:
    /// <list type="bullet">
    /// <item>numeric/blank -&gt; <c>0</c> (legacy <c>SetIfNull(dr[key], 0)</c>), NOT an error;</item>
    /// <item>bool -&gt; <c>text == "YES"</c>, everything else (including blank and "TRUE") is false;</item>
    /// <item>a blank date, reference or enum cell is NOT an error -- it yields default/null. Legacy had the
    /// "is blank" error branches commented out in all three helpers;</item>
    /// <item>a date column whose name contains "time" keeps only the time-of-day.</item>
    /// </list>
    /// </summary>
    private object ConvertCell(PropertyInfo property, ExcelCell cell, Dictionary<Type, Dictionary<string, int>> lookupCaches, List<string> errors)
    {
        var text = cell?.Text ?? "";
        var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        if (IsReferenceProperty(property.PropertyType))
        {
            if (text.Length == 0)
            {
                return null;
            }

            var map = GetLookupCache(property.PropertyType, lookupCaches);
            if (map.TryGetValue(text, out var id))
            {
                return id;
            }

            // Legacy: `msg + col + " : '" + value + "' Not exists."`
            errors.Add(L("BulkImportReferenceNotFound", property.Name, text));
            return null;
        }

        if (propertyType == typeof(string))
        {
            return text;
        }

        if (propertyType == typeof(bool))
        {
            return string.Equals(text, "YES", StringComparison.OrdinalIgnoreCase);
        }

        if (propertyType.IsEnum)
        {
            if (text.Length == 0)
            {
                return null;
            }

            if (Enum.TryParse(propertyType, text, true, out var enumValue))
            {
                return enumValue;
            }

            errors.Add(L("BulkImportReferenceNotFound", property.Name, text));
            return null;
        }

        if (propertyType == typeof(DateTime))
        {
            return ConvertDateTime(property.Name, cell, errors);
        }

        if (propertyType == typeof(TimeSpan))
        {
            if (text.Length == 0)
            {
                return null;
            }

            if (TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out var ts))
            {
                return ts;
            }

            errors.Add(L("BulkImportValueNotValid", property.Name));
            return null;
        }

        if (propertyType == typeof(Guid))
        {
            if (text.Length == 0)
            {
                return Guid.Empty;
            }

            if (Guid.TryParse(text, out var g))
            {
                return g;
            }

            errors.Add(L("BulkImportValueNotValid", property.Name));
            return null;
        }

        // Numerics. Legacy substituted 0 for blank and let a malformed value throw out of the row.
        var numericText = text.Length == 0 ? "0" : text;
        try
        {
            if (propertyType == typeof(int)) return int.Parse(numericText, CultureInfo.InvariantCulture);
            if (propertyType == typeof(long)) return long.Parse(numericText, CultureInfo.InvariantCulture);
            if (propertyType == typeof(short)) return short.Parse(numericText, CultureInfo.InvariantCulture);
            if (propertyType == typeof(double)) return double.Parse(numericText, CultureInfo.InvariantCulture);
            if (propertyType == typeof(float)) return float.Parse(numericText, CultureInfo.InvariantCulture);
            if (propertyType == typeof(decimal)) return decimal.Parse(numericText, CultureInfo.InvariantCulture);
        }
        catch (FormatException)
        {
            errors.Add(L("BulkImportValueNotValid", property.Name));
            return null;
        }
        catch (OverflowException)
        {
            errors.Add(L("BulkImportValueNotValid", property.Name));
            return null;
        }

        errors.Add(L("BulkImportValueNotValid", property.Name));
        return null;
    }

    /// <summary>Direct port of legacy <c>GetDateTimeValue</c>.</summary>
    private object ConvertDateTime(string columnName, ExcelCell cell, List<string> errors)
    {
        var text = cell?.Text ?? "";

        if (text.Length == 0)
        {
            // Legacy left `goahead` alone here -- a blank date is not an error, it becomes default(DateTime).
            return default(DateTime);
        }

        if (columnName.IndexOf("time", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            // Legacy: `DateTime.Parse(text).GetTime()` -- time-of-day only, on the zero date.
            if (cell.DateValue.HasValue)
            {
                return default(DateTime).Add(cell.DateValue.Value.TimeOfDay);
            }

            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedTime))
            {
                return default(DateTime).Add(parsedTime.TimeOfDay);
            }

            if (TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out var ts))
            {
                return default(DateTime).Add(ts);
            }

            errors.Add(L("BulkImportValueNotValid", columnName));
            return default(DateTime);
        }

        // Legacy: typed OLE DB value wins, otherwise dd-MM-yyyy then dd/MM/yyyy, both InvariantCulture.
        if (cell.DateValue.HasValue)
        {
            return cell.DateValue.Value;
        }

        if (DateTime.TryParseExact(text, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d1))
        {
            return d1;
        }

        if (DateTime.TryParseExact(text, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d2))
        {
            return d2;
        }

        errors.Add(L("BulkImportValueNotValid", columnName));
        return default(DateTime);
    }

    /// <summary>
    /// Legacy <c>GetClassObject</c> issued one <c>FindObject</c> per cell. That is O(rows x reference
    /// columns) round trips; this port loads each referenced type once per run into a
    /// display-value -&gt; id map. Same matching rule (<c>Code</c>, else <c>NameEn</c>, else <c>Name</c>),
    /// same "not exists" error.
    /// </summary>
    private Dictionary<string, int> GetLookupCache(Type referencedType, Dictionary<Type, Dictionary<string, int>> caches)
    {
        if (caches.TryGetValue(referencedType, out var existing))
        {
            return existing;
        }

        var displayProperty = FindDisplayProperty(referencedType);
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        if (displayProperty != null)
        {
            foreach (var row in GetAllEntities(referencedType))
            {
                var key = Convert.ToString(displayProperty.GetValue(row), CultureInfo.InvariantCulture);
                if (!string.IsNullOrEmpty(key) && !map.ContainsKey(key))
                {
                    // Legacy used FindObject, i.e. "first match wins" on duplicates. Preserved.
                    map[key] = ((IEntity<int>)row).Id;
                }
            }
        }

        caches[referencedType] = map;
        return map;
    }

    /// <summary>
    /// Writes a converted value onto the new entity. For reference columns the converted value is the target
    /// <c>Id</c>, which is written to the <c>{Nav}Id</c> shadow scalar when the entity has one (every EF
    /// entity in this port does) rather than by attaching a tracked navigation instance.
    /// </summary>
    private static void ApplyValue(object entity, PropertyInfo property, object value)
    {
        if (IsReferenceProperty(property.PropertyType))
        {
            var fk = entity.GetType().GetProperty(property.Name + "Id", BindingFlags.Public | BindingFlags.Instance);
            if (fk == null)
            {
                throw new BulkImportExportException($"Entity '{entity.GetType().Name}' has no foreign key property '{property.Name}Id' for reference column '{property.Name}'.");
            }

            var fkType = Nullable.GetUnderlyingType(fk.PropertyType) ?? fk.PropertyType;
            if (value == null)
            {
                if (Nullable.GetUnderlyingType(fk.PropertyType) == null)
                {
                    // A required reference left blank: legacy would have written 0 and let the database
                    // reject it. Fail loudly instead so the operator gets a row-level message.
                    throw new BulkImportExportException($"'{property.Name}' is required and was blank.");
                }

                fk.SetValue(entity, null);
                return;
            }

            fk.SetValue(entity, Convert.ChangeType(value, fkType, CultureInfo.InvariantCulture));
            return;
        }

        if (value == null)
        {
            property.SetValue(entity, null);
            return;
        }

        var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        property.SetValue(entity, targetType.IsEnum || targetType == typeof(DateTime) || targetType == typeof(TimeSpan) || targetType == typeof(Guid) || targetType == typeof(string)
            ? value
            : Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture));
    }

    #endregion

    #region Workbook reading

    private sealed class ExcelCell
    {
        public string Text { get; set; }
        public DateTime? DateValue { get; set; }
    }

    private sealed class ExcelRow
    {
        public int RowNumber { get; set; }
        public Dictionary<string, ExcelCell> Cells { get; } = new Dictionary<string, ExcelCell>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Legacy <c>GetDataFromExcel(fileName)</c> opened the file with the <c>Microsoft.ACE.OLEDB.12.0</c>
    /// provider and took the FIRST sheet whose name ended in <c>$</c>. ClosedXML reads the same
    /// <c>.xlsx</c> without an out-of-process COM provider; "first sheet" is preserved.
    ///
    /// <para>Legacy swallowed every exception here and returned an empty <c>DataTable</c>, so a corrupt
    /// upload silently "succeeded" with zero rows. That is not reproduced -- a corrupt file throws.</para>
    /// </summary>
    private IXLWorksheet ReadSheet(byte[] content)
    {
        try
        {
            using (var stream = new MemoryStream(content))
            {
                var workbook = new XLWorkbook(stream);
                var sheet = workbook.Worksheets.FirstOrDefault();
                if (sheet == null)
                {
                    throw new BulkImportExportException(L("BulkImportWorkbookHasNoSheet"));
                }

                return sheet;
            }
        }
        catch (BulkImportExportException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new BulkImportExportException(L("BulkImportWorkbookUnreadable", ex.Message));
        }
    }

    private static List<string> ReadHeaders(IXLWorksheet sheet)
    {
        var lastColumn = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        var headers = new List<string>();

        for (var c = 1; c <= lastColumn; c++)
        {
            var header = sheet.Cell(1, c).GetString()?.Trim();
            if (!string.IsNullOrEmpty(header))
            {
                headers.Add(header);
            }
        }

        return headers;
    }

    private static List<ExcelRow> ReadDataRows(IXLWorksheet sheet, List<string> headers)
    {
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        var rows = new List<ExcelRow>();

        for (var r = 2; r <= lastRow; r++)
        {
            var row = new ExcelRow { RowNumber = r };
            var anyValue = false;

            for (var c = 0; c < headers.Count; c++)
            {
                var cell = sheet.Cell(r, c + 1);
                var text = cell.GetString()?.Trim() ?? "";
                DateTime? dateValue = null;

                if (cell.DataType == XLDataType.DateTime)
                {
                    try
                    {
                        dateValue = cell.GetDateTime();
                        text = dateValue.Value.ToString("dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                    }
                    catch (Exception)
                    {
                        // fall back to the raw text
                    }
                }

                if (text.Length > 0)
                {
                    anyValue = true;
                }

                row.Cells[headers[c]] = new ExcelCell { Text = text, DateValue = dateValue };
            }

            // Trailing formatted-but-empty rows are common in hand-edited templates and legacy's OLE DB
            // reader did not return them. Skipping them keeps the Failed count honest.
            if (anyValue)
            {
                rows.Add(row);
            }
        }

        return rows;
    }

    #endregion

    #region Generic repository access

    private Type ResolveEntityType(string entityFullName)
    {
        var type = typeof(BulkUploadLog).Assembly.GetType(entityFullName, false, true);
        if (type == null)
        {
            throw new BulkImportExportException(L("BulkImportEntityNotFound", entityFullName));
        }

        return type;
    }

    /// <summary>
    /// The layer-correct stand-in for legacy's raw <c>SysService.GetData(sql)</c>: resolves the closed
    /// <c>IRepository&lt;TEntity, int&gt;</c> for a runtime-known type and materialises it. Tenant filtering
    /// and soft-delete filtering are therefore applied by ABP automatically -- legacy's raw SQL had neither.
    /// </summary>
    private List<object> GetAllEntities(Type entityType)
    {
        // Opens its own unit of work rather than relying on an ambient one: BuildWorkbook is a plain
        // synchronous call with no [UnitOfWork] on the path, so without this the resolved repository would
        // reach for an already-disposed DbContext. A nested Begin() joins an ambient UOW when there is one
        // (which is the case during ImportAsync's lookup-cache warm-up).
        using (var uow = _unitOfWorkManager.Begin())
        {
            var repositoryType = typeof(IRepository<,>).MakeGenericType(entityType, typeof(int));
            using (var repository = _iocResolver.ResolveAsDisposable(repositoryType))
            {
                var getAll = repositoryType.GetMethod("GetAll", Type.EmptyTypes);
                var queryable = (IQueryable)getAll.Invoke(repository.Object, null);
                var rows = queryable.Cast<object>().ToList();
                uow.Complete();
                return rows;
            }
        }
    }

    private async Task InsertAsync(Type entityType, object entity)
    {
        var repositoryType = typeof(IRepository<,>).MakeGenericType(entityType, typeof(int));
        using (var repository = _iocResolver.ResolveAsDisposable(repositoryType))
        {
            var insert = repositoryType.GetMethod("InsertAsync", new[] { entityType });
            await (Task)insert.Invoke(repository.Object, new[] { entity });
        }
    }

    /// <summary>Legacy: <c>DELETE FROM [X]; DBCC CHECKIDENT ([X], RESEED, 0)</c>. See
    /// <see cref="BulkImportType.Purge"/> for the deviations this repository-based version carries.</summary>
    private async Task PurgeAsync(int tenantId, Type entityType)
    {
        await RunInOwnUnitOfWorkAsync(tenantId, async () =>
        {
            var repositoryType = typeof(IRepository<,>).MakeGenericType(entityType, typeof(int));
            using (var repository = _iocResolver.ResolveAsDisposable(repositoryType))
            {
                var getAll = repositoryType.GetMethod("GetAll", Type.EmptyTypes);
                var queryable = (IQueryable)getAll.Invoke(repository.Object, null);
                var delete = repositoryType.GetMethod("DeleteAsync", new[] { entityType });

                foreach (var row in queryable.Cast<object>().ToList())
                {
                    await (Task)delete.Invoke(repository.Object, new[] { row });
                }
            }
        });
    }

    #endregion

    #region Logging

    private async Task<int> CreateExecutionLogAsync(BulkImportRunInput input, BulkEntityDescriptor descriptor, int totalRows, DateTime startedAt)
    {
        var log = new ExecutionLog
        {
            TenantId = input.TenantId,
            UserName = input.CurrentUserName,
            ActionDateTime = startedAt,
            Action = ExecutionLogAction.RUN,
            Module = ExecutionLogModule.BulkImportExport,
            // The legacy screen name is preserved verbatim so existing execution-log reports keep working,
            // matching what the Time Engine port does.
            ScreenName = "uscBulkImportExport",
            ScreenCaption = descriptor.Name,
            Parameters = string.Format(
                CultureInfo.InvariantCulture,
                "Entity={0}; ImportType={1}; ValidateOnly={2}; File={3}",
                descriptor.FullName, input.ImportType, input.ValidateOnly, input.FileName),
            ExecutionStatus = ExecutionLogStatus.INPRECESS,
            TotalRecords = totalRows,
            Successfull = 0,
            Failed = 0
        };

        var id = 0;
        await RunInOwnUnitOfWorkAsync(input.TenantId, async () =>
        {
            // InsertAndGetIdAsync, not InsertAsync: the identity value is read back below.
            id = await _executionLogRepository.InsertAndGetIdAsync(log);
        });

        return id;
    }

    private async Task CompleteExecutionLogAsync(int tenantId, int executionLogId, BulkImportRunResult result)
    {
        await RunInOwnUnitOfWorkAsync(tenantId, async () =>
        {
            var log = await _executionLogRepository.FirstOrDefaultAsync(executionLogId);
            if (log == null)
            {
                return;
            }

            log.TotalRecords = result.TotalRecords;
            log.Successfull = result.Successfull;
            log.Failed = result.Failed;
            log.ExecutionStatus = result.Aborted
                ? ExecutionLogStatus.ABORTED
                : result.Failed == 0
                    ? ExecutionLogStatus.SUCCESS
                    : result.Successfull == 0
                        ? ExecutionLogStatus.FAILED
                        : ExecutionLogStatus.PARTIAL;

            await _executionLogRepository.UpdateAsync(log);
        });
    }

    /// <summary>Direct port of legacy <c>SaveLog(...)</c>, including its 1000-character message truncation.</summary>
    private async Task SaveLogAsync(int tenantId, string message, int status, int totalRows, int rowNumber)
    {
        message ??= "";
        var length = message.Length > 1000 ? 1000 : message.Length;

        await RunInOwnUnitOfWorkAsync(tenantId, async () =>
        {
            await _bulkUploadLogRepository.InsertAsync(new BulkUploadLog
            {
                TenantId = tenantId,
                LogType = BulkUploadLogType.BulkDataImport,
                // Legacy passed `Employee emp` = null for every generic-import row.
                EmployeeId = null,
                Message = message.Substring(0, length),
                Status = status,
                RunTimeStamp = Clock.Now,
                InPeriod = Clock.Now.Date,
                NoOfEmployee = totalRows,
                RecordNo = rowNumber
            });
        });
    }

    private async Task<bool> IsStopRequestedAsync(int tenantId)
    {
        var stopped = false;
        await RunInOwnUnitOfWorkAsync(tenantId, async () =>
        {
            stopped = await _bulkUploadLogRepository.CountAsync(x => x.StopFlag) > 0;
        });

        return stopped;
    }

    /// <summary>
    /// Legacy created a brand-new <c>IObjectSpace</c> per row (and per log write) so one bad row could not
    /// take the rest of the batch with it. The ABP equivalent is a nested unit of work with
    /// <see cref="TransactionScopeOption.RequiresNew"/>; the tenant has to be re-applied because a
    /// background job starts outside any tenant scope.
    /// </summary>
    private async Task RunInOwnUnitOfWorkAsync(int tenantId, Func<Task> action)
    {
        using (var uow = _unitOfWorkManager.Begin(TransactionScopeOption.RequiresNew))
        {
            using (_unitOfWorkManager.Current.SetTenantId(tenantId))
            {
                await action();
                await uow.CompleteAsync();
            }
        }
    }

    #endregion
}
