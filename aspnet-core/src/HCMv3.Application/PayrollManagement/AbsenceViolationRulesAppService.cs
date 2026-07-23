using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement;

[AbpAuthorize(AppPermissions.Pages_PayrollManagement_AbsenceViolationRules)]
public class AbsenceViolationRulesAppService : SimpleLookupAppServiceBase<AbsenceViolationRule>, IAbsenceViolationRulesAppService
{
    public AbsenceViolationRulesAppService(IRepository<AbsenceViolationRule> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_PayrollManagement_AbsenceViolationRules_Create;
    protected override string EditPermissionName => AppPermissions.Pages_PayrollManagement_AbsenceViolationRules_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_PayrollManagement_AbsenceViolationRules_Delete;
}
