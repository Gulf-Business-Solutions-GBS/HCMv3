using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace HCMv3.PersonnelAdministration.Workflow;

/// <summary>How a personal data field's stored value is turned into words for a reader.</summary>
public enum PersonalDataFieldType
{
    Text,
    Date,
    Number,
    Gender,
    Title,
    Nationality,
    Religion,
    MaritalStatus,
    City,
    Governorate,
    Country
}

/// <summary>One of the forty fields: where it is read from on each side, and what it is called.</summary>
public sealed class PersonalDataField
{
    public PersonalDataField(
        string name,
        PersonalDataFieldType type,
        string labelKey,
        string labelEn,
        string labelAr,
        Func<EmployeePersonalDataChange, string> proposed,
        Func<Employee, string> current)
    {
        Name = name;
        Type = type;
        LabelKey = labelKey;
        LabelEn = labelEn;
        LabelAr = labelAr;
        Proposed = proposed;
        Current = current;
    }

    /// <summary>The property name on <see cref="EmployeePersonalDataChange"/>, e.g. <c>NationalityId</c>.</summary>
    public string Name { get; }

    public PersonalDataFieldType Type { get; }

    /// <summary>Localization key for the label a reader is shown.</summary>
    public string LabelKey { get; }

    /// <summary>The label frozen into the request's one-line summary, which is stored in both languages.</summary>
    public string LabelEn { get; }

    public string LabelAr { get; }

    /// <summary>The proposed value in its stored form, or null when the employee did not propose one.</summary>
    public Func<EmployeePersonalDataChange, string> Proposed { get; }

    /// <summary>What the personnel file holds, in the same stored form.</summary>
    public Func<Employee, string> Current { get; }
}

/// <summary>A field the request changes: what it held before and what is proposed, both in stored form.</summary>
public sealed class PersonalDataFieldChange
{
    public PersonalDataFieldChange(PersonalDataField field, string previous, string proposed)
    {
        Field = field;
        Previous = previous;
        Proposed = proposed;
    }

    public PersonalDataField Field { get; }

    public string Previous { get; }

    public string Proposed { get; }
}

