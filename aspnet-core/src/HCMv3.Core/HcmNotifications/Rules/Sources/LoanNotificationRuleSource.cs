using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using HCMv3.BusinessRules;
using HCMv3.BusinessRules.Services;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.HcmNotifications.Rules.Sources;

/// <summary>
/// Employee loans. Sub type: the loan type. The date it is about: the loan end date.
/// </summary>
public class LoanNotificationRuleSource : NotificationRuleSourceBase<EmployeeLoan>
{
    public const string Parameter = "Loan";

    private readonly IRepository<LoanType> _loanTypeRepository;

    public LoanNotificationRuleSource(
        IRepository<EmployeeLoan> repository,
        IRepository<NotificationSentLog, long> sentLogRepository,
        IRepository<LoanType> loanTypeRepository)
        : base(repository, sentLogRepository)
    {
        _loanTypeRepository = loanTypeRepository;
    }

    public override string Code => NotificationRuleSourceCodes.Loan;

    public override string DisplayNameKey => "NotificationRuleSource_Loan";

    public override string SubTypeDisplayNameKey => "LoanType";

    public override string ParameterName => Parameter;

    protected override IQueryable<EmployeeLoan> Records()
    {
        return Repository.GetAll()
            .Include(l => l.Employee)
            .Include(l => l.LoanType);
    }

    protected override Expression<Func<EmployeeLoan, bool>> SubTypeFilter(int subTypeId)
    {
        return l => l.LoanTypeId == subTypeId;
    }

    public override async Task<List<NotificationRuleSubTypeItem>> GetSubTypesAsync()
    {
        return await _loanTypeRepository.GetAll()
            .OrderBy(t => t.NameEn)
            .Select(t => new NotificationRuleSubTypeItem { Id = t.Id, Name = t.NameEn })
            .ToListAsync();
    }

    protected override DateTime? SubjectDate(EmployeeLoan record) => DateOrNull(record.EndDate);

    protected override string Describe(EmployeeLoan record)
    {
        return $"{record.Employee?.Code} - {record.LoanType?.NameEn} {record.StartDate:yyyy-MM-dd}".Trim();
    }

    protected override RuleBaseObjectDescriptor BuildCriterion()
    {
        return RuleBaseObjectBuilder<EmployeeLoan>
            .For(Code, DisplayNameKey)
            .Parameter<EmployeeLoan>(Parameter, "Loan", isWritable: false, p => p
                .Read("Id", "Id", RuleDataType.Integer, l => (long)l.Id)
                .Read("EmployeeId", "Employee", RuleDataType.Integer, l => (long)l.EmployeeId)
                .Read("LoanTypeId", "LoanType", RuleDataType.Lookup, l => (long)l.LoanTypeId, "LoanType")
                .Read("StartDate", "StartDate", RuleDataType.Date, l => l.StartDate)
                .Read("RecoveryStartDate", "RecoveryStartDate", RuleDataType.Date, l => l.RecoveryStartDate)
                .Read("EndDate", "EndDate", RuleDataType.Date, l => l.EndDate)
                .Read("ActualAmount", "ActualAmount", RuleDataType.Decimal, l => (decimal)l.ActualAmount)
                .Read("DueAmount", "DueAmount", RuleDataType.Decimal, l => (decimal)l.DueAmount)
                .Read("BalanceDueAmount", "BalanceDueAmount", RuleDataType.Decimal, l => (decimal)l.BalanceDueAmount)
                .Read("FixedAmount", "FixedAmount", RuleDataType.Decimal, l => (decimal)l.FixedAmount)
                .Navigate<Employee>("Employee", "Employee", l => l.EmployeeId, NotificationRuleFields.Employee)
                .Navigate<LoanType>("LoanType", "LoanType", l => l.LoanTypeId, t => t
                    .Read("Code", "Code", RuleDataType.String, x => x.Code)
                    .Read("NameEn", "NameEn", RuleDataType.String, x => x.NameEn)))
            .Build();
    }
}
