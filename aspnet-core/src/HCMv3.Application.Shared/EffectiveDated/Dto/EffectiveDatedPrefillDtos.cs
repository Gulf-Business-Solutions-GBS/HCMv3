using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.EffectiveDated.Dto;

/// <summary>
/// Asks for the values of the record that was in force on <see cref="EffectiveDate"/>, so a new record can start
/// from them. See <c>IEffectiveDatedPrefillAppService</c>.
/// </summary>
public class EffectiveDatedPrefillInput
{
    /// <summary>Which kind of record, e.g. "EmployeeBasicPay". The names are listed in <c>EffectiveDatedCatalog</c>.</summary>
    [Required]
    [StringLength(128)]
    public string RecordType { get; set; }

    /// <summary>
    /// Whose history to look in: the employee, wage type, organisation unit and so on. A record type grouped by more
    /// than one key -- a fare is per route, passenger type and class -- needs all of them.
    /// </summary>
    public List<EffectiveDatedKeyDto> Keys { get; set; } = new List<EffectiveDatedKeyDto>();

    /// <summary>The date the new record is to take effect from.</summary>
    [Required]
    public DateTime EffectiveDate { get; set; }
}

/// <summary>One of the keys that group a history, by property name.</summary>
public class EffectiveDatedKeyDto
{
    [Required]
    [StringLength(128)]
    public string Name { get; set; }

    [Required]
    public long Value { get; set; }
}

/// <summary>
/// The values to start the new record from: the record in force on the asked-for date, field by field.
/// </summary>
public class EffectiveDatedPrefillDto
{
    /// <summary>False when nothing was in force then -- the form is left as it is, empty.</summary>
    public bool Found { get; set; }

    /// <summary>The effective date of the record the values came from, so the screen can say where they are from.</summary>
    public DateTime? SourceDate { get; set; }

    /// <summary>
    /// Field name to value, named as the record's own fields are. Dates, the id and the audit fields are left out:
    /// the new record gets the date the user typed, and is a new record.
    /// </summary>
    public Dictionary<string, object> Values { get; set; } = new Dictionary<string, object>();
}
