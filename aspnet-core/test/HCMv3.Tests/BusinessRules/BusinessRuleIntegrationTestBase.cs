using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.BusinessRules;
using HCMv3.BusinessRules.Definition;
using HCMv3.BusinessRules.Dto;
using HCMv3.BusinessRules.Seed;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.BusinessRules;
using HCMv3.Test.Base;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Tests.BusinessRules;

/// <summary>
/// Shared fixture for tests that arm a rule through the real app service and fire it with a real save.
///
/// <para>
/// Everything that saves a leave goes through <see cref="BeginProductionLikeUnitOfWork"/>. The test harness
/// runs non-transactional (HCMv3TestBaseModule sets IsTransactional = false) while production is transactional,
/// and ABP raises the "-ing" entity events AFTER that SaveChanges has executed its SQL -- so a rule refusing a
/// save undoes it only by rolling the transaction back. A non-transactional test reports the write surviving
/// the refusal: true of the harness, false of the product.
/// </para>
/// </summary>
public abstract class BusinessRuleIntegrationTestBase : AppTestBase
{
    protected BusinessRuleIntegrationTestBase()
    {
        RulesAppService = Resolve<IBusinessRulesAppService>();
        UnitOfWorkManager = Resolve<IUnitOfWorkManager>();
        AbsenceRepository = Resolve<IRepository<EmployeeAbsence>>();

        EmployeeId = CreateEmployee();
        AbsenceTypeId = CreateAbsenceType();
    }

    protected IBusinessRulesAppService RulesAppService { get; }

    protected IUnitOfWorkManager UnitOfWorkManager { get; }

    protected IRepository<EmployeeAbsence> AbsenceRepository { get; }

    protected int EmployeeId { get; }

    protected int AbsenceTypeId { get; }

    // ---------------------------------------------------------------- arming

    protected async Task<int> ArmAsync(string code, RulePurpose purpose, RuleDefinition definition, RuleEventType eventType)
    {
        var ruleId = await CreateRuleAsync(code, purpose, definition);

        await RulesAppService.CreateOrEditAssignment(new CreateOrEditBusinessRuleAssignmentDto
        {
            BusinessRuleId = ruleId,
            EventType = eventType,
            IsActive = true
        });

        return ruleId;
    }

    protected async Task<int> CreateRuleAsync(string code, RulePurpose purpose, RuleDefinition definition)
    {
        await RulesAppService.CreateOrEditBusinessRule(RuleInput(code, purpose, definition));

        return UsingDbContext(context => context.Set<BusinessRule>().Single(r => r.Code == code).Id);
    }

    protected static CreateOrEditBusinessRuleDto RuleInput(string code, RulePurpose purpose, RuleDefinition definition, int? id = null)
    {
        return new CreateOrEditBusinessRuleDto
        {
            Id = id,
            Code = code,
            NameEn = code,
            NameAr = code,
            BaseObject = EmployeeAbsenceRuleBaseObjectProvider.BaseObjectCode,
            Purpose = purpose,
            DefinitionJson = RuleDefinitionSerializer.Serialize(definition),
            EffectiveFrom = DateTime.Today.AddYears(-1),
            IsActive = true
        };
    }

    // ---------------------------------------------------------------- rule builders

    protected static RuleDefinition Rule(params RuleAction[] actions)
    {
        return new RuleDefinition
        {
            Blocks = new List<RuleBlock> { new() { Then = new List<RuleAction>(actions) } }
        };
    }

    protected static RuleDefinition AlwaysRaise() => Rule(RaiseError());

    protected static RuleDefinition RaiseWhenLeaveDaysOver(decimal days)
    {
        return new RuleDefinition
        {
            Blocks = new List<RuleBlock>
            {
                new()
                {
                    If = new RuleComparisonExpression
                    {
                        Operator = RuleComparisonOperator.GreaterThan,
                        Left = LeaveField("TotalLeaveDays"),
                        Right = Const(days, RuleDataType.Decimal)
                    },
                    Then = new List<RuleAction> { RaiseError() }
                }
            }
        };
    }

    protected static RuleDefinition SetLeaveDays(decimal days)
    {
        return Rule(new RuleSetAction
        {
            Parameter = EmployeeAbsenceRuleBaseObjectProvider.LeaveParameter,
            Field = "TotalLeaveDays",
            Value = Const(days, RuleDataType.Decimal)
        });
    }

    protected static RuleErrorAction RaiseError()
    {
        return new RuleErrorAction { MessageKey = SampleBusinessRuleDefinitions.MaternityLeaveGenderErrorKey };
    }

