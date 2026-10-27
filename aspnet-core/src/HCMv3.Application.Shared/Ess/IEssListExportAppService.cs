using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.Dto;
using HCMv3.Ess.Dto;

namespace HCMv3.Ess;

/// <summary>Export to Excel for the self-service request lists.</summary>
public interface IEssListExportAppService : IApplicationService
{
    /// <summary>Turns the list the employee has on screen into a workbook, with real numbers and dates.</summary>
    Task<FileDto> ExportToExcel(EssListExportInput input);
}
