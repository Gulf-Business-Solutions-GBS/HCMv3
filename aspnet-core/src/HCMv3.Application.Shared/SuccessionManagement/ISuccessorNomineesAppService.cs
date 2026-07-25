using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SuccessionManagement.Dto;

namespace HCMv3.SuccessionManagement;

public interface ISuccessorNomineesAppService : IApplicationService
{
    Task<PagedResultDto<SuccessorNomineeDto>> GetAll(GetAllSuccessorNomineesInput input);

    Task<GetSuccessorNomineeForEditOutput> GetSuccessorNomineeForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditSuccessorNomineeDto input);

    Task Delete(EntityDto input);
}
