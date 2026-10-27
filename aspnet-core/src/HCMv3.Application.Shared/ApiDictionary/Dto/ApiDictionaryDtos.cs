using System.Collections.Generic;

namespace HCMv3.ApiDictionary.Dto;

/// <summary>A record type listed in the API dictionary, for the picker.</summary>
public class ApiDictionaryEntitySummaryDto
{
    /// <summary>The name the dictionary knows it by, e.g. <c>Employee</c>.</summary>
    public string Name { get; set; }

    /// <summary>Localized display name.</summary>
    public string DisplayName { get; set; }
}

/// <summary>Everything a third party needs to call one record type's API.</summary>
public class ApiDictionaryEntityDto
{
    public string Name { get; set; }

    public string DisplayName { get; set; }

    /// <summary>
    /// The base address the URLs below are written against. Always the placeholder <c>https://servername</c>:
    /// the dictionary is handed to people outside the company, and the real host is not its to give away.
    /// </summary>
    public string BaseUrl { get; set; }

    public List<ApiDictionaryOperationDto> Operations { get; set; } = new();

    public List<ApiDictionaryFieldDto> Fields { get; set; } = new();
}

/// <summary>One call a third party can make on the record type.</summary>
public class ApiDictionaryOperationDto
{
    /// <summary><c>Query</c>, <c>Read</c>, <c>Upsert</c> or <c>Delete</c> -- the same words SAP's dictionary uses.</summary>
    public string Operation { get; set; }

    /// <summary>GET, POST, PUT or DELETE, exactly as the server answers it.</summary>
    public string HttpMethod { get; set; }

    /// <summary>The full address on the placeholder host, including any query string, e.g. <c>?Id={id}</c>.</summary>
    public string Url { get; set; }

    /// <summary>The permissions the calling user needs, as "Pages.X" or "Create: Pages.X.Create".</summary>
    public List<string> Permissions { get; set; } = new();

    /// <summary>What to send: the query-string or JSON body fields, in the camelCase the server expects.</summary>
    public List<string> RequestFields { get; set; } = new();

    /// <summary>A short description of what comes back.</summary>
    public string Response { get; set; }
}

/// <summary>One field of the record type, with what an integration may do with it.</summary>
public class ApiDictionaryFieldDto
{
    /// <summary>The JSON name, camelCase, exactly as it appears on the wire.</summary>
    public string PropertyName { get; set; }

    public string Label { get; set; }

    /// <summary>string, integer (int32), integer (int64), number, boolean, date-time, enum, binary (base64).</summary>
    public string Type { get; set; }

    /// <summary>For an enum: its values as "Name = number". Empty otherwise.</summary>
    public List<string> EnumValues { get; set; } = new();

    public bool IsKey { get; set; }

    /// <summary>Must be supplied on create and update.</summary>
    public bool IsRequired { get; set; }

    /// <summary>Longest text accepted. Null when there is no limit or the field is not text.</summary>
    public int? MaxLength { get; set; }

    public bool IsNullable { get; set; }

    /// <summary>Returned by the Query operation's list results.</summary>
    public bool InQueryResults { get; set; }

    /// <summary>Returned by the Read operation.</summary>
    public bool InReadResult { get; set; }

    public bool IsCreatable { get; set; }

    public bool IsUpdatable { get; set; }

    /// <summary>The record type this field points to, e.g. <c>NationalityId</c> links to <c>Nationality</c>.</summary>
    public string LinksTo { get; set; }

    /// <summary>
    /// The kind of sensitive personal data the field holds -- Identity, Contact, Address, Family, Health,
    /// Religion, Photo, Financial -- or null when it holds none.
    /// </summary>
    public string SensitiveCategory { get; set; }

    /// <summary>How the server treats the field when that is not obvious from the rest, localized. Null otherwise.</summary>
    public string Note { get; set; }
}
