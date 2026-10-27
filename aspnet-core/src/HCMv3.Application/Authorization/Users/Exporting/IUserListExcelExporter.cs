using System.Collections.Generic;
using System.Threading.Tasks;
using HCMv3.Authorization.Users.Dto;
using HCMv3.Dto;

namespace HCMv3.Authorization.Users.Exporting;

public interface IUserListExcelExporter
{
    Task<FileDto> ExportToFile(List<UserListDto> userListDtos, List<string> selectedColumns);
}