using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeFundsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeFundDto>> GetAll(GetAllEmployeeFundsInput input);

    Task<GetEmployeeFundForEditOutput> GetEmployeeFundForEdit(EntityDto input);

    /// <summary>Creates or updates the fund and returns its id (so the dialog can add detail rows under a new fund).</summary>
    Task<int> CreateOrEdit(CreateOrEditEmployeeFundDto input);

    /// <summary>Deletes the fund together with its detail and exemption rows (legacy aggregated collections).</summary>
    Task Delete(EntityDto input);

    /// <summary>Employees to pick from when adding a fund from the Fund list, ordered by code.</summary>
    Task<ListResultDto<SimpleLookupDto>> GetEmployeesForLookup(GetEmployeeFundEmployeesInput input);
}
