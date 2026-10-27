using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.AiAgent.Tools;

/// <summary>
/// The employee fields the assistant may change, and how each reads from and writes to the Employees screen's
/// edit DTO. Anything not listed here -- names, Civil ID, nationality, birth date, joining date, status, bank --
/// the assistant cannot touch; those stay on the screen.
/// </summary>
public static class AiEmployeeFields
{
    public class Field
    {
        /// <summary>The tool argument name, as the model sends it.</summary>
        public string Key { get; init; }

        /// <summary>The existing localization key the Employees screen uses for the field.</summary>
        public string LabelKey { get; init; }

        public bool IsPhoneNumber { get; init; }

        /// <summary>Set for a lookup field: the stored value is an id the text must be resolved to.</summary>
        public AiMasterDataKind? Lookup { get; init; }

        public Func<CreateOrEditEmployeeDto, string> Get { get; init; }

        public Action<CreateOrEditEmployeeDto, string> Set { get; init; }
    }

    public static readonly IReadOnlyList<Field> All = new List<Field>
    {
        Text("email", "Email", d => d.Email, (d, v) => d.Email = v),
        Text("alternateEmail", "AlternateEmail", d => d.AlternateEmail, (d, v) => d.AlternateEmail = v),
        Phone("mobile", "Mobile", d => d.Mobile, (d, v) => d.Mobile = v),
        Phone("telephone", "Telephone", d => d.Telephone, (d, v) => d.Telephone = v),
        Text("businessLandline", "BusinessLandline", d => d.BusinessLandline, (d, v) => d.BusinessLandline = v),
        Text("businessExtension", "BusinessExtension", d => d.BusinessExtension, (d, v) => d.BusinessExtension = v),
        Text("currentAddress", "CurrentAddress", d => d.CurrentAddress, (d, v) => d.CurrentAddress = v),
        Text("permanentAddress", "PermanentAddress", d => d.PermanentAddress, (d, v) => d.PermanentAddress = v),
        Text("emergencyContact", "EmergencyContact", d => d.EmergencyContact, (d, v) => d.EmergencyContact = v),
        Text("emergencyNumber", "EmergencyNumber", d => d.EmergencyNumber, (d, v) => d.EmergencyNumber = v),
        Text("emergencyRelation", "EmergencyRelation", d => d.EmergencyRelation, (d, v) => d.EmergencyRelation = v),
        Text("emergencyContactAddress", "EmergencyContactAddress", d => d.EmergencyContactAddress, (d, v) => d.EmergencyContactAddress = v),
        new Field
        {
            Key = "maritalStatus",
            LabelKey = "MaritalStatus",
            Lookup = AiMasterDataKind.MaritalStatus,
            Get = d => d.MaritalStatusId?.ToString(CultureInfo.InvariantCulture),
            Set = (d, v) => d.MaritalStatusId = string.IsNullOrEmpty(v) ? null : int.Parse(v, CultureInfo.InvariantCulture)
        }
    };

    public static Field Find(string key) => All.FirstOrDefault(f => f.Key == key);

    /// <summary>
    /// Mobile and telephone are stored as numbers, so "+965 5555-1234" is kept as 96555551234: digits only, and a
    /// leading 00 dropped like a +. Returns null when what was given is not a phone number at all.
    /// </summary>
    public static string NormalisePhone(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Any(char.IsLetter))
        {
            return null;
        }

        var digits = new string(text.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("00", StringComparison.Ordinal))
        {
            digits = digits.Substring(2);
        }

        digits = digits.TrimStart('0');
        return digits.Length is >= 5 and <= 15 ? digits : null;
    }

    private static Field Text(string key, string label, Func<CreateOrEditEmployeeDto, string> get, Action<CreateOrEditEmployeeDto, string> set) =>
        new() { Key = key, LabelKey = label, Get = get, Set = set };

    private static Field Phone(string key, string label, Func<CreateOrEditEmployeeDto, long> get, Action<CreateOrEditEmployeeDto, long> set) =>
        new()
        {
            Key = key,
            LabelKey = label,
            IsPhoneNumber = true,
            Get = d => get(d) == 0 ? null : get(d).ToString(CultureInfo.InvariantCulture),
            Set = (d, v) => set(d, string.IsNullOrEmpty(v) ? 0 : long.Parse(v, CultureInfo.InvariantCulture))
        };
}
