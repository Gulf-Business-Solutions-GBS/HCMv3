using Abp.Dependency;
using QRCoder;
using System;

namespace HCMv3.Authorization.QrLogin;

public class QrLoginImageService : ITransientDependency
{
    public string GenerateSetupCode(string connectionId, string sessionId)
    {
        var data = $"{connectionId}|{sessionId}";

        using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
        using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q))
        using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
        {
            byte[] qrCodeImage = qrCode.GetGraphic(20);
            return $"data:image/png;base64,{Convert.ToBase64String(qrCodeImage)}";
        }
    }
}