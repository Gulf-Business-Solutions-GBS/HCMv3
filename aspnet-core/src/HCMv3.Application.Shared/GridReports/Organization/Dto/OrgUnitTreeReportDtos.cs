using System.Collections.Generic;
using Abp.Application.Services.Dto;

namespace HCMv3.GridReports.Organization.Dto;

/// <summary>
/// One node of the Organization Unit tree (port of legacy <c>uscOrgUnitTree.ascx</c>). Legacy built a
/// DevExpress <c>TreeViewNode</c> tree recursively from the org unit hierarchy with only a display text
/// per node (<c>node.Text = rec.NameEn</c>); this DTO mirrors that shape for the Angular/React tree
/// widget. Legacy's <c>txtSearch</c>/<c>btnSearch</c> "find and expand to node" behaviour was pure
/// client-side DOM manipulation over the already-rendered tree (<c>FindAllRecursive</c> +
/// <c>ExpandToNode</c>) and has no server-side equivalent -- the client owns search/expand over the tree
/// this endpoint returns.
/// </summary>
public class OrgUnitTreeNodeDto : EntityDto<long>
{
    public string Name { get; set; }

    public List<OrgUnitTreeNodeDto> Children { get; set; } = new();
}
