using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Recruitment.Dto;

namespace HCMv3.Recruitment;

public interface IVacanciesAppService : IApplicationService
{
    Task<PagedResultDto<VacancyDto>> GetAll(GetAllVacanciesInput input);

    Task<GetVacancyForEditOutput> GetVacancyForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditVacancyDto input);

    Task Delete(EntityDto input);
}
