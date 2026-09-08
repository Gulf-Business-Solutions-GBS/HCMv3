using System.Collections.Generic;
using Abp.Dependency;
using HCMv3.BusinessRules;
using HCMv3.BusinessRules.Services;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PersonnelAdministration.BusinessRules;

/// <summary>
/// Exposes <see cref="Employee"/> to the rule engine.
///
/// <para>
/// This class IS the security boundary for employee rules. A field absent from here cannot be read or
/// written by any rule, whatever JSON a client posts, because the evaluator has no other way to reach an
/// object's members. So the list below is a deliberate, reviewable decision about what configuration is
/// allowed to touch -- not a convenience export of the entity.
/// </para>
///
/// <para><b>What is writable and why.</b> Only the derived fields: probation end date and the two full-name
/// concatenations. These are exactly the values <c>EmployeesAppService.ApplyComputedFields</c> computes
/// today in C#, which is the point of the exercise -- a tenant whose probation policy is "joining date plus
/// 100 calendar days" rather than "plus ProbationDays" can now express that without a release. Identity,
/// status, salary-adjacent and audit fields stay read-only: a rule that could rewrite a civil ID or flip an
/// employee to Active would be a privilege escalation dressed up as configuration.</para>
///
/// <para><b>What is deliberately absent.</b> The photo (a blob, useless in a condition), the address
/// fields (no rule has ever needed them and each one is another string to keep localized), and every
/// navigation property. Adding a field later is a one-line change here; removing one after tenants have
/// written rules against it is not, so the bias is towards adding on demand.</para>
/// </summary>
public class EmployeeRuleBaseObjectProvider : IRuleBaseObjectProvider, ISingletonDependency
{
    /// <summary>The base object code stored in <c>BusinessRule.BaseObject</c>. Never rename.</summary>
    public const string BaseObjectCode = "Employee";

    /// <summary>The parameter name rules use, e.g. <c>Employee.JoiningDate</c>.</summary>
    public const string EmployeeParameter = "Employee";

    public IEnumerable<RuleBaseObjectDescriptor> GetBaseObjects()
    {
        yield return RuleBaseObjectBuilder<Employee>
            .For(BaseObjectCode, "Employee")
            .SupportsEvents(
                RuleEventType.OnInit,
                RuleEventType.BeforeSave,
                RuleEventType.AfterSave,
                RuleEventType.OnValidate)
            .Parameter<Employee>(EmployeeParameter, "Employee", isWritable: true, p => p
                // --- Identity and status: readable so rules can branch, never writable. ---
                .Read("Id", "Id", RuleDataType.Integer, e => (long)e.Id)
                .Read("Code", "Code", RuleDataType.String, e => e.Code)
                .Read("Status", "Status", RuleDataType.Enum, e => (long)e.Status, nameof(EmployeeStatus))
                .Read("EmploymentType", "EmploymentType", RuleDataType.Enum, e => (long)e.EmploymentType, nameof(EmploymentType))
                .Read("Gender", "Gender", RuleDataType.Enum, e => (long)e.Gender, nameof(Gender))
                .Read("NationalityId", "Nationality", RuleDataType.Lookup, e => (long)e.NationalityId, "Nationality")
                .Read("TitleId", "Title", RuleDataType.Lookup, e => e.TitleId, "Title")
                .Read("MaritalStatusId", "MaritalStatus", RuleDataType.Lookup, e => e.MaritalStatusId, "MaritalStatus")
                .Read("ReligionId", "Religion", RuleDataType.Lookup, e => e.ReligionId, "Religion")
                .Read("QualificationId", "Qualification", RuleDataType.Lookup, e => e.QualificationId, "Qualification")
                .Read("RecruitmentTypeId", "RecruitmentType", RuleDataType.Lookup, e => e.RecruitmentTypeId, "RecruitmentType")
                .Read("BirthDate", "BirthDate", RuleDataType.Date, e => e.BirthDate)
                .Read("Email", "Email", RuleDataType.String, e => e.Email)

                // --- Name parts: read the sources, write the concatenations. ---
                .Read("FirstNameEn", "FirstName", RuleDataType.String, e => e.FirstNameEn)
                .Read("SecondNameEn", "SecondName", RuleDataType.String, e => e.SecondNameEn)
                .Read("ThirdNameEn", "ThirdName", RuleDataType.String, e => e.ThirdNameEn)
                .Read("LastNameEn", "LastName", RuleDataType.String, e => e.LastNameEn)
                .ReadWrite("FullNameEn", "FullName", RuleDataType.String, e => e.FullNameEn, (e, v) => e.FullNameEn = v)
                .ReadWrite("FullNameAr", "FullNameAr", RuleDataType.String, e => e.FullNameAr, (e, v) => e.FullNameAr = v)

                // --- Employment dates: the probation rule's subject. ---
                .Read("JoiningDate", "JoiningDate", RuleDataType.Date, e => e.JoiningDate)
                .ReadWrite("ProbationDays", "ProbationDays", RuleDataType.Integer, e => (long)e.ProbationDays, (e, v) => e.ProbationDays = (int)v)
                .ReadWrite("ProbationEndDate", "ProbationEndDate", RuleDataType.Date, e => e.ProbationEndDate, (e, v) => e.ProbationEndDate = v)

                // References the engine follows by key, so a rule can compare against a nationality NAME
                // rather than an id that differs per environment. Read-only in both directions: a rule may
                // read the related row, never write it.
                .Navigate<Nationality>("Nationality", "Nationality", e => e.NationalityId, n => n
                    .Read("Code", "Code", RuleDataType.String, x => x.Code)
                    .Read("NameEn", "NameEn", RuleDataType.String, x => x.NameEn)
                    .Read("NameAr", "NameAr", RuleDataType.String, x => x.NameAr))
                .Navigate<Religion>("Religion", "Religion", e => e.ReligionId, r => r
                    .Read("Code", "Code", RuleDataType.String, x => x.Code)
                    .Read("NameEn", "NameEn", RuleDataType.String, x => x.NameEn))
                .Navigate<Qualification>("Qualification", "Qualification", e => e.QualificationId, q => q
                    .Read("Code", "Code", RuleDataType.String, x => x.Code)
                    .Read("NameEn", "NameEn", RuleDataType.String, x => x.NameEn))
                .Navigate<Title>("Title", "Title", e => e.TitleId, t => t
                    .Read("Code", "Code", RuleDataType.String, x => x.Code)
                    .Read("NameEn", "NameEn", RuleDataType.String, x => x.NameEn)))
            .Build();
    }
}
