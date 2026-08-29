using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;

namespace HCMv3.GoLiveSetup.Apply;

/// <summary>
/// Reads a step's saved payload back into something a stage can walk.
///
/// <para>The payload is JSON the client owns, so everything here is defensive: a missing step, an
/// unparseable payload and a cell of the wrong type all produce an empty or null result rather than an
/// exception. Apply reports what it could not use; it does not fall over.</para>
/// </summary>
public class GoLiveDraftReader : ITransientDependency
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    private readonly IRepository<GoLiveStepState> _states;

    public GoLiveDraftReader(IRepository<GoLiveStepState> states)
    {
        _states = states;
    }

    /// <summary>Rows saved for a data step, or empty when the step was never filled in.</summary>
    public async Task<List<Dictionary<string, JsonElement>>> ReadRowsAsync(string stepId)
    {
        var payload = await ReadPayloadAsync(stepId);
        if (payload == null)
        {
            return new List<Dictionary<string, JsonElement>>();
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<RowsPayload>(payload, Options);
            return parsed?.Rows ?? new List<Dictionary<string, JsonElement>>();
        }
        catch (JsonException)
        {
            return new List<Dictionary<string, JsonElement>>();
        }
    }

    /// <summary>Answers saved for a question step, keyed by question ref.</summary>
    public async Task<Dictionary<string, JsonElement>> ReadAnswersAsync(string stepId)
    {
        var payload = await ReadPayloadAsync(stepId);
        if (payload == null)
        {
            return new Dictionary<string, JsonElement>();
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<AnswersPayload>(payload, Options);
            return parsed?.Answers ?? new Dictionary<string, JsonElement>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, JsonElement>();
        }
    }

    /// <summary>A cell as trimmed text, or null when absent, null or blank.</summary>
    public static string Text(Dictionary<string, JsonElement> row, string key)
    {
        if (!row.TryGetValue(key, out var value))
        {
            return null;
        }

        var text = value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null,
        };

        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    /// <summary>A cell as a number. The grid stores numbers typed, but a pasted value may still be text.</summary>
    public static double? Number(Dictionary<string, JsonElement> row, string key)
    {
        if (!row.TryGetValue(key, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var direct))
        {
            return direct;
        }

        var text = Text(row, key);
        return double.TryParse(text, out var parsed) ? parsed : null;
    }

    /// <summary>A cell as a date. The grid normalises to ISO before storing, so this is a strict parse.</summary>
    public static DateTime? Date(Dictionary<string, JsonElement> row, string key)
    {
        var text = Text(row, key);
        if (text == null)
        {
            return null;
        }

        return DateTime.TryParse(text, out var parsed) ? parsed.Date : null;
    }

    /// <summary>True when nothing in the row was filled in -- the grid's trailing padding.</summary>
    public static bool IsEmpty(Dictionary<string, JsonElement> row)
    {
        return row.Keys.All(k => Text(row, k) == null);
    }

    private async Task<string> ReadPayloadAsync(string stepId)
    {
        var state = await _states.FirstOrDefaultAsync(s => s.StepId == stepId);
        return string.IsNullOrWhiteSpace(state?.Payload) ? null : state.Payload;
    }

    private sealed class RowsPayload
    {
        public List<Dictionary<string, JsonElement>> Rows { get; set; }
    }

    private sealed class AnswersPayload
    {
        public Dictionary<string, JsonElement> Answers { get; set; }
    }
}
