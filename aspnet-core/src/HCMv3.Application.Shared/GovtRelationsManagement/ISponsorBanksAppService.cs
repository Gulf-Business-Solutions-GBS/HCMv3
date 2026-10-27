using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GovtRelationsManagement.Dto;

namespace HCMv3.GovtRelationsManagement;

public interface ISponsorBanksAppService : IApplicationService
{
    Task<PagedResultDto<SponsorBankDto>> GetAll(GetAllSponsorBanksInput input);

    Task<GetSponsorBankForEditOutput> GetSponsorBankForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditSponsorBankDto input);

    Task Delete(EntityDto input);
}
