using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Uow;
using Abp.Localization;
using HCMv3.BusinessRules;
using HCMv3.BusinessRules.Definition;
using HCMv3.GeneralMasterInfo;
using HCMv3.HcmNotifications;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using NSubstitute;

namespace HCMv3.Tests.HcmNotifications;

/// <summary>
/// Fixtures the notification tests share: active employees, configurations, document types and documents,
/// criteria written as the formula editor would store them, and a recording delivery service.
///
/// <para>
/// The services under test are built by hand around substitutes so a test can read exactly what was sent.
/// No interceptor opens a unit of work for a hand-built service, so <see cref="InUnitOfWorkAsync{T}"/> opens
/// one around each call -- which is also what the daily worker does.
/// </para>
///
/// <para>
/// Depends on two lines the shared files must carry (see the build report): <c>DbSet&lt;NotificationSentLog&gt;</c>
/// in <c>HCMv3DbContext</c>, and <c>HCMv3HcmNotificationsModule</c> in <c>HCMv3CoreModule</c>'s DependsOn.
/// </para>
/// </summary>
public abstract class HcmNotificationTestBase : AppTestBase
{
    protected static readonly DateTime Today = new(2026, 10, 19);
    protected static readonly DateTime Now = Today.AddHours(9);

    protected readonly IUnitOfWorkManager UnitOfWorkManager;

    /// <summary>Every delivery the service under test asked for, in order.</summary>
    protected readonly List<NotificationDelivery> Deliveries = new();

    /// <summary>Every direct in-app delivery: name, message, users.</summary>
    protected readonly List<(string Name, string Message, List<long> UserIds)> InAppDeliveries = new();

    protected readonly INotificationDeliveryService Delivery = Substitute.For<INotificationDeliveryService>();

