using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Dependency;
using HCMv3.Dto;

namespace HCMv3.DataImporting.Excel;

public interface IExcelInvalidEntityExporter<TEntityDto> : ITransientDependency
{
    Task<FileDto> ExportToFile(List<TEntityDto> entities);
}