/// <summary>
/// The forty fields of a personal data update, read the same way from the proposal and from the personnel
/// file so the two can be compared.
///
/// <para>
/// Every value is reduced to one stored form -- text as it is, a date as <c>yyyy-MM-dd</c>, a number in
/// invariant digits, a lookup as its id, the gender as its enum name -- so that "did this change" is one
/// comparison for all forty rather than a rule per type, and so the personnel file's values can be kept as
/// JSON beside the request (<see cref="EmployeePersonalDataChange.PreviousValuesJson"/>).
/// </para>
///
/// <para>
/// This list decides what a reader is TOLD changed. What is actually written to the employee is
/// <see cref="PersonalDataChangeWorkflowTransactionHandler.Apply"/>, which stays forty explicit lines.
/// </para>
/// </summary>
public static class PersonalDataChangeFields
{
    public static readonly IReadOnlyList<PersonalDataField> All = new List<PersonalDataField>
    {
        Text(nameof(EmployeePersonalDataChange.FirstNameEn), "FirstNameEn", "First Name (En)", "الاسم الأول (إنجليزي)", c => c.FirstNameEn, e => e.FirstNameEn),
        Text(nameof(EmployeePersonalDataChange.SecondNameEn), "SecondNameEn", "Second Name (En)", "الاسم الثاني (إنجليزي)", c => c.SecondNameEn, e => e.SecondNameEn),
        Text(nameof(EmployeePersonalDataChange.ThirdNameEn), "ThirdNameEn", "Third Name (En)", "الاسم الثالث (إنجليزي)", c => c.ThirdNameEn, e => e.ThirdNameEn),
        Text(nameof(EmployeePersonalDataChange.LastNameEn), "LastNameEn", "Last Name (En)", "اسم العائلة (إنجليزي)", c => c.LastNameEn, e => e.LastNameEn),
        Text(nameof(EmployeePersonalDataChange.FirstNameAr), "FirstNameAr", "First Name (Ar)", "الاسم الأول (عربي)", c => c.FirstNameAr, e => e.FirstNameAr),
        Text(nameof(EmployeePersonalDataChange.SecondNameAr), "SecondNameAr", "Second Name (Ar)", "الاسم الثاني (عربي)", c => c.SecondNameAr, e => e.SecondNameAr),
        Text(nameof(EmployeePersonalDataChange.ThirdNameAr), "ThirdNameAr", "Third Name (Ar)", "الاسم الثالث (عربي)", c => c.ThirdNameAr, e => e.ThirdNameAr),
        Text(nameof(EmployeePersonalDataChange.LastNameAr), "LastNameAr", "Last Name (Ar)", "اسم العائلة (عربي)", c => c.LastNameAr, e => e.LastNameAr),

        Lookup(PersonalDataFieldType.Title, nameof(EmployeePersonalDataChange.TitleId), "Title", "Title", "اللقب", c => c.TitleId, e => e.TitleId),
        new PersonalDataField(nameof(EmployeePersonalDataChange.BirthDate), PersonalDataFieldType.Date, "BirthDate", "Birth Date", "تاريخ الميلاد", c => Date(c.BirthDate), e => Date(e.BirthDate)),
        Lookup(PersonalDataFieldType.Nationality, nameof(EmployeePersonalDataChange.NationalityId), "Nationality", "Nationality", "الجنسية", c => c.NationalityId, e => e.NationalityId),
        Lookup(PersonalDataFieldType.Religion, nameof(EmployeePersonalDataChange.ReligionId), "Religion", "Religion", "الديانة", c => c.ReligionId, e => e.ReligionId),
        new PersonalDataField(nameof(EmployeePersonalDataChange.Gender), PersonalDataFieldType.Gender, "Gender", "Gender", "الجنس", c => c.Gender?.ToString(), e => e.Gender.ToString()),
        Lookup(PersonalDataFieldType.MaritalStatus, nameof(EmployeePersonalDataChange.MaritalStatusId), "MaritalStatus", "Marital Status", "الحالة الاجتماعية", c => c.MaritalStatusId, e => e.MaritalStatusId),

        Text(nameof(EmployeePersonalDataChange.FlatNumn), "FlatNumber", "Flat No.", "رقم الشقة", c => c.FlatNumn, e => e.FlatNumn),
        Text(nameof(EmployeePersonalDataChange.FloorNum), "FloorNumber", "Floor No.", "رقم الطابق", c => c.FloorNum, e => e.FloorNum),
        Text(nameof(EmployeePersonalDataChange.BuildingNum), "BuildingNumber", "Building No.", "رقم المبنى", c => c.BuildingNum, e => e.BuildingNum),
        Text(nameof(EmployeePersonalDataChange.StreetNum), "StreetNumber", "Street No.", "رقم الشارع", c => c.StreetNum, e => e.StreetNum),
        Text(nameof(EmployeePersonalDataChange.BlockNum), "BlockNumber", "Block No.", "رقم القطعة", c => c.BlockNum, e => e.BlockNum),
        Text(nameof(EmployeePersonalDataChange.ZipCode), "ZipCode", "Zip Code", "الرمز البريدي", c => c.ZipCode, e => e.ZipCode),
        Text(nameof(EmployeePersonalDataChange.City), "City", "City", "المدينة", c => c.City, e => e.City),
        Lookup(PersonalDataFieldType.Governorate, nameof(EmployeePersonalDataChange.GovernorateId), "Governorate", "Governorate", "المحافظة", c => c.GovernorateId, e => e.GovernorateId),
        Lookup(PersonalDataFieldType.Country, nameof(EmployeePersonalDataChange.CountryId), "Country", "Country", "الدولة", c => c.CountryId, e => e.CountryId),
        Number(nameof(EmployeePersonalDataChange.Telephone), "Telephone", "Telephone", "الهاتف", c => c.Telephone, e => e.Telephone),
        Number(nameof(EmployeePersonalDataChange.Mobile), "Mobile", "Mobile", "الهاتف النقال", c => c.Mobile, e => e.Mobile),
        Text(nameof(EmployeePersonalDataChange.CurrentAddress), "CurrentAddress", "Current Address", "العنوان الحالي", c => c.CurrentAddress, e => e.CurrentAddress),
        Text(nameof(EmployeePersonalDataChange.PermanentAddress), "PermanentAddress", "Permanent Address", "العنوان الدائم", c => c.PermanentAddress, e => e.PermanentAddress),

        Text(nameof(EmployeePersonalDataChange.FatherNameEn), "FatherNameEn", "Father's Name (En)", "اسم الأب (إنجليزي)", c => c.FatherNameEn, e => e.FatherNameEn),
        Text(nameof(EmployeePersonalDataChange.FatherNameAr), "FatherNameAr", "Father's Name (Ar)", "اسم الأب (عربي)", c => c.FatherNameAr, e => e.FatherNameAr),
        Text(nameof(EmployeePersonalDataChange.MotherNameEn), "MotherNameEn", "Mother's Name (En)", "اسم الأم (إنجليزي)", c => c.MotherNameEn, e => e.MotherNameEn),
        Text(nameof(EmployeePersonalDataChange.MotherNameAr), "MotherNameAr", "Mother's Name (Ar)", "اسم الأم (عربي)", c => c.MotherNameAr, e => e.MotherNameAr),

        Text(nameof(EmployeePersonalDataChange.BloodGroup), "BloodGroup", "Blood Group", "فصيلة الدم", c => c.BloodGroup, e => e.BloodGroup),
        Text(nameof(EmployeePersonalDataChange.Email), "Email", "Email", "البريد الإلكتروني", c => c.Email, e => e.Email),
        Text(nameof(EmployeePersonalDataChange.PACINumber), "PaciNumber", "PACI Number", "الرقم الآلي", c => c.PACINumber, e => e.PACINumber),
        Text(nameof(EmployeePersonalDataChange.EmergencyContact), "EmergencyContact", "Emergency Contact", "جهة الاتصال للطوارئ", c => c.EmergencyContact, e => e.EmergencyContact),
        Text(nameof(EmployeePersonalDataChange.CriticalIllness), "CriticalIllness", "Critical Illness", "الأمراض المزمنة", c => c.CriticalIllness, e => e.CriticalIllness),
        Text(nameof(EmployeePersonalDataChange.AlternateEmail), "AlternateEmail", "Alternate Email", "بريد إلكتروني بديل", c => c.AlternateEmail, e => e.AlternateEmail),
        Text(nameof(EmployeePersonalDataChange.EmergencyNumber), "EmergencyNumber", "Emergency Number", "رقم الطوارئ", c => c.EmergencyNumber, e => e.EmergencyNumber),
        Text(nameof(EmployeePersonalDataChange.EmergencyRelation), "EmergencyRelation", "Emergency Relation", "صلة القرابة", c => c.EmergencyRelation, e => e.EmergencyRelation),
        Lookup(PersonalDataFieldType.City, nameof(EmployeePersonalDataChange.BirthPlaceId), "BirthPlace", "Birth Place", "مكان الميلاد", c => c.BirthPlaceId, e => e.BirthPlaceId)
    };

