using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeDocumentMvmtsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeDocumentMvmtDto>> GetAll(GetAllEmployeeDocumentMvmtsInput input);

    Task<GetEmployeeDocumentMvmtForEditOutput> GetEmployeeDocumentMvmtForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeDocumentMvmtDto input);

    Task Delete(EntityDto input);

    /// <summary>
    /// Records the day the document was physically handed to the employee, and who recorded it. Calling it
    /// again corrects the date.
    /// </summary>
    Task RecordHandOver(RecordDocumentHandOverInput input);

    /// <summary>
    /// Records the document as back in custody, taken at the counter. Refused while the employee's own
    /// return request for the same movement is still awaiting approval.
    /// </summary>
    Task RecordReturn(RecordDocumentReturnInput input);
}
