using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.MultiTenancy.Accounting.Dto;

namespace HCMv3.MultiTenancy.Accounting;

public interface IInvoiceAppService
{
    Task<InvoiceDto> GetInvoiceInfo(EntityDto<long> input);

    Task CreateInvoice(CreateInvoiceDto input);
}
