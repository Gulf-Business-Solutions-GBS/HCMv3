using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HCMv3.AiAgent.Tools;

/// <summary>
/// The model's arguments, read tolerantly.
///
/// <para>
/// A small model sends <c>"3"</c> where the schema says integer, <c>"true"</c> for a boolean, and dates in
/// whatever form the user typed them. Rejecting those outright makes the assistant fail on questions it
/// understood perfectly well; accepting anything makes it guess. So: common variants are accepted, anything
/// else throws <see cref="AiToolArgumentException"/> with a message the model can act on.
/// </para>
/// </summary>
public class AiToolArguments
{
    private static readonly string[] DateFormats =
    {
        "yyyy-MM-dd", "yyyy-M-d", "yyyy/MM/dd", "d/M/yyyy", "dd/MM/yyyy", "d-M-yyyy", "dd-MM-yyyy", "d MMM yyyy", "d MMMM yyyy"
    };

    private readonly JsonElement _root;

    public AiToolArguments(JsonElement root)
    {
        _root = root;
    }

    public static AiToolArguments Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            json = "{}";
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new AiToolArgumentException("Arguments must be a JSON object.");
            }

            // Cloned so the element outlives the document.
            return new AiToolArguments(document.RootElement.Clone());
        }
        catch (JsonException)
        {
            throw new AiToolArgumentException("Arguments are not valid JSON. Send a JSON object.");
        }
    }

    public string GetString(string name)
    {
        if (!TryGet(name, out var value))
        {
            return null;
        }

        var text = value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null
        };

        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    public int? GetInt(string name)
    {
        if (!TryGet(name, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String
            && int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        throw new AiToolArgumentException($"'{name}' must be a whole number.");
    }

    /// <summary>A money amount or other decimal. Accepts "50", "50.000" and "50 KD" -- models copy the user's units.</summary>
    public double? GetNumber(string name)
    {
        if (!TryGet(name, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number)
        {
            return value.GetDouble();
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            var text = value.GetString()!.Trim();
            foreach (var unit in new[] { "KWD", "KD", "د.ك", "دينار" })
            {
                text = text.Replace(unit, string.Empty, StringComparison.OrdinalIgnoreCase);
            }

            if (double.TryParse(text.Replace(",", string.Empty).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }
        }

        throw new AiToolArgumentException($"'{name}' must be a number, e.g. 50 or 12.500.");
    }

    public bool TryGetArray(string name, out List<JsonElement> items)
    {
        items = new List<JsonElement>();

        if (!TryGet(name, out var value))
        {
            return false;
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            items.AddRange(value.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.Object));
            return true;
        }

        // Some models send a single object where the schema says array.
        if (value.ValueKind == JsonValueKind.Object)
        {
            items.Add(value);
            return true;
        }

        return false;
    }

    public bool? GetBool(string name)
    {
        if (!TryGet(name, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(value.GetString(), out var parsed) => parsed,
            _ => throw new AiToolArgumentException($"'{name}' must be true or false.")
        };
    }

    public DateTime? GetDate(string name)
    {
        var text = GetString(name);
        if (text == null)
        {
            return null;
        }

        if (DateTime.TryParseExact(text, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date.Date;
        }

        // A full timestamp ("2026-10-01T00:00:00") is still an unambiguous date.
        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out date) && text.Contains('T'))
        {
            return date.Date;
        }

        throw new AiToolArgumentException($"'{name}' must be a date in yyyy-MM-dd format, e.g. 2026-10-01.");
    }

    private bool TryGet(string name, out JsonElement value)
    {
        if (_root.ValueKind == JsonValueKind.Object
            && _root.TryGetProperty(name, out value)
            && value.ValueKind != JsonValueKind.Null
            && value.ValueKind != JsonValueKind.Undefined)
        {
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>A schema with no arguments.</summary>
    public static JsonObject NoParameters() => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject()
    };
}

/// <summary>The model sent arguments the tool cannot use. The message goes back to the model, in English.</summary>
public class AiToolArgumentException : Exception
{
    public AiToolArgumentException(string message) : base(message)
    {
    }
}
