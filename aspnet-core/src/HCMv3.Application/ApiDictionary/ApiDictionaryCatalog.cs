using System;
using System.Collections.Generic;
using HCMv3.PersonnelAdministration;

namespace HCMv3.ApiDictionary;

/// <summary>
/// The record types the API dictionary documents, and which method on each service plays which part.
///
/// <para>
/// Declared rather than discovered. The methods follow a naming pattern in most services, but not all of them,
/// and a dictionary that guessed wrong would send a third party to the wrong endpoint. Adding a record type is
/// one entry here; everything else -- verbs, URLs, fields, links -- is read from the code.
/// </para>
/// </summary>
public static class ApiDictionaryCatalog
{
    public static readonly IReadOnlyList<ApiDictionaryCatalogEntry> Entries = new List<ApiDictionaryCatalogEntry>
    {
        new(
            name: "Employee",
            displayNameKey: "Employee",
            serviceImplementation: typeof(EmployeesAppService),
            entityType: typeof(Employee),
            queryMethod: nameof(EmployeesAppService.GetAll),
            readMethod: nameof(EmployeesAppService.GetEmployeeForEdit),
            upsertMethod: nameof(EmployeesAppService.CreateOrEdit),
            deleteMethod: nameof(EmployeesAppService.Delete),
            fieldNotes: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Both behaviours live in EmployeesAppService.CreateAsync and are invisible to reflection, so they
                // are stated here. A third party that sent a Code or a Status would otherwise be surprised.
                ["Code"] = "ApiFieldNoteGeneratedWhenEmpty",
                ["Status"] = "ApiFieldNoteActiveOnCreate"
            },
            ignoredOnCreate: new[] { "Status" })
    };
}

/// <summary>One documented record type.</summary>
public class ApiDictionaryCatalogEntry
{
    public ApiDictionaryCatalogEntry(
        string name,
        string displayNameKey,
        Type serviceImplementation,
        Type entityType,
        string queryMethod,
        string readMethod,
        string upsertMethod,
        string deleteMethod,
        IReadOnlyDictionary<string, string> fieldNotes = null,
        IReadOnlyCollection<string> ignoredOnCreate = null)
    {
        Name = name;
        DisplayNameKey = displayNameKey;
        ServiceImplementation = serviceImplementation;
        EntityType = entityType;
        QueryMethod = queryMethod;
        ReadMethod = readMethod;
        UpsertMethod = upsertMethod;
        DeleteMethod = deleteMethod;
        FieldNotes = fieldNotes ?? new Dictionary<string, string>();
        IgnoredOnCreate = ignoredOnCreate ?? Array.Empty<string>();
    }

    public string Name { get; }

    public string DisplayNameKey { get; }

    /// <summary>The app service class. Its public methods are what ABP publishes as the HTTP API.</summary>
    public Type ServiceImplementation { get; }

    /// <summary>The domain entity, used to tell which record a foreign key points to.</summary>
    public Type EntityType { get; }

    /// <summary>List and search. Null when the service has no such method.</summary>
    public string QueryMethod { get; }

    /// <summary>Read one record by id. Null when unsupported.</summary>
    public string ReadMethod { get; }

    /// <summary>Create, or update when an id is sent. Null when unsupported.</summary>
    public string UpsertMethod { get; }

    /// <summary>Delete by id. Null when unsupported.</summary>
    public string DeleteMethod { get; }

    /// <summary>Field name to the localization key of a note about how the server treats it.</summary>
    public IReadOnlyDictionary<string, string> FieldNotes { get; }

    /// <summary>Fields the create path overwrites, so sending them on create has no effect.</summary>
    public IReadOnlyCollection<string> IgnoredOnCreate { get; }
}
