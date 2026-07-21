using Abp.Application.Services.Dto;
using Microsoft.AspNetCore.Components;
using HCMv3.Authorization;
using HCMv3.Authorization.Users;
using HCMv3.Authorization.Users.Dto;
using HCMv3.Maui.Core.Components;
using HCMv3.Maui.Core.Threading;
using HCMv3.Maui.Models.User;
using HCMv3.Maui.Services.Navigation;
using HCMv3.Maui.Services.User;

namespace HCMv3.Maui.Pages.User;

public partial class CreateOrEditUser : HCMv3MainLayoutPageComponentBase
{
    [Parameter] public long? UserId { get; set; }

    private readonly bool _hasCreatePermission;

    private readonly bool _hasEditPermission;

    private readonly bool _hasDeletePermission;

    private readonly bool _hasUnlockPermission;

    private IUserAppService UserAppService { get; }
    private IUserProfileService UserProfileService { get; }

    public CreateOrEditUserModel CreateOrEditUserModel { get; set; } = new();

    public CreateOrEditUser()
    {
        UserAppService = Resolve<IUserAppService>();
        UserProfileService = Resolve<IUserProfileService>();
        _hasCreatePermission = PermissionService.HasPermission(AppPermissions.Pages_Administration_Users_Create);
        _hasEditPermission = PermissionService.HasPermission(AppPermissions.Pages_Administration_Users_Edit);
        _hasDeletePermission = PermissionService.HasPermission(AppPermissions.Pages_Administration_Users_Delete);
        _hasUnlockPermission = PermissionService.HasPermission(AppPermissions.Pages_Administration_Users_Unlock);
    }

    protected override async Task OnInitializedAsync()
    {
        CreateOrEditUserModel = new CreateOrEditUserModel();

        if (UserId.HasValue)
        {
            await SetBusyAsync(async () =>
            {
                await WebRequestExecuter.Execute(
                    async () => await UserAppService.GetUserForEdit(new NullableIdDto<long>(UserId)),
                    getUserForEditOutput =>
                    {
                        InitializeModel(getUserForEditOutput);
                        return Task.CompletedTask;
                    }
                );
            });
        }

        if (CreateOrEditUserModel.Id.HasValue)
        {
            CreateOrEditUserModel.Photo = await UserProfileService.GetProfilePicture(CreateOrEditUserModel.Id.Value);
        }
        else
        {
            CreateOrEditUserModel.Photo = UserProfileService.GetDefaultProfilePicture();
            CreateOrEditUserModel.IsActive = true;
            CreateOrEditUserModel.IsLockoutEnabled = true;
            CreateOrEditUserModel.ShouldChangePasswordOnNextLogin = true;
        }

        var title = CreateOrEditUserModel.Id.HasValue ? L("EditUser") : L("CreateNewUser");

        await SetPageHeader(title);
    }

    private void InitializeModel(GetUserForEditOutput getUserForEditOutput)
    {
        CreateOrEditUserModel = ObjectMapper.Map<CreateOrEditUserModel>(getUserForEditOutput.User);
        CreateOrEditUserModel.Roles = getUserForEditOutput.Roles;
        CreateOrEditUserModel.MemberedOrganizationUnits = getUserForEditOutput.MemberedOrganizationUnits;

        CreateOrEditUserModel.SelectedOrganizationUnits =
            ObjectMapper.Map<List<OrganizationUnitModel>>(getUserForEditOutput.AllOrganizationUnits);
    }

    private async Task SaveUser()
    {
        var user = ObjectMapper.Map<UserEditDto>(CreateOrEditUserModel);

        var input = new CreateOrUpdateUserInput
        {
            User = user,
            AssignedRoleNames = CreateOrEditUserModel.Roles.Where(x => x.IsAssigned).Select(x => x.RoleName).ToArray(),
            OrganizationUnits = CreateOrEditUserModel.SelectedOrganizationUnits.Where(x => x.IsAssigned)
                .Select(x => x.Id).ToList(),
            SendActivationEmail = CreateOrEditUserModel.SendActivationEmail,
            SetRandomPassword = CreateOrEditUserModel.SetRandomPassword
        };

        await SetBusyAsync(async () =>
        {
            await WebRequestExecuter.Execute(
                async () => await UserAppService.CreateOrUpdateUser(input),
                async () =>
                {
                    NavigationService.NavigateTo(NavigationUrlConsts.Users);
                    await UserDialogsService.AlertSuccess(L("SuccessfullySaved"));
                }
            );
        });
    }

    private string OUCodeIntentConverter(string code)
    {
        var resultWithIndent = "";

        var indentCharacter = ".";
        foreach (var character in code)
        {
            if (character == '.')
            {
                resultWithIndent += indentCharacter;
            }
        }

        return resultWithIndent;
    }

    private async Task UnlockUser()
    {
        if (!CreateOrEditUserModel.Id.HasValue)
        {
            return;
        }

        await SetBusyAsync(async () =>
        {
            await WebRequestExecuter.Execute(
                async () => await UserAppService.UnlockUser(new EntityDto<long>(CreateOrEditUserModel.Id.Value)),
                async () =>
                {
                    await UserDialogsService.AlertSuccess(L("UnlockedTheUser", CreateOrEditUserModel.UserName));
                }
            );
        });
    }

    private async Task DeleteUser()
    {
        if (!CreateOrEditUserModel.Id.HasValue)
        {
            return;
        }

        var isConfirmed =
            await UserDialogsService.Confirm(
                L("UserDeleteWarningMessage", "\"" + CreateOrEditUserModel.UserName + "\""), L("AreYouSure"));
        if (isConfirmed)
        {
            await SetBusyAsync(async () =>
            {
                await WebRequestExecuter.Execute(
                    async () => await UserAppService.DeleteUser(new EntityDto<long>(CreateOrEditUserModel.Id.Value)),
                    async () =>
                    {
                        await UserDialogsService.AlertSuccess(L("SuccessfullyDeleted"));
                        NavigationService.NavigateTo(NavigationUrlConsts.Users);
                    }
                );
            });
        }
    }
}