    /// <summary>The personnel file's forty values in stored form, keyed by field name.</summary>
    public static Dictionary<string, string> Read(Employee employee)
    {
        return All.ToDictionary(f => f.Name, f => employee == null ? null : f.Current(employee));
    }

    /// <summary>The same values as JSON, for <see cref="EmployeePersonalDataChange.PreviousValuesJson"/>.</summary>
    public static string Serialize(Employee employee)
    {
        return JsonSerializer.Serialize(Read(employee));
    }

    /// <summary>Reads values kept by <see cref="Serialize"/>, or null when there are none to read.</summary>
    public static Dictionary<string, string> Deserialize(string json)
    {
        return string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<Dictionary<string, string>>(json);
    }

    /// <summary>
    /// The fields the proposal genuinely changes, against the values given.
    ///
    /// <para>
    /// A field the employee did not propose (null) is not a change. Neither is one that came back as it
    /// went out, and empty compares equal to missing: a box that was empty and stayed empty has not
    /// changed, whichever of the two the client sent.
    /// </para>
    /// </summary>
    public static List<PersonalDataFieldChange> Changes(
        EmployeePersonalDataChange change,
        IReadOnlyDictionary<string, string> previousValues)
    {
        var changes = new List<PersonalDataFieldChange>();

        foreach (var field in All)
        {
            var proposed = field.Proposed(change);
            if (proposed == null)
            {
                continue;
            }

            string previous = null;
            previousValues?.TryGetValue(field.Name, out previous);

            var same = string.IsNullOrEmpty(proposed) && string.IsNullOrEmpty(previous)
                       || string.Equals(proposed, previous, StringComparison.Ordinal);

            if (!same)
            {
                changes.Add(new PersonalDataFieldChange(field, previous, proposed));
            }
        }

        return changes;
    }

    private static PersonalDataField Text(
        string name,
        string labelKey,
        string labelEn,
        string labelAr,
        Func<EmployeePersonalDataChange, string> proposed,
        Func<Employee, string> current)
    {
        return new PersonalDataField(name, PersonalDataFieldType.Text, labelKey, labelEn, labelAr, proposed, current);
    }

    private static PersonalDataField Number(
        string name,
        string labelKey,
        string labelEn,
        string labelAr,
        Func<EmployeePersonalDataChange, long?> proposed,
        Func<Employee, long> current)
    {
        return new PersonalDataField(
            name,
            PersonalDataFieldType.Number,
            labelKey,
            labelEn,
            labelAr,
            c => proposed(c)?.ToString(CultureInfo.InvariantCulture),
            e => current(e).ToString(CultureInfo.InvariantCulture));
    }

    private static PersonalDataField Lookup(
        PersonalDataFieldType type,
        string name,
        string labelKey,
        string labelEn,
        string labelAr,
        Func<EmployeePersonalDataChange, int?> proposed,
        Func<Employee, int?> current)
    {
        return new PersonalDataField(
            name,
            type,
            labelKey,
            labelEn,
            labelAr,
            c => proposed(c)?.ToString(CultureInfo.InvariantCulture),
            e => current(e)?.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>An unset date is no date, not the first of January in the year 1.</summary>
    private static string Date(DateTime? value)
    {
        return value.HasValue && value.Value != default
            ? value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : null;
    }
}