    protected static RuleFieldExpression LeaveField(string field)
    {
        return new RuleFieldExpression { Parameter = EmployeeAbsenceRuleBaseObjectProvider.LeaveParameter, Field = field };
    }

    protected static RuleConstantExpression Const(object value, RuleDataType dataType)
    {
        return new RuleConstantExpression
        {
            DataType = dataType,
            Value = Convert.ToString(value, CultureInfo.InvariantCulture)
        };
    }

    protected static RuleRecordFieldValue Value(string field, RuleExpression value)
    {
        return new RuleRecordFieldValue { Field = field, Value = value };
    }

    // ---------------------------------------------------------------- saving

    protected IUnitOfWorkCompleteHandle BeginProductionLikeUnitOfWork()
    {
        return UnitOfWorkManager.Begin(new UnitOfWorkOptions { IsTransactional = true });
    }

    protected async Task<int> InsertAbsenceAsync(double totalLeaveDays)
    {
        using var uow = BeginProductionLikeUnitOfWork();
        var absence = NewAbsence(totalLeaveDays);
        await AbsenceRepository.InsertAsync(absence);
        await UnitOfWorkManager.Current.SaveChangesAsync();
        await uow.CompleteAsync();
        return absence.Id;
    }

    protected async Task DeleteAbsenceAsync(int absenceId)
    {
        using var uow = BeginProductionLikeUnitOfWork();
        await AbsenceRepository.DeleteAsync(absenceId);
        await UnitOfWorkManager.Current.SaveChangesAsync();
        await uow.CompleteAsync();
    }

    // ---------------------------------------------------------------- reading back

    /// <summary>Through a standalone context, so no rule runs while the fixture is being built.</summary>
    protected int CreateAbsence(double totalLeaveDays)
    {
        return UsingDbContext(context => context.Set<EmployeeAbsence>().Add(NewAbsence(totalLeaveDays)).Entity).Id;
    }

    protected bool IsDeleted(int absenceId)
    {
        return UsingDbContext(context => context.Set<EmployeeAbsence>().IgnoreQueryFilters().Single(a => a.Id == absenceId).IsDeleted);
    }

    protected double StoredLeaveDays(int absenceId)
    {
        return UsingDbContext(context => context.Set<EmployeeAbsence>().IgnoreQueryFilters().Single(a => a.Id == absenceId).TotalLeaveDays);
    }

    protected int StoredAbsenceCount()
    {
        return UsingDbContext(context => context.Set<EmployeeAbsence>().IgnoreQueryFilters().Count(a => a.EmployeeId == EmployeeId));
    }

    // ---------------------------------------------------------------- fixture data

    protected EmployeeAbsence NewAbsence(double totalLeaveDays)
    {
        return new EmployeeAbsence
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeId = EmployeeId,
            AbsenceTypeId = AbsenceTypeId,
            StartDate = new DateTime(2026, 1, 5),
            EndDate = new DateTime(2026, 1, 9),
            TotalLeaveDays = totalLeaveDays
        };
    }

    protected int CreateEmployee(int? tenantId = null)
    {
        var tenant = tenantId ?? AbpSession.TenantId ?? 1;
        var code = Guid.NewGuid().ToString("N").Substring(0, 8);

        var nationalityId = UsingDbContext(context => context.Set<Nationality>().Add(new Nationality
        {
            TenantId = tenant,
            Code = code,
            NameEn = "N",
            NameAr = "N"
        }).Entity).Id;

        return UsingDbContext(context => context.Set<Employee>().Add(new Employee
        {
            TenantId = tenant,
            Code = code,
            FirstNameEn = "First",
            LastNameEn = "Last",
            FirstNameAr = "ف",
            LastNameAr = "ل",
            FullNameEn = "First Last",
            FullNameAr = "ف ل",
            BirthDate = new DateTime(1990, 1, 1),
            JoiningDate = new DateTime(2020, 1, 1),
            Email = code + "@gbskw.com",
            Status = EmployeeStatus.Active,
            NationalityId = nationalityId
        }).Entity).Id;
    }

    private int CreateAbsenceType()
    {
        return UsingDbContext(context => context.Set<AbsenceType>().Add(new AbsenceType
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = "AT " + Guid.NewGuid(),
            NameAr = "AT",
            IsActive = true,
            IsPaid = true,
            IsReturnDateRequired = false
        }).Entity).Id;
    }
}
