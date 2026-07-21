using System.Threading.Tasks;

namespace HCMv3.Net.Sms;

public interface ISmsSender
{
    Task SendAsync(string number, string message);
}