    protected HcmNotificationTestBase()
    {
        LoginAsDefaultTenantAdmin();
        UnitOfWorkManager = Resolve<IUnitOfWorkManager>();

        Delivery.DeliverAsync(Arg.Any<NotificationDelivery>()).Returns(call =>
        {
            Deliveries.Add(call.Arg<NotificationDelivery>());
            return Task.FromResult(new NotificationDeliveryResult { RecipientCount = 1, InAppCount = 1, EmailSent = true });
        });

        Delivery.DeliverInAppAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IEnumerable<long>>()).Returns(call =>
        {
            var users = call.ArgAt<IEnumerable<long>>(2).ToList();
            InAppDeliveries.Add((call.ArgAt<string>(0), call.ArgAt<string>(1), users));
            return Task.FromResult(users.Count);
        });
    }

    protected int TenantId => AbpSession.TenantId ?? 1;

    protected ILocalizationManager LocalizationManager => Resolve<ILocalizationManager>();

    protected async Task<T> InUnitOfWorkAsync<T>(Func<Task<T>> action)
    {
        using (var unitOfWork = UnitOfWorkManager.Begin())
        {
            var result = await action();
            await unitOfWork.CompleteAsync();
            return result;
        }
    }

    protected int CreateEmployee(
        string code,
        long? userId = null,
        EmployeeStatus status = EmployeeStatus.Active,
        DateTime? probationEndDate = null)
    {
        return UsingDbContext(context =>
        {
            var nationality = context.Set<Nationality>().Add(new Nationality
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
            context.SaveChanges();

            var employee = context.Set<Employee>().Add(new Employee
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
                ProbationEndDate = probationEndDate ?? new DateTime(2020, 4, 1),
                Status = status,
                UserId = userId,
                Email = code.ToLowerInvariant() + "@example.com",
                NationalityId = nationality.Id
            }).Entity;
            context.SaveChanges();

            return employee.Id;
        });
    }

    protected int CreateNotificationConfig(bool sendNotification = true, bool sendEmail = true, int? emailTemplateId = null)
    {
        return UsingDbContext(context =>
        {
            var config = context.Set<NotificationConfig>().Add(new NotificationConfig
            {
                TenantId = TenantId,
                Name = "Config " + Guid.NewGuid().ToString("N").Substring(0, 6),
                SendNotification = sendNotification,
                SendEmail = sendEmail,
                ToEmployee = true,
                EmailTemplateId = emailTemplateId
            }).Entity;
            context.SaveChanges();
            return config.Id;
        });
    }

    protected int CreateEmailTemplate(string code, string subject = "Subject")
    {
        return UsingDbContext(context =>
        {
            var template = context.Set<EmailTemplate>().Add(new EmailTemplate
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code,
                NameAr = code,
                Subject = subject,
                EmailBodyText = "Body"
            }).Entity;
            context.SaveChanges();
            return template.Id;
        });
    }

    protected int CreateDocumentType(
        string code,
        int? notificationConfigId = null,
        int notificationPeriod = 30,
        NotificationFrequency frequency = NotificationFrequency.Once,
        int countLimit = 0,
        DocumentBelongsTo belongsTo = DocumentBelongsTo.Employee)
    {
        return UsingDbContext(context =>
        {
            var type = context.Set<DocumentType>().Add(new DocumentType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code,
                NameAr = code,
                NotificationConfigId = notificationConfigId,
                NotificationPeriod = notificationPeriod,
                NotificationFrequency = frequency,
                NotificationCountLimit = countLimit,
                DocumentBelongsTo = belongsTo
            }).Entity;
            context.SaveChanges();
            return type.Id;
        });
    }

    protected int CreateEmployeeDocument(int employeeId, int documentTypeId, DateTime expiryDate, DateTime? issueDate = null)
    {
        return UsingDbContext(context =>
        {
            var document = context.Set<EmployeeDocument>().Add(new EmployeeDocument
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                DocumentTypeId = documentTypeId,
                DocumentNum = "D" + Guid.NewGuid().ToString("N").Substring(0, 6),
                IssueDate = issueDate ?? expiryDate.AddYears(-5),
                ExpiryDate = expiryDate
            }).Entity;
            context.SaveChanges();
            return document.Id;
        });
    }

    protected int CreateRule(
        string notificationType,
        int notificationConfigId,
        string criterion = null,
        int? subTypeId = null,
        bool isEnabled = true)
    {
        return UsingDbContext(context =>
        {
            var rule = context.Set<NotificationMaster>().Add(new NotificationMaster
            {
                TenantId = TenantId,
                NotificationType = notificationType,
                NotificationSubTypeId = subTypeId,
                NotificationConfigId = notificationConfigId,
                Criterion = criterion,
                IsEnabled = isEnabled
            }).Entity;
            context.SaveChanges();
            return rule.Id;
        });
    }

    protected List<NotificationSentLog> SentLog()
    {
        return UsingDbContext(context => context.Set<NotificationSentLog>().OrderBy(l => l.Id).ToList());
    }

    // ------------------------------------------------------------------------------------------------------
    // Criteria, written the way the formula editor stores them
    // ------------------------------------------------------------------------------------------------------

    /// <summary><c>DaysUntil(Parameter.Field) &lt;= days</c>.</summary>
    protected static string DueWithin(string parameter, string field, int days)
    {
        return RuleDefinitionSerializer.SerializeExpression(new RuleComparisonExpression
        {
            Operator = RuleComparisonOperator.LessThanOrEqual,
            Left = new RuleFunctionExpression
            {
                Name = "DaysUntil",
                Args = { new RuleFieldExpression { Parameter = parameter, Field = field } }
            },
            Right = Constant(RuleDataType.Integer, days.ToString())
        });
    }

    /// <summary><c>Parameter.Field = value</c>.</summary>
    protected static string FieldEquals(string parameter, string field, RuleDataType dataType, string value)
    {
        return RuleDefinitionSerializer.SerializeExpression(new RuleComparisonExpression
        {
            Operator = RuleComparisonOperator.Equal,
            Left = new RuleFieldExpression { Parameter = parameter, Field = field },
            Right = Constant(dataType, value)
        });
    }

    protected static RuleConstantExpression Constant(RuleDataType dataType, string value)
    {
        return new RuleConstantExpression { DataType = dataType, Value = value };
    }
}
