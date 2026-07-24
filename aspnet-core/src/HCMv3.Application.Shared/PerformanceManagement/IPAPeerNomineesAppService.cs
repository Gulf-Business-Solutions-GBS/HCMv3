using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PerformanceManagement.Dto;

namespace HCMv3.PerformanceManagement;

public interface IPAPeerNomineesAppService : IApplicationService
{
    Task<PagedResultDto<PAPeerNomineeDto>> GetAll(GetAllPAPeerNomineesInput input);

    Task<GetPAPeerNomineeForEditOutput> GetPAPeerNomineeForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditPAPeerNomineeDto input);

    Task Delete(EntityDto input);
}
