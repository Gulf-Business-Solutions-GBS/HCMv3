using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IAbsenceTypeExceptionsAppService : IApplicationService
{
    Task<PagedResultDto<AbsenceTypeExceptionDto>> GetAll(GetAllAbsenceTypeExceptionsInput input);

    Task<GetAbsenceTypeExceptionForEditOutput> GetAbsenceTypeExceptionForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditAbsenceTypeExceptionDto input);

    Task Delete(EntityDto input);
}
