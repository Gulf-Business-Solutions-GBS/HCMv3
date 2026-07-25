using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.JobProfileManagement;

[AbpAuthorize(AppPermissions.Pages_JobProfileManagement_InterviewQuestions)]
public class InterviewQuestionsAppService : SimpleLookupAppServiceBase<InterviewQuestion>, IInterviewQuestionsAppService
{
    public InterviewQuestionsAppService(IRepository<InterviewQuestion> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_JobProfileManagement_InterviewQuestions_Create;
    protected override string EditPermissionName => AppPermissions.Pages_JobProfileManagement_InterviewQuestions_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_JobProfileManagement_InterviewQuestions_Delete;
}
