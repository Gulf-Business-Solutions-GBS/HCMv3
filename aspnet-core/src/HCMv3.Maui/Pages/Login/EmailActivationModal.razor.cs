using Microsoft.AspNetCore.Components;
using HCMv3.Authorization.Accounts;
using HCMv3.Authorization.Accounts.Dto;
using HCMv3.Maui.Core.Components;
using HCMv3.Maui.Core.Threading;
using HCMv3.Maui.Models.Login;

namespace HCMv3.Maui.Pages.Login;

public partial class EmailActivationModal : ModalBase
{
    public override string ModalId => "email-activation-modal";

    [Parameter] public EventCallback OnSave { get; set; }

    public EmailActivationModel emailActivationModel { get; set; } = new EmailActivationModel();

    private readonly IAccountAppService _accountAppService;

    public EmailActivationModal()
    {
        _accountAppService = Resolve<IAccountAppService>();
    }

    protected virtual async Task Save()
    {
        await SetBusyAsync(async () =>
        {
            await WebRequestExecuter.Execute(
                async () =>
                    await _accountAppService.SendEmailActivationLink(new SendEmailActivationLinkInput
                    {
                        EmailAddress = emailActivationModel.EmailAddress
                    }),
                async () =>
                {
                    await OnSave.InvokeAsync();
                }
            );
        });
    }

    protected virtual async Task Cancel()
    {
        await Hide();
    }
}