using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp;
using Abp.Runtime.Session;
using Abp.Timing.Timezone;
using HCMv3.Chat.Dto;
using HCMv3.DataExporting.Excel.MiniExcel;
using HCMv3.Dto;
using HCMv3.Storage;

namespace HCMv3.Chat.Exporting;

public class ChatMessageListExcelExporter : MiniExcelExcelExporterBase, IChatMessageListExcelExporter
{
    private readonly ITimeZoneConverter _timeZoneConverter;

    public ChatMessageListExcelExporter(
        ITempFileCacheManager tempFileCacheManager,
        ITimeZoneConverter timeZoneConverter
        ) : base(tempFileCacheManager)
    {
        _timeZoneConverter = timeZoneConverter;
    }

    public async Task<FileDto> ExportToFile(UserIdentifier user, List<ChatMessageExportDto> messages)
    {
        var tenancyName = messages.Count > 0 ? messages.First().TargetTenantName : L("Anonymous");
        var userName = messages.Count > 0 ? messages.First().TargetUserName : L("Anonymous");

        var items = new List<Dictionary<string, object>>();

        foreach (var message in messages)
        {
            items.Add(new Dictionary<string, object>()
            {
                {L("ChatMessage_From"), message.Side == ChatSide.Receiver ? (message.TargetTenantName + "/" + message.TargetUserName) : L("You")},
                {L("ChatMessage_To"), message.Side == ChatSide.Receiver ? L("You") : (message.TargetTenantName + "/" + message.TargetUserName)},
                {L("Message"), message.Message},
                {L("ReadState"), message.Side == ChatSide.Receiver ? message.ReadState : message.ReceiverReadState},
                {L("CreationTime"), _timeZoneConverter.Convert(message.CreationTime, user.TenantId, user.UserId)},
            });
        }

        return await CreateExcelPackageAsync($"Chat_{tenancyName}_{userName}.xlsx", items);
    }
}