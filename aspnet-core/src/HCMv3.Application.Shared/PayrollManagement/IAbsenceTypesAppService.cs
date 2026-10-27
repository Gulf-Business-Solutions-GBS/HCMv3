using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IAbsenceTypesAppService : IApplicationService
{
    Task<PagedResultDto<AbsenceTypeDto>> GetAll(GetAllAbsenceTypesInput input);

    Task<GetAbsenceTypeForEditOutput> GetAbsenceTypeForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditAbsenceTypeDto input);

    Task Delete(EntityDto input);
}
