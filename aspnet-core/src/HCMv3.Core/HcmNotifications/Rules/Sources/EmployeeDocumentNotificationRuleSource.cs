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
/// Employee documents (passport, civil ID, visa...). Sub type: the document type. The date it is about: the
/// expiry date.
/// </summary>
public class EmployeeDocumentNotificationRuleSource : NotificationRuleSourceBase<EmployeeDocument>
{
    public const string Parameter = "Document";

    private readonly IRepository<DocumentType> _documentTypeRepository;

    public EmployeeDocumentNotificationRuleSource(
        IRepository<EmployeeDocument> repository,
        IRepository<NotificationSentLog, long> sentLogRepository,
        IRepository<DocumentType> documentTypeRepository)
        : base(repository, sentLogRepository)
    {
        _documentTypeRepository = documentTypeRepository;
    }

    public override string Code => NotificationRuleSourceCodes.EmployeeDocument;

    public override string DisplayNameKey => "NotificationRuleSource_EmployeeDocument";

    public override string SubTypeDisplayNameKey => "DocumentType";

    public override string ParameterName => Parameter;

    protected override IQueryable<EmployeeDocument> Records()
    {
        return Repository.GetAll()
            .Include(d => d.Employee)
            .Include(d => d.DocumentType);
    }

    protected override Expression<Func<EmployeeDocument, bool>> SubTypeFilter(int subTypeId)
    {
        return d => d.DocumentTypeId == subTypeId;
    }

    public override async Task<List<NotificationRuleSubTypeItem>> GetSubTypesAsync()
    {
        return await _documentTypeRepository.GetAll()
            .Where(t => t.DocumentBelongsTo != DocumentBelongsTo.Organization)
            .OrderBy(t => t.NameEn)
            .Select(t => new NotificationRuleSubTypeItem { Id = t.Id, Name = t.NameEn })
            .ToListAsync();
    }

    protected override DateTime? SubjectDate(EmployeeDocument record)
    {
        return record.ExpiryDate == EmployeeDocument.NoExpiryDate ? null : DateOrNull(record.ExpiryDate);
    }

    protected override string Describe(EmployeeDocument record)
    {
        return $"{record.Employee?.Code} - {record.DocumentType?.NameEn} {record.DocumentNum}".Trim();
    }

    protected override RuleBaseObjectDescriptor BuildCriterion()
    {
        return RuleBaseObjectBuilder<EmployeeDocument>
            .For(Code, DisplayNameKey)
            .Parameter<EmployeeDocument>(Parameter, "Document", isWritable: false, p => p
                .Read("Id", "Id", RuleDataType.Integer, d => (long)d.Id)
                .Read("EmployeeId", "Employee", RuleDataType.Integer, d => (long)d.EmployeeId)
                .Read("DocumentTypeId", "DocumentType", RuleDataType.Lookup, d => (long)d.DocumentTypeId, "DocumentType")
                .Read("DocumentNum", "DocumentNum", RuleDataType.String, d => d.DocumentNum)
                .Read("IssueDate", "IssueDate", RuleDataType.Date, d => d.IssueDate)
                .Read("ExpiryDate", "ExpiryDate", RuleDataType.Date, d => d.ExpiryDate)
                .Navigate<Employee>("Employee", "Employee", d => d.EmployeeId, NotificationRuleFields.Employee)
                .Navigate<DocumentType>("DocumentType", "DocumentType", d => d.DocumentTypeId, t => t
                    .Read("Code", "Code", RuleDataType.String, x => x.Code)
                    .Read("NameEn", "NameEn", RuleDataType.String, x => x.NameEn)
                    .Read("NotificationPeriod", "NotificationPeriod", RuleDataType.Integer, x => (long)x.NotificationPeriod)))
            .Build();
    }
}
