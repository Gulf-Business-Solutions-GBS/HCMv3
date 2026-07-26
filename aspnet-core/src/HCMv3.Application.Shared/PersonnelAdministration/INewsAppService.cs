using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface INewsAppService : IApplicationService
{
    Task<PagedResultDto<NewsDto>> GetAll(GetAllNewsInput input);

    Task<GetNewsForEditOutput> GetNewsForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditNewsDto input);

    Task Delete(EntityDto input);
}
