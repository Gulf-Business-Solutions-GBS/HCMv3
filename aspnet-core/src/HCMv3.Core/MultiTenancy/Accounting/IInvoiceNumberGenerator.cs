using System.Threading.Tasks;
using Abp.Dependency;

namespace HCMv3.MultiTenancy.Accounting;

public interface IInvoiceNumberGenerator : ITransientDependency
{
    Task<string> GetNewInvoiceNumber();
}

