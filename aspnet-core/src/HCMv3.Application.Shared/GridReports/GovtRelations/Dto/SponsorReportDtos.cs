using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.GovtRelations.Dto;

/// <summary>
/// Filter input for the Sponsor analysis grid report (legacy <c>uscRptSponsor.ascx</c>). Legacy's
/// <c>Table1()</c> has NO filters at all -- it lists every <c>Sponsor</c> row unconditionally
/// (uscRptSponsor.ascx.cs). The common FromDate/ToDate/EmployeeCode/EmployeeStatus fields are kept for
/// shape consistency but are hidden on the Angular filter panel and unused server-side. No RLS either --
/// Sponsor is a master/reference entity, not employee data.
/// </summary>
public class GetSponsorReportInput : GridReportInputBase
{
    protected override string DefaultSorting => "Name ASC";
}

/// <summary>
/// One grid row. Mirrors the legacy <c>DataTable</c> built in <c>uscRptSponsor.ascx.cs:528-...</c>.
///
/// LEGACY COLUMN SET actually populated (several declared string labels -- TradeLicenseNum,
/// LicenseIssueDate, LicenseExpiryDate, CivilId, ActvitiyType -- were declared but NEVER added to
/// <c>table.Columns</c>, so they never appeared in the legacy grid; not ported here either): OID, Code,
/// Name, OrganizationUnit, UnifiedNum, SocialSecurityNum, MOSALID, Governate, Email, Gender, profession,
/// Building, Street, Area, Telephone, Nationality, BusinessType, Block, Fax, POBox.
///
/// LEGACY DEFECT FLAGGED FOR REVIEW (reproduced, not a data-loss defect so kept faithful per the porting
/// rules): legacy's "Code" column is fed <c>rec.Oid</c> (the surrogate key), NOT <c>rec.Code</c>
/// (uscRptSponsor.ascx.cs: <c>Code = rec.Oid</c> inside the projection) -- i.e. the legacy grid's Code
/// column never actually showed the Sponsor's business code. Reproduced here as <see cref="Code"/> = the
/// row Id, with the real business code exposed separately as <see cref="SponsorCode"/> so the data is not
/// silently lost.
/// </summary>
public class SponsorReportRowDto : EntityDto<int>
{
    /// <summary>Legacy "Code" column -- actually the Oid, see class remarks.</summary>
    public int Code { get; set; }

    /// <summary>The Sponsor's real business code (legacy never displayed this despite the column's name).</summary>
    public string SponsorCode { get; set; }

    public string Name { get; set; }

    public string OrganizationUnitName { get; set; }

    public long UnifiedNum { get; set; }

    public long SocialSecurityNum { get; set; }

    public long MOSALID { get; set; }

    public string GovernorateName { get; set; }

    public string Email { get; set; }

    public GeneralMasterInfo.Gender Gender { get; set; }

    public string Profession { get; set; }

    public string Building { get; set; }

    public string Street { get; set; }

    public string Area { get; set; }

    public int Telephone { get; set; }

    public string NationalityName { get; set; }

    public string BusinessTypeName { get; set; }

    public string Block { get; set; }

    public string Fax { get; set; }

    public string POBox { get; set; }
}
