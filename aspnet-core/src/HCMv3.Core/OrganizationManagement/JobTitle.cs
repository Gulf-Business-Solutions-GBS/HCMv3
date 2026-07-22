using HCMv3.GeneralMasterInfo;

namespace HCMv3.OrganizationManagement;

/// <summary>
/// Job title master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.OrganizationManagement.JobTitle</c>).
/// Pure Code/NameEn/NameAr lookup — see <see cref="SimpleLookupEntity"/>.
/// The legacy <c>JobTitleDetail</c> child (requiring an unported WorkflowRole FK) is intentionally
/// out of scope for this port.
/// </summary>
public class JobTitle : SimpleLookupEntity
{
}
