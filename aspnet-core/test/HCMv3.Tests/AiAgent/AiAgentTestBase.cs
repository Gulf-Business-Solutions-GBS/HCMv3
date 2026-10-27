using System;
using Abp.Authorization.Users;
using Castle.MicroKernel.Registration;
using HCMv3.AiAgent;
using HCMv3.AiAgent.Chat;
using HCMv3.Authorization.Users;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;

namespace HCMv3.Tests.AiAgent;

/// <summary>A scripted model plus the smallest employee, leave type, quota and leave rows the tools need.</summary>
public abstract class AiAgentTestBase : AppTestBase
{
    protected readonly FakeAiChatClient Model = new();

    /// <summary>
    /// The model's limits, as the server is configured: the hcmv3-qwen3-4b variant with 12288 tokens. Tests that
    /// are about the limit change these. (The code's own default, 4096, is too small for an administrator's 18
    /// tools -- which is exactly why the server install sets up the larger variant.)
    /// </summary>
    protected readonly TestModelLimits ModelLimits;

    protected AiAgentTestBase()
    {
        LoginAsDefaultTenantAdmin();
        LocalIocManager.IocContainer.Register(Component.For<IAiChatClient>().Instance(Model).IsDefault());

        ModelLimits = new TestModelLimits(Resolve<HCMv3.Configuration.IAppConfigurationAccessor>());
        LocalIocManager.IocContainer.Register(Component.For<AiAgentConfiguration>().Instance(ModelLimits).IsDefault());
    }

    protected class TestModelLimits : AiAgentConfiguration
    {
        public TestModelLimits(HCMv3.Configuration.IAppConfigurationAccessor accessor) : base(accessor)
        {
        }

        public int Context { get; set; } = 12288;

        public int Reserved { get; set; } = 1024;

        public override int ContextTokens => Context;

        public override int ReservedOutputTokens => Reserved;
    }

    protected IAiAgentAppService Service => Resolve<IAiAgentAppService>();

    protected int TenantId => AbpSession.TenantId ?? 1;

    protected long CreateUser(params string[] grantedPermissions)
    {
        var userId = 0L;

        UsingDbContext(context =>
        {
            var unique = Guid.NewGuid().ToString("N").Substring(0, 8);
            var user = new User
            {
                TenantId = TenantId,
                UserName = "ai" + unique,
                Name = "AI",
                Surname = "Tester",
                EmailAddress = "ai" + unique + "@testdomain.com",
                IsEmailConfirmed = true,
                Password = "AM4OLBpptxBYmM79lGOX9egzZk3vIQU3d/gFCJzaBjAPXzYIK3tQ2N7X4fcrHtElTw=="
            };
            user.SetNormalizedNames();

            context.Users.Add(user);
            context.SaveChanges();
            userId = user.Id;

            foreach (var permission in grantedPermissions)
            {
                context.UserPermissions.Add(new UserPermissionSetting
                {
                    TenantId = TenantId,
                    UserId = userId,
                    Name = permission,
                    IsGranted = true
                });
            }
        });

        return userId;
    }

    protected int CreateEmployee(string code, long? userId = null)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Set<Nationality>().Add(new Nationality
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
        });

        Employee employee = null;
        UsingDbContext(context =>
        {
            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId,
                Code = code,
                FirstNameEn = code,
                LastNameEn = "T",
                FirstNameAr = "ت",
                LastNameAr = "ت",
                FullNameEn = code + " T",
                FullNameAr = "ت ت",
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                UserId = userId,
                NationalityId = nationality.Id
            }).Entity;
        });

        return employee.Id;
    }

    protected int CreateAbsenceType(string nameEn, string nameAr)
    {
        AbsenceType absenceType = null;
        UsingDbContext(context =>
        {
            absenceType = context.Set<AbsenceType>().Add(new AbsenceType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = nameEn,
                NameAr = nameAr
            }).Entity;
        });

        return absenceType.Id;
    }

    protected void CreateQuota(int employeeId, int absenceTypeId, double accrued, double used = 0)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeAbsenceQuota>().Add(new EmployeeAbsenceQuota
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                AbsenceTypeId = absenceTypeId,
                EntitlementFrom = DateTime.Today.AddYears(-1),
                EntitlementTo = DateTime.Today.AddYears(1),
                DeductionFrom = DateTime.Today.AddYears(-1),
                DeductionTo = DateTime.Today.AddYears(1),
                QuotaAccrued = accrued,
                QuotaUsed = used
            });
        });
    }

    protected int CreateLeave(int employeeId, int absenceTypeId, DateTime start, int days, string comments = null,
        RequestStatus? status = null)
    {
        EmployeeAbsence leave = null;
        UsingDbContext(context =>
        {
            leave = context.Set<EmployeeAbsence>().Add(new EmployeeAbsence
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                AbsenceTypeId = absenceTypeId,
                StartDate = start,
                EndDate = start.AddDays(days - 1),
                InitialEndDate = start.AddDays(days - 1),
                CalendarDays = days,
                TotalLeaveDays = days,
                Comments = comments,
                WorkflowStatus = status
            }).Entity;
        });

        return leave.Id;
    }
}
