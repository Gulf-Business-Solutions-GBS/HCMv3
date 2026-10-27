using System;
using System.IO;
using System.Net.Mail;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Net.Mail;

namespace HCMv3.ReportManagement.Services;

/// <summary>
/// Delivers a rendered slice as an e-mail attachment to one recipient.
///
/// Uses ABP's <see cref="IEmailSender"/> rather than HCMv3's <c>IEmailService</c> because the latter is
/// built around an <c>EmailTemplate</c> row and an <c>Employee</c> recipient. Report distribution has
/// neither: the subject and body come from the destination's templates, and a recipient may be an
/// external address with no employee record at all.
/// </summary>
public class EmailReportSliceDeliveryChannel : IReportSliceDeliveryChannel, ITransientDependency
{
    private readonly IEmailSender _emailSender;

    public EmailReportSliceDeliveryChannel(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public ReportDestinationMethod Method => ReportDestinationMethod.Email;

    public async Task DeliverAsync(
        ReportSchedule schedule,
        ReportScheduleDestination destination,
        ReportBurstSlice slice,
        ReportSliceRecipient recipient,
        RenderedReportSlice rendered)
    {
        if (string.IsNullOrWhiteSpace(recipient?.Email))
        {
            throw new InvalidOperationException("Recipient has no e-mail address.");
        }

        if (rendered?.Content == null || rendered.Content.Length == 0)
        {
            // The executor already skips empty slices; reaching here means a renderer reported rows but
            // produced no bytes, which is a bug worth surfacing rather than sending a 0-byte attachment.
            throw new InvalidOperationException("Nothing to attach: the rendered slice is empty.");
        }

        var subject = ApplyTokens(
            string.IsNullOrWhiteSpace(destination.EmailSubjectTemplate)
                ? "{ReportName}"
                : destination.EmailSubjectTemplate,
            schedule,
            slice);

        var body = ApplyTokens(destination.EmailBodyTemplate ?? string.Empty, schedule, slice);

        var message = new MailMessage
        {
            Subject = subject,
            Body = body,
            IsBodyHtml = true,
        };

        message.To.Add(recipient.Email.Trim());

        // The stream is owned by the Attachment, which the MailMessage disposes with itself.
        var stream = new MemoryStream(rendered.Content);
        message.Attachments.Add(new Attachment(stream, rendered.FileName, rendered.ContentType));

        using (message)
        {
            await _emailSender.SendAsync(message);
        }
    }

    /// <summary>
    /// Substitutes the small token set the wizard documents. Deliberately not a general template
    /// engine -- these strings are authored by administrators and rendered into e-mail, so the set of
    /// things they can interpolate stays fixed and known.
    /// </summary>
    private static string ApplyTokens(string template, ReportSchedule schedule, ReportBurstSlice slice)
    {
        if (string.IsNullOrEmpty(template))
        {
            return string.Empty;
        }

        return template
            .Replace("{ReportName}", schedule.Name ?? string.Empty)
            .Replace("{SliceName}", slice?.Display ?? string.Empty)
            .Replace("{ReportKey}", schedule.ReportKey ?? string.Empty);
    }
}
