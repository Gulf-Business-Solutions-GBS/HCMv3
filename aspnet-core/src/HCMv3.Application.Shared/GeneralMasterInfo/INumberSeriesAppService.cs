using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo.Dto;

namespace HCMv3.GeneralMasterInfo;

public interface INumberSeriesAppService : IApplicationService
{
    Task<PagedResultDto<NumberSeriesDto>> GetAll(GetAllNumberSeriesInput input);

    Task<GetNumberSeriesForEditOutput> GetNumberSeriesForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditNumberSeriesDto input);

    Task Delete(EntityDto input);
}
