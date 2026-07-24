using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.GovtRelationsManagement;
using HCMv3.GovtRelationsManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class SponsorBanksAppService_Tests : AppTestBase
{
    private readonly ISponsorBanksAppService _appService;

    public SponsorBanksAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<ISponsorBanksAppService>();
    }

    private Sponsor CreateTestSponsor(string code)
    {
        Nationality nationality = null;
        BusinessType businessType = null;
        Governorate governorate = null;
        Abp.Organizations.OrganizationUnit orgUnit = null;
        UsingDbContext(context =>
        {
            nationality = context.Set<Nationality>().Add(new Nationality
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;

            businessType = context.Set<BusinessType>().Add(new BusinessType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;

            governorate = context.Set<Governorate>().Add(new Governorate
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;

            orgUnit = context.Set<Abp.Organizations.OrganizationUnit>().Add(
                new Abp.Organizations.OrganizationUnit(AbpSession.TenantId, code, null) { Code = code }).Entity;
        });

        Sponsor sponsor = null;
        UsingDbContext(context =>
        {
            sponsor = context.Set<Sponsor>().Add(new Sponsor
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code,
                NationalityId = nationality.Id,
                BusinessTypeId = businessType.Id,
                GovernorateId = governorate.Id,
                OrganizationUnitId = orgUnit.Id
            }).Entity;
        });
        return sponsor;
    }

    private Bank CreateTestBank(string code)
    {
        Bank bank = null;
        UsingDbContext(context =>
        {
            bank = context.Set<Bank>().Add(new Bank
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });
        return bank;
    }

    [Fact]
    public async Task Should_Create_SponsorBank()
    {
        //Arrange
        var sponsor = CreateTestSponsor("SB100");
        var paymentBank = CreateTestBank("PB100");
        var employeeBank = CreateTestBank("EB100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditSponsorBankDto
        {
            SponsorId = sponsor.Id,
            PaymentBankId = paymentBank.Id,
            EmployeeBankId = employeeBank.Id,
            AccountNumber = "ACC-001",
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 12, 31),
            IBAN = "KW00TEST0000000000000001"
        });

        //Assert
        var result = await _appService.GetAll(new GetAllSponsorBanksInput { SponsorId = sponsor.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].AccountNumber.ShouldBe("ACC-001");
    }

    [Fact]
    public async Task Should_Throw_When_EndDate_Earlier_Than_StartDate()
    {
        //Arrange
        var sponsor = CreateTestSponsor("SB200");
        var paymentBank = CreateTestBank("PB200");

        //Act & Assert
        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditSponsorBankDto
            {
                SponsorId = sponsor.Id,
                PaymentBankId = paymentBank.Id,
                StartDate = new DateTime(2024, 12, 31),
                EndDate = new DateTime(2024, 1, 1)
            }));
    }
}
