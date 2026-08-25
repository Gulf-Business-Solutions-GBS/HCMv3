using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.GridReports.Organization.Dto;

namespace HCMv3.GridReports.Organization;

/// <summary>
/// Organization Unit tree browse (port of legacy <c>uscOrgUnitTree.ascx</c>). Unlike the rest of the
/// analysis-report family this is not a filtered/paged grid -- legacy has no server-side filters at all,
/// so this is a single no-input operation returning the whole hierarchy.
/// </summary>
public interface IOrgUnitTreeReportAppService : IApplicationService
{
    Task<List<OrgUnitTreeNodeDto>> GetOrgUnitTree();
}
