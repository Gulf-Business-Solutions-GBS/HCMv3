using System;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Domain.Entities;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>The loan's Edit dialog shows its employee and its balance (read-only, as in legacy).</summary>
// ReSharper disable once InconsistentNaming
public class EmployeeLoansAppService_GetEmployeeLoanForEdit_Tests : AppTestBase
{
    private readonly IEmployeeLoansAppService _appService;

    public EmployeeLoansAppService_GetEmployeeLoanForEdit_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeLoansAppService>();
    }

    private int CreateLoan()
    {
        var tenantId = AbpSession.TenantId ?? 1;
        var id = 0;
        UsingDbContext(context =>
        {
            var nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = tenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
            var loanType = context.Set<LoanType>().Add(new LoanType
            {
                TenantId = tenantId,
                Code = "LTE01",
                NameEn = "Car Loan",
                NameAr = "Car Loan"
            }).Entity;
            context.SaveChanges();

            var employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = tenantId,
                Code = "LNE01",
                FirstNameEn = "F",
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "Huda Nasser",
                FullNameAr = "هدى ناصر",
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationality.Id
            }).Entity;
            context.SaveChanges();

            var loan = context.Set<EmployeeLoan>().Add(new EmployeeLoan
            {
                TenantId = tenantId,
                EmployeeId = employee.Id,
                LoanTypeId = loanType.Id,
                StartDate = new DateTime(2024, 1, 1),
                RecoveryStartDate = new DateTime(2024, 2, 1),
                EndDate = new DateTime(2024, 12, 31),
                ActualAmount = 1000,
                DueAmount = 1000,
                BalanceDueAmount = 640
            }).Entity;
            context.SaveChanges();
            id = loan.Id;
        });
        return id;
    }

    [Fact]
    public async Task Should_Return_The_Employee_And_Balance_For_Display()
    {
        var loanId = CreateLoan();

        var output = await _appService.GetEmployeeLoanForEdit(new EntityDto(loanId));

        output.EmployeeCode.ShouldBe("LNE01");
        output.EmployeeFullNameEn.ShouldBe("Huda Nasser");
        output.BalanceDueAmount.ShouldBe(640);
        output.EmployeeLoan.Id.ShouldBe(loanId);
        output.EmployeeLoan.DueAmount.ShouldBe(1000);
    }

    [Fact]
    public async Task Should_Throw_For_A_Missing_Loan()
    {
        await Should.ThrowAsync<EntityNotFoundException>(() => _appService.GetEmployeeLoanForEdit(new EntityDto(int.MaxValue)));
    }
}
