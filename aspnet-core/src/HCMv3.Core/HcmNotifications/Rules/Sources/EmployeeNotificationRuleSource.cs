using System;
using System.Linq;
using Abp.Domain.Repositories;
using HCMv3.BusinessRules;
using HCMv3.BusinessRules.Services;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;

namespace HCMv3.HcmNotifications.Rules.Sources;

/// <summary>
/// The employee record itself, for date-driven notices such as "probation ends in 14 days". No sub type, and
/// no single date it is about -- the criterion names the date it cares about.
///
/// <para>
/// The record is its own subject: the recipient resolver treats an <see cref="Employee"/> as the person the
/// notice is about, so "to employee" and "to manager" work exactly as on the other sources.
/// </para>
/// </summary>
public class EmployeeNotificationRuleSource : NotificationRuleSourceBase<Employee>
{
    public const string Parameter = "Employee";

    public EmployeeNotificationRuleSource(
        IRepository<Employee> repository,
        IRepository<NotificationSentLog, long> sentLogRepository)
        : base(repository, sentLogRepository)
    {
    }

    public override string Code => NotificationRuleSourceCodes.Employee;

    public override string DisplayNameKey => "NotificationRuleSource_Employee";

    public override string SubTypeDisplayNameKey => null;

    public override string ParameterName => Parameter;

    protected override IQueryable<Employee> Records() => Repository.GetAll();

    protected override DateTime? SubjectDate(Employee record) => null;

    protected override string Describe(Employee record) => $"{record.Code} - {record.FullNameEn}".Trim();

    protected override RuleBaseObjectDescriptor BuildCriterion()
    {
        return RuleBaseObjectBuilder<Employee>
            .For(Code, DisplayNameKey)
            .Parameter<Employee>(Parameter, "Employee", isWritable: false, p =>
            {
                p.Read("Id", "Id", RuleDataType.Integer, e => (long)e.Id)
                    .Read("ProbationDays", "ProbationDays", RuleDataType.Integer, e => (long)e.ProbationDays);

                NotificationRuleFields.Employee(p);
            })
            .Build();
    }
}
