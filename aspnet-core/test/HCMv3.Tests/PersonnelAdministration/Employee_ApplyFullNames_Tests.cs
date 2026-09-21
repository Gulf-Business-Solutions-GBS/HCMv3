using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class Employee_ApplyFullNames_Tests : AppTestBase
{
    [Fact]
    public void Should_Join_All_Four_Name_Parts_In_Both_Languages()
    {
        var employee = new Employee
        {
            FirstNameEn = "John", SecondNameEn = "Paul", ThirdNameEn = "George", LastNameEn = "Doe",
            FirstNameAr = "جون", SecondNameAr = "بول", ThirdNameAr = "جورج", LastNameAr = "دو"
        };

        employee.ApplyFullNames();

        employee.FullNameEn.ShouldBe("John Paul George Doe");
        employee.FullNameAr.ShouldBe("جون بول جورج دو");
    }

    [Fact]
    public void Should_Skip_Blank_Name_Parts()
    {
        var employee = new Employee
        {
            FirstNameEn = "John", SecondNameEn = " ", ThirdNameEn = null, LastNameEn = "Doe",
            FirstNameAr = "جون", SecondNameAr = "", LastNameAr = "دو"
        };

        employee.ApplyFullNames();

        employee.FullNameEn.ShouldBe("John Doe");
        employee.FullNameAr.ShouldBe("جون دو");
    }
}
