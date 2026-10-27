using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface ITempEmployeeBenefitsAppService : IApplicationService
{
    Task<PagedResultDto<TempEmployeeBenefitDto>> GetAll(GetAllTempEmployeeBenefitsInput input);

    Task<GetTempEmployeeBenefitForEditOutput> GetTempEmployeeBenefitForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditTempEmployeeBenefitDto input);

    Task Delete(EntityDto input);
}
