using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Abp.Localization;
using Abp.MailKit;
using Abp.Net.Mail.Smtp;
using Abp.UI;
using MailKit.Net.Smtp;

namespace HCMv3.Net.Emailing;

public class HCMv3MailKitSmtpBuilder : DefaultMailKitSmtpBuilder
{
    private readonly ILocalizationManager _localizationManager;
    private readonly IEmailSettingsChecker _emailSettingsChecker;

    public HCMv3MailKitSmtpBuilder(
        ISmtpEmailSenderConfiguration smtpEmailSenderConfiguration,
        IAbpMailKitConfiguration abpMailKitConfiguration,
        ILocalizationManager localizationManager,
        IEmailSettingsChecker emailSettingsChecker) : base(smtpEmailSenderConfiguration, abpMailKitConfiguration)
    {
        _localizationManager = localizationManager;
        _emailSettingsChecker = emailSettingsChecker;
    }

    protected override void ConfigureClient(SmtpClient client)
    {
        if (!_emailSettingsChecker.EmailSettingsValid())
        {
            throw new UserFriendlyException(L("SMTPSettingsNotProvidedWarningText"));
        }

        client.ServerCertificateValidationCallback = SslCertificateValidationCallback;
        base.ConfigureClient(client);
    }

    private string L(string name)
    {
        return _localizationManager.GetString(HCMv3Consts.LocalizationSourceName, name);
    }

    private static bool SslCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        // If there are no errors, then everything went smoothly.
        if (sslPolicyErrors == SslPolicyErrors.None)
            return true;

        // Note: MailKit will always pass the host name string as the `sender` argument.
        var host = (string)sender;

        if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNotAvailable) != 0)
        {
            // This means that the remote certificate is unavailable. Notify the user and return false.
            Console.WriteLine("The SSL certificate was not available for {0}", host);
            return false;
        }

        if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) != 0)
        {
            // This means that the server's SSL certificate did not match the host name that we are trying to connect to.
            var cn = certificate is X509Certificate2 certificate2 ? certificate2.GetNameInfo(X509NameType.SimpleName, false) : certificate.Subject;

            Console.WriteLine("The Common Name for the SSL certificate did not match {0}. Instead, it was {1}.", host, cn);
            return false;
        }

        // The only other errors left are chain errors.
        Console.WriteLine("The SSL certificate for the server could not be validated for the following reasons:");

        // The first element's certificate will be the server's SSL certificate (and will match the `certificate` argument)
        // while the last element in the chain will typically either be the Root Certificate Authority's certificate -or- it
        // will be a non-authoritative self-signed certificate that the server admin created. 
        foreach (var element in chain.ChainElements)
        {
            // Each element in the chain will have its own status list. If the status list is empty, it means that the
            // certificate itself did not contain any errors.
            if (element.ChainElementStatus.Length == 0)
                continue;

            Console.WriteLine("\u2022 {0}", element.Certificate.Subject);
            foreach (var error in element.ChainElementStatus)
            {
                // `error.StatusInformation` contains a human-readable error string while `error.Status` is the corresponding enum value.
                Console.WriteLine("\t\u2022 {0}", error.StatusInformation);
            }
        }

        return false;
    }
}

