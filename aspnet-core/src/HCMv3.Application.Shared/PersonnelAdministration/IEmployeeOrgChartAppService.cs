using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Read-only feed for the Employee Org Chart. Every method answers a question about the existing
/// <c>EmployeeManager</c> reporting lines -- nothing here changes them; that stays with
/// <see cref="IEmployeeManagersAppService"/>.
/// </summary>
public interface IEmployeeOrgChartAppService : IApplicationService
{
    /// <summary>Returns one person, the chain of managers above them, and their direct reports.</summary>
    Task<EmployeeOrgChartDto> GetNode(GetEmployeeOrgChartNodeInput input);

    /// <summary>Type-ahead over people who appear in the reporting structure on the given date.</summary>
    Task<List<EmployeeOrgChartNodeDto>> SearchNodes(SearchEmployeeOrgChartNodesInput input);
}
