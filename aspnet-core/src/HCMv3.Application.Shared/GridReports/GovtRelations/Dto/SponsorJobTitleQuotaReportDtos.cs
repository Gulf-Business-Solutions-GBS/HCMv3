using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.GovtRelations.Dto;

/// <summary>
/// Filter input for the Sponsor Job Title Quota analysis grid report (legacy
/// <c>uscRptSponsorJobTitleQuota.ascx</c>). Legacy's only filter is <c>cmbLeaveType</c> (despite its name,
/// bound to Sponsor and matched on the Sponsor CODE -- session key "SponsorCode"). No date/employee
/// filters, no RLS: rows are Sponsor+GovtJobTitle quota records, not employee data.
/// </summary>
public class GetSponsorJobTitleQuotaReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbLeaveType</c> (misnamed control, actually a Sponsor picker): matches Sponsor CODE.</summary>
    public string SponsorCode { get; set; }

    protected override string DefaultSorting => "SponsorName ASC";
}

/// <summary>
/// One grid row. Mirrors the legacy <c>DataTable</c> built in
/// <c>uscRptSponsorJobTitleQuota.ascx.cs:476-587</c>.
///
/// LEGACY COLUMN SET: OID, Sponsor, GovtJobTitle, GovtJobCategory, NoOfPosition, CitizenPercent,
/// CitizenQuotaAccrued, CitizenQuotaUsed, CitizenQuotaBalance, NonCitizenPercent, NonCitizenQuotaAccrued,
/// NonCitizenQuotaUsed, NonCitizenQuotaBalance. Balance columns are Accrued - Used, computed here exactly
/// as legacy's XPO read-only getter did (<see cref="GovtRelationsManagement.SponsorJobTitle"/> remarks:
/// Balance is a DTO-only computed value in HCMv3, not a stored column).
/// </summary>
public class SponsorJobTitleQuotaReportRowDto : EntityDto<int>
{
    public string SponsorName { get; set; }

    public string GovtJobTitleName { get; set; }

    public string GovtJobCategoryName { get; set; }

    public int NoofPosition { get; set; }

    public int CitizenPercent { get; set; }

    public int CitizenQuotaAccrued { get; set; }

    public int CitizenQuotaUsed { get; set; }

    public int CitizenQuotaBalance { get; set; }

    public int NonCitizenPercent { get; set; }

    public int NonCitizenQuotaAccrued { get; set; }

    public int NonCitizenQuotaUsed { get; set; }

    public int NonCitizenQuotaBalance { get; set; }
}
