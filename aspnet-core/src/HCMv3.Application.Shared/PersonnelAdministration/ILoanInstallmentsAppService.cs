using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface ILoanInstallmentsAppService : IApplicationService
{
    Task<PagedResultDto<LoanInstallmentDto>> GetAll(GetAllLoanInstallmentsInput input);

    Task<GetLoanInstallmentForEditOutput> GetLoanInstallmentForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditLoanInstallmentDto input);

    Task Delete(EntityDto input);
}
