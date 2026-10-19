using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using HCMv3.BusinessRules;
using HCMv3.BusinessRules.Services;
using HCMv3.GeneralMasterInfo;
using HCMv3.GovtRelationsManagement;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.HcmNotifications.Rules.Sources;

/// <summary>
/// Company (organization unit) documents: trade licences, signatory authorisations and the like. Sub type:
/// the document type. The date it is about: the expiry date.
///
/// <para>
/// There is no employee on these records, so a Notification Config's "to employee" and "to manager" find
/// nobody; its role groups and "send to all" still apply. Legacy behaved the same way -- its task event's
/// employee was read off a property this record does not have.
/// </para>
/// </summary>
public class CompanyDocumentNotificationRuleSource : NotificationRuleSourceBase<OrganizationDocument>
{
    public const string Parameter = "CompanyDocument";

    private readonly IRepository<DocumentType> _documentTypeRepository;

    public CompanyDocumentNotificationRuleSource(
        IRepository<OrganizationDocument> repository,
        IRepository<NotificationSentLog, long> sentLogRepository,
        IRepository<DocumentType> documentTypeRepository)
        : base(repository, sentLogRepository)
    {
        _documentTypeRepository = documentTypeRepository;
    }

    public override string Code => NotificationRuleSourceCodes.CompanyDocument;

    public override string DisplayNameKey => "NotificationRuleSource_CompanyDocument";

    public override string SubTypeDisplayNameKey => "DocumentType";

    public override string ParameterName => Parameter;

    protected override IQueryable<OrganizationDocument> Records()
    {
        return Repository.GetAll()
            .Include(d => d.OrganizationUnit)
            .Include(d => d.DocumentType);
    }

    protected override Expression<Func<OrganizationDocument, bool>> SubTypeFilter(int subTypeId)
    {
        return d => d.DocumentTypeId == subTypeId;
    }

    public override async Task<List<NotificationRuleSubTypeItem>> GetSubTypesAsync()
    {
        return await _documentTypeRepository.GetAll()
            .Where(t => t.DocumentBelongsTo != DocumentBelongsTo.Employee)
            .OrderBy(t => t.NameEn)
            .Select(t => new NotificationRuleSubTypeItem { Id = t.Id, Name = t.NameEn })
            .ToListAsync();
    }

    protected override DateTime? SubjectDate(OrganizationDocument record) => DateOrNull(record.ExpiryDate);

    protected override string Describe(OrganizationDocument record)
    {
        return $"{record.OrganizationUnit?.DisplayName} - {record.DocumentType?.NameEn} {record.DocumentNum}".Trim();
    }

    protected override RuleBaseObjectDescriptor BuildCriterion()
    {
        return RuleBaseObjectBuilder<OrganizationDocument>
            .For(Code, DisplayNameKey)
            .Parameter<OrganizationDocument>(Parameter, "Document", isWritable: false, p => p
                .Read("Id", "Id", RuleDataType.Integer, d => (long)d.Id)
                .Read("OrganizationUnitId", "OrganizationUnit", RuleDataType.Integer, d => d.OrganizationUnitId)
                .Read("DocumentTypeId", "DocumentType", RuleDataType.Lookup, d => (long)d.DocumentTypeId, "DocumentType")
                .Read("DocumentNum", "DocumentNum", RuleDataType.String, d => d.DocumentNum)
                .Read("IssueDate", "IssueDate", RuleDataType.Date, d => d.IssueDate)
                .Read("ExpiryDate", "ExpiryDate", RuleDataType.Date, d => d.ExpiryDate)
                .Read("DocumentPlace", "DocumentPlace", RuleDataType.String, d => d.DocumentPlace)
                .Navigate<DocumentType>("DocumentType", "DocumentType", d => d.DocumentTypeId, t => t
                    .Read("Code", "Code", RuleDataType.String, x => x.Code)
                    .Read("NameEn", "NameEn", RuleDataType.String, x => x.NameEn)
                    .Read("NotificationPeriod", "NotificationPeriod", RuleDataType.Integer, x => (long)x.NotificationPeriod)))
            .Build();
    }
}
