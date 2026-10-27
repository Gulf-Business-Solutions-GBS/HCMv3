using HCMv3.BusinessRules;
using HCMv3.BusinessRules.Services;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;

namespace HCMv3.HcmNotifications.Rules.Sources;

/// <summary>
/// The employee fields every employee-linked source offers through its <c>Employee</c> reference, declared
/// once so "Leave.Employee.Status" and "Loan.Employee.Status" cannot drift into meaning different things.
/// Mirrors the reference the leave rule base object already exposes.
/// </summary>
internal static class NotificationRuleFields
{
    public static void Employee(RuleParameterBuilder<Employee> e)
    {
        e.Read("Code", "Code", RuleDataType.String, x => x.Code)
            .Read("FullNameEn", "FullName", RuleDataType.String, x => x.FullNameEn)
            .Read("Gender", "Gender", RuleDataType.Enum, x => (long)x.Gender, nameof(Gender))
            .Read("BirthDate", "BirthDate", RuleDataType.Date, x => x.BirthDate)
            .Read("JoiningDate", "JoiningDate", RuleDataType.Date, x => x.JoiningDate)
            .Read("ProbationEndDate", "ProbationEndDate", RuleDataType.Date, x => x.ProbationEndDate)
            .Read("Status", "Status", RuleDataType.Enum, x => (long)x.Status, nameof(EmployeeStatus))
            .Read("EmploymentType", "EmploymentType", RuleDataType.Enum, x => (long)x.EmploymentType, nameof(EmploymentType))
            .Read("Email", "Email", RuleDataType.String, x => x.Email)
            .Navigate<Nationality>("Nationality", "Nationality", x => x.NationalityId, n => n
                .Read("Code", "Code", RuleDataType.String, x => x.Code)
                .Read("NameEn", "NameEn", RuleDataType.String, x => x.NameEn));
    }
}
