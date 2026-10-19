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
/// Disciplinary records. Sub type: the disciplinary type. The date it is about: the disciplinary date.
/// </summary>
public class DisciplinaryNotificationRuleSource : NotificationRuleSourceBase<EmployeeDisciplinary>
{
    public const string Parameter = "Disciplinary";

    private readonly IRepository<DisciplinaryType> _disciplinaryTypeRepository;

    public DisciplinaryNotificationRuleSource(
        IRepository<EmployeeDisciplinary> repository,
        IRepository<NotificationSentLog, long> sentLogRepository,
        IRepository<DisciplinaryType> disciplinaryTypeRepository)
        : base(repository, sentLogRepository)
    {
        _disciplinaryTypeRepository = disciplinaryTypeRepository;
    }

    public override string Code => NotificationRuleSourceCodes.Disciplinary;

    public override string DisplayNameKey => "NotificationRuleSource_Disciplinary";

    public override string SubTypeDisplayNameKey => "DisciplinaryType";

    public override string ParameterName => Parameter;

    protected override IQueryable<EmployeeDisciplinary> Records()
    {
        return Repository.GetAll()
            .Include(d => d.Employee)
            .Include(d => d.DisciplinaryType);
    }

    protected override Expression<Func<EmployeeDisciplinary, bool>> SubTypeFilter(int subTypeId)
    {
        return d => d.DisciplinaryTypeId == subTypeId;
    }

    public override async Task<List<NotificationRuleSubTypeItem>> GetSubTypesAsync()
    {
        return await _disciplinaryTypeRepository.GetAll()
            .OrderBy(t => t.NameEn)
            .Select(t => new NotificationRuleSubTypeItem { Id = t.Id, Name = t.NameEn })
            .ToListAsync();
    }

    protected override DateTime? SubjectDate(EmployeeDisciplinary record) => DateOrNull(record.DisciplinaryDate);

    protected override string Describe(EmployeeDisciplinary record)
    {
        return $"{record.Employee?.Code} - {record.DisciplinaryType?.NameEn} {record.DisciplinaryDate:yyyy-MM-dd}".Trim();
    }

    protected override RuleBaseObjectDescriptor BuildCriterion()
    {
        return RuleBaseObjectBuilder<EmployeeDisciplinary>
            .For(Code, DisplayNameKey)
            .Parameter<EmployeeDisciplinary>(Parameter, "Violation", isWritable: false, p => p
                .Read("Id", "Id", RuleDataType.Integer, d => (long)d.Id)
                .Read("EmployeeId", "Employee", RuleDataType.Integer, d => (long)d.EmployeeId)
                .Read("DisciplinaryTypeId", "DisciplinaryType", RuleDataType.Lookup, d => (long)d.DisciplinaryTypeId, "DisciplinaryType")
                .Read("DisciplinaryDate", "DisciplinaryDate", RuleDataType.Date, d => d.DisciplinaryDate)
                .Read("Severity", "Severity", RuleDataType.Enum, d => (long)d.Severity, nameof(Severity))
                .Read("Comments", "Comments", RuleDataType.String, d => d.Comments)
                .Read("WLSendToEmp", "WlSendToEmp", RuleDataType.Boolean, d => d.WLSendToEmp)
                .Read("WLRecvFromEmp", "WlRecvFromEmp", RuleDataType.Boolean, d => d.WLRecvFromEmp)
                .Navigate<Employee>("Employee", "Employee", d => d.EmployeeId, NotificationRuleFields.Employee)
                .Navigate<DisciplinaryType>("DisciplinaryType", "DisciplinaryType", d => d.DisciplinaryTypeId, t => t
                    .Read("Code", "Code", RuleDataType.String, x => x.Code)
                    .Read("NameEn", "NameEn", RuleDataType.String, x => x.NameEn)))
            .Build();
    }
}
