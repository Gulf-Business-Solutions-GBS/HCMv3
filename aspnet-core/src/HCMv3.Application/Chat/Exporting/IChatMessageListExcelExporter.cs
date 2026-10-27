using System.Collections.Generic;
using System.Threading.Tasks;
using Abp;
using HCMv3.Chat.Dto;
using HCMv3.Dto;

namespace HCMv3.Chat.Exporting;

public interface IChatMessageListExcelExporter
{
    Task<FileDto> ExportToFile(UserIdentifier user, List<ChatMessageExportDto> messages);
}