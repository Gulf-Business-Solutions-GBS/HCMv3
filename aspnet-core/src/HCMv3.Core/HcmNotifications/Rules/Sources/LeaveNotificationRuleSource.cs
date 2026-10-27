using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using HCMv3.BusinessRules;
using HCMv3.BusinessRules.Services;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.HcmNotifications.Rules.Sources;

/// <summary>
/// Leave (<see cref="EmployeeAbsence"/>). Sub type: the absence type. The date it is about: the start date.
/// Pending requests are not seen -- the ApprovedOnly data filter hides them, as it does from payroll.
/// </summary>
public class LeaveNotificationRuleSource : NotificationRuleSourceBase<EmployeeAbsence>
{
    public const string Parameter = "Leave";

    private readonly IRepository<AbsenceType> _absenceTypeRepository;

    public LeaveNotificationRuleSource(
        IRepository<EmployeeAbsence> repository,
        IRepository<NotificationSentLog, long> sentLogRepository,
        IRepository<AbsenceType> absenceTypeRepository)
        : base(repository, sentLogRepository)
    {
        _absenceTypeRepository = absenceTypeRepository;
    }

    public override string Code => NotificationRuleSourceCodes.Leave;

    public override string DisplayNameKey => "NotificationRuleSource_Leave";

    public override string SubTypeDisplayNameKey => "AbsenceType";

    public override string ParameterName => Parameter;

    protected override IQueryable<EmployeeAbsence> Records()
    {
        return Repository.GetAll()
            .Include(a => a.Employee)
            .Include(a => a.AbsenceType);
    }

    protected override Expression<Func<EmployeeAbsence, bool>> SubTypeFilter(int subTypeId)
    {
        return a => a.AbsenceTypeId == subTypeId;
    }

    public override async Task<List<NotificationRuleSubTypeItem>> GetSubTypesAsync()
    {
        return await _absenceTypeRepository.GetAll()
            .OrderBy(t => t.NameEn)
            .Select(t => new NotificationRuleSubTypeItem { Id = t.Id, Name = t.NameEn })
            .ToListAsync();
    }

    protected override DateTime? SubjectDate(EmployeeAbsence record) => DateOrNull(record.StartDate);

    protected override string Describe(EmployeeAbsence record)
    {
        return $"{record.Employee?.Code} - {record.AbsenceType?.NameEn} {record.StartDate:yyyy-MM-dd}".Trim();
    }

    protected override RuleBaseObjectDescriptor BuildCriterion()
    {
        return RuleBaseObjectBuilder<EmployeeAbsence>
            .For(Code, DisplayNameKey)
            .Parameter<EmployeeAbsence>(Parameter, "Leave", isWritable: false, p => p
                .Read("Id", "Id", RuleDataType.Integer, a => (long)a.Id)
                .Read("EmployeeId", "Employee", RuleDataType.Integer, a => (long)a.EmployeeId)
                .Read("AbsenceTypeId", "AbsenceType", RuleDataType.Lookup, a => (long)a.AbsenceTypeId, "AbsenceType")
                .Read("StartDate", "StartDate", RuleDataType.Date, a => a.StartDate)
                .Read("EndDate", "EndDate", RuleDataType.Date, a => a.EndDate)
                .Read("ReturnDate", "ReturnDate", RuleDataType.Date, a => a.ReturnDate)
                .Read("IsReturned", "IsReturned", RuleDataType.Boolean, a => a.IsReturned)
                .Read("TotalLeaveDays", "TotalLeaveDays", RuleDataType.Decimal, a => (decimal)a.TotalLeaveDays)
                .Read("CalendarDays", "CalendarDays", RuleDataType.Decimal, a => (decimal)a.CalendarDays)
                .Read("IsTravelling", "IsTravelling", RuleDataType.Boolean, a => a.IsTravelling)
                .Read("AdvanceLeavePaid", "AdvanceLeavePaid", RuleDataType.Boolean, a => a.AdvanceLeavePaid)
                .Navigate<Employee>("Employee", "Employee", a => a.EmployeeId, NotificationRuleFields.Employee)
                .Navigate<AbsenceType>("AbsenceType", "AbsenceType", a => a.AbsenceTypeId, t => t
                    .Read("Code", "Code", RuleDataType.String, x => x.Code)
                    .Read("NameEn", "NameEn", RuleDataType.String, x => x.NameEn)
                    .Read("IsPaid", "IsPaid", RuleDataType.Boolean, x => x.IsPaid)
                    .Read("IsReturnDateRequired", "IsReturnDateRequired", RuleDataType.Boolean, x => x.IsReturnDateRequired)))
            .Build();
    }
}
