namespace HCMv3.Net.Emailing;

public interface IEmailTemplateProvider
{
    string GetDefaultTemplate(int? tenantId);
}

