using System;
using System.Collections.Generic;

namespace HCMv3.ApiDictionary;

/// <summary>
/// Which fields hold sensitive personal data, and of what kind -- the dictionary's answer to SAP's
/// <c>sap:sensitive-personal-data</c> column.
///
/// <para>
/// <b>One list, in one place.</b> Tagging each DTO property would scatter the decision across hundreds of files
/// where nobody could review it; here it can be read in a minute and changed in one commit. The categories follow
/// the special categories in data-protection law (health, religion) plus the identifiers and contact details an
/// integration most often leaks.
/// </para>
///
/// <para>
/// <b>Exact names, not substrings.</b> Matching on "Mobile" would flag a payroll MobileAllowance as a phone
/// number. A name is flagged only when it is listed, or when it carries one of the few suffixes that cannot mean
/// anything else. Ordinary names -- first name, last name -- are deliberately not flagged: they are personal data,
/// but flagging them would flag nearly every field and make the column useless.
/// </para>
/// </summary>
public static class ApiDictionarySensitiveFields
{
    public const string Identity = "Identity";
    public const string Contact = "Contact";
    public const string Address = "Address";
    public const string Family = "Family";
    public const string Health = "Health";
    public const string Religion = "Religion";
    public const string Photo = "Photo";
    public const string Financial = "Financial";

    private static readonly Dictionary<string, string> ByName = new(StringComparer.OrdinalIgnoreCase)
    {
        // Identity documents and birth details.
        ["CivilID"] = Identity,
        ["CivilId"] = Identity,
        ["PACINumber"] = Identity,
        ["PassportNo"] = Identity,
        ["PassportNumber"] = Identity,
        ["BirthDate"] = Identity,
        ["DateOfBirth"] = Identity,
        ["BirthPlaceId"] = Identity,

        // Personal contact details. Business landline and extension are the company's, not the person's.
        ["Telephone"] = Contact,
        ["Mobile"] = Contact,
        ["PermanentTelephone"] = Contact,
        ["Email"] = Contact,
        ["AlternateEmail"] = Contact,
        ["EmergencyNumber"] = Contact,

        // Where the person lives.
        ["FlatNumn"] = Address,
        ["FloorNum"] = Address,
        ["BuildingNum"] = Address,
        ["StreetNum"] = Address,
        ["BlockNum"] = Address,
        ["LaneNumber"] = Address,
        ["ZipCode"] = Address,
        ["City"] = Address,
        ["GovernorateId"] = Address,

        // Family and relationships.
        ["FatherNameEn"] = Family,
        ["FatherNameAr"] = Family,
        ["MotherNameEn"] = Family,
        ["MotherNameAr"] = Family,
        ["MaritalStatusId"] = Family,
        ["EmergencyContact"] = Family,
        ["EmergencyRelation"] = Family,

        // Special categories.
        ["BloodGroup"] = Health,
        ["CriticalIllness"] = Health,
        ["ReligionId"] = Religion,
        ["Photo"] = Photo,

        // Pay and bank details, for the record types that carry them.
        ["Iban"] = Financial,
        ["IBAN"] = Financial,
        ["AccountNumber"] = Financial,
        ["BankAccountNumber"] = Financial,
        ["BasicSalary"] = Financial,
        ["Salary"] = Financial
    };

    /// <summary>The category <paramref name="fieldName"/> falls in, or null when it holds no sensitive data.</summary>
    public static string Categorize(string fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            return null;
        }

        if (ByName.TryGetValue(fieldName, out var category))
        {
            return category;
        }

        // A suffix that can only mean a home address, whatever it is prefixed with.
        return fieldName.EndsWith("Address", StringComparison.OrdinalIgnoreCase) ? Address : null;
    }
}
