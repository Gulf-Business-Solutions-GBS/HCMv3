using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Test.Base;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.BusinessRules;

/// <summary>
/// Covers the hook's SAVE path -- the rules firing from an entity event during SaveChanges.
///
/// The engine's other tests build a <c>RuleExecutionContext</c> by hand and evaluate it, which leaves the
/// path that actually runs in production untested. That gap is why a hook that threw
/// <see cref="ObjectDisposedException"/> on absence saves reached main: nothing saved an absence through
/// the DbContext until an unrelated notification test did, and then six of those failed with a stack
/// trace pointing into the rules engine.
///
/// Both shapes are covered on purpose. Inside a unit of work is what an application service does. Through
/// a standalone context is what a seeder, a background job, or the test harness does -- and a leave record
/// has to save either way, because the rules engine is a passenger on this event rather than its owner.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeAbsenceBusinessRuleHook_Tests : AppTestBase
{
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IRepository<EmployeeAbsence> _absenceRepository;
    private readonly int _employeeId;
    private readonly int _absenceTypeId;

    public EmployeeAbsenceBusinessRuleHook_Tests()
    {
        _unitOfWorkManager = Resolve<IUnitOfWorkManager>();
        _absenceRepository = Resolve<IRepository<EmployeeAbsence>>();

        // Real rows, so a foreign key cannot fail the save and hide what the hook did.
        _employeeId = CreateEmployee();
        _absenceTypeId = CreateAbsenceType();
    }

    [Fact]
    public async Task Should_Save_An_Absence_Inside_A_Unit_Of_Work()
    {
        var exception = await Record.ExceptionAsync(async () =>
        {
            using (var uow = _unitOfWorkManager.Begin())
            {
                await _absenceRepository.InsertAsync(NewAbsence());
                await _unitOfWorkManager.Current.SaveChangesAsync();
                await uow.CompleteAsync();
            }
        });

        exception.ShouldBeNull();
    }

    [Fact]
    public void Should_Save_An_Absence_Through_A_Standalone_Context()
    {
        var exception = Record.Exception(() =>
            UsingDbContext(context => context.Set<EmployeeAbsence>().Add(NewAbsence())));

        exception.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Persist_The_Absence_It_Saved()
    {
        using (var uow = _unitOfWorkManager.Begin())
        {
            await _absenceRepository.InsertAsync(NewAbsence());
            await _unitOfWorkManager.Current.SaveChangesAsync();
            await uow.CompleteAsync();
        }

        var saved = UsingDbContext(context =>
            context.Set<EmployeeAbsence>().FirstOrDefault(a => a.EmployeeId == _employeeId));

        saved.ShouldNotBeNull();
    }

    private EmployeeAbsence NewAbsence()
    {
        return new EmployeeAbsence
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeId = _employeeId,
            AbsenceTypeId = _absenceTypeId,
            StartDate = new DateTime(2026, 1, 5),
            EndDate = new DateTime(2026, 1, 9)
        };
    }

    private int CreateEmployee()
    {
        var code = Guid.NewGuid().ToString("N").Substring(0, 8);

        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Set<Nationality>().Add(new Nationality
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = "N",
                NameAr = "N"
            }).Entity;
        });

        Employee employee = null;
        UsingDbContext(context =>
        {
            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = AbpSession.TenantId ?? 1,
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
                NationalityId = nationality.Id
            }).Entity;
        });

        return employee.Id;
    }

    private int CreateAbsenceType()
    {
        AbsenceType absenceType = null;
        UsingDbContext(context =>
        {
            absenceType = context.Set<AbsenceType>().Add(new AbsenceType
            {
                TenantId = AbpSession.TenantId ?? 1,
                NameEn = "AT " + Guid.NewGuid(),
                NameAr = "AT",
                IsActive = true,
                IsPaid = true,
                IsReturnDateRequired = false
            }).Entity;
        });

        return absenceType.Id;
    }
}
