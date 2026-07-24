using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GovtRelationsManagement.Dto;

namespace HCMv3.GovtRelationsManagement;

public interface ISignatoriesAppService : IApplicationService
{
    Task<PagedResultDto<SignatoryDto>> GetAll(GetAllSignatoriesInput input);

    Task<GetSignatoryForEditOutput> GetSignatoryForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditSignatoryDto input);

    Task Delete(EntityDto input);
}
