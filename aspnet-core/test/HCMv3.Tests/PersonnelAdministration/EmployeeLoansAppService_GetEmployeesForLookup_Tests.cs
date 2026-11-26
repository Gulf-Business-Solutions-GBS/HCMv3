using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>The employee dropdown on the Employee Loans list's create dialog.</summary>
// ReSharper disable once InconsistentNaming
public class EmployeeLoansAppService_GetEmployeesForLookup_Tests : AppTestBase
{
    private readonly IEmployeeLoansAppService _appService;

    public EmployeeLoansAppService_GetEmployeesForLookup_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeLoansAppService>();
    }

    private void CreateEmployees(params (string Code, string NameEn, string NameAr)[] employees)
    {
        var tenantId = AbpSession.TenantId ?? 1;
        UsingDbContext(context =>
        {
            var nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = tenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
            context.SaveChanges();

            foreach (var (code, nameEn, nameAr) in employees)
            {
                context.Set<Employee>().Add(new Employee
                {
                    TenantId = tenantId,
                    Code = code,
                    FirstNameEn = "F",
                    LastNameEn = "L",
                    FirstNameAr = "ف",
                    LastNameAr = "ل",
                    FullNameEn = nameEn,
                    FullNameAr = nameAr,
                    BirthDate = new DateTime(1990, 1, 1),
                    JoiningDate = new DateTime(2020, 1, 1),
                    ProbationEndDate = new DateTime(2020, 4, 1),
                    NationalityId = nationality.Id
                });
            }

            context.SaveChanges();
        });
    }

    [Theory]
    [InlineData("LKP77")]
    [InlineData("Zainab")]
    [InlineData("زينب")]
    public async Task Should_Find_Employees_By_Code_Or_Name(string filter)
    {
        CreateEmployees(("LKP77", "Zainab Hadi", "زينب هادي"), ("LKP78", "Omar Fahd", "عمر فهد"));

        var result = await _appService.GetEmployeesForLookup(new GetEmployeeLoanEmployeesInput { Filter = filter });

        result.Items.Count.ShouldBe(1);
        result.Items[0].Code.ShouldBe("LKP77");
        result.Items[0].NameEn.ShouldBe("Zainab Hadi");
        result.Items[0].NameAr.ShouldBe("زينب هادي");
    }

    [Fact]
    public async Task Should_Return_All_Employees_Without_A_Cap()
    {
        var many = Enumerable.Range(1, 120)
            .Select(i => ($"LKM{i:000}", $"Many {i}", $"Many {i}"))
            .ToArray();
        CreateEmployees(many);

        var result = await _appService.GetEmployeesForLookup(new GetEmployeeLoanEmployeesInput { Filter = "LKM" });

        result.Items.Count.ShouldBe(120);
    }

    [Fact]
    public async Task Should_Order_Numeric_Codes_In_Number_Order()
    {
        CreateEmployees(("ZQ9102", "Ten Two", "Ten Two"), ("ZQ91", "Nine One", "Nine One"), ("ZQ910", "Nine Ten", "Nine Ten"));

        var result = await _appService.GetEmployeesForLookup(new GetEmployeeLoanEmployeesInput { Filter = "ZQ91" });

        result.Items.Select(i => i.Code).ShouldBe(new[] { "ZQ91", "ZQ910", "ZQ9102" });
    }
}
