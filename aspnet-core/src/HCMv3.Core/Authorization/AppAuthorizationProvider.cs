using Abp.Authorization;
using Abp.Configuration.Startup;
using Abp.Localization;
using Abp.MultiTenancy;

namespace HCMv3.Authorization;

/// <summary>
/// Application's authorization provider.
/// Defines permissions for the application.
/// See <see cref="AppPermissions"/> for all permission names.
/// </summary>
public class AppAuthorizationProvider : AuthorizationProvider
{
    private readonly bool _isMultiTenancyEnabled;

    public AppAuthorizationProvider(bool isMultiTenancyEnabled)
    {
        _isMultiTenancyEnabled = isMultiTenancyEnabled;
    }

    public AppAuthorizationProvider(IMultiTenancyConfig multiTenancyConfig)
    {
        _isMultiTenancyEnabled = multiTenancyConfig.IsEnabled;
    }

    public override void SetPermissions(IPermissionDefinitionContext context)
    {
        //COMMON PERMISSIONS (FOR BOTH OF TENANTS AND HOST)

        var pages = context.GetPermissionOrNull(AppPermissions.Pages) ?? context.CreatePermission(AppPermissions.Pages, L("Pages"));
        pages.CreateChildPermission(AppPermissions.Pages_DemoUiComponents, L("DemoUiComponents"));
        pages.CreateChildPermission(AppPermissions.Pages_GoLiveSetup, L("GoLiveSetup"));

        //EMPLOYEE SELF SERVICE
        //
        //A separate root from "Pages" on purpose -- see the note on AppPermissions.Ess. Tenant-side only:
        //ESS is an employee acting on their own records, and the host has no employees.
        var ess = context.CreatePermission(AppPermissions.Ess, L("EmployeeSelfService"), multiTenancySides: MultiTenancySides.Tenant);
        ess.CreateChildPermission(AppPermissions.Ess_Inbox, L("EssInbox"), multiTenancySides: MultiTenancySides.Tenant);
        ess.CreateChildPermission(AppPermissions.Ess_MyRequests, L("EssMyRequests"), multiTenancySides: MultiTenancySides.Tenant);
        ess.CreateChildPermission(AppPermissions.Ess_MyProfile, L("EssMyProfile"), multiTenancySides: MultiTenancySides.Tenant);
        ess.CreateChildPermission(AppPermissions.Ess_MyTeam, L("EssMyTeam"), multiTenancySides: MultiTenancySides.Tenant);
        ess.CreateChildPermission(AppPermissions.Ess_MyLeave, L("EssMyLeave"), multiTenancySides: MultiTenancySides.Tenant);
        ess.CreateChildPermission(AppPermissions.Ess_MyAssets, L("EssMyAssets"), multiTenancySides: MultiTenancySides.Tenant);
        ess.CreateChildPermission(AppPermissions.Ess_MyDocuments, L("EssMyDocuments"), multiTenancySides: MultiTenancySides.Tenant);
        ess.CreateChildPermission(AppPermissions.Ess_MyLoans, L("EssMyLoans"), multiTenancySides: MultiTenancySides.Tenant);
        ess.CreateChildPermission(AppPermissions.Ess_MyLoanExemptions, L("EssMyLoanExemptions"), multiTenancySides: MultiTenancySides.Tenant);
        ess.CreateChildPermission(AppPermissions.Ess_MyOvertime, L("EssMyOvertime"), multiTenancySides: MultiTenancySides.Tenant);
        ess.CreateChildPermission(AppPermissions.Ess_MyTickets, L("EssMyTickets"), multiTenancySides: MultiTenancySides.Tenant);
        ess.CreateChildPermission(AppPermissions.Ess_MyAttendance, L("EssMyAttendance"), multiTenancySides: MultiTenancySides.Tenant);
        ess.CreateChildPermission(AppPermissions.Ess_MyBank, L("EssMyBank"), multiTenancySides: MultiTenancySides.Tenant);
        ess.CreateChildPermission(AppPermissions.Ess_MyTravel, L("EssMyTravel"), multiTenancySides: MultiTenancySides.Tenant);
        ess.CreateChildPermission(AppPermissions.Ess_MyTravelClaims, L("EssMyTravelClaims"), multiTenancySides: MultiTenancySides.Tenant);
        ess.CreateChildPermission(AppPermissions.Ess_MyPassportWithdrawals, L("EssMyPassportWithdrawals"), multiTenancySides: MultiTenancySides.Tenant);
        ess.CreateChildPermission(AppPermissions.Ess_MyExitPermits, L("EssMyExitPermits"), multiTenancySides: MultiTenancySides.Tenant);
        ess.CreateChildPermission(AppPermissions.Ess_MyHRServices, L("EssMyHRServices"), multiTenancySides: MultiTenancySides.Tenant);
        ess.CreateChildPermission(AppPermissions.Ess_MyLetterRequests, L("EssMyLetterRequests"), multiTenancySides: MultiTenancySides.Tenant);
        ess.CreateChildPermission(AppPermissions.Ess_MyPunches, L("EssMyPunches"), multiTenancySides: MultiTenancySides.Tenant);
        ess.CreateChildPermission(AppPermissions.Ess_MyClearances, L("EssMyClearances"), multiTenancySides: MultiTenancySides.Tenant);
        ess.CreateChildPermission(AppPermissions.Ess_MyHealthReimburses, L("EssMyHealthReimburses"), multiTenancySides: MultiTenancySides.Tenant);
        ess.CreateChildPermission(AppPermissions.Ess_MyDelegations, L("EssMyDelegations"), multiTenancySides: MultiTenancySides.Tenant);

        //GENERAL MASTER INFO
        var generalMasterInfo = pages.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo, L("GeneralMasterInfo"));

        var accessUnits = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_AccessUnits, L("AccessUnits"));
        accessUnits.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_AccessUnits_Create, L("CreatingNewAccessUnit"));
        accessUnits.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_AccessUnits_Edit, L("EditingAccessUnit"));
        accessUnits.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_AccessUnits_Delete, L("DeletingAccessUnit"));

        var countries = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Countries, L("Countries"));
        countries.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Countries_Create, L("CreateNewItem"));
        countries.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Countries_Edit, L("EditItem"));
        countries.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Countries_Delete, L("DeleteItem"));

        var bankFiles = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_BankFiles, L("BankFiles"));
        bankFiles.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_BankFiles_Create, L("CreateNewItem"));
        bankFiles.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_BankFiles_Edit, L("EditItem"));
        bankFiles.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_BankFiles_Delete, L("DeleteItem"));

        var benefitTypes = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_BenefitTypes, L("BenefitTypes"));
        benefitTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_BenefitTypes_Create, L("CreateNewItem"));
        benefitTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_BenefitTypes_Edit, L("EditItem"));
        benefitTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_BenefitTypes_Delete, L("DeleteItem"));

        var contractTypes = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_ContractTypes, L("ContractTypes"));
        contractTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_ContractTypes_Create, L("CreateNewItem"));
        contractTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_ContractTypes_Edit, L("EditItem"));
        contractTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_ContractTypes_Delete, L("DeleteItem"));

        var dateTypes = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_DateTypes, L("DateTypes"));
        dateTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_DateTypes_Create, L("CreateNewItem"));
        dateTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_DateTypes_Edit, L("EditItem"));
        dateTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_DateTypes_Delete, L("DeleteItem"));

        var documentPlaces = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_DocumentPlaces, L("DocumentPlaces"));
        documentPlaces.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_DocumentPlaces_Create, L("CreateNewItem"));
        documentPlaces.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_DocumentPlaces_Edit, L("EditItem"));
        documentPlaces.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_DocumentPlaces_Delete, L("DeleteItem"));

        var educations = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Educations, L("Educations"));
        educations.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Educations_Create, L("CreateNewItem"));
        educations.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Educations_Edit, L("EditItem"));
        educations.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Educations_Delete, L("DeleteItem"));

        var educationBranches = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_EducationBranches, L("EducationBranches"));
        educationBranches.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_EducationBranches_Create, L("CreateNewItem"));
        educationBranches.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_EducationBranches_Edit, L("EditItem"));
        educationBranches.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_EducationBranches_Delete, L("DeleteItem"));

        var governorates = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Governorates, L("Governorates"));
        governorates.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Governorates_Create, L("CreateNewItem"));
        governorates.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Governorates_Edit, L("EditItem"));
        governorates.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Governorates_Delete, L("DeleteItem"));

        var grievanceTypes = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_GrievanceTypes, L("GrievanceTypes"));
        grievanceTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_GrievanceTypes_Create, L("CreateNewItem"));
        grievanceTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_GrievanceTypes_Edit, L("EditItem"));
        grievanceTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_GrievanceTypes_Delete, L("DeleteItem"));

        var hRServiceTypes = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_HRServiceTypes, L("HRServiceTypes"));
        hRServiceTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_HRServiceTypes_Create, L("CreateNewItem"));
        hRServiceTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_HRServiceTypes_Edit, L("EditItem"));
        hRServiceTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_HRServiceTypes_Delete, L("DeleteItem"));

        var iTServiceTypes = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_ITServiceTypes, L("ITServiceTypes"));
        iTServiceTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_ITServiceTypes_Create, L("CreateNewItem"));
        iTServiceTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_ITServiceTypes_Edit, L("EditItem"));
        iTServiceTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_ITServiceTypes_Delete, L("DeleteItem"));

        var languageInfos = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_LanguageInfos, L("LanguageInfos"));
        languageInfos.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_LanguageInfos_Create, L("CreateNewItem"));
        languageInfos.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_LanguageInfos_Edit, L("EditItem"));
        languageInfos.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_LanguageInfos_Delete, L("DeleteItem"));

        var maritalStatuses = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_MaritalStatuses, L("MaritalStatuses"));
        maritalStatuses.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_MaritalStatuses_Create, L("CreateNewItem"));
        maritalStatuses.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_MaritalStatuses_Edit, L("EditItem"));
        maritalStatuses.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_MaritalStatuses_Delete, L("DeleteItem"));

        var nationalityGroups = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_NationalityGroups, L("NationalityGroups"));
        nationalityGroups.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_NationalityGroups_Create, L("CreateNewItem"));
        nationalityGroups.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_NationalityGroups_Edit, L("EditItem"));
        nationalityGroups.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_NationalityGroups_Delete, L("DeleteItem"));

        var newsTypes = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_NewsTypes, L("NewsTypes"));
        newsTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_NewsTypes_Create, L("CreateNewItem"));
        newsTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_NewsTypes_Edit, L("EditItem"));
        newsTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_NewsTypes_Delete, L("DeleteItem"));

        var passportTypes = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_PassportTypes, L("PassportTypes"));
        passportTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_PassportTypes_Create, L("CreateNewItem"));
        passportTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_PassportTypes_Edit, L("EditItem"));
        passportTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_PassportTypes_Delete, L("DeleteItem"));

        var postingGroups = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_PostingGroups, L("PostingGroups"));
        postingGroups.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_PostingGroups_Create, L("CreateNewItem"));
        postingGroups.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_PostingGroups_Edit, L("EditItem"));
        postingGroups.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_PostingGroups_Delete, L("DeleteItem"));

        var qualifications = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Qualifications, L("Qualifications"));
        qualifications.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Qualifications_Create, L("CreateNewItem"));
        qualifications.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Qualifications_Edit, L("EditItem"));
        qualifications.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Qualifications_Delete, L("DeleteItem"));

        var recruitmentTypes = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_RecruitmentTypes, L("RecruitmentTypes"));
        recruitmentTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_RecruitmentTypes_Create, L("CreateNewItem"));
        recruitmentTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_RecruitmentTypes_Edit, L("EditItem"));
        recruitmentTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_RecruitmentTypes_Delete, L("DeleteItem"));

        var relationshipTypes = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_RelationshipTypes, L("RelationshipTypes"));
        relationshipTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_RelationshipTypes_Create, L("CreateNewItem"));
        relationshipTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_RelationshipTypes_Edit, L("EditItem"));
        relationshipTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_RelationshipTypes_Delete, L("DeleteItem"));

        var religions = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Religions, L("Religions"));
        religions.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Religions_Create, L("CreateNewItem"));
        religions.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Religions_Edit, L("EditItem"));
        religions.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Religions_Delete, L("DeleteItem"));

        var states = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_States, L("States"));
        states.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_States_Create, L("CreateNewItem"));
        states.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_States_Edit, L("EditItem"));
        states.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_States_Delete, L("DeleteItem"));

        var taskTypes = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_TaskTypes, L("TaskTypes"));
        taskTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_TaskTypes_Create, L("CreateNewItem"));
        taskTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_TaskTypes_Edit, L("EditItem"));
        taskTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_TaskTypes_Delete, L("DeleteItem"));

        var titles = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Titles, L("Titles"));
        titles.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Titles_Create, L("CreateNewItem"));
        titles.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Titles_Edit, L("EditItem"));
        titles.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Titles_Delete, L("DeleteItem"));

        var trainingTypes = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_TrainingTypes, L("TrainingTypes"));
        trainingTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_TrainingTypes_Create, L("CreateNewItem"));
        trainingTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_TrainingTypes_Edit, L("EditItem"));
        trainingTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_TrainingTypes_Delete, L("DeleteItem"));

        var universities = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Universities, L("Universities"));
        universities.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Universities_Create, L("CreateNewItem"));
        universities.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Universities_Edit, L("EditItem"));
        universities.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Universities_Delete, L("DeleteItem"));

        var visaTypes = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_VisaTypes, L("VisaTypes"));
        visaTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_VisaTypes_Create, L("CreateNewItem"));
        visaTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_VisaTypes_Edit, L("EditItem"));
        visaTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_VisaTypes_Delete, L("DeleteItem"));

        var workVisaTypes = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_WorkVisaTypes, L("WorkVisaTypes"));
        workVisaTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_WorkVisaTypes_Create, L("CreateNewItem"));
        workVisaTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_WorkVisaTypes_Edit, L("EditItem"));
        workVisaTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_WorkVisaTypes_Delete, L("DeleteItem"));

        var assetTypes = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_AssetTypes, L("AssetTypes"));
        assetTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_AssetTypes_Create, L("CreateNewItem"));
        assetTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_AssetTypes_Edit, L("EditItem"));
        assetTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_AssetTypes_Delete, L("DeleteItem"));

        var cities = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Cities, L("Cities"));
        cities.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Cities_Create, L("CreateNewItem"));
        cities.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Cities_Edit, L("EditItem"));
        cities.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Cities_Delete, L("DeleteItem"));

        var institutions = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Institutions, L("Institutions"));
        institutions.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Institutions_Create, L("CreateNewItem"));
        institutions.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Institutions_Edit, L("EditItem"));
        institutions.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Institutions_Delete, L("DeleteItem"));

        var nationalities = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Nationalities, L("Nationalities"));
        nationalities.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Nationalities_Create, L("CreateNewItem"));
        nationalities.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Nationalities_Edit, L("EditItem"));
        nationalities.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Nationalities_Delete, L("DeleteItem"));

        var dependantTypes = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_DependantTypes, L("DependantTypes"));
        dependantTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_DependantTypes_Create, L("CreateNewItem"));
        dependantTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_DependantTypes_Edit, L("EditItem"));
        dependantTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_DependantTypes_Delete, L("DeleteItem"));

        var currencies = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Currencies, L("Currencies"));
        currencies.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Currencies_Create, L("CreateNewItem"));
        currencies.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Currencies_Edit, L("EditItem"));
        currencies.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Currencies_Delete, L("DeleteItem"));

        var separationReasons = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_SeparationReasons, L("SeparationReasons"));
        separationReasons.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_SeparationReasons_Create, L("CreateNewItem"));
        separationReasons.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_SeparationReasons_Edit, L("EditItem"));
        separationReasons.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_SeparationReasons_Delete, L("DeleteItem"));

        var disciplinaryDeductionTypes = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_DisciplinaryDeductionTypes, L("DisciplinaryDeductionTypes"));
        disciplinaryDeductionTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_DisciplinaryDeductionTypes_Create, L("CreateNewItem"));
        disciplinaryDeductionTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_DisciplinaryDeductionTypes_Edit, L("EditItem"));
        disciplinaryDeductionTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_DisciplinaryDeductionTypes_Delete, L("DeleteItem"));

        var personalActions = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_PersonalActions, L("PersonalActions"));
        personalActions.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_PersonalActions_Create, L("CreateNewItem"));
        personalActions.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_PersonalActions_Edit, L("EditItem"));
        personalActions.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_PersonalActions_Delete, L("DeleteItem"));

        var personalActionReasons = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_PersonalActionReasons, L("PersonalActionReasons"));
        personalActionReasons.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_PersonalActionReasons_Create, L("CreateNewItem"));
        personalActionReasons.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_PersonalActionReasons_Edit, L("EditItem"));
        personalActionReasons.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_PersonalActionReasons_Delete, L("DeleteItem"));

        var banks = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Banks, L("Banks"));
        banks.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Banks_Create, L("CreateNewItem"));
        banks.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Banks_Edit, L("EditItem"));
        banks.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Banks_Delete, L("DeleteItem"));

        var payeeBankDetails = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_PayeeBankDetails, L("PayeeBankDetails"));
        payeeBankDetails.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_PayeeBankDetails_Create, L("CreateNewItem"));
        payeeBankDetails.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_PayeeBankDetails_Edit, L("EditItem"));
        payeeBankDetails.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_PayeeBankDetails_Delete, L("DeleteItem"));

        var budgetingGroups = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_BudgetingGroups, L("BudgetingGroups"));
        budgetingGroups.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_BudgetingGroups_Create, L("CreateNewItem"));
        budgetingGroups.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_BudgetingGroups_Edit, L("EditItem"));
        budgetingGroups.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_BudgetingGroups_Delete, L("DeleteItem"));

        var budgetingGroupRateDetails = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_BudgetingGroupRateDetails, L("BudgetingGroupRateDetails"));
        budgetingGroupRateDetails.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_BudgetingGroupRateDetails_Create, L("CreateNewItem"));
        budgetingGroupRateDetails.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_BudgetingGroupRateDetails_Edit, L("EditItem"));
        budgetingGroupRateDetails.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_BudgetingGroupRateDetails_Delete, L("DeleteItem"));

        var clearanceRequestTypes = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_ClearanceRequestTypes, L("ClearanceRequestTypes"));
        clearanceRequestTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_ClearanceRequestTypes_Create, L("CreateNewItem"));
        clearanceRequestTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_ClearanceRequestTypes_Edit, L("EditItem"));
        clearanceRequestTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_ClearanceRequestTypes_Delete, L("DeleteItem"));

        var clearanceRequestFields = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_ClearanceRequestFields, L("ClearanceRequestFields"));
        clearanceRequestFields.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_ClearanceRequestFields_Create, L("CreateNewItem"));
        clearanceRequestFields.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_ClearanceRequestFields_Edit, L("EditItem"));
        clearanceRequestFields.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_ClearanceRequestFields_Delete, L("DeleteItem"));

        var clearanceRequestFieldDetails = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_ClearanceRequestFieldDetails, L("ClearanceRequestFieldDetails"));
        clearanceRequestFieldDetails.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_ClearanceRequestFieldDetails_Create, L("CreateNewItem"));
        clearanceRequestFieldDetails.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_ClearanceRequestFieldDetails_Edit, L("EditItem"));
        clearanceRequestFieldDetails.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_ClearanceRequestFieldDetails_Delete, L("DeleteItem"));

        var disciplinaryTypes = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_DisciplinaryTypes, L("DisciplinaryTypes"));
        disciplinaryTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_DisciplinaryTypes_Create, L("CreateNewItem"));
        disciplinaryTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_DisciplinaryTypes_Edit, L("EditItem"));
        disciplinaryTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_DisciplinaryTypes_Delete, L("DeleteItem"));

        var disciplinaryTypeDetails = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_DisciplinaryTypeDetails, L("DisciplinaryTypeDetails"));
        disciplinaryTypeDetails.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_DisciplinaryTypeDetails_Create, L("CreateNewItem"));
        disciplinaryTypeDetails.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_DisciplinaryTypeDetails_Edit, L("EditItem"));
        disciplinaryTypeDetails.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_DisciplinaryTypeDetails_Delete, L("DeleteItem"));

        var notificationConfigs = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_NotificationConfigs, L("NotificationConfigs"));
        notificationConfigs.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_NotificationConfigs_Create, L("CreateNewItem"));
        notificationConfigs.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_NotificationConfigs_Edit, L("EditItem"));
        notificationConfigs.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_NotificationConfigs_Delete, L("DeleteItem"));

        var notificationGroups = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_NotificationGroups, L("NotificationGroups"));
        notificationGroups.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_NotificationGroups_Create, L("CreateNewItem"));
        notificationGroups.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_NotificationGroups_Edit, L("EditItem"));
        notificationGroups.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_NotificationGroups_Delete, L("DeleteItem"));

        var documentTypes = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_DocumentTypes, L("DocumentTypes"));
        documentTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_DocumentTypes_Create, L("CreateNewItem"));
        documentTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_DocumentTypes_Edit, L("EditItem"));
        documentTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_DocumentTypes_Delete, L("DeleteItem"));

        var documentPrintForms = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_DocumentPrintForms, L("DocumentPrintForms"));
        documentPrintForms.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_DocumentPrintForms_Create, L("CreateNewItem"));
        documentPrintForms.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_DocumentPrintForms_Edit, L("EditItem"));
        documentPrintForms.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_DocumentPrintForms_Delete, L("DeleteItem"));

        var loanTypes = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_LoanTypes, L("LoanTypes"));
        loanTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_LoanTypes_Create, L("CreateNewItem"));
        loanTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_LoanTypes_Edit, L("EditItem"));
        loanTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_LoanTypes_Delete, L("DeleteItem"));

        var loanTypeDetails = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_LoanTypeDetails, L("LoanTypeDetails"));
        loanTypeDetails.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_LoanTypeDetails_Create, L("CreateNewItem"));
        loanTypeDetails.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_LoanTypeDetails_Edit, L("EditItem"));
        loanTypeDetails.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_LoanTypeDetails_Delete, L("DeleteItem"));

        var overtimeTypes = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_OvertimeTypes, L("OvertimeTypes"));
        overtimeTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_OvertimeTypes_Create, L("CreateNewItem"));
        overtimeTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_OvertimeTypes_Edit, L("EditItem"));
        overtimeTypes.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_OvertimeTypes_Delete, L("DeleteItem"));

        var overtimeTypeDetails = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_OvertimeTypeDetails, L("OvertimeTypeDetails"));
        overtimeTypeDetails.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_OvertimeTypeDetails_Create, L("CreateNewItem"));
        overtimeTypeDetails.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_OvertimeTypeDetails_Edit, L("EditItem"));
        overtimeTypeDetails.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_OvertimeTypeDetails_Delete, L("DeleteItem"));

        var assets = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Assets, L("Assets"));
        assets.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Assets_Create, L("CreateNewItem"));
        assets.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Assets_Edit, L("EditItem"));
        assets.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_Assets_Delete, L("DeleteItem"));

        var numberSeries = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_NumberSeries, L("NumberSeries"));
        numberSeries.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_NumberSeries_Create, L("CreateNewItem"));
        numberSeries.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_NumberSeries_Edit, L("EditItem"));
        numberSeries.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_NumberSeries_Delete, L("DeleteItem"));

        var currencyConvertors = generalMasterInfo.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_CurrencyConvertors, L("CurrencyConvertors"));
        currencyConvertors.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_CurrencyConvertors_Create, L("CreateNewItem"));
        currencyConvertors.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_CurrencyConvertors_Edit, L("EditItem"));
        currencyConvertors.CreateChildPermission(AppPermissions.Pages_GeneralMasterInfo_CurrencyConvertors_Delete, L("DeleteItem"));

        //ORGANIZATION MANAGEMENT
        var organizationManagement = pages.CreateChildPermission(AppPermissions.Pages_OrganizationManagement, L("OrganizationManagement"));

        var costCenterGroups = organizationManagement.CreateChildPermission(AppPermissions.Pages_OrganizationManagement_CostCenterGroups, L("CostCenterGroups"));
        costCenterGroups.CreateChildPermission(AppPermissions.Pages_OrganizationManagement_CostCenterGroups_Create, L("CreateNewItem"));
        costCenterGroups.CreateChildPermission(AppPermissions.Pages_OrganizationManagement_CostCenterGroups_Edit, L("EditItem"));
        costCenterGroups.CreateChildPermission(AppPermissions.Pages_OrganizationManagement_CostCenterGroups_Delete, L("DeleteItem"));

        var costCenters = organizationManagement.CreateChildPermission(AppPermissions.Pages_OrganizationManagement_CostCenters, L("CostCenters"));
        costCenters.CreateChildPermission(AppPermissions.Pages_OrganizationManagement_CostCenters_Create, L("CreateNewItem"));
        costCenters.CreateChildPermission(AppPermissions.Pages_OrganizationManagement_CostCenters_Edit, L("EditItem"));
        costCenters.CreateChildPermission(AppPermissions.Pages_OrganizationManagement_CostCenters_Delete, L("DeleteItem"));

        var costElementTypes = organizationManagement.CreateChildPermission(AppPermissions.Pages_OrganizationManagement_CostElementTypes, L("CostElementTypes"));
        costElementTypes.CreateChildPermission(AppPermissions.Pages_OrganizationManagement_CostElementTypes_Create, L("CreateNewItem"));
        costElementTypes.CreateChildPermission(AppPermissions.Pages_OrganizationManagement_CostElementTypes_Edit, L("EditItem"));
        costElementTypes.CreateChildPermission(AppPermissions.Pages_OrganizationManagement_CostElementTypes_Delete, L("DeleteItem"));

        var costElements = organizationManagement.CreateChildPermission(AppPermissions.Pages_OrganizationManagement_CostElements, L("CostElements"));
        costElements.CreateChildPermission(AppPermissions.Pages_OrganizationManagement_CostElements_Create, L("CreateNewItem"));
        costElements.CreateChildPermission(AppPermissions.Pages_OrganizationManagement_CostElements_Edit, L("EditItem"));
        costElements.CreateChildPermission(AppPermissions.Pages_OrganizationManagement_CostElements_Delete, L("DeleteItem"));

        var jobTitles = organizationManagement.CreateChildPermission(AppPermissions.Pages_OrganizationManagement_JobTitles, L("JobTitles"));
        jobTitles.CreateChildPermission(AppPermissions.Pages_OrganizationManagement_JobTitles_Create, L("CreateNewItem"));
        jobTitles.CreateChildPermission(AppPermissions.Pages_OrganizationManagement_JobTitles_Edit, L("EditItem"));
        jobTitles.CreateChildPermission(AppPermissions.Pages_OrganizationManagement_JobTitles_Delete, L("DeleteItem"));

        var organizationUnitTypes = organizationManagement.CreateChildPermission(AppPermissions.Pages_OrganizationManagement_OrganizationUnitTypes, L("OrganizationUnitTypes"));
        organizationUnitTypes.CreateChildPermission(AppPermissions.Pages_OrganizationManagement_OrganizationUnitTypes_Create, L("CreateNewItem"));
        organizationUnitTypes.CreateChildPermission(AppPermissions.Pages_OrganizationManagement_OrganizationUnitTypes_Edit, L("EditItem"));
        organizationUnitTypes.CreateChildPermission(AppPermissions.Pages_OrganizationManagement_OrganizationUnitTypes_Delete, L("DeleteItem"));

        var orgUnits = organizationManagement.CreateChildPermission(AppPermissions.Pages_OrganizationManagement_OrgUnits, L("OrgUnits"));
        orgUnits.CreateChildPermission(AppPermissions.Pages_OrganizationManagement_OrgUnits_Create, L("CreateNewItem"));
        orgUnits.CreateChildPermission(AppPermissions.Pages_OrganizationManagement_OrgUnits_Edit, L("EditItem"));
        orgUnits.CreateChildPermission(AppPermissions.Pages_OrganizationManagement_OrgUnits_Delete, L("DeleteItem"));

        var organizationBankInfos = organizationManagement.CreateChildPermission(AppPermissions.Pages_OrganizationManagement_OrganizationBankInfos, L("OrganizationBankInfos"));
        organizationBankInfos.CreateChildPermission(AppPermissions.Pages_OrganizationManagement_OrganizationBankInfos_Create, L("CreateNewItem"));
        organizationBankInfos.CreateChildPermission(AppPermissions.Pages_OrganizationManagement_OrganizationBankInfos_Edit, L("EditItem"));
        organizationBankInfos.CreateChildPermission(AppPermissions.Pages_OrganizationManagement_OrganizationBankInfos_Delete, L("DeleteItem"));

        var workLocations = organizationManagement.CreateChildPermission(AppPermissions.Pages_OrganizationManagement_WorkLocations, L("WorkLocations"));
        workLocations.CreateChildPermission(AppPermissions.Pages_OrganizationManagement_WorkLocations_Create, L("CreateNewItem"));
        workLocations.CreateChildPermission(AppPermissions.Pages_OrganizationManagement_WorkLocations_Edit, L("EditItem"));
        workLocations.CreateChildPermission(AppPermissions.Pages_OrganizationManagement_WorkLocations_Delete, L("DeleteItem"));


        //PERSONNEL ADMINISTRATION (WAVE A)
        var personnelAdministration = pages.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration, L("PersonnelAdministration"));

        var employees = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_Employees, L("Employees"));
        employees.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_Employees_Create, L("CreateNewItem"));
        employees.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_Employees_Edit, L("EditItem"));
        employees.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_Employees_Delete, L("DeleteItem"));
        employees.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_Employees_ManageUserLink, L("ManageUserLink"));

        var employeeDates = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeDates, L("EmployeeDates"));
        employeeDates.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeDates_Create, L("CreateNewItem"));
        employeeDates.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeDates_Edit, L("EditItem"));
        employeeDates.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeDates_Delete, L("DeleteItem"));

        var employeeManagers = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeManagers, L("EmployeeManagers"));
        employeeManagers.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeManagers_Create, L("CreateNewItem"));
        employeeManagers.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeManagers_Edit, L("EditItem"));
        employeeManagers.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeManagers_Delete, L("DeleteItem"));

        var employeeDottedLineManagers = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeDottedLineManagers, L("EmployeeDottedLineManagers"));
        employeeDottedLineManagers.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeDottedLineManagers_Create, L("CreateNewItem"));
        employeeDottedLineManagers.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeDottedLineManagers_Edit, L("EditItem"));
        employeeDottedLineManagers.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeDottedLineManagers_Delete, L("DeleteItem"));

        var employeeDelegations = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeDelegations, L("EmployeeDelegations"));
        employeeDelegations.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeDelegations_Create, L("CreateNewItem"));
        employeeDelegations.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeDelegations_Edit, L("EditItem"));
        employeeDelegations.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeDelegations_Delete, L("DeleteItem"));

        var employeeRelationships = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeRelationships, L("EmployeeRelationships"));
        employeeRelationships.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeRelationships_Create, L("CreateNewItem"));
        employeeRelationships.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeRelationships_Edit, L("EditItem"));
        employeeRelationships.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeRelationships_Delete, L("DeleteItem"));

        var employeeSignatures = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeSignatures, L("EmployeeSignatures"));
        employeeSignatures.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeSignatures_Create, L("CreateNewItem"));
        employeeSignatures.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeSignatures_Edit, L("EditItem"));
        employeeSignatures.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeSignatures_Delete, L("DeleteItem"));

        var employeeEducations = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeEducations, L("EmployeeEducations"));
        employeeEducations.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeEducations_Create, L("CreateNewItem"));
        employeeEducations.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeEducations_Edit, L("EditItem"));
        employeeEducations.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeEducations_Delete, L("DeleteItem"));

        var employeeCertifications = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeCertifications, L("EmployeeCertifications"));
        employeeCertifications.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeCertifications_Create, L("CreateNewItem"));
        employeeCertifications.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeCertifications_Edit, L("EditItem"));
        employeeCertifications.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeCertifications_Delete, L("DeleteItem"));

        var employeeWorkExperiences = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeWorkExperiences, L("EmployeeWorkExperiences"));
        employeeWorkExperiences.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeWorkExperiences_Create, L("CreateNewItem"));
        employeeWorkExperiences.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeWorkExperiences_Edit, L("EditItem"));
        employeeWorkExperiences.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeWorkExperiences_Delete, L("DeleteItem"));

        var employeeDocuments = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeDocuments, L("EmployeeDocuments"));
        employeeDocuments.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeDocuments_Create, L("CreateNewItem"));
        employeeDocuments.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeDocuments_Edit, L("EditItem"));
        employeeDocuments.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeDocuments_Delete, L("DeleteItem"));

        var employeeDocumentMvmts = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeDocumentMvmts, L("EmployeeDocumentMvmts"));
        employeeDocumentMvmts.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeDocumentMvmts_Create, L("CreateNewItem"));
        employeeDocumentMvmts.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeDocumentMvmts_Edit, L("EditItem"));
        employeeDocumentMvmts.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeDocumentMvmts_Delete, L("DeleteItem"));

        var employeeVisaInfos = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeVisaInfos, L("EmployeeVisaInfos"));
        employeeVisaInfos.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeVisaInfos_Create, L("CreateNewItem"));
        employeeVisaInfos.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeVisaInfos_Edit, L("EditItem"));
        employeeVisaInfos.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeVisaInfos_Delete, L("DeleteItem"));

        var employeeOrgAssignments = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeOrgAssignments, L("EmployeeOrgAssignments"));
        employeeOrgAssignments.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeOrgAssignments_Create, L("CreateNewItem"));
        employeeOrgAssignments.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeOrgAssignments_Edit, L("EditItem"));
        employeeOrgAssignments.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeOrgAssignments_Delete, L("DeleteItem"));

        var employeeContracts = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeContracts, L("EmployeeContracts"));
        employeeContracts.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeContracts_Create, L("CreateNewItem"));
        employeeContracts.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeContracts_Edit, L("EditItem"));
        employeeContracts.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeContracts_Delete, L("DeleteItem"));

        var employeeActions = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeActions, L("EmployeeActions"));
        employeeActions.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeActions_Create, L("CreateNewItem"));
        employeeActions.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeActions_Edit, L("EditItem"));
        employeeActions.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeActions_Delete, L("DeleteItem"));

        var employeeSeparations = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeSeparations, L("EmployeeSeparations"));
        employeeSeparations.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeSeparations_Create, L("CreateNewItem"));
        employeeSeparations.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeSeparations_Edit, L("EditItem"));
        employeeSeparations.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeSeparations_Delete, L("DeleteItem"));

        var employeeRejoins = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeRejoins, L("EmployeeRejoins"));
        employeeRejoins.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeRejoins_Create, L("CreateNewItem"));
        employeeRejoins.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeRejoins_Edit, L("EditItem"));
        employeeRejoins.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeRejoins_Delete, L("DeleteItem"));

        var employeeAbsences = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsences, L("EmployeeAbsences"));
        employeeAbsences.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsences_Create, L("CreateNewItem"));
        employeeAbsences.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsences_Edit, L("EditItem"));
        employeeAbsences.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsences_Delete, L("DeleteItem"));

        var employeeAbsenceQuotas = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsenceQuotas, L("EmployeeAbsenceQuotas"));
        employeeAbsenceQuotas.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsenceQuotas_Create, L("CreateNewItem"));
        employeeAbsenceQuotas.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsenceQuotas_Edit, L("EditItem"));
        employeeAbsenceQuotas.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsenceQuotas_Delete, L("DeleteItem"));

        var employeeAbsAdjustments = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsAdjustments, L("EmployeeAbsAdjustments"));
        employeeAbsAdjustments.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsAdjustments_Create, L("CreateNewItem"));
        employeeAbsAdjustments.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsAdjustments_Edit, L("EditItem"));
        employeeAbsAdjustments.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsAdjustments_Delete, L("DeleteItem"));

        var employeeAbsQuotaForfeits = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsQuotaForfeits, L("EmployeeAbsQuotaForfeits"));
        employeeAbsQuotaForfeits.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsQuotaForfeits_Create, L("CreateNewItem"));
        employeeAbsQuotaForfeits.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsQuotaForfeits_Edit, L("EditItem"));
        employeeAbsQuotaForfeits.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsQuotaForfeits_Delete, L("DeleteItem"));

        var employeeAttendances = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeAttendances, L("EmployeeAttendances"));
        employeeAttendances.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeAttendances_Create, L("CreateNewItem"));
        employeeAttendances.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeAttendances_Edit, L("EditItem"));
        employeeAttendances.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeAttendances_Delete, L("DeleteItem"));

        var employeeAttendanceQuotas = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeAttendanceQuotas, L("EmployeeAttendanceQuotas"));
        employeeAttendanceQuotas.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeAttendanceQuotas_Create, L("CreateNewItem"));
        employeeAttendanceQuotas.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeAttendanceQuotas_Edit, L("EditItem"));
        employeeAttendanceQuotas.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeAttendanceQuotas_Delete, L("DeleteItem"));

        var employeeBasicPays = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPays, L("EmployeeBasicPays"));
        employeeBasicPays.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPays_Create, L("CreateNewItem"));
        employeeBasicPays.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPays_Edit, L("EditItem"));
        employeeBasicPays.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPays_Delete, L("DeleteItem"));

        var employeeBasicPayDetails = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPayDetails, L("EmployeeBasicPayDetails"));
        employeeBasicPayDetails.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPayDetails_Create, L("CreateNewItem"));
        employeeBasicPayDetails.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPayDetails_Edit, L("EditItem"));
        employeeBasicPayDetails.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPayDetails_Delete, L("DeleteItem"));

        var employeeOnHolds = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeOnHolds, L("EmployeeOnHolds"));
        employeeOnHolds.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeOnHolds_Create, L("CreateNewItem"));
        employeeOnHolds.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeOnHolds_Edit, L("EditItem"));
        employeeOnHolds.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeOnHolds_Delete, L("DeleteItem"));

        var employeeGratuities = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeGratuities, L("EmployeeGratuities"));
        employeeGratuities.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeGratuities_Create, L("CreateNewItem"));
        employeeGratuities.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeGratuities_Edit, L("EditItem"));
        employeeGratuities.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeGratuities_Delete, L("DeleteItem"));

        var employeeGratuityQuotas = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeGratuityQuotas, L("EmployeeGratuityQuotas"));
        employeeGratuityQuotas.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeGratuityQuotas_Create, L("CreateNewItem"));
        employeeGratuityQuotas.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeGratuityQuotas_Edit, L("EditItem"));
        employeeGratuityQuotas.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeGratuityQuotas_Delete, L("DeleteItem"));

        var employeeLoans = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeLoans, L("EmployeeLoans"));
        employeeLoans.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeLoans_Create, L("CreateNewItem"));
        employeeLoans.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeLoans_Edit, L("EditItem"));
        employeeLoans.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeLoans_Delete, L("DeleteItem"));

        var employeeLoanExemptions = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeLoanExemptions, L("EmployeeLoanExemptions"));
        employeeLoanExemptions.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeLoanExemptions_Create, L("CreateNewItem"));
        employeeLoanExemptions.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeLoanExemptions_Edit, L("EditItem"));
        employeeLoanExemptions.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeLoanExemptions_Delete, L("DeleteItem"));

        var employeeLoanRecoveries = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeLoanRecoveries, L("EmployeeLoanRecoveries"));
        employeeLoanRecoveries.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeLoanRecoveries_Create, L("CreateNewItem"));
        employeeLoanRecoveries.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeLoanRecoveries_Edit, L("EditItem"));
        employeeLoanRecoveries.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeLoanRecoveries_Delete, L("DeleteItem"));

        var loanInstallments = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_LoanInstallments, L("LoanInstallments"));
        loanInstallments.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_LoanInstallments_Create, L("CreateNewItem"));
        loanInstallments.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_LoanInstallments_Edit, L("EditItem"));
        loanInstallments.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_LoanInstallments_Delete, L("DeleteItem"));

        var employeeFunds = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeFunds, L("EmployeeFunds"));
        employeeFunds.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeFunds_Create, L("CreateNewItem"));
        employeeFunds.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeFunds_Edit, L("EditItem"));
        employeeFunds.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeFunds_Delete, L("DeleteItem"));

        var employeeFundDetails = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeFundDetails, L("EmployeeFundDetails"));
        employeeFundDetails.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeFundDetails_Create, L("CreateNewItem"));
        employeeFundDetails.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeFundDetails_Edit, L("EditItem"));
        employeeFundDetails.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeFundDetails_Delete, L("DeleteItem"));

        var employeeFundExemptions = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeFundExemptions, L("EmployeeFundExemptions"));
        employeeFundExemptions.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeFundExemptions_Create, L("CreateNewItem"));
        employeeFundExemptions.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeFundExemptions_Edit, L("EditItem"));
        employeeFundExemptions.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeFundExemptions_Delete, L("DeleteItem"));

        var employeeTickets = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeTickets, L("EmployeeTickets"));
        employeeTickets.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeTickets_Create, L("CreateNewItem"));
        employeeTickets.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeTickets_Edit, L("EditItem"));
        employeeTickets.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeTickets_Delete, L("DeleteItem"));

        var employeeTicketQuotas = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeTicketQuotas, L("EmployeeTicketQuotas"));
        employeeTicketQuotas.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeTicketQuotas_Create, L("CreateNewItem"));
        employeeTicketQuotas.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeTicketQuotas_Edit, L("EditItem"));
        employeeTicketQuotas.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeTicketQuotas_Delete, L("DeleteItem"));

        var employeeTicketRoutes = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeTicketRoutes, L("EmployeeTicketRoutes"));
        employeeTicketRoutes.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeTicketRoutes_Create, L("CreateNewItem"));
        employeeTicketRoutes.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeTicketRoutes_Edit, L("EditItem"));
        employeeTicketRoutes.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeTicketRoutes_Delete, L("DeleteItem"));

        var employeeTktAdjustments = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeTktAdjustments, L("EmployeeTktAdjustments"));
        employeeTktAdjustments.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeTktAdjustments_Create, L("CreateNewItem"));
        employeeTktAdjustments.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeTktAdjustments_Edit, L("EditItem"));
        employeeTktAdjustments.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeTktAdjustments_Delete, L("DeleteItem"));

        var employeeSocialSecurities = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeSocialSecurities, L("EmployeeSocialSecurities"));
        employeeSocialSecurities.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeSocialSecurities_Create, L("CreateNewItem"));
        employeeSocialSecurities.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeSocialSecurities_Edit, L("EditItem"));
        employeeSocialSecurities.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeSocialSecurities_Delete, L("DeleteItem"));

        var employeeSocialSecurityClaims = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeSocialSecurityClaims, L("EmployeeSocialSecurityClaims"));
        employeeSocialSecurityClaims.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeSocialSecurityClaims_Create, L("CreateNewItem"));
        employeeSocialSecurityClaims.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeSocialSecurityClaims_Edit, L("EditItem"));
        employeeSocialSecurityClaims.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeSocialSecurityClaims_Delete, L("DeleteItem"));

        var employeeDependants = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeDependants, L("EmployeeDependants"));
        employeeDependants.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeDependants_Create, L("CreateNewItem"));
        employeeDependants.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeDependants_Edit, L("EditItem"));
        employeeDependants.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeDependants_Delete, L("DeleteItem"));

        var employeeDependantExceptions = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeDependantExceptions, L("EmployeeDependantExceptions"));
        employeeDependantExceptions.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeDependantExceptions_Create, L("CreateNewItem"));
        employeeDependantExceptions.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeDependantExceptions_Edit, L("EditItem"));
        employeeDependantExceptions.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeDependantExceptions_Delete, L("DeleteItem"));

        var employeeSponsors = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeSponsors, L("EmployeeSponsors"));
        employeeSponsors.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeSponsors_Create, L("CreateNewItem"));
        employeeSponsors.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeSponsors_Edit, L("EditItem"));
        employeeSponsors.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeSponsors_Delete, L("DeleteItem"));

        var employeeFnFPayments = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeFnFPayments, L("EmployeeFnFPayments"));
        employeeFnFPayments.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeFnFPayments_Create, L("CreateNewItem"));
        employeeFnFPayments.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeFnFPayments_Edit, L("EditItem"));
        employeeFnFPayments.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeFnFPayments_Delete, L("DeleteItem"));

        var employeeBanks = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeBanks, L("EmployeeBanks"));
        employeeBanks.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeBanks_Create, L("CreateNewItem"));
        employeeBanks.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeBanks_Edit, L("EditItem"));
        employeeBanks.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeBanks_Delete, L("DeleteItem"));

        var employeeAssets = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeAssets, L("EmployeeAssets"));
        employeeAssets.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeAssets_Create, L("CreateNewItem"));
        employeeAssets.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeAssets_Edit, L("EditItem"));
        employeeAssets.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeAssets_Delete, L("DeleteItem"));

        var employeeHRServices = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeHRServices, L("EmployeeHRServices"));
        employeeHRServices.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeHRServices_Create, L("CreateNewItem"));
        employeeHRServices.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeHRServices_Edit, L("EditItem"));
        employeeHRServices.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeHRServices_Delete, L("DeleteItem"));

        var employeeLetterRequests = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeLetterRequests, L("EmployeeLetterRequests"));
        employeeLetterRequests.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeLetterRequests_Create, L("CreateNewItem"));
        employeeLetterRequests.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeLetterRequests_Edit, L("EditItem"));
        employeeLetterRequests.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeLetterRequests_Delete, L("DeleteItem"));

        var employeeITServices = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeITServices, L("EmployeeITServices"));
        employeeITServices.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeITServices_Create, L("CreateNewItem"));
        employeeITServices.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeITServices_Edit, L("EditItem"));
        employeeITServices.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeITServices_Delete, L("DeleteItem"));

        var employeeAssetInvoices = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeAssetInvoices, L("EmployeeAssetInvoices"));
        employeeAssetInvoices.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeAssetInvoices_Create, L("CreateNewItem"));
        employeeAssetInvoices.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeAssetInvoices_Edit, L("EditItem"));
        employeeAssetInvoices.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeAssetInvoices_Delete, L("DeleteItem"));

        var employeeDisciplinaries = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeDisciplinaries, L("EmployeeDisciplinaries"));
        employeeDisciplinaries.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeDisciplinaries_Create, L("CreateNewItem"));
        employeeDisciplinaries.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeDisciplinaries_Edit, L("EditItem"));
        employeeDisciplinaries.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeDisciplinaries_Delete, L("DeleteItem"));

        var employeeGrievances = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeGrievances, L("EmployeeGrievances"));
        employeeGrievances.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeGrievances_Create, L("CreateNewItem"));
        employeeGrievances.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeGrievances_Edit, L("EditItem"));
        employeeGrievances.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeGrievances_Delete, L("DeleteItem"));

        var employeeHealthReimburses = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeHealthReimburses, L("EmployeeHealthReimburses"));
        employeeHealthReimburses.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeHealthReimburses_Create, L("CreateNewItem"));
        employeeHealthReimburses.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeHealthReimburses_Edit, L("EditItem"));
        employeeHealthReimburses.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeHealthReimburses_Delete, L("DeleteItem"));

        var employeeOvertimes = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeOvertimes, L("EmployeeOvertimes"));
        employeeOvertimes.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeOvertimes_Create, L("CreateNewItem"));
        employeeOvertimes.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeOvertimes_Edit, L("EditItem"));
        employeeOvertimes.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeOvertimes_Delete, L("DeleteItem"));

        var employeePayments = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeePayments, L("EmployeePayments"));
        employeePayments.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeePayments_Create, L("CreateNewItem"));
        employeePayments.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeePayments_Edit, L("EditItem"));
        employeePayments.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeePayments_Delete, L("DeleteItem"));

        var employeePaymentBatches = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeePaymentBatches, L("EmployeePaymentBatches"));
        employeePaymentBatches.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeePaymentBatches_Create, L("CreateNewItem"));
        employeePaymentBatches.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeePaymentBatches_Edit, L("EditItem"));
        employeePaymentBatches.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeePaymentBatches_Delete, L("DeleteItem"));

        var employeeRecurringPayments = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeRecurringPayments, L("EmployeeRecurringPayments"));
        employeeRecurringPayments.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeRecurringPayments_Create, L("CreateNewItem"));
        employeeRecurringPayments.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeRecurringPayments_Edit, L("EditItem"));
        employeeRecurringPayments.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeRecurringPayments_Delete, L("DeleteItem"));

        var employeeRemunerations = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeRemunerations, L("EmployeeRemunerations"));
        employeeRemunerations.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeRemunerations_Create, L("CreateNewItem"));
        employeeRemunerations.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeRemunerations_Edit, L("EditItem"));
        employeeRemunerations.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeRemunerations_Delete, L("DeleteItem"));

        var employeeRewards = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeRewards, L("EmployeeRewards"));
        employeeRewards.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeRewards_Create, L("CreateNewItem"));
        employeeRewards.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeRewards_Edit, L("EditItem"));
        employeeRewards.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeRewards_Delete, L("DeleteItem"));

        var employeeCostDistributions = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeCostDistributions, L("EmployeeCostDistributions"));
        employeeCostDistributions.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeCostDistributions_Create, L("CreateNewItem"));
        employeeCostDistributions.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeCostDistributions_Edit, L("EditItem"));
        employeeCostDistributions.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeCostDistributions_Delete, L("DeleteItem"));

        var employeeAccountAssignments = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeAccountAssignments, L("EmployeeAccountAssignments"));
        employeeAccountAssignments.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeAccountAssignments_Create, L("CreateNewItem"));
        employeeAccountAssignments.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeAccountAssignments_Edit, L("EditItem"));
        employeeAccountAssignments.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeAccountAssignments_Delete, L("DeleteItem"));

        var employeeExtdWorkLocations = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeExtdWorkLocations, L("EmployeeExtdWorkLocations"));
        employeeExtdWorkLocations.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeExtdWorkLocations_Create, L("CreateNewItem"));
        employeeExtdWorkLocations.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeExtdWorkLocations_Edit, L("EditItem"));
        employeeExtdWorkLocations.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeExtdWorkLocations_Delete, L("DeleteItem"));

        var employeeBenefitExceptions = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeBenefitExceptions, L("EmployeeBenefitExceptions"));
        employeeBenefitExceptions.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeBenefitExceptions_Create, L("CreateNewItem"));
        employeeBenefitExceptions.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeBenefitExceptions_Edit, L("EditItem"));
        employeeBenefitExceptions.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeBenefitExceptions_Delete, L("DeleteItem"));

        var employeeNonPayrollTrans = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeNonPayrollTrans, L("EmployeeNonPayrollTrans"));
        employeeNonPayrollTrans.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeNonPayrollTrans_Create, L("CreateNewItem"));
        employeeNonPayrollTrans.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeNonPayrollTrans_Edit, L("EditItem"));
        employeeNonPayrollTrans.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeNonPayrollTrans_Delete, L("DeleteItem"));

        var employeeTasks = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeTasks, L("EmployeeTasks"));
        employeeTasks.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeTasks_Create, L("CreateNewItem"));
        employeeTasks.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeTasks_Edit, L("EditItem"));
        employeeTasks.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_EmployeeTasks_Delete, L("DeleteItem"));

        var news = personnelAdministration.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_News, L("News"));
        news.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_News_Create, L("CreateNewItem"));
        news.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_News_Edit, L("EditItem"));
        news.CreateChildPermission(AppPermissions.Pages_PersonnelAdministration_News_Delete, L("DeleteItem"));

        var administration = pages.CreateChildPermission(AppPermissions.Pages_Administration, L("Administration"));

        var roles = administration.CreateChildPermission(AppPermissions.Pages_Administration_Roles, L("Roles"));
        roles.CreateChildPermission(AppPermissions.Pages_Administration_Roles_Create, L("CreatingNewRole"));
        roles.CreateChildPermission(AppPermissions.Pages_Administration_Roles_Edit, L("EditingRole"));
        roles.CreateChildPermission(AppPermissions.Pages_Administration_Roles_Delete, L("DeletingRole"));

        var users = administration.CreateChildPermission(AppPermissions.Pages_Administration_Users, L("Users"));
        users.CreateChildPermission(AppPermissions.Pages_Administration_Users_Create, L("CreatingNewUser"));
        users.CreateChildPermission(AppPermissions.Pages_Administration_Users_Edit, L("EditingUser"));
        users.CreateChildPermission(AppPermissions.Pages_Administration_Users_Delete, L("DeletingUser"));
        users.CreateChildPermission(AppPermissions.Pages_Administration_Users_ChangePermissions, L("ChangingPermissions"));
        users.CreateChildPermission(AppPermissions.Pages_Administration_Users_Impersonation, L("LoginForUsers"));
        users.CreateChildPermission(AppPermissions.Pages_Administration_Users_Unlock, L("Unlock"));
        users.CreateChildPermission(AppPermissions.Pages_Administration_Users_ChangeProfilePicture, L("UpdateUsersProfilePicture"));

        var languages = administration.CreateChildPermission(AppPermissions.Pages_Administration_Languages, L("Languages"));
        languages.CreateChildPermission(AppPermissions.Pages_Administration_Languages_Create, L("CreatingNewLanguage"), multiTenancySides: _isMultiTenancyEnabled ? MultiTenancySides.Host : MultiTenancySides.Tenant);
        languages.CreateChildPermission(AppPermissions.Pages_Administration_Languages_Edit, L("EditingLanguage"), multiTenancySides: _isMultiTenancyEnabled ? MultiTenancySides.Host : MultiTenancySides.Tenant);
        languages.CreateChildPermission(AppPermissions.Pages_Administration_Languages_Delete, L("DeletingLanguages"), multiTenancySides: _isMultiTenancyEnabled ? MultiTenancySides.Host : MultiTenancySides.Tenant);
        languages.CreateChildPermission(AppPermissions.Pages_Administration_Languages_ChangeTexts, L("ChangingTexts"));
        languages.CreateChildPermission(AppPermissions.Pages_Administration_Languages_ChangeDefaultLanguage, L("ChangeDefaultLanguage"));

        administration.CreateChildPermission(AppPermissions.Pages_Administration_AuditLogs, L("AuditLogs"));
        administration.CreateChildPermission(AppPermissions.Pages_Administration_ActiveSessions, L("ActiveSessions"));

        var organizationUnits = administration.CreateChildPermission(AppPermissions.Pages_Administration_OrganizationUnits, L("OrganizationUnits"));
        organizationUnits.CreateChildPermission(AppPermissions.Pages_Administration_OrganizationUnits_ManageOrganizationTree, L("ManagingOrganizationTree"));
        organizationUnits.CreateChildPermission(AppPermissions.Pages_Administration_OrganizationUnits_ManageMembers, L("ManagingMembers"));
        organizationUnits.CreateChildPermission(AppPermissions.Pages_Administration_OrganizationUnits_ManageRoles, L("ManagingRoles"));

        administration.CreateChildPermission(AppPermissions.Pages_Administration_UiCustomization, L("VisualSettings"));
        administration.CreateChildPermission(AppPermissions.Pages_Administration_Navigation_ManageScreenCodes, L("ManageScreenCodes"));
        administration.CreateChildPermission(AppPermissions.Pages_Administration_Navigation_ManageQuickLinks, L("ManageQuickLinks"));

        var webhooks = administration.CreateChildPermission(AppPermissions.Pages_Administration_WebhookSubscription, L("Webhooks"));
        webhooks.CreateChildPermission(AppPermissions.Pages_Administration_WebhookSubscription_Create, L("CreatingWebhooks"));
        webhooks.CreateChildPermission(AppPermissions.Pages_Administration_WebhookSubscription_Edit, L("EditingWebhooks"));
        webhooks.CreateChildPermission(AppPermissions.Pages_Administration_WebhookSubscription_ChangeActivity, L("ChangingWebhookActivity"));
        webhooks.CreateChildPermission(AppPermissions.Pages_Administration_WebhookSubscription_Detail, L("DetailingSubscription"));
        webhooks.CreateChildPermission(AppPermissions.Pages_Administration_Webhook_ListSendAttempts, L("ListingSendAttempts"));
        webhooks.CreateChildPermission(AppPermissions.Pages_Administration_Webhook_ResendWebhook, L("ResendingWebhook"));

        var dynamicProperties = administration.CreateChildPermission(AppPermissions.Pages_Administration_DynamicProperties, L("DynamicProperties"));
        dynamicProperties.CreateChildPermission(AppPermissions.Pages_Administration_DynamicProperties_Create, L("CreatingDynamicProperties"));
        dynamicProperties.CreateChildPermission(AppPermissions.Pages_Administration_DynamicProperties_Edit, L("EditingDynamicProperties"));
        dynamicProperties.CreateChildPermission(AppPermissions.Pages_Administration_DynamicProperties_Delete, L("DeletingDynamicProperties"));

        var dynamicPropertyValues = dynamicProperties.CreateChildPermission(AppPermissions.Pages_Administration_DynamicPropertyValue, L("DynamicPropertyValue"));
        dynamicPropertyValues.CreateChildPermission(AppPermissions.Pages_Administration_DynamicPropertyValue_Create, L("CreatingDynamicPropertyValue"));
        dynamicPropertyValues.CreateChildPermission(AppPermissions.Pages_Administration_DynamicPropertyValue_Edit, L("EditingDynamicPropertyValue"));
        dynamicPropertyValues.CreateChildPermission(AppPermissions.Pages_Administration_DynamicPropertyValue_Delete, L("DeletingDynamicPropertyValue"));

        var dynamicEntityProperties = dynamicProperties.CreateChildPermission(AppPermissions.Pages_Administration_DynamicEntityProperties, L("DynamicEntityProperties"));
        dynamicEntityProperties.CreateChildPermission(AppPermissions.Pages_Administration_DynamicEntityProperties_Create, L("CreatingDynamicEntityProperties"));
        dynamicEntityProperties.CreateChildPermission(AppPermissions.Pages_Administration_DynamicEntityProperties_Edit, L("EditingDynamicEntityProperties"));
        dynamicEntityProperties.CreateChildPermission(AppPermissions.Pages_Administration_DynamicEntityProperties_Delete, L("DeletingDynamicEntityProperties"));

        var dynamicEntityPropertyValues = dynamicProperties.CreateChildPermission(AppPermissions.Pages_Administration_DynamicEntityPropertyValue, L("EntityDynamicPropertyValue"));
        dynamicEntityPropertyValues.CreateChildPermission(AppPermissions.Pages_Administration_DynamicEntityPropertyValue_Create, L("CreatingDynamicEntityPropertyValue"));
        dynamicEntityPropertyValues.CreateChildPermission(AppPermissions.Pages_Administration_DynamicEntityPropertyValue_Edit, L("EditingDynamicEntityPropertyValue"));
        dynamicEntityPropertyValues.CreateChildPermission(AppPermissions.Pages_Administration_DynamicEntityPropertyValue_Delete, L("DeletingDynamicEntityPropertyValue"));

        var massNotification = administration.CreateChildPermission(AppPermissions.Pages_Administration_MassNotification, L("MassNotifications"));
        massNotification.CreateChildPermission(AppPermissions.Pages_Administration_MassNotification_Create, L("MassNotificationCreate"));

        administration.CreateChildPermission(AppPermissions.Pages_Administration_EntityChanges_FullHistory, L("EntityChanges_FullHistory"));

        //TIME MANAGEMENT (WAVE 1)
        var timeManagement = pages.CreateChildPermission(AppPermissions.Pages_TimeManagement, L("TimeManagement"));

        var timeTypes = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_TimeTypes, L("TimeTypes"));
        timeTypes.CreateChildPermission(AppPermissions.Pages_TimeManagement_TimeTypes_Create, L("CreateNewItem"));
        timeTypes.CreateChildPermission(AppPermissions.Pages_TimeManagement_TimeTypes_Edit, L("EditItem"));
        timeTypes.CreateChildPermission(AppPermissions.Pages_TimeManagement_TimeTypes_Delete, L("DeleteItem"));

        var transTypes = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_TransTypes, L("TransTypes"));
        transTypes.CreateChildPermission(AppPermissions.Pages_TimeManagement_TransTypes_Create, L("CreateNewItem"));
        transTypes.CreateChildPermission(AppPermissions.Pages_TimeManagement_TransTypes_Edit, L("EditItem"));
        transTypes.CreateChildPermission(AppPermissions.Pages_TimeManagement_TransTypes_Delete, L("DeleteItem"));

        var timeTransferTypes = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_TimeTransferTypes, L("TimeTransferTypes"));
        timeTransferTypes.CreateChildPermission(AppPermissions.Pages_TimeManagement_TimeTransferTypes_Create, L("CreateNewItem"));
        timeTransferTypes.CreateChildPermission(AppPermissions.Pages_TimeManagement_TimeTransferTypes_Edit, L("EditItem"));
        timeTransferTypes.CreateChildPermission(AppPermissions.Pages_TimeManagement_TimeTransferTypes_Delete, L("DeleteItem"));

        var breakShifts = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_BreakShifts, L("BreakShifts"));
        breakShifts.CreateChildPermission(AppPermissions.Pages_TimeManagement_BreakShifts_Create, L("CreateNewItem"));
        breakShifts.CreateChildPermission(AppPermissions.Pages_TimeManagement_BreakShifts_Edit, L("EditItem"));
        breakShifts.CreateChildPermission(AppPermissions.Pages_TimeManagement_BreakShifts_Delete, L("DeleteItem"));

        var duties = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_Duties, L("Duties"));
        duties.CreateChildPermission(AppPermissions.Pages_TimeManagement_Duties_Create, L("CreateNewItem"));
        duties.CreateChildPermission(AppPermissions.Pages_TimeManagement_Duties_Edit, L("EditItem"));
        duties.CreateChildPermission(AppPermissions.Pages_TimeManagement_Duties_Delete, L("DeleteItem"));

        var dailyDuties = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_DailyDuties, L("DailyDuties"));
        dailyDuties.CreateChildPermission(AppPermissions.Pages_TimeManagement_DailyDuties_Create, L("CreateNewItem"));
        dailyDuties.CreateChildPermission(AppPermissions.Pages_TimeManagement_DailyDuties_Edit, L("EditItem"));
        dailyDuties.CreateChildPermission(AppPermissions.Pages_TimeManagement_DailyDuties_Delete, L("DeleteItem"));

        var holidayCalendars = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_HolidayCalendars, L("HolidayCalendars"));
        holidayCalendars.CreateChildPermission(AppPermissions.Pages_TimeManagement_HolidayCalendars_Create, L("CreateNewItem"));
        holidayCalendars.CreateChildPermission(AppPermissions.Pages_TimeManagement_HolidayCalendars_Edit, L("EditItem"));
        holidayCalendars.CreateChildPermission(AppPermissions.Pages_TimeManagement_HolidayCalendars_Delete, L("DeleteItem"));

        var holidays = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_Holidays, L("Holidays"));
        holidays.CreateChildPermission(AppPermissions.Pages_TimeManagement_Holidays_Create, L("CreateNewItem"));
        holidays.CreateChildPermission(AppPermissions.Pages_TimeManagement_Holidays_Edit, L("EditItem"));
        holidays.CreateChildPermission(AppPermissions.Pages_TimeManagement_Holidays_Delete, L("DeleteItem"));

        var holidayPeriods = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_HolidayPeriods, L("HolidayPeriods"));
        holidayPeriods.CreateChildPermission(AppPermissions.Pages_TimeManagement_HolidayPeriods_Create, L("CreateNewItem"));
        holidayPeriods.CreateChildPermission(AppPermissions.Pages_TimeManagement_HolidayPeriods_Edit, L("EditItem"));
        holidayPeriods.CreateChildPermission(AppPermissions.Pages_TimeManagement_HolidayPeriods_Delete, L("DeleteItem"));

        var timePolicies = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_TimePolicies, L("TimePolicies"));
        timePolicies.CreateChildPermission(AppPermissions.Pages_TimeManagement_TimePolicies_Create, L("CreateNewItem"));
        timePolicies.CreateChildPermission(AppPermissions.Pages_TimeManagement_TimePolicies_Edit, L("EditItem"));
        timePolicies.CreateChildPermission(AppPermissions.Pages_TimeManagement_TimePolicies_Delete, L("DeleteItem"));

        //TIME MANAGEMENT (WAVE 2)
        var dutyPeriods = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_DutyPeriods, L("DutyPeriods"));
        dutyPeriods.CreateChildPermission(AppPermissions.Pages_TimeManagement_DutyPeriods_Create, L("CreateNewItem"));
        dutyPeriods.CreateChildPermission(AppPermissions.Pages_TimeManagement_DutyPeriods_Edit, L("EditItem"));
        dutyPeriods.CreateChildPermission(AppPermissions.Pages_TimeManagement_DutyPeriods_Delete, L("DeleteItem"));

        var dutyPeriodDetails = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_DutyPeriodDetails, L("DutyPeriodDetails"));
        dutyPeriodDetails.CreateChildPermission(AppPermissions.Pages_TimeManagement_DutyPeriodDetails_Create, L("CreateNewItem"));
        dutyPeriodDetails.CreateChildPermission(AppPermissions.Pages_TimeManagement_DutyPeriodDetails_Edit, L("EditItem"));
        dutyPeriodDetails.CreateChildPermission(AppPermissions.Pages_TimeManagement_DutyPeriodDetails_Delete, L("DeleteItem"));

        var rosterDutyPeriods = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_RosterDutyPeriods, L("RosterDutyPeriods"));
        rosterDutyPeriods.CreateChildPermission(AppPermissions.Pages_TimeManagement_RosterDutyPeriods_Create, L("CreateNewItem"));
        rosterDutyPeriods.CreateChildPermission(AppPermissions.Pages_TimeManagement_RosterDutyPeriods_Edit, L("EditItem"));
        rosterDutyPeriods.CreateChildPermission(AppPermissions.Pages_TimeManagement_RosterDutyPeriods_Delete, L("DeleteItem"));

        var rosterDutyPeriodDetails = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_RosterDutyPeriodDetails, L("RosterDutyPeriodDetails"));
        rosterDutyPeriodDetails.CreateChildPermission(AppPermissions.Pages_TimeManagement_RosterDutyPeriodDetails_Create, L("CreateNewItem"));
        rosterDutyPeriodDetails.CreateChildPermission(AppPermissions.Pages_TimeManagement_RosterDutyPeriodDetails_Edit, L("EditItem"));
        rosterDutyPeriodDetails.CreateChildPermission(AppPermissions.Pages_TimeManagement_RosterDutyPeriodDetails_Delete, L("DeleteItem"));

        var rosterDutySchedules = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_RosterDutySchedules, L("RosterDutySchedules"));
        rosterDutySchedules.CreateChildPermission(AppPermissions.Pages_TimeManagement_RosterDutySchedules_Create, L("CreateNewItem"));
        rosterDutySchedules.CreateChildPermission(AppPermissions.Pages_TimeManagement_RosterDutySchedules_Edit, L("EditItem"));
        rosterDutySchedules.CreateChildPermission(AppPermissions.Pages_TimeManagement_RosterDutySchedules_Delete, L("DeleteItem"));

        var rosterGroups = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_RosterGroups, L("RosterGroups"));
        rosterGroups.CreateChildPermission(AppPermissions.Pages_TimeManagement_RosterGroups_Create, L("CreateNewItem"));
        rosterGroups.CreateChildPermission(AppPermissions.Pages_TimeManagement_RosterGroups_Edit, L("EditItem"));
        rosterGroups.CreateChildPermission(AppPermissions.Pages_TimeManagement_RosterGroups_Delete, L("DeleteItem"));

        var rosterGroupDetails = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_RosterGroupDetails, L("RosterGroupDetails"));
        rosterGroupDetails.CreateChildPermission(AppPermissions.Pages_TimeManagement_RosterGroupDetails_Create, L("CreateNewItem"));
        rosterGroupDetails.CreateChildPermission(AppPermissions.Pages_TimeManagement_RosterGroupDetails_Edit, L("EditItem"));
        rosterGroupDetails.CreateChildPermission(AppPermissions.Pages_TimeManagement_RosterGroupDetails_Delete, L("DeleteItem"));

        var rosterGroupSchedules = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_RosterGroupSchedules, L("RosterGroupSchedules"));
        rosterGroupSchedules.CreateChildPermission(AppPermissions.Pages_TimeManagement_RosterGroupSchedules_Create, L("CreateNewItem"));
        rosterGroupSchedules.CreateChildPermission(AppPermissions.Pages_TimeManagement_RosterGroupSchedules_Edit, L("EditItem"));
        rosterGroupSchedules.CreateChildPermission(AppPermissions.Pages_TimeManagement_RosterGroupSchedules_Delete, L("DeleteItem"));

        var rosterDailyDuties = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_RosterDailyDuties, L("RosterDailyDuties"));
        rosterDailyDuties.CreateChildPermission(AppPermissions.Pages_TimeManagement_RosterDailyDuties_Create, L("CreateNewItem"));
        rosterDailyDuties.CreateChildPermission(AppPermissions.Pages_TimeManagement_RosterDailyDuties_Edit, L("EditItem"));
        rosterDailyDuties.CreateChildPermission(AppPermissions.Pages_TimeManagement_RosterDailyDuties_Delete, L("DeleteItem"));

        // Read-only viewer: no Create/Edit/Delete children by design.
        timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_RosterScheduleViewer, L("RosterScheduleViewer"));

        // Wave 3 (employee-assignment records)
        var employeeDutyPeriods = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_EmployeeDutyPeriods, L("EmployeeDutyPeriods"));
        employeeDutyPeriods.CreateChildPermission(AppPermissions.Pages_TimeManagement_EmployeeDutyPeriods_Create, L("CreateNewItem"));
        employeeDutyPeriods.CreateChildPermission(AppPermissions.Pages_TimeManagement_EmployeeDutyPeriods_Edit, L("EditItem"));
        employeeDutyPeriods.CreateChildPermission(AppPermissions.Pages_TimeManagement_EmployeeDutyPeriods_Delete, L("DeleteItem"));

        var employeeTimePolicies = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_EmployeeTimePolicies, L("EmployeeTimePolicies"));
        employeeTimePolicies.CreateChildPermission(AppPermissions.Pages_TimeManagement_EmployeeTimePolicies_Create, L("CreateNewItem"));
        employeeTimePolicies.CreateChildPermission(AppPermissions.Pages_TimeManagement_EmployeeTimePolicies_Edit, L("EditItem"));
        employeeTimePolicies.CreateChildPermission(AppPermissions.Pages_TimeManagement_EmployeeTimePolicies_Delete, L("DeleteItem"));

        var employeeSubstituteDuties = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_EmployeeSubstituteDuties, L("EmployeeSubstituteDuties"));
        employeeSubstituteDuties.CreateChildPermission(AppPermissions.Pages_TimeManagement_EmployeeSubstituteDuties_Create, L("CreateNewItem"));
        employeeSubstituteDuties.CreateChildPermission(AppPermissions.Pages_TimeManagement_EmployeeSubstituteDuties_Edit, L("EditItem"));
        employeeSubstituteDuties.CreateChildPermission(AppPermissions.Pages_TimeManagement_EmployeeSubstituteDuties_Delete, L("DeleteItem"));

        var employeeDutySchedules = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_EmployeeDutySchedules, L("EmployeeDutySchedules"));
        employeeDutySchedules.CreateChildPermission(AppPermissions.Pages_TimeManagement_EmployeeDutySchedules_Create, L("CreateNewItem"));
        employeeDutySchedules.CreateChildPermission(AppPermissions.Pages_TimeManagement_EmployeeDutySchedules_Edit, L("EditItem"));
        employeeDutySchedules.CreateChildPermission(AppPermissions.Pages_TimeManagement_EmployeeDutySchedules_Delete, L("DeleteItem"));

        var workLocationDutyPeriods = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_WorkLocationDutyPeriods, L("WorkLocationDutyPeriods"));
        workLocationDutyPeriods.CreateChildPermission(AppPermissions.Pages_TimeManagement_WorkLocationDutyPeriods_Create, L("CreateNewItem"));
        workLocationDutyPeriods.CreateChildPermission(AppPermissions.Pages_TimeManagement_WorkLocationDutyPeriods_Edit, L("EditItem"));
        workLocationDutyPeriods.CreateChildPermission(AppPermissions.Pages_TimeManagement_WorkLocationDutyPeriods_Delete, L("DeleteItem"));

        var employeeTimesheets = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_EmployeeTimesheets, L("EmployeeTimesheets"));
        employeeTimesheets.CreateChildPermission(AppPermissions.Pages_TimeManagement_EmployeeTimesheets_Create, L("CreateNewItem"));
        employeeTimesheets.CreateChildPermission(AppPermissions.Pages_TimeManagement_EmployeeTimesheets_Edit, L("EditItem"));
        employeeTimesheets.CreateChildPermission(AppPermissions.Pages_TimeManagement_EmployeeTimesheets_Delete, L("DeleteItem"));

        // Wave 4 (raw event/log data + calculated register data)
        var timeEvents = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_TimeEvents, L("TimeEvents"));
        timeEvents.CreateChildPermission(AppPermissions.Pages_TimeManagement_TimeEvents_Create, L("CreateNewItem"));
        timeEvents.CreateChildPermission(AppPermissions.Pages_TimeManagement_TimeEvents_Edit, L("EditItem"));
        timeEvents.CreateChildPermission(AppPermissions.Pages_TimeManagement_TimeEvents_Delete, L("DeleteItem"));

        var timeAttAPIs = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_TimeAttAPIs, L("TimeAttAPIs"));
        timeAttAPIs.CreateChildPermission(AppPermissions.Pages_TimeManagement_TimeAttAPIs_Create, L("CreateNewItem"));
        timeAttAPIs.CreateChildPermission(AppPermissions.Pages_TimeManagement_TimeAttAPIs_Edit, L("EditItem"));
        timeAttAPIs.CreateChildPermission(AppPermissions.Pages_TimeManagement_TimeAttAPIs_Delete, L("DeleteItem"));

        var timeLogs = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_TimeLogs, L("TimeLogs"));
        timeLogs.CreateChildPermission(AppPermissions.Pages_TimeManagement_TimeLogs_Create, L("CreateNewItem"));
        timeLogs.CreateChildPermission(AppPermissions.Pages_TimeManagement_TimeLogs_Edit, L("EditItem"));
        timeLogs.CreateChildPermission(AppPermissions.Pages_TimeManagement_TimeLogs_Delete, L("DeleteItem"));

        var timeRegisters = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_TimeRegisters, L("TimeRegisters"));
        timeRegisters.CreateChildPermission(AppPermissions.Pages_TimeManagement_TimeRegisters_Create, L("CreateNewItem"));
        timeRegisters.CreateChildPermission(AppPermissions.Pages_TimeManagement_TimeRegisters_Edit, L("EditItem"));
        timeRegisters.CreateChildPermission(AppPermissions.Pages_TimeManagement_TimeRegisters_Delete, L("DeleteItem"));

        var timeLogApprovals = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_TimeLogApprovals, L("TimeLogApprovals"));
        timeLogApprovals.CreateChildPermission(AppPermissions.Pages_TimeManagement_TimeLogApprovals_Create, L("CreateNewItem"));

        var monthlyTimeRegisters = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_MonthlyTimeRegisters, L("MonthlyTimeRegisters"));
        monthlyTimeRegisters.CreateChildPermission(AppPermissions.Pages_TimeManagement_MonthlyTimeRegisters_Create, L("CreateNewItem"));
        monthlyTimeRegisters.CreateChildPermission(AppPermissions.Pages_TimeManagement_MonthlyTimeRegisters_Edit, L("EditItem"));
        monthlyTimeRegisters.CreateChildPermission(AppPermissions.Pages_TimeManagement_MonthlyTimeRegisters_Delete, L("DeleteItem"));

        var monthlyTimeRegisterDetails = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_MonthlyTimeRegisterDetails, L("MonthlyTimeRegisterDetails"));
        monthlyTimeRegisterDetails.CreateChildPermission(AppPermissions.Pages_TimeManagement_MonthlyTimeRegisterDetails_Create, L("CreateNewItem"));
        monthlyTimeRegisterDetails.CreateChildPermission(AppPermissions.Pages_TimeManagement_MonthlyTimeRegisterDetails_Edit, L("EditItem"));
        monthlyTimeRegisterDetails.CreateChildPermission(AppPermissions.Pages_TimeManagement_MonthlyTimeRegisterDetails_Delete, L("DeleteItem"));

        var yearlyTimeRegisters = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_YearlyTimeRegisters, L("YearlyTimeRegisters"));
        yearlyTimeRegisters.CreateChildPermission(AppPermissions.Pages_TimeManagement_YearlyTimeRegisters_Create, L("CreateNewItem"));
        yearlyTimeRegisters.CreateChildPermission(AppPermissions.Pages_TimeManagement_YearlyTimeRegisters_Edit, L("EditItem"));
        yearlyTimeRegisters.CreateChildPermission(AppPermissions.Pages_TimeManagement_YearlyTimeRegisters_Delete, L("DeleteItem"));

        var yearlyTimeRegisterDetails = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_YearlyTimeRegisterDetails, L("YearlyTimeRegisterDetails"));
        yearlyTimeRegisterDetails.CreateChildPermission(AppPermissions.Pages_TimeManagement_YearlyTimeRegisterDetails_Create, L("CreateNewItem"));
        yearlyTimeRegisterDetails.CreateChildPermission(AppPermissions.Pages_TimeManagement_YearlyTimeRegisterDetails_Edit, L("EditItem"));
        yearlyTimeRegisterDetails.CreateChildPermission(AppPermissions.Pages_TimeManagement_YearlyTimeRegisterDetails_Delete, L("DeleteItem"));

        var simulationTimeRegisters = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_SimulationTimeRegisters, L("SimulationTimeRegisters"));
        simulationTimeRegisters.CreateChildPermission(AppPermissions.Pages_TimeManagement_SimulationTimeRegisters_Create, L("CreateNewItem"));
        simulationTimeRegisters.CreateChildPermission(AppPermissions.Pages_TimeManagement_SimulationTimeRegisters_Edit, L("EditItem"));
        simulationTimeRegisters.CreateChildPermission(AppPermissions.Pages_TimeManagement_SimulationTimeRegisters_Delete, L("DeleteItem"));

        var simulationTimeRegisterDetails = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_SimulationTimeRegisterDetails, L("SimulationTimeRegisterDetails"));
        simulationTimeRegisterDetails.CreateChildPermission(AppPermissions.Pages_TimeManagement_SimulationTimeRegisterDetails_Create, L("CreateNewItem"));
        simulationTimeRegisterDetails.CreateChildPermission(AppPermissions.Pages_TimeManagement_SimulationTimeRegisterDetails_Edit, L("EditItem"));
        simulationTimeRegisterDetails.CreateChildPermission(AppPermissions.Pages_TimeManagement_SimulationTimeRegisterDetails_Delete, L("DeleteItem"));

        var timeTransfers = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_TimeTransfers, L("TimeTransfers"));
        timeTransfers.CreateChildPermission(AppPermissions.Pages_TimeManagement_TimeTransfers_Create, L("CreateNewItem"));
        timeTransfers.CreateChildPermission(AppPermissions.Pages_TimeManagement_TimeTransfers_Edit, L("EditItem"));
        timeTransfers.CreateChildPermission(AppPermissions.Pages_TimeManagement_TimeTransfers_Delete, L("DeleteItem"));

        var timeEngine = timeManagement.CreateChildPermission(AppPermissions.Pages_TimeManagement_TimeEngine, L("TimeEngine"));
        timeEngine.CreateChildPermission(AppPermissions.Pages_TimeManagement_TimeEngine_Run, L("RunTimeEngine"));

        var payrollManagement = pages.CreateChildPermission(AppPermissions.Pages_PayrollManagement, L("PayrollManagement"));

        var costingFile = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_CostingFile, L("CostingFile"));
        costingFile.CreateChildPermission(AppPermissions.Pages_PayrollManagement_CostingFile_Run, L("RunCostEngine"));
        costingFile.CreateChildPermission(AppPermissions.Pages_PayrollManagement_CostingFile_Export, L("ExportCostingFile"));

        var postJv = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_PostJv, L("PostJv"));
        postJv.CreateChildPermission(AppPermissions.Pages_PayrollManagement_PostJv_Post, L("PostJournalVoucher"));

        var axBatches = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AXBatches, L("AXBatches"));
        axBatches.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AXBatches_Create, L("CreateNewItem"));
        axBatches.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AXBatches_Edit, L("EditItem"));
        axBatches.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AXBatches_Delete, L("DeleteItem"));

        var absenceTypes = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsenceTypes, L("AbsenceTypes"));
        absenceTypes.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsenceTypes_Create, L("CreateNewItem"));
        absenceTypes.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsenceTypes_Edit, L("EditItem"));
        absenceTypes.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsenceTypes_Delete, L("DeleteItem"));

        var absenceTypeExceptions = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsenceTypeExceptions, L("AbsenceTypeExceptions"));
        absenceTypeExceptions.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsenceTypeExceptions_Create, L("CreateNewItem"));
        absenceTypeExceptions.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsenceTypeExceptions_Edit, L("EditItem"));
        absenceTypeExceptions.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsenceTypeExceptions_Delete, L("DeleteItem"));

        var absenceDeductionRules = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsenceDeductionRules, L("AbsenceDeductionRules"));
        absenceDeductionRules.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsenceDeductionRules_Create, L("CreateNewItem"));
        absenceDeductionRules.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsenceDeductionRules_Edit, L("EditItem"));
        absenceDeductionRules.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsenceDeductionRules_Delete, L("DeleteItem"));

        var absenceDedRuleDetails = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsenceDedRuleDetails, L("AbsenceDedRuleDetails"));
        absenceDedRuleDetails.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsenceDedRuleDetails_Create, L("CreateNewItem"));
        absenceDedRuleDetails.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsenceDedRuleDetails_Edit, L("EditItem"));
        absenceDedRuleDetails.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsenceDedRuleDetails_Delete, L("DeleteItem"));

        var absenceViolationRules = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsenceViolationRules, L("AbsenceViolationRules"));
        absenceViolationRules.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsenceViolationRules_Create, L("CreateNewItem"));
        absenceViolationRules.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsenceViolationRules_Edit, L("EditItem"));
        absenceViolationRules.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsenceViolationRules_Delete, L("DeleteItem"));

        var absenceViolationRuleDetails = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsenceViolationRuleDetails, L("AbsenceViolationRuleDetails"));
        absenceViolationRuleDetails.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsenceViolationRuleDetails_Create, L("CreateNewItem"));
        absenceViolationRuleDetails.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsenceViolationRuleDetails_Edit, L("EditItem"));
        absenceViolationRuleDetails.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsenceViolationRuleDetails_Delete, L("DeleteItem"));

        var absenceViolationRuleExceptions = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsenceViolationRuleExceptions, L("AbsenceViolationRuleExceptions"));
        absenceViolationRuleExceptions.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsenceViolationRuleExceptions_Create, L("CreateNewItem"));
        absenceViolationRuleExceptions.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsenceViolationRuleExceptions_Edit, L("EditItem"));
        absenceViolationRuleExceptions.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsenceViolationRuleExceptions_Delete, L("DeleteItem"));

        var attendanceTypes = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AttendanceTypes, L("AttendanceTypes"));
        attendanceTypes.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AttendanceTypes_Create, L("CreateNewItem"));
        attendanceTypes.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AttendanceTypes_Edit, L("EditItem"));
        attendanceTypes.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AttendanceTypes_Delete, L("DeleteItem"));

        var accountTypes = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AccountTypes, L("AccountTypes"));
        accountTypes.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AccountTypes_Create, L("CreateNewItem"));
        accountTypes.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AccountTypes_Edit, L("EditItem"));
        accountTypes.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AccountTypes_Delete, L("DeleteItem"));

        var accountInfos = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AccountInfos, L("AccountInfos"));
        accountInfos.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AccountInfos_Create, L("CreateNewItem"));
        accountInfos.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AccountInfos_Edit, L("EditItem"));
        accountInfos.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AccountInfos_Delete, L("DeleteItem"));

        var employeeGroups = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeGroups, L("EmployeeGroups"));
        employeeGroups.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeGroups_Create, L("CreateNewItem"));
        employeeGroups.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeGroups_Edit, L("EditItem"));
        employeeGroups.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeGroups_Delete, L("DeleteItem"));

        var payrollReasonTypes = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_PayrollReasonTypes, L("PayrollReasonTypes"));
        payrollReasonTypes.CreateChildPermission(AppPermissions.Pages_PayrollManagement_PayrollReasonTypes_Create, L("CreateNewItem"));
        payrollReasonTypes.CreateChildPermission(AppPermissions.Pages_PayrollManagement_PayrollReasonTypes_Edit, L("EditItem"));
        payrollReasonTypes.CreateChildPermission(AppPermissions.Pages_PayrollManagement_PayrollReasonTypes_Delete, L("DeleteItem"));

        var employeeSubGroups = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroups, L("EmployeeSubGroups"));
        employeeSubGroups.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroups_Create, L("CreateNewItem"));
        employeeSubGroups.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroups_Edit, L("EditItem"));
        employeeSubGroups.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroups_Delete, L("DeleteItem"));

        var employeeSubGroupAbsences = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupAbsences, L("EmployeeSubGroupAbsences"));
        employeeSubGroupAbsences.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupAbsences_Create, L("CreateNewItem"));
        employeeSubGroupAbsences.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupAbsences_Edit, L("EditItem"));
        employeeSubGroupAbsences.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupAbsences_Delete, L("DeleteItem"));

        var employeeSubGroupAbsenceTypes = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupAbsenceTypes, L("EmployeeSubGroupAbsenceTypes"));
        employeeSubGroupAbsenceTypes.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupAbsenceTypes_Create, L("CreateNewItem"));
        employeeSubGroupAbsenceTypes.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupAbsenceTypes_Edit, L("EditItem"));
        employeeSubGroupAbsenceTypes.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupAbsenceTypes_Delete, L("DeleteItem"));

        var employeeSubGroupAssetAllowances = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupAssetAllowances, L("EmployeeSubGroupAssetAllowances"));
        employeeSubGroupAssetAllowances.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupAssetAllowances_Create, L("CreateNewItem"));
        employeeSubGroupAssetAllowances.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupAssetAllowances_Edit, L("EditItem"));
        employeeSubGroupAssetAllowances.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupAssetAllowances_Delete, L("DeleteItem"));

        var employeeSubGroupBenefits = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupBenefits, L("EmployeeSubGroupBenefits"));
        employeeSubGroupBenefits.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupBenefits_Create, L("CreateNewItem"));
        employeeSubGroupBenefits.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupBenefits_Edit, L("EditItem"));
        employeeSubGroupBenefits.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupBenefits_Delete, L("DeleteItem"));

        var employeeSubGroupBenefitDetails = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupBenefitDetails, L("EmployeeSubGroupBenefitDetails"));
        employeeSubGroupBenefitDetails.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupBenefitDetails_Create, L("CreateNewItem"));
        employeeSubGroupBenefitDetails.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupBenefitDetails_Edit, L("EditItem"));
        employeeSubGroupBenefitDetails.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupBenefitDetails_Delete, L("DeleteItem"));

        var employeeSubGroupBonuses = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupBonuses, L("EmployeeSubGroupBonuses"));
        employeeSubGroupBonuses.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupBonuses_Create, L("CreateNewItem"));
        employeeSubGroupBonuses.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupBonuses_Edit, L("EditItem"));
        employeeSubGroupBonuses.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupBonuses_Delete, L("DeleteItem"));

        var employeeSubGroupCommissions = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupCommissions, L("EmployeeSubGroupCommissions"));
        employeeSubGroupCommissions.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupCommissions_Create, L("CreateNewItem"));
        employeeSubGroupCommissions.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupCommissions_Edit, L("EditItem"));
        employeeSubGroupCommissions.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupCommissions_Delete, L("DeleteItem"));

        var employeeSubGroupFunds = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupFunds, L("EmployeeSubGroupFunds"));
        employeeSubGroupFunds.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupFunds_Create, L("CreateNewItem"));
        employeeSubGroupFunds.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupFunds_Edit, L("EditItem"));
        employeeSubGroupFunds.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupFunds_Delete, L("DeleteItem"));

        var employeeSubGroupGratuities = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupGratuities, L("EmployeeSubGroupGratuities"));
        employeeSubGroupGratuities.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupGratuities_Create, L("CreateNewItem"));
        employeeSubGroupGratuities.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupGratuities_Edit, L("EditItem"));
        employeeSubGroupGratuities.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupGratuities_Delete, L("DeleteItem"));

        var employeeSubGroupHealthReimburses = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupHealthReimburses, L("EmployeeSubGroupHealthReimburses"));
        employeeSubGroupHealthReimburses.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupHealthReimburses_Create, L("CreateNewItem"));
        employeeSubGroupHealthReimburses.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupHealthReimburses_Edit, L("EditItem"));
        employeeSubGroupHealthReimburses.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupHealthReimburses_Delete, L("DeleteItem"));

        var employeeSubGroupPayments = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupPayments, L("EmployeeSubGroupPayments"));
        employeeSubGroupPayments.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupPayments_Create, L("CreateNewItem"));
        employeeSubGroupPayments.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupPayments_Edit, L("EditItem"));
        employeeSubGroupPayments.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupPayments_Delete, L("DeleteItem"));

        var employeeSubGroupTickets = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupTickets, L("EmployeeSubGroupTickets"));
        employeeSubGroupTickets.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupTickets_Create, L("CreateNewItem"));
        employeeSubGroupTickets.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupTickets_Edit, L("EditItem"));
        employeeSubGroupTickets.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupTickets_Delete, L("DeleteItem"));

        var employeeSubGroupWageTypes = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupWageTypes, L("EmployeeSubGroupWageTypes"));
        employeeSubGroupWageTypes.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupWageTypes_Create, L("CreateNewItem"));
        employeeSubGroupWageTypes.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupWageTypes_Edit, L("EditItem"));
        employeeSubGroupWageTypes.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupWageTypes_Delete, L("DeleteItem"));

        var payrollPeriods = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_PayrollPeriods, L("PayrollPeriods"));
        payrollPeriods.CreateChildPermission(AppPermissions.Pages_PayrollManagement_PayrollPeriods_Create, L("CreateNewItem"));
        payrollPeriods.CreateChildPermission(AppPermissions.Pages_PayrollManagement_PayrollPeriods_Edit, L("EditItem"));
        payrollPeriods.CreateChildPermission(AppPermissions.Pages_PayrollManagement_PayrollPeriods_Delete, L("DeleteItem"));

        var payrollAreas = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_PayrollAreas, L("PayrollAreas"));
        payrollAreas.CreateChildPermission(AppPermissions.Pages_PayrollManagement_PayrollAreas_Create, L("CreateNewItem"));
        payrollAreas.CreateChildPermission(AppPermissions.Pages_PayrollManagement_PayrollAreas_Edit, L("EditItem"));
        payrollAreas.CreateChildPermission(AppPermissions.Pages_PayrollManagement_PayrollAreas_Delete, L("DeleteItem"));

        var payrollCalendars = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_PayrollCalendars, L("PayrollCalendars"));
        payrollCalendars.CreateChildPermission(AppPermissions.Pages_PayrollManagement_PayrollCalendars_Create, L("CreateNewItem"));
        payrollCalendars.CreateChildPermission(AppPermissions.Pages_PayrollManagement_PayrollCalendars_Edit, L("EditItem"));
        payrollCalendars.CreateChildPermission(AppPermissions.Pages_PayrollManagement_PayrollCalendars_Delete, L("DeleteItem"));

        var payrollControlRecords = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_PayrollControlRecords, L("PayrollControlRecords"));
        payrollControlRecords.CreateChildPermission(AppPermissions.Pages_PayrollManagement_PayrollControlRecords_Create, L("CreateNewItem"));
        payrollControlRecords.CreateChildPermission(AppPermissions.Pages_PayrollManagement_PayrollControlRecords_Edit, L("EditItem"));
        payrollControlRecords.CreateChildPermission(AppPermissions.Pages_PayrollManagement_PayrollControlRecords_Delete, L("DeleteItem"));

        var indemnityPayRules = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_IndemnityPayRules, L("IndemnityPayRules"));
        indemnityPayRules.CreateChildPermission(AppPermissions.Pages_PayrollManagement_IndemnityPayRules_Create, L("CreateNewItem"));
        indemnityPayRules.CreateChildPermission(AppPermissions.Pages_PayrollManagement_IndemnityPayRules_Edit, L("EditItem"));
        indemnityPayRules.CreateChildPermission(AppPermissions.Pages_PayrollManagement_IndemnityPayRules_Delete, L("DeleteItem"));

        var indemnityPayRuleDetails = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_IndemnityPayRuleDetails, L("IndemnityPayRuleDetails"));
        indemnityPayRuleDetails.CreateChildPermission(AppPermissions.Pages_PayrollManagement_IndemnityPayRuleDetails_Create, L("CreateNewItem"));
        indemnityPayRuleDetails.CreateChildPermission(AppPermissions.Pages_PayrollManagement_IndemnityPayRuleDetails_Edit, L("EditItem"));
        indemnityPayRuleDetails.CreateChildPermission(AppPermissions.Pages_PayrollManagement_IndemnityPayRuleDetails_Delete, L("DeleteItem"));

        var orgUnitWageAccGroups = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_OrgUnitWageAccGroups, L("OrgUnitWageAccGroups"));
        orgUnitWageAccGroups.CreateChildPermission(AppPermissions.Pages_PayrollManagement_OrgUnitWageAccGroups_Create, L("CreateNewItem"));
        orgUnitWageAccGroups.CreateChildPermission(AppPermissions.Pages_PayrollManagement_OrgUnitWageAccGroups_Edit, L("EditItem"));
        orgUnitWageAccGroups.CreateChildPermission(AppPermissions.Pages_PayrollManagement_OrgUnitWageAccGroups_Delete, L("DeleteItem"));

        var wageAccGroups = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_WageAccGroups, L("WageAccGroups"));
        wageAccGroups.CreateChildPermission(AppPermissions.Pages_PayrollManagement_WageAccGroups_Create, L("CreateNewItem"));
        wageAccGroups.CreateChildPermission(AppPermissions.Pages_PayrollManagement_WageAccGroups_Edit, L("EditItem"));
        wageAccGroups.CreateChildPermission(AppPermissions.Pages_PayrollManagement_WageAccGroups_Delete, L("DeleteItem"));

        var wageAccGroupDetails = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_WageAccGroupDetails, L("WageAccGroupDetails"));
        wageAccGroupDetails.CreateChildPermission(AppPermissions.Pages_PayrollManagement_WageAccGroupDetails_Create, L("CreateNewItem"));
        wageAccGroupDetails.CreateChildPermission(AppPermissions.Pages_PayrollManagement_WageAccGroupDetails_Edit, L("EditItem"));
        wageAccGroupDetails.CreateChildPermission(AppPermissions.Pages_PayrollManagement_WageAccGroupDetails_Delete, L("DeleteItem"));

        var wageTypes = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_WageTypes, L("WageTypes"));
        wageTypes.CreateChildPermission(AppPermissions.Pages_PayrollManagement_WageTypes_Create, L("CreateNewItem"));
        wageTypes.CreateChildPermission(AppPermissions.Pages_PayrollManagement_WageTypes_Edit, L("EditItem"));
        wageTypes.CreateChildPermission(AppPermissions.Pages_PayrollManagement_WageTypes_Delete, L("DeleteItem"));

        var wageTypeExceptions = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_WageTypeExceptions, L("WageTypeExceptions"));
        wageTypeExceptions.CreateChildPermission(AppPermissions.Pages_PayrollManagement_WageTypeExceptions_Create, L("CreateNewItem"));
        wageTypeExceptions.CreateChildPermission(AppPermissions.Pages_PayrollManagement_WageTypeExceptions_Edit, L("EditItem"));
        wageTypeExceptions.CreateChildPermission(AppPermissions.Pages_PayrollManagement_WageTypeExceptions_Delete, L("DeleteItem"));

        var payrollEngine = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_PayrollEngine, L("PayrollEngine"));
        payrollEngine.CreateChildPermission(AppPermissions.Pages_PayrollManagement_PayrollEngine_Run, L("RunPayrollEngine"));

        var wageTypeLeaves = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_WageTypeLeaves, L("WageTypeLeaves"));
        wageTypeLeaves.CreateChildPermission(AppPermissions.Pages_PayrollManagement_WageTypeLeaves_Create, L("CreateNewItem"));
        wageTypeLeaves.CreateChildPermission(AppPermissions.Pages_PayrollManagement_WageTypeLeaves_Edit, L("EditItem"));
        wageTypeLeaves.CreateChildPermission(AppPermissions.Pages_PayrollManagement_WageTypeLeaves_Delete, L("DeleteItem"));

        var wageTypeSecurityRoles = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_WageTypeSecurityRoles, L("WageTypeSecurityRoles"));
        wageTypeSecurityRoles.CreateChildPermission(AppPermissions.Pages_PayrollManagement_WageTypeSecurityRoles_Create, L("CreateNewItem"));
        wageTypeSecurityRoles.CreateChildPermission(AppPermissions.Pages_PayrollManagement_WageTypeSecurityRoles_Edit, L("EditItem"));
        wageTypeSecurityRoles.CreateChildPermission(AppPermissions.Pages_PayrollManagement_WageTypeSecurityRoles_Delete, L("DeleteItem"));

        var wageAccounts = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_WageAccounts, L("WageAccounts"));
        wageAccounts.CreateChildPermission(AppPermissions.Pages_PayrollManagement_WageAccounts_Create, L("CreateNewItem"));
        wageAccounts.CreateChildPermission(AppPermissions.Pages_PayrollManagement_WageAccounts_Edit, L("EditItem"));
        wageAccounts.CreateChildPermission(AppPermissions.Pages_PayrollManagement_WageAccounts_Delete, L("DeleteItem"));

        var socialSecurities = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SocialSecurities, L("SocialSecurities"));
        socialSecurities.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SocialSecurities_Create, L("CreateNewItem"));
        socialSecurities.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SocialSecurities_Edit, L("EditItem"));
        socialSecurities.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SocialSecurities_Delete, L("DeleteItem"));

        var socialSecurityDetails = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SocialSecurityDetails, L("SocialSecurityDetails"));
        socialSecurityDetails.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SocialSecurityDetails_Create, L("CreateNewItem"));
        socialSecurityDetails.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SocialSecurityDetails_Edit, L("EditItem"));
        socialSecurityDetails.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SocialSecurityDetails_Delete, L("DeleteItem"));

        var socialSecurityMaritalAllowances = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SocialSecurityMaritalAllowances, L("SocialSecurityMaritalAllowances"));
        socialSecurityMaritalAllowances.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SocialSecurityMaritalAllowances_Create, L("CreateNewItem"));
        socialSecurityMaritalAllowances.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SocialSecurityMaritalAllowances_Edit, L("EditItem"));
        socialSecurityMaritalAllowances.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SocialSecurityMaritalAllowances_Delete, L("DeleteItem"));

        var socialSecurityEducationAllowances = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SocialSecurityEducationAllowances, L("SocialSecurityEducationAllowances"));
        socialSecurityEducationAllowances.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SocialSecurityEducationAllowances_Create, L("CreateNewItem"));
        socialSecurityEducationAllowances.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SocialSecurityEducationAllowances_Edit, L("EditItem"));
        socialSecurityEducationAllowances.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SocialSecurityEducationAllowances_Delete, L("DeleteItem"));

        var socialSecurityRules = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SocialSecurityRules, L("SocialSecurityRules"));
        socialSecurityRules.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SocialSecurityRules_Create, L("CreateNewItem"));
        socialSecurityRules.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SocialSecurityRules_Edit, L("EditItem"));
        socialSecurityRules.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SocialSecurityRules_Delete, L("DeleteItem"));

        var ticketClasses = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_TicketClasses, L("TicketClasses"));
        ticketClasses.CreateChildPermission(AppPermissions.Pages_PayrollManagement_TicketClasses_Create, L("CreateNewItem"));
        ticketClasses.CreateChildPermission(AppPermissions.Pages_PayrollManagement_TicketClasses_Edit, L("EditItem"));
        ticketClasses.CreateChildPermission(AppPermissions.Pages_PayrollManagement_TicketClasses_Delete, L("DeleteItem"));

        var ticketPaxTypes = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_TicketPaxTypes, L("TicketPaxTypes"));
        ticketPaxTypes.CreateChildPermission(AppPermissions.Pages_PayrollManagement_TicketPaxTypes_Create, L("CreateNewItem"));
        ticketPaxTypes.CreateChildPermission(AppPermissions.Pages_PayrollManagement_TicketPaxTypes_Edit, L("EditItem"));
        ticketPaxTypes.CreateChildPermission(AppPermissions.Pages_PayrollManagement_TicketPaxTypes_Delete, L("DeleteItem"));

        var ticketRoutes = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_TicketRoutes, L("TicketRoutes"));
        ticketRoutes.CreateChildPermission(AppPermissions.Pages_PayrollManagement_TicketRoutes_Create, L("CreateNewItem"));
        ticketRoutes.CreateChildPermission(AppPermissions.Pages_PayrollManagement_TicketRoutes_Edit, L("EditItem"));
        ticketRoutes.CreateChildPermission(AppPermissions.Pages_PayrollManagement_TicketRoutes_Delete, L("DeleteItem"));

        var ticketFares = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_TicketFares, L("TicketFares"));
        ticketFares.CreateChildPermission(AppPermissions.Pages_PayrollManagement_TicketFares_Create, L("CreateNewItem"));
        ticketFares.CreateChildPermission(AppPermissions.Pages_PayrollManagement_TicketFares_Edit, L("EditItem"));
        ticketFares.CreateChildPermission(AppPermissions.Pages_PayrollManagement_TicketFares_Delete, L("DeleteItem"));

        var ticketTypes = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_TicketTypes, L("TicketTypes"));
        ticketTypes.CreateChildPermission(AppPermissions.Pages_PayrollManagement_TicketTypes_Create, L("CreateNewItem"));
        ticketTypes.CreateChildPermission(AppPermissions.Pages_PayrollManagement_TicketTypes_Edit, L("EditItem"));
        ticketTypes.CreateChildPermission(AppPermissions.Pages_PayrollManagement_TicketTypes_Delete, L("DeleteItem"));

        var employeeWageAccGroups = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeWageAccGroups, L("EmployeeWageAccGroups"));
        employeeWageAccGroups.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeWageAccGroups_Create, L("CreateNewItem"));
        employeeWageAccGroups.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeWageAccGroups_Edit, L("EditItem"));
        employeeWageAccGroups.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeWageAccGroups_Delete, L("DeleteItem"));

        var employeeWageAccounts = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeWageAccounts, L("EmployeeWageAccounts"));
        employeeWageAccounts.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeWageAccounts_Create, L("CreateNewItem"));
        employeeWageAccounts.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeWageAccounts_Edit, L("EditItem"));
        employeeWageAccounts.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeeWageAccounts_Delete, L("DeleteItem"));

        var employeePayrollStatuses = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeePayrollStatuses, L("EmployeePayrollStatuses"));
        employeePayrollStatuses.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeePayrollStatuses_Create, L("CreateNewItem"));
        employeePayrollStatuses.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeePayrollStatuses_Edit, L("EditItem"));
        employeePayrollStatuses.CreateChildPermission(AppPermissions.Pages_PayrollManagement_EmployeePayrollStatuses_Delete, L("DeleteItem"));

        var absencePayments = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsencePayments, L("AbsencePayments"));
        absencePayments.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsencePayments_Create, L("CreateNewItem"));
        absencePayments.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsencePayments_Edit, L("EditItem"));
        absencePayments.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsencePayments_Delete, L("DeleteItem"));

        var tempBenefitTypes = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_TempBenefitTypes, L("TempBenefitTypes"));
        tempBenefitTypes.CreateChildPermission(AppPermissions.Pages_PayrollManagement_TempBenefitTypes_Create, L("CreateNewItem"));
        tempBenefitTypes.CreateChildPermission(AppPermissions.Pages_PayrollManagement_TempBenefitTypes_Edit, L("EditItem"));
        tempBenefitTypes.CreateChildPermission(AppPermissions.Pages_PayrollManagement_TempBenefitTypes_Delete, L("DeleteItem"));

        var tempEmployeeBenefits = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_TempEmployeeBenefits, L("TempEmployeeBenefits"));
        tempEmployeeBenefits.CreateChildPermission(AppPermissions.Pages_PayrollManagement_TempEmployeeBenefits_Create, L("CreateNewItem"));
        tempEmployeeBenefits.CreateChildPermission(AppPermissions.Pages_PayrollManagement_TempEmployeeBenefits_Edit, L("EditItem"));
        tempEmployeeBenefits.CreateChildPermission(AppPermissions.Pages_PayrollManagement_TempEmployeeBenefits_Delete, L("DeleteItem"));

        var wageTypeRegisters = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_WageTypeRegisters, L("WageTypeRegisters"));
        wageTypeRegisters.CreateChildPermission(AppPermissions.Pages_PayrollManagement_WageTypeRegisters_Create, L("CreateNewItem"));
        wageTypeRegisters.CreateChildPermission(AppPermissions.Pages_PayrollManagement_WageTypeRegisters_Edit, L("EditItem"));
        wageTypeRegisters.CreateChildPermission(AppPermissions.Pages_PayrollManagement_WageTypeRegisters_Delete, L("DeleteItem"));

        var payrollLogs = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_PayrollLogs, L("PayrollLogs"));
        payrollLogs.CreateChildPermission(AppPermissions.Pages_PayrollManagement_PayrollLogs_Create, L("CreateNewItem"));
        payrollLogs.CreateChildPermission(AppPermissions.Pages_PayrollManagement_PayrollLogs_Edit, L("EditItem"));
        payrollLogs.CreateChildPermission(AppPermissions.Pages_PayrollManagement_PayrollLogs_Delete, L("DeleteItem"));

        var costRegisters = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_CostRegisters, L("CostRegisters"));
        costRegisters.CreateChildPermission(AppPermissions.Pages_PayrollManagement_CostRegisters_Create, L("CreateNewItem"));
        costRegisters.CreateChildPermission(AppPermissions.Pages_PayrollManagement_CostRegisters_Edit, L("EditItem"));
        costRegisters.CreateChildPermission(AppPermissions.Pages_PayrollManagement_CostRegisters_Delete, L("DeleteItem"));

        var costRegisterDetails = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_CostRegisterDetails, L("CostRegisterDetails"));
        costRegisterDetails.CreateChildPermission(AppPermissions.Pages_PayrollManagement_CostRegisterDetails_Create, L("CreateNewItem"));
        costRegisterDetails.CreateChildPermission(AppPermissions.Pages_PayrollManagement_CostRegisterDetails_Edit, L("EditItem"));
        costRegisterDetails.CreateChildPermission(AppPermissions.Pages_PayrollManagement_CostRegisterDetails_Delete, L("DeleteItem"));

        var payrollBankFiles = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_BankFiles, L("BankFiles"));
        payrollBankFiles.CreateChildPermission(AppPermissions.Pages_PayrollManagement_BankFiles_Generate, L("GeneratingBankFile"));

        var salaryRegisters = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SalaryRegisters, L("SalaryRegisters"));
        salaryRegisters.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SalaryRegisters_Create, L("CreateNewItem"));
        salaryRegisters.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SalaryRegisters_Edit, L("EditItem"));
        salaryRegisters.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SalaryRegisters_Delete, L("DeleteItem"));

        var salaryRegisterDetails = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SalaryRegisterDetails, L("SalaryRegisterDetails"));
        salaryRegisterDetails.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SalaryRegisterDetails_Create, L("CreateNewItem"));
        salaryRegisterDetails.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SalaryRegisterDetails_Edit, L("EditItem"));
        salaryRegisterDetails.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SalaryRegisterDetails_Delete, L("DeleteItem"));

        var salaryRegisterDaysDetails = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SalaryRegisterDaysDetails, L("SalaryRegisterDaysDetails"));
        salaryRegisterDaysDetails.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SalaryRegisterDaysDetails_Create, L("CreateNewItem"));
        salaryRegisterDaysDetails.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SalaryRegisterDaysDetails_Edit, L("EditItem"));
        salaryRegisterDaysDetails.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SalaryRegisterDaysDetails_Delete, L("DeleteItem"));

        var simulationSalaryRegisters = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SimulationSalaryRegisters, L("SimulationSalaryRegisters"));
        simulationSalaryRegisters.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SimulationSalaryRegisters_Create, L("CreateNewItem"));
        simulationSalaryRegisters.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SimulationSalaryRegisters_Edit, L("EditItem"));
        simulationSalaryRegisters.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SimulationSalaryRegisters_Delete, L("DeleteItem"));

        var simulationSalaryRegisterDetails = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SimulationSalaryRegisterDetails, L("SimulationSalaryRegisterDetails"));
        simulationSalaryRegisterDetails.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SimulationSalaryRegisterDetails_Create, L("CreateNewItem"));
        simulationSalaryRegisterDetails.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SimulationSalaryRegisterDetails_Edit, L("EditItem"));
        simulationSalaryRegisterDetails.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SimulationSalaryRegisterDetails_Delete, L("DeleteItem"));

        var simulationSalaryRegisterDaysDetails = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SimulationSalaryRegisterDaysDetails, L("SimulationSalaryRegisterDaysDetails"));
        simulationSalaryRegisterDaysDetails.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SimulationSalaryRegisterDaysDetails_Create, L("CreateNewItem"));
        simulationSalaryRegisterDaysDetails.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SimulationSalaryRegisterDaysDetails_Edit, L("EditItem"));
        simulationSalaryRegisterDaysDetails.CreateChildPermission(AppPermissions.Pages_PayrollManagement_SimulationSalaryRegisterDaysDetails_Delete, L("DeleteItem"));

        var absenceViolationActionRecords = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsenceViolationActionRecords, L("AbsenceViolationActionRecords"));
        absenceViolationActionRecords.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsenceViolationActionRecords_Create, L("CreateNewItem"));
        absenceViolationActionRecords.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsenceViolationActionRecords_Edit, L("EditItem"));
        absenceViolationActionRecords.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsenceViolationActionRecords_Delete, L("DeleteItem"));

        var absenceViolationActionLogs = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsenceViolationActionLogs, L("AbsenceViolationActionLogs"));
        absenceViolationActionLogs.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsenceViolationActionLogs_Create, L("CreateNewItem"));
        absenceViolationActionLogs.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsenceViolationActionLogs_Edit, L("EditItem"));
        absenceViolationActionLogs.CreateChildPermission(AppPermissions.Pages_PayrollManagement_AbsenceViolationActionLogs_Delete, L("DeleteItem"));

        var gratuityAccrualSummaries = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_GratuityAccrualSummaries, L("GratuityAccrualSummaries"));
        gratuityAccrualSummaries.CreateChildPermission(AppPermissions.Pages_PayrollManagement_GratuityAccrualSummaries_Create, L("CreateNewItem"));
        gratuityAccrualSummaries.CreateChildPermission(AppPermissions.Pages_PayrollManagement_GratuityAccrualSummaries_Edit, L("EditItem"));
        gratuityAccrualSummaries.CreateChildPermission(AppPermissions.Pages_PayrollManagement_GratuityAccrualSummaries_Delete, L("DeleteItem"));

        var leaveAccrualSummaries = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_LeaveAccrualSummaries, L("LeaveAccrualSummaries"));
        leaveAccrualSummaries.CreateChildPermission(AppPermissions.Pages_PayrollManagement_LeaveAccrualSummaries_Create, L("CreateNewItem"));
        leaveAccrualSummaries.CreateChildPermission(AppPermissions.Pages_PayrollManagement_LeaveAccrualSummaries_Edit, L("EditItem"));
        leaveAccrualSummaries.CreateChildPermission(AppPermissions.Pages_PayrollManagement_LeaveAccrualSummaries_Delete, L("DeleteItem"));

        var ticketAccrualSummaries = payrollManagement.CreateChildPermission(AppPermissions.Pages_PayrollManagement_TicketAccrualSummaries, L("TicketAccrualSummaries"));
        ticketAccrualSummaries.CreateChildPermission(AppPermissions.Pages_PayrollManagement_TicketAccrualSummaries_Create, L("CreateNewItem"));
        ticketAccrualSummaries.CreateChildPermission(AppPermissions.Pages_PayrollManagement_TicketAccrualSummaries_Edit, L("EditItem"));
        ticketAccrualSummaries.CreateChildPermission(AppPermissions.Pages_PayrollManagement_TicketAccrualSummaries_Delete, L("DeleteItem"));

        var projectManagement = pages.CreateChildPermission(AppPermissions.Pages_ProjectManagement, L("ProjectManagement"));

        var projectTypes = projectManagement.CreateChildPermission(AppPermissions.Pages_ProjectManagement_ProjectTypes, L("ProjectTypes"));
        projectTypes.CreateChildPermission(AppPermissions.Pages_ProjectManagement_ProjectTypes_Create, L("CreateNewItem"));
        projectTypes.CreateChildPermission(AppPermissions.Pages_ProjectManagement_ProjectTypes_Edit, L("EditItem"));
        projectTypes.CreateChildPermission(AppPermissions.Pages_ProjectManagement_ProjectTypes_Delete, L("DeleteItem"));

        var projects = projectManagement.CreateChildPermission(AppPermissions.Pages_ProjectManagement_Projects, L("Projects"));
        projects.CreateChildPermission(AppPermissions.Pages_ProjectManagement_Projects_Create, L("CreateNewItem"));
        projects.CreateChildPermission(AppPermissions.Pages_ProjectManagement_Projects_Edit, L("EditItem"));
        projects.CreateChildPermission(AppPermissions.Pages_ProjectManagement_Projects_Delete, L("DeleteItem"));

        var projectWageTypes = projectManagement.CreateChildPermission(AppPermissions.Pages_ProjectManagement_ProjectWageTypes, L("ProjectWageTypes"));
        projectWageTypes.CreateChildPermission(AppPermissions.Pages_ProjectManagement_ProjectWageTypes_Create, L("CreateNewItem"));
        projectWageTypes.CreateChildPermission(AppPermissions.Pages_ProjectManagement_ProjectWageTypes_Edit, L("EditItem"));
        projectWageTypes.CreateChildPermission(AppPermissions.Pages_ProjectManagement_ProjectWageTypes_Delete, L("DeleteItem"));

        var employeeProjects = projectManagement.CreateChildPermission(AppPermissions.Pages_ProjectManagement_EmployeeProjects, L("EmployeeProjects"));
        employeeProjects.CreateChildPermission(AppPermissions.Pages_ProjectManagement_EmployeeProjects_Create, L("CreateNewItem"));
        employeeProjects.CreateChildPermission(AppPermissions.Pages_ProjectManagement_EmployeeProjects_Edit, L("EditItem"));
        employeeProjects.CreateChildPermission(AppPermissions.Pages_ProjectManagement_EmployeeProjects_Delete, L("DeleteItem"));

        var employeeProjectDetails = projectManagement.CreateChildPermission(AppPermissions.Pages_ProjectManagement_EmployeeProjectDetails, L("EmployeeProjectDetails"));
        employeeProjectDetails.CreateChildPermission(AppPermissions.Pages_ProjectManagement_EmployeeProjectDetails_Create, L("CreateNewItem"));
        employeeProjectDetails.CreateChildPermission(AppPermissions.Pages_ProjectManagement_EmployeeProjectDetails_Edit, L("EditItem"));
        employeeProjectDetails.CreateChildPermission(AppPermissions.Pages_ProjectManagement_EmployeeProjectDetails_Delete, L("DeleteItem"));

        var projectRegisters = projectManagement.CreateChildPermission(AppPermissions.Pages_ProjectManagement_ProjectRegisters, L("ProjectRegisters"));
        projectRegisters.CreateChildPermission(AppPermissions.Pages_ProjectManagement_ProjectRegisters_Create, L("CreateNewItem"));
        projectRegisters.CreateChildPermission(AppPermissions.Pages_ProjectManagement_ProjectRegisters_Edit, L("EditItem"));
        projectRegisters.CreateChildPermission(AppPermissions.Pages_ProjectManagement_ProjectRegisters_Delete, L("DeleteItem"));

        var projectRegisterDetails = projectManagement.CreateChildPermission(AppPermissions.Pages_ProjectManagement_ProjectRegisterDetails, L("ProjectRegisterDetails"));
        projectRegisterDetails.CreateChildPermission(AppPermissions.Pages_ProjectManagement_ProjectRegisterDetails_Create, L("CreateNewItem"));
        projectRegisterDetails.CreateChildPermission(AppPermissions.Pages_ProjectManagement_ProjectRegisterDetails_Edit, L("EditItem"));
        projectRegisterDetails.CreateChildPermission(AppPermissions.Pages_ProjectManagement_ProjectRegisterDetails_Delete, L("DeleteItem"));

        var projectLogs = projectManagement.CreateChildPermission(AppPermissions.Pages_ProjectManagement_ProjectLogs, L("ProjectLogs"));
        projectLogs.CreateChildPermission(AppPermissions.Pages_ProjectManagement_ProjectLogs_Create, L("CreateNewItem"));
        projectLogs.CreateChildPermission(AppPermissions.Pages_ProjectManagement_ProjectLogs_Edit, L("EditItem"));
        projectLogs.CreateChildPermission(AppPermissions.Pages_ProjectManagement_ProjectLogs_Delete, L("DeleteItem"));

        var projectCostEngine = projectManagement.CreateChildPermission(AppPermissions.Pages_ProjectManagement_ProjectCostEngine, L("ProjectCostEngine"));
        projectCostEngine.CreateChildPermission(AppPermissions.Pages_ProjectManagement_ProjectCostEngine_Run, L("RunProjectCostEngine"));

        var travelManagement = pages.CreateChildPermission(AppPermissions.Pages_TravelManagement, L("TravelManagement"));

        var expenseTypes = travelManagement.CreateChildPermission(AppPermissions.Pages_TravelManagement_ExpenseTypes, L("ExpenseTypes"));
        expenseTypes.CreateChildPermission(AppPermissions.Pages_TravelManagement_ExpenseTypes_Create, L("CreateNewItem"));
        expenseTypes.CreateChildPermission(AppPermissions.Pages_TravelManagement_ExpenseTypes_Edit, L("EditItem"));
        expenseTypes.CreateChildPermission(AppPermissions.Pages_TravelManagement_ExpenseTypes_Delete, L("DeleteItem"));

        var hotels = travelManagement.CreateChildPermission(AppPermissions.Pages_TravelManagement_Hotels, L("Hotels"));
        hotels.CreateChildPermission(AppPermissions.Pages_TravelManagement_Hotels_Create, L("CreateNewItem"));
        hotels.CreateChildPermission(AppPermissions.Pages_TravelManagement_Hotels_Edit, L("EditItem"));
        hotels.CreateChildPermission(AppPermissions.Pages_TravelManagement_Hotels_Delete, L("DeleteItem"));

        var travelAirlines = travelManagement.CreateChildPermission(AppPermissions.Pages_TravelManagement_TravelAirlines, L("TravelAirlines"));
        travelAirlines.CreateChildPermission(AppPermissions.Pages_TravelManagement_TravelAirlines_Create, L("CreateNewItem"));
        travelAirlines.CreateChildPermission(AppPermissions.Pages_TravelManagement_TravelAirlines_Edit, L("EditItem"));
        travelAirlines.CreateChildPermission(AppPermissions.Pages_TravelManagement_TravelAirlines_Delete, L("DeleteItem"));

        var travelCancelReasons = travelManagement.CreateChildPermission(AppPermissions.Pages_TravelManagement_TravelCancelReasons, L("TravelCancelReasons"));
        travelCancelReasons.CreateChildPermission(AppPermissions.Pages_TravelManagement_TravelCancelReasons_Create, L("CreateNewItem"));
        travelCancelReasons.CreateChildPermission(AppPermissions.Pages_TravelManagement_TravelCancelReasons_Edit, L("EditItem"));
        travelCancelReasons.CreateChildPermission(AppPermissions.Pages_TravelManagement_TravelCancelReasons_Delete, L("DeleteItem"));

        var travelDestinations = travelManagement.CreateChildPermission(AppPermissions.Pages_TravelManagement_TravelDestinations, L("TravelDestinations"));
        travelDestinations.CreateChildPermission(AppPermissions.Pages_TravelManagement_TravelDestinations_Create, L("CreateNewItem"));
        travelDestinations.CreateChildPermission(AppPermissions.Pages_TravelManagement_TravelDestinations_Edit, L("EditItem"));
        travelDestinations.CreateChildPermission(AppPermissions.Pages_TravelManagement_TravelDestinations_Delete, L("DeleteItem"));

        var travelTypes = travelManagement.CreateChildPermission(AppPermissions.Pages_TravelManagement_TravelTypes, L("TravelTypes"));
        travelTypes.CreateChildPermission(AppPermissions.Pages_TravelManagement_TravelTypes_Create, L("CreateNewItem"));
        travelTypes.CreateChildPermission(AppPermissions.Pages_TravelManagement_TravelTypes_Edit, L("EditItem"));
        travelTypes.CreateChildPermission(AppPermissions.Pages_TravelManagement_TravelTypes_Delete, L("DeleteItem"));

        var travelPerDiems = travelManagement.CreateChildPermission(AppPermissions.Pages_TravelManagement_TravelPerDiems, L("TravelPerDiems"));
        travelPerDiems.CreateChildPermission(AppPermissions.Pages_TravelManagement_TravelPerDiems_Create, L("CreateNewItem"));
        travelPerDiems.CreateChildPermission(AppPermissions.Pages_TravelManagement_TravelPerDiems_Edit, L("EditItem"));
        travelPerDiems.CreateChildPermission(AppPermissions.Pages_TravelManagement_TravelPerDiems_Delete, L("DeleteItem"));

        var travels = travelManagement.CreateChildPermission(AppPermissions.Pages_TravelManagement_Travels, L("Travels"));
        travels.CreateChildPermission(AppPermissions.Pages_TravelManagement_Travels_Create, L("CreateNewItem"));
        travels.CreateChildPermission(AppPermissions.Pages_TravelManagement_Travels_Edit, L("EditItem"));
        travels.CreateChildPermission(AppPermissions.Pages_TravelManagement_Travels_Delete, L("DeleteItem"));

        var travelDetails = travelManagement.CreateChildPermission(AppPermissions.Pages_TravelManagement_TravelDetails, L("TravelDetails"));
        travelDetails.CreateChildPermission(AppPermissions.Pages_TravelManagement_TravelDetails_Create, L("CreateNewItem"));
        travelDetails.CreateChildPermission(AppPermissions.Pages_TravelManagement_TravelDetails_Edit, L("EditItem"));
        travelDetails.CreateChildPermission(AppPermissions.Pages_TravelManagement_TravelDetails_Delete, L("DeleteItem"));

        var travelDetailCosts = travelManagement.CreateChildPermission(AppPermissions.Pages_TravelManagement_TravelDetailCosts, L("TravelDetailCosts"));
        travelDetailCosts.CreateChildPermission(AppPermissions.Pages_TravelManagement_TravelDetailCosts_Create, L("CreateNewItem"));
        travelDetailCosts.CreateChildPermission(AppPermissions.Pages_TravelManagement_TravelDetailCosts_Edit, L("EditItem"));
        travelDetailCosts.CreateChildPermission(AppPermissions.Pages_TravelManagement_TravelDetailCosts_Delete, L("DeleteItem"));

        var travelFeedbacks = travelManagement.CreateChildPermission(AppPermissions.Pages_TravelManagement_TravelFeedbacks, L("TravelFeedbacks"));
        travelFeedbacks.CreateChildPermission(AppPermissions.Pages_TravelManagement_TravelFeedbacks_Create, L("CreateNewItem"));
        travelFeedbacks.CreateChildPermission(AppPermissions.Pages_TravelManagement_TravelFeedbacks_Edit, L("EditItem"));
        travelFeedbacks.CreateChildPermission(AppPermissions.Pages_TravelManagement_TravelFeedbacks_Delete, L("DeleteItem"));

        var govtRelationsManagement = pages.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement, L("GovtRelationsManagement"));

        var businessTypes = govtRelationsManagement.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_BusinessTypes, L("BusinessTypes"));
        businessTypes.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_BusinessTypes_Create, L("CreateNewItem"));
        businessTypes.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_BusinessTypes_Edit, L("EditItem"));
        businessTypes.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_BusinessTypes_Delete, L("DeleteItem"));

        var govtJobCategories = govtRelationsManagement.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_GovtJobCategories, L("GovtJobCategories"));
        govtJobCategories.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_GovtJobCategories_Create, L("CreateNewItem"));
        govtJobCategories.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_GovtJobCategories_Edit, L("EditItem"));
        govtJobCategories.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_GovtJobCategories_Delete, L("DeleteItem"));

        var govtJobTitles = govtRelationsManagement.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_GovtJobTitles, L("GovtJobTitles"));
        govtJobTitles.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_GovtJobTitles_Create, L("CreateNewItem"));
        govtJobTitles.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_GovtJobTitles_Edit, L("EditItem"));
        govtJobTitles.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_GovtJobTitles_Delete, L("DeleteItem"));

        var signatories = govtRelationsManagement.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_Signatories, L("Signatories"));
        signatories.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_Signatories_Create, L("CreateNewItem"));
        signatories.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_Signatories_Edit, L("EditItem"));
        signatories.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_Signatories_Delete, L("DeleteItem"));

        var sponsors = govtRelationsManagement.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_Sponsors, L("Sponsors"));
        sponsors.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_Sponsors_Create, L("CreateNewItem"));
        sponsors.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_Sponsors_Edit, L("EditItem"));
        sponsors.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_Sponsors_Delete, L("DeleteItem"));

        var sponsorJobTitles = govtRelationsManagement.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_SponsorJobTitles, L("SponsorJobTitles"));
        sponsorJobTitles.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_SponsorJobTitles_Create, L("CreateNewItem"));
        sponsorJobTitles.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_SponsorJobTitles_Edit, L("EditItem"));
        sponsorJobTitles.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_SponsorJobTitles_Delete, L("DeleteItem"));

        var sponsorSignatories = govtRelationsManagement.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_SponsorSignatories, L("SponsorSignatories"));
        sponsorSignatories.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_SponsorSignatories_Create, L("CreateNewItem"));
        sponsorSignatories.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_SponsorSignatories_Edit, L("EditItem"));
        sponsorSignatories.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_SponsorSignatories_Delete, L("DeleteItem"));

        var sponsorGovtRepresentatives = govtRelationsManagement.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_SponsorGovtRepresentatives, L("SponsorGovtRepresentatives"));
        sponsorGovtRepresentatives.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_SponsorGovtRepresentatives_Create, L("CreateNewItem"));
        sponsorGovtRepresentatives.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_SponsorGovtRepresentatives_Edit, L("EditItem"));
        sponsorGovtRepresentatives.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_SponsorGovtRepresentatives_Delete, L("DeleteItem"));

        var sponsorLicenseInfos = govtRelationsManagement.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_SponsorLicenseInfos, L("SponsorLicenseInfos"));
        sponsorLicenseInfos.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_SponsorLicenseInfos_Create, L("CreateNewItem"));
        sponsorLicenseInfos.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_SponsorLicenseInfos_Edit, L("EditItem"));
        sponsorLicenseInfos.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_SponsorLicenseInfos_Delete, L("DeleteItem"));

        var sponsorBanks = govtRelationsManagement.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_SponsorBanks, L("SponsorBanks"));
        sponsorBanks.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_SponsorBanks_Create, L("CreateNewItem"));
        sponsorBanks.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_SponsorBanks_Edit, L("EditItem"));
        sponsorBanks.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_SponsorBanks_Delete, L("DeleteItem"));

        var organizationDocuments = govtRelationsManagement.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_OrganizationDocuments, L("OrganizationDocuments"));
        organizationDocuments.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_OrganizationDocuments_Create, L("CreateNewItem"));
        organizationDocuments.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_OrganizationDocuments_Edit, L("EditItem"));
        organizationDocuments.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_OrganizationDocuments_Delete, L("DeleteItem"));

        var employeeExitPermits = govtRelationsManagement.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_EmployeeExitPermits, L("EmployeeExitPermits"));
        employeeExitPermits.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_EmployeeExitPermits_Create, L("CreateNewItem"));
        employeeExitPermits.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_EmployeeExitPermits_Edit, L("EditItem"));
        employeeExitPermits.CreateChildPermission(AppPermissions.Pages_GovtRelationsManagement_EmployeeExitPermits_Delete, L("DeleteItem"));

        var accommodationManagement = pages.CreateChildPermission(AppPermissions.Pages_AccommodationManagement, L("AccommodationManagement"));

        var locations = accommodationManagement.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_Locations, L("Locations"));
        locations.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_Locations_Create, L("CreateNewItem"));
        locations.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_Locations_Edit, L("EditItem"));
        locations.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_Locations_Delete, L("DeleteItem"));

        var buildings = accommodationManagement.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_Buildings, L("Buildings"));
        buildings.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_Buildings_Create, L("CreateNewItem"));
        buildings.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_Buildings_Edit, L("EditItem"));
        buildings.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_Buildings_Delete, L("DeleteItem"));

        var buildingContracts = accommodationManagement.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_BuildingContracts, L("BuildingContracts"));
        buildingContracts.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_BuildingContracts_Create, L("CreateNewItem"));
        buildingContracts.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_BuildingContracts_Edit, L("EditItem"));
        buildingContracts.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_BuildingContracts_Delete, L("DeleteItem"));

        var floors = accommodationManagement.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_Floors, L("Floors"));
        floors.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_Floors_Create, L("CreateNewItem"));
        floors.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_Floors_Edit, L("EditItem"));
        floors.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_Floors_Delete, L("DeleteItem"));

        var flats = accommodationManagement.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_Flats, L("Flats"));
        flats.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_Flats_Create, L("CreateNewItem"));
        flats.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_Flats_Edit, L("EditItem"));
        flats.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_Flats_Delete, L("DeleteItem"));

        var rooms = accommodationManagement.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_Rooms, L("Rooms"));
        rooms.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_Rooms_Create, L("CreateNewItem"));
        rooms.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_Rooms_Edit, L("EditItem"));
        rooms.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_Rooms_Delete, L("DeleteItem"));

        var beds = accommodationManagement.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_Beds, L("Beds"));
        beds.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_Beds_Create, L("CreateNewItem"));
        beds.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_Beds_Edit, L("EditItem"));
        beds.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_Beds_Delete, L("DeleteItem"));

        var accommodationTypes = accommodationManagement.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_AccommodationTypes, L("AccommodationTypes"));
        accommodationTypes.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_AccommodationTypes_Create, L("CreateNewItem"));
        accommodationTypes.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_AccommodationTypes_Edit, L("EditItem"));
        accommodationTypes.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_AccommodationTypes_Delete, L("DeleteItem"));

        var accommodationServiceTypes = accommodationManagement.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_AccommodationServiceTypes, L("AccommodationServiceTypes"));
        accommodationServiceTypes.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_AccommodationServiceTypes_Create, L("CreateNewItem"));
        accommodationServiceTypes.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_AccommodationServiceTypes_Edit, L("EditItem"));
        accommodationServiceTypes.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_AccommodationServiceTypes_Delete, L("DeleteItem"));

        var employeeAccommodations = accommodationManagement.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_EmployeeAccommodations, L("EmployeeAccommodations"));
        employeeAccommodations.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_EmployeeAccommodations_Create, L("CreateNewItem"));
        employeeAccommodations.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_EmployeeAccommodations_Edit, L("EditItem"));
        employeeAccommodations.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_EmployeeAccommodations_Delete, L("DeleteItem"));

        var accommodationServices = accommodationManagement.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_AccommodationServices, L("AccommodationServices"));
        accommodationServices.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_AccommodationServices_Create, L("CreateNewItem"));
        accommodationServices.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_AccommodationServices_Edit, L("EditItem"));
        accommodationServices.CreateChildPermission(AppPermissions.Pages_AccommodationManagement_AccommodationServices_Delete, L("DeleteItem"));

        var gatePassManagement = pages.CreateChildPermission(AppPermissions.Pages_GatePassManagement, L("GatePassManagement"));

        var gatePasses = gatePassManagement.CreateChildPermission(AppPermissions.Pages_GatePassManagement_GatePasses, L("GatePasses"));
        gatePasses.CreateChildPermission(AppPermissions.Pages_GatePassManagement_GatePasses_Create, L("CreateNewItem"));
        gatePasses.CreateChildPermission(AppPermissions.Pages_GatePassManagement_GatePasses_Edit, L("EditItem"));
        gatePasses.CreateChildPermission(AppPermissions.Pages_GatePassManagement_GatePasses_Delete, L("DeleteItem"));

        var gatePassMaterials = gatePassManagement.CreateChildPermission(AppPermissions.Pages_GatePassManagement_GatePassMaterials, L("GatePassMaterials"));
        gatePassMaterials.CreateChildPermission(AppPermissions.Pages_GatePassManagement_GatePassMaterials_Create, L("CreateNewItem"));
        gatePassMaterials.CreateChildPermission(AppPermissions.Pages_GatePassManagement_GatePassMaterials_Edit, L("EditItem"));
        gatePassMaterials.CreateChildPermission(AppPermissions.Pages_GatePassManagement_GatePassMaterials_Delete, L("DeleteItem"));

        var gatePassPersons = gatePassManagement.CreateChildPermission(AppPermissions.Pages_GatePassManagement_GatePassPersons, L("GatePassPersons"));
        gatePassPersons.CreateChildPermission(AppPermissions.Pages_GatePassManagement_GatePassPersons_Create, L("CreateNewItem"));
        gatePassPersons.CreateChildPermission(AppPermissions.Pages_GatePassManagement_GatePassPersons_Edit, L("EditItem"));
        gatePassPersons.CreateChildPermission(AppPermissions.Pages_GatePassManagement_GatePassPersons_Delete, L("DeleteItem"));

        var gatePassVehicles = gatePassManagement.CreateChildPermission(AppPermissions.Pages_GatePassManagement_GatePassVehicles, L("GatePassVehicles"));
        gatePassVehicles.CreateChildPermission(AppPermissions.Pages_GatePassManagement_GatePassVehicles_Create, L("CreateNewItem"));
        gatePassVehicles.CreateChildPermission(AppPermissions.Pages_GatePassManagement_GatePassVehicles_Edit, L("EditItem"));
        gatePassVehicles.CreateChildPermission(AppPermissions.Pages_GatePassManagement_GatePassVehicles_Delete, L("DeleteItem"));

        var gatePassTypes = gatePassManagement.CreateChildPermission(AppPermissions.Pages_GatePassManagement_GatePassTypes, L("GatePassTypes"));
        gatePassTypes.CreateChildPermission(AppPermissions.Pages_GatePassManagement_GatePassTypes_Create, L("CreateNewItem"));
        gatePassTypes.CreateChildPermission(AppPermissions.Pages_GatePassManagement_GatePassTypes_Edit, L("EditItem"));
        gatePassTypes.CreateChildPermission(AppPermissions.Pages_GatePassManagement_GatePassTypes_Delete, L("DeleteItem"));

        var gatePassMovements = gatePassManagement.CreateChildPermission(AppPermissions.Pages_GatePassManagement_GatePassMovements, L("GatePassMovements"));
        gatePassMovements.CreateChildPermission(AppPermissions.Pages_GatePassManagement_GatePassMovements_Create, L("CreateNewItem"));
        gatePassMovements.CreateChildPermission(AppPermissions.Pages_GatePassManagement_GatePassMovements_Edit, L("EditItem"));
        gatePassMovements.CreateChildPermission(AppPermissions.Pages_GatePassManagement_GatePassMovements_Delete, L("DeleteItem"));

        var gatePassStatuses = gatePassManagement.CreateChildPermission(AppPermissions.Pages_GatePassManagement_GatePassStatuses, L("GatePassStatuses"));
        gatePassStatuses.CreateChildPermission(AppPermissions.Pages_GatePassManagement_GatePassStatuses_Create, L("CreateNewItem"));
        gatePassStatuses.CreateChildPermission(AppPermissions.Pages_GatePassManagement_GatePassStatuses_Edit, L("EditItem"));
        gatePassStatuses.CreateChildPermission(AppPermissions.Pages_GatePassManagement_GatePassStatuses_Delete, L("DeleteItem"));

        var gatePassVendors = gatePassManagement.CreateChildPermission(AppPermissions.Pages_GatePassManagement_GatePassVendors, L("GatePassVendors"));
        gatePassVendors.CreateChildPermission(AppPermissions.Pages_GatePassManagement_GatePassVendors_Create, L("CreateNewItem"));
        gatePassVendors.CreateChildPermission(AppPermissions.Pages_GatePassManagement_GatePassVendors_Edit, L("EditItem"));
        gatePassVendors.CreateChildPermission(AppPermissions.Pages_GatePassManagement_GatePassVendors_Delete, L("DeleteItem"));

        var gatePassPersonTypes = gatePassManagement.CreateChildPermission(AppPermissions.Pages_GatePassManagement_PersonTypes, L("PersonTypes"));
        gatePassPersonTypes.CreateChildPermission(AppPermissions.Pages_GatePassManagement_PersonTypes_Create, L("CreateNewItem"));
        gatePassPersonTypes.CreateChildPermission(AppPermissions.Pages_GatePassManagement_PersonTypes_Edit, L("EditItem"));
        gatePassPersonTypes.CreateChildPermission(AppPermissions.Pages_GatePassManagement_PersonTypes_Delete, L("DeleteItem"));

        var gatePassVehicleTypes = gatePassManagement.CreateChildPermission(AppPermissions.Pages_GatePassManagement_VehicleTypes, L("VehicleTypes"));
        gatePassVehicleTypes.CreateChildPermission(AppPermissions.Pages_GatePassManagement_VehicleTypes_Create, L("CreateNewItem"));
        gatePassVehicleTypes.CreateChildPermission(AppPermissions.Pages_GatePassManagement_VehicleTypes_Edit, L("EditItem"));
        gatePassVehicleTypes.CreateChildPermission(AppPermissions.Pages_GatePassManagement_VehicleTypes_Delete, L("DeleteItem"));

        var gatePassVehicleBrands = gatePassManagement.CreateChildPermission(AppPermissions.Pages_GatePassManagement_VehicleBrands, L("VehicleBrands"));
        gatePassVehicleBrands.CreateChildPermission(AppPermissions.Pages_GatePassManagement_VehicleBrands_Create, L("CreateNewItem"));
        gatePassVehicleBrands.CreateChildPermission(AppPermissions.Pages_GatePassManagement_VehicleBrands_Edit, L("EditItem"));
        gatePassVehicleBrands.CreateChildPermission(AppPermissions.Pages_GatePassManagement_VehicleBrands_Delete, L("DeleteItem"));

        var gatePassVehicleModels = gatePassManagement.CreateChildPermission(AppPermissions.Pages_GatePassManagement_VehicleModels, L("VehicleModels"));
        gatePassVehicleModels.CreateChildPermission(AppPermissions.Pages_GatePassManagement_VehicleModels_Create, L("CreateNewItem"));
        gatePassVehicleModels.CreateChildPermission(AppPermissions.Pages_GatePassManagement_VehicleModels_Edit, L("EditItem"));
        gatePassVehicleModels.CreateChildPermission(AppPermissions.Pages_GatePassManagement_VehicleModels_Delete, L("DeleteItem"));

        var poolCars = gatePassManagement.CreateChildPermission(AppPermissions.Pages_GatePassManagement_PoolCars, L("PoolCars"));
        poolCars.CreateChildPermission(AppPermissions.Pages_GatePassManagement_PoolCars_Create, L("CreateNewItem"));
        poolCars.CreateChildPermission(AppPermissions.Pages_GatePassManagement_PoolCars_Edit, L("EditItem"));
        poolCars.CreateChildPermission(AppPermissions.Pages_GatePassManagement_PoolCars_Delete, L("DeleteItem"));

        var employeePoolCars = gatePassManagement.CreateChildPermission(AppPermissions.Pages_GatePassManagement_EmployeePoolCars, L("EmployeePoolCars"));
        employeePoolCars.CreateChildPermission(AppPermissions.Pages_GatePassManagement_EmployeePoolCars_Create, L("CreateNewItem"));
        employeePoolCars.CreateChildPermission(AppPermissions.Pages_GatePassManagement_EmployeePoolCars_Edit, L("EditItem"));
        employeePoolCars.CreateChildPermission(AppPermissions.Pages_GatePassManagement_EmployeePoolCars_Delete, L("DeleteItem"));

        var performanceManagement = pages.CreateChildPermission(AppPermissions.Pages_PerformanceManagement, L("PerformanceManagement"));

        var formTypes = performanceManagement.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_FormTypes, L("FormTypes"));
        formTypes.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_FormTypes_Create, L("CreateNewItem"));
        formTypes.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_FormTypes_Edit, L("EditItem"));
        formTypes.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_FormTypes_Delete, L("DeleteItem"));

        var kpiCategories = performanceManagement.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_KpiCategories, L("KpiCategories"));
        kpiCategories.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_KpiCategories_Create, L("CreateNewItem"));
        kpiCategories.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_KpiCategories_Edit, L("EditItem"));
        kpiCategories.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_KpiCategories_Delete, L("DeleteItem"));

        var kpiInfos = performanceManagement.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_KpiInfos, L("KpiInfos"));
        kpiInfos.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_KpiInfos_Create, L("CreateNewItem"));
        kpiInfos.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_KpiInfos_Edit, L("EditItem"));
        kpiInfos.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_KpiInfos_Delete, L("DeleteItem"));

        var paRatings = performanceManagement.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_PARatings, L("PARatings"));
        paRatings.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_PARatings_Create, L("CreateNewItem"));
        paRatings.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_PARatings_Edit, L("EditItem"));
        paRatings.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_PARatings_Delete, L("DeleteItem"));

        var paCycles = performanceManagement.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_PACycles, L("PACycles"));
        paCycles.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_PACycles_Create, L("CreateNewItem"));
        paCycles.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_PACycles_Edit, L("EditItem"));
        paCycles.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_PACycles_Delete, L("DeleteItem"));

        var paPeriods = performanceManagement.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_PAPeriods, L("PAPeriods"));
        paPeriods.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_PAPeriods_Create, L("CreateNewItem"));
        paPeriods.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_PAPeriods_Edit, L("EditItem"));
        paPeriods.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_PAPeriods_Delete, L("DeleteItem"));

        var paTimelines = performanceManagement.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_PATimelines, L("PATimelines"));
        paTimelines.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_PATimelines_Create, L("CreateNewItem"));
        paTimelines.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_PATimelines_Edit, L("EditItem"));
        paTimelines.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_PATimelines_Delete, L("DeleteItem"));

        var paTimelineExceptions = performanceManagement.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_PATimelineExceptions, L("PATimelineExceptions"));
        paTimelineExceptions.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_PATimelineExceptions_Create, L("CreateNewItem"));
        paTimelineExceptions.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_PATimelineExceptions_Edit, L("EditItem"));
        paTimelineExceptions.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_PATimelineExceptions_Delete, L("DeleteItem"));

        var paPeerNominees = performanceManagement.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_PAPeerNominees, L("PAPeerNominees"));
        paPeerNominees.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_PAPeerNominees_Create, L("CreateNewItem"));
        paPeerNominees.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_PAPeerNominees_Edit, L("EditItem"));
        paPeerNominees.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_PAPeerNominees_Delete, L("DeleteItem"));

        var kpiByJobTitles = performanceManagement.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_KpiByJobTitles, L("KpiByJobTitles"));
        kpiByJobTitles.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_KpiByJobTitles_Create, L("CreateNewItem"));
        kpiByJobTitles.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_KpiByJobTitles_Edit, L("EditItem"));
        kpiByJobTitles.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_KpiByJobTitles_Delete, L("DeleteItem"));

        var kpiByOrgUnits = performanceManagement.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_KpiByOrgUnits, L("KpiByOrgUnits"));
        kpiByOrgUnits.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_KpiByOrgUnits_Create, L("CreateNewItem"));
        kpiByOrgUnits.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_KpiByOrgUnits_Edit, L("EditItem"));
        kpiByOrgUnits.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_KpiByOrgUnits_Delete, L("DeleteItem"));

        var paForms = performanceManagement.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_PAForms, L("PAForms"));
        paForms.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_PAForms_Create, L("CreateNewItem"));
        paForms.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_PAForms_Edit, L("EditItem"));
        paForms.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_PAForms_Delete, L("DeleteItem"));

        var paEmployees = performanceManagement.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_PAEmployees, L("PAEmployees"));
        paEmployees.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_PAEmployees_Create, L("CreateNewItem"));
        paEmployees.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_PAEmployees_Edit, L("EditItem"));
        paEmployees.CreateChildPermission(AppPermissions.Pages_PerformanceManagement_PAEmployees_Delete, L("DeleteItem"));

        var successionManagement = pages.CreateChildPermission(AppPermissions.Pages_SuccessionManagement, L("SuccessionManagement"));

        var impactOfLosses = successionManagement.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_ImpactOfLosses, L("ImpactOfLosses"));
        impactOfLosses.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_ImpactOfLosses_Create, L("CreateNewItem"));
        impactOfLosses.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_ImpactOfLosses_Edit, L("EditItem"));
        impactOfLosses.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_ImpactOfLosses_Delete, L("DeleteItem"));

        var nineBoxRatings = successionManagement.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_NineBoxRatings, L("NineBoxRatings"));
        nineBoxRatings.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_NineBoxRatings_Create, L("CreateNewItem"));
        nineBoxRatings.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_NineBoxRatings_Edit, L("EditItem"));
        nineBoxRatings.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_NineBoxRatings_Delete, L("DeleteItem"));

        var rankings = successionManagement.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_Rankings, L("Rankings"));
        rankings.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_Rankings_Create, L("CreateNewItem"));
        rankings.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_Rankings_Edit, L("EditItem"));
        rankings.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_Rankings_Delete, L("DeleteItem"));

        var readinesses = successionManagement.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_Readinesses, L("Readinesses"));
        readinesses.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_Readinesses_Create, L("CreateNewItem"));
        readinesses.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_Readinesses_Edit, L("EditItem"));
        readinesses.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_Readinesses_Delete, L("DeleteItem"));

        var reasonForLeavings = successionManagement.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_ReasonForLeavings, L("ReasonForLeavings"));
        reasonForLeavings.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_ReasonForLeavings_Create, L("CreateNewItem"));
        reasonForLeavings.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_ReasonForLeavings_Edit, L("EditItem"));
        reasonForLeavings.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_ReasonForLeavings_Delete, L("DeleteItem"));

        var riskOfLosses = successionManagement.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_RiskOfLosses, L("RiskOfLosses"));
        riskOfLosses.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_RiskOfLosses_Create, L("CreateNewItem"));
        riskOfLosses.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_RiskOfLosses_Edit, L("EditItem"));
        riskOfLosses.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_RiskOfLosses_Delete, L("DeleteItem"));

        var successionRatings = successionManagement.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_SuccessionRatings, L("SuccessionRatings"));
        successionRatings.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_SuccessionRatings_Create, L("CreateNewItem"));
        successionRatings.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_SuccessionRatings_Edit, L("EditItem"));
        successionRatings.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_SuccessionRatings_Delete, L("DeleteItem"));

        var talentPools = successionManagement.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_TalentPools, L("TalentPools"));
        talentPools.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_TalentPools_Create, L("CreateNewItem"));
        talentPools.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_TalentPools_Edit, L("EditItem"));
        talentPools.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_TalentPools_Delete, L("DeleteItem"));

        var nineBoxMappings = successionManagement.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_NineBoxMappings, L("NineBoxMappings"));
        nineBoxMappings.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_NineBoxMappings_Create, L("CreateNewItem"));
        nineBoxMappings.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_NineBoxMappings_Edit, L("EditItem"));
        nineBoxMappings.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_NineBoxMappings_Delete, L("DeleteItem"));

        var empPerformanceHistories = successionManagement.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_EmpPerformanceHistories, L("EmpPerformanceHistories"));
        empPerformanceHistories.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_EmpPerformanceHistories_Create, L("CreateNewItem"));
        empPerformanceHistories.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_EmpPerformanceHistories_Edit, L("EditItem"));
        empPerformanceHistories.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_EmpPerformanceHistories_Delete, L("DeleteItem"));

        var empPotentialAssessments = successionManagement.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_EmpPotentialAssessments, L("EmpPotentialAssessments"));
        empPotentialAssessments.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_EmpPotentialAssessments_Create, L("CreateNewItem"));
        empPotentialAssessments.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_EmpPotentialAssessments_Edit, L("EditItem"));
        empPotentialAssessments.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_EmpPotentialAssessments_Delete, L("DeleteItem"));

        var empTalentMatrixDatas = successionManagement.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_EmpTalentMatrixDatas, L("EmpTalentMatrixDatas"));
        empTalentMatrixDatas.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_EmpTalentMatrixDatas_Create, L("CreateNewItem"));
        empTalentMatrixDatas.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_EmpTalentMatrixDatas_Edit, L("EditItem"));
        empTalentMatrixDatas.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_EmpTalentMatrixDatas_Delete, L("DeleteItem"));

        var employeeTalentFlags = successionManagement.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_EmployeeTalentFlags, L("EmployeeTalentFlags"));
        employeeTalentFlags.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_EmployeeTalentFlags_Create, L("CreateNewItem"));
        employeeTalentFlags.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_EmployeeTalentFlags_Edit, L("EditItem"));
        employeeTalentFlags.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_EmployeeTalentFlags_Delete, L("DeleteItem"));

        var successorNominees = successionManagement.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_SuccessorNominees, L("SuccessorNominees"));
        successorNominees.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_SuccessorNominees_Create, L("CreateNewItem"));
        successorNominees.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_SuccessorNominees_Edit, L("EditItem"));
        successorNominees.CreateChildPermission(AppPermissions.Pages_SuccessionManagement_SuccessorNominees_Delete, L("DeleteItem"));

        var manpowerPlanningManagement = pages.CreateChildPermission(AppPermissions.Pages_ManpowerPlanningManagement, L("ManpowerPlanningManagement"));

        var manpowerPlanningPeriods = manpowerPlanningManagement.CreateChildPermission(AppPermissions.Pages_ManpowerPlanningManagement_ManpowerPlanningPeriods, L("ManpowerPlanningPeriods"));
        manpowerPlanningPeriods.CreateChildPermission(AppPermissions.Pages_ManpowerPlanningManagement_ManpowerPlanningPeriods_Create, L("CreateNewItem"));
        manpowerPlanningPeriods.CreateChildPermission(AppPermissions.Pages_ManpowerPlanningManagement_ManpowerPlanningPeriods_Edit, L("EditItem"));
        manpowerPlanningPeriods.CreateChildPermission(AppPermissions.Pages_ManpowerPlanningManagement_ManpowerPlanningPeriods_Delete, L("DeleteItem"));

        var manpowerPlannings = manpowerPlanningManagement.CreateChildPermission(AppPermissions.Pages_ManpowerPlanningManagement_ManpowerPlannings, L("ManpowerPlannings"));
        manpowerPlannings.CreateChildPermission(AppPermissions.Pages_ManpowerPlanningManagement_ManpowerPlannings_Create, L("CreateNewItem"));
        manpowerPlannings.CreateChildPermission(AppPermissions.Pages_ManpowerPlanningManagement_ManpowerPlannings_Edit, L("EditItem"));
        manpowerPlannings.CreateChildPermission(AppPermissions.Pages_ManpowerPlanningManagement_ManpowerPlannings_Delete, L("DeleteItem"));

        var manpowerPlanningDetails = manpowerPlanningManagement.CreateChildPermission(AppPermissions.Pages_ManpowerPlanningManagement_ManpowerPlanningDetails, L("ManpowerPlanningDetails"));
        manpowerPlanningDetails.CreateChildPermission(AppPermissions.Pages_ManpowerPlanningManagement_ManpowerPlanningDetails_Create, L("CreateNewItem"));
        manpowerPlanningDetails.CreateChildPermission(AppPermissions.Pages_ManpowerPlanningManagement_ManpowerPlanningDetails_Edit, L("EditItem"));
        manpowerPlanningDetails.CreateChildPermission(AppPermissions.Pages_ManpowerPlanningManagement_ManpowerPlanningDetails_Delete, L("DeleteItem"));

        var positions = manpowerPlanningManagement.CreateChildPermission(AppPermissions.Pages_ManpowerPlanningManagement_Positions, L("Positions"));
        positions.CreateChildPermission(AppPermissions.Pages_ManpowerPlanningManagement_Positions_Create, L("CreateNewItem"));
        positions.CreateChildPermission(AppPermissions.Pages_ManpowerPlanningManagement_Positions_Edit, L("EditItem"));
        positions.CreateChildPermission(AppPermissions.Pages_ManpowerPlanningManagement_Positions_Delete, L("DeleteItem"));

        var positionRelationships = manpowerPlanningManagement.CreateChildPermission(AppPermissions.Pages_ManpowerPlanningManagement_PositionRelationships, L("PositionRelationships"));
        positionRelationships.CreateChildPermission(AppPermissions.Pages_ManpowerPlanningManagement_PositionRelationships_Create, L("CreateNewItem"));
        positionRelationships.CreateChildPermission(AppPermissions.Pages_ManpowerPlanningManagement_PositionRelationships_Edit, L("EditItem"));
        positionRelationships.CreateChildPermission(AppPermissions.Pages_ManpowerPlanningManagement_PositionRelationships_Delete, L("DeleteItem"));

        var workflow = pages.CreateChildPermission(AppPermissions.Pages_Workflow, L("Workflow"));

        var workflowRoles = workflow.CreateChildPermission(AppPermissions.Pages_Workflow_WorkflowRoles, L("WorkflowRoles"));
        workflowRoles.CreateChildPermission(AppPermissions.Pages_Workflow_WorkflowRoles_Create, L("CreateNewItem"));
        workflowRoles.CreateChildPermission(AppPermissions.Pages_Workflow_WorkflowRoles_Edit, L("EditItem"));
        workflowRoles.CreateChildPermission(AppPermissions.Pages_Workflow_WorkflowRoles_Delete, L("DeleteItem"));

        var employeeWorkflowRoles = workflow.CreateChildPermission(AppPermissions.Pages_Workflow_EmployeeWorkflowRoles, L("EmployeeWorkflowRoles"));
        employeeWorkflowRoles.CreateChildPermission(AppPermissions.Pages_Workflow_EmployeeWorkflowRoles_Create, L("CreateNewItem"));
        employeeWorkflowRoles.CreateChildPermission(AppPermissions.Pages_Workflow_EmployeeWorkflowRoles_Edit, L("EditItem"));
        employeeWorkflowRoles.CreateChildPermission(AppPermissions.Pages_Workflow_EmployeeWorkflowRoles_Delete, L("DeleteItem"));

        var workflowGroups = workflow.CreateChildPermission(AppPermissions.Pages_Workflow_WorkflowGroups, L("WorkflowGroups"));
        workflowGroups.CreateChildPermission(AppPermissions.Pages_Workflow_WorkflowGroups_Create, L("CreateNewItem"));
        workflowGroups.CreateChildPermission(AppPermissions.Pages_Workflow_WorkflowGroups_Edit, L("EditItem"));
        workflowGroups.CreateChildPermission(AppPermissions.Pages_Workflow_WorkflowGroups_Delete, L("DeleteItem"));

        var workflowGroupDetails = workflow.CreateChildPermission(AppPermissions.Pages_Workflow_WorkflowGroupDetails, L("WorkflowGroupDetails"));
        workflowGroupDetails.CreateChildPermission(AppPermissions.Pages_Workflow_WorkflowGroupDetails_Create, L("CreateNewItem"));
        workflowGroupDetails.CreateChildPermission(AppPermissions.Pages_Workflow_WorkflowGroupDetails_Edit, L("EditItem"));
        workflowGroupDetails.CreateChildPermission(AppPermissions.Pages_Workflow_WorkflowGroupDetails_Delete, L("DeleteItem"));

        var workflowGroupFieldAccesses = workflow.CreateChildPermission(AppPermissions.Pages_Workflow_WorkflowGroupFieldAccesses, L("WorkflowGroupFieldAccesses"));
        workflowGroupFieldAccesses.CreateChildPermission(AppPermissions.Pages_Workflow_WorkflowGroupFieldAccesses_Create, L("CreateNewItem"));
        workflowGroupFieldAccesses.CreateChildPermission(AppPermissions.Pages_Workflow_WorkflowGroupFieldAccesses_Edit, L("EditItem"));
        workflowGroupFieldAccesses.CreateChildPermission(AppPermissions.Pages_Workflow_WorkflowGroupFieldAccesses_Delete, L("DeleteItem"));

        var requestMasters = workflow.CreateChildPermission(AppPermissions.Pages_Workflow_RequestMasters, L("RequestMasters"));
        requestMasters.CreateChildPermission(AppPermissions.Pages_Workflow_RequestMasters_Create, L("CreateNewItem"));
        requestMasters.CreateChildPermission(AppPermissions.Pages_Workflow_RequestMasters_Edit, L("EditItem"));
        requestMasters.CreateChildPermission(AppPermissions.Pages_Workflow_RequestMasters_Delete, L("DeleteItem"));

        var workflowTransactionTypes = workflow.CreateChildPermission(AppPermissions.Pages_Workflow_WorkflowTransactionTypes, L("WorkflowTransactionTypes"));
        workflowTransactionTypes.CreateChildPermission(AppPermissions.Pages_Workflow_WorkflowTransactionTypes_Create, L("CreateNewItem"));
        workflowTransactionTypes.CreateChildPermission(AppPermissions.Pages_Workflow_WorkflowTransactionTypes_Edit, L("EditItem"));
        workflowTransactionTypes.CreateChildPermission(AppPermissions.Pages_Workflow_WorkflowTransactionTypes_Delete, L("DeleteItem"));

        var requestDetails = workflow.CreateChildPermission(AppPermissions.Pages_Workflow_RequestDetails, L("RequestDetails"));
        requestDetails.CreateChildPermission(AppPermissions.Pages_Workflow_RequestDetails_Create, L("CreateNewItem"));
        requestDetails.CreateChildPermission(AppPermissions.Pages_Workflow_RequestDetails_Edit, L("EditItem"));
        requestDetails.CreateChildPermission(AppPermissions.Pages_Workflow_RequestDetails_Delete, L("DeleteItem"));

        var requestDetailParallels = workflow.CreateChildPermission(AppPermissions.Pages_Workflow_RequestDetailParallels, L("RequestDetailParallels"));
        requestDetailParallels.CreateChildPermission(AppPermissions.Pages_Workflow_RequestDetailParallels_Create, L("CreateNewItem"));
        requestDetailParallels.CreateChildPermission(AppPermissions.Pages_Workflow_RequestDetailParallels_Edit, L("EditItem"));
        requestDetailParallels.CreateChildPermission(AppPermissions.Pages_Workflow_RequestDetailParallels_Delete, L("DeleteItem"));

        var workflowRequests = workflow.CreateChildPermission(AppPermissions.Pages_Workflow_WorkflowRequests, L("WorkflowRequests"));
        workflowRequests.CreateChildPermission(AppPermissions.Pages_Workflow_WorkflowRequests_Create, L("CreateNewItem"));
        workflowRequests.CreateChildPermission(AppPermissions.Pages_Workflow_WorkflowRequests_Edit, L("EditItem"));
        workflowRequests.CreateChildPermission(AppPermissions.Pages_Workflow_WorkflowRequests_Delete, L("DeleteItem"));
        workflowRequests.CreateChildPermission(AppPermissions.Pages_Workflow_WorkflowRequests_Submit, L("SubmittingWorkflowRequest"));
        workflowRequests.CreateChildPermission(AppPermissions.Pages_Workflow_WorkflowRequests_Approve, L("ApprovingWorkflowRequest"));
        workflowRequests.CreateChildPermission(AppPermissions.Pages_Workflow_WorkflowRequests_Reject, L("RejectingWorkflowRequest"));
        workflowRequests.CreateChildPermission(AppPermissions.Pages_Workflow_WorkflowRequests_SendBack, L("SendingBackWorkflowRequest"));
        workflowRequests.CreateChildPermission(AppPermissions.Pages_Workflow_WorkflowRequests_Reassign, L("ReassigningWorkflowRequest"));

        var requestLogs = workflow.CreateChildPermission(AppPermissions.Pages_Workflow_RequestLogs, L("RequestLogs"));
        requestLogs.CreateChildPermission(AppPermissions.Pages_Workflow_RequestLogs_Create, L("CreateNewItem"));
        requestLogs.CreateChildPermission(AppPermissions.Pages_Workflow_RequestLogs_Delete, L("DeleteItem"));

        var workFlowApprovers = workflow.CreateChildPermission(AppPermissions.Pages_Workflow_WorkFlowApprovers, L("WorkFlowApprovers"));
        workFlowApprovers.CreateChildPermission(AppPermissions.Pages_Workflow_WorkFlowApprovers_Create, L("CreateNewItem"));
        workFlowApprovers.CreateChildPermission(AppPermissions.Pages_Workflow_WorkFlowApprovers_Edit, L("EditItem"));
        workFlowApprovers.CreateChildPermission(AppPermissions.Pages_Workflow_WorkFlowApprovers_Delete, L("DeleteItem"));

        var workflowRemarks = workflow.CreateChildPermission(AppPermissions.Pages_Workflow_WorkflowRemarks, L("WorkflowRemarks"));
        workflowRemarks.CreateChildPermission(AppPermissions.Pages_Workflow_WorkflowRemarks_Create, L("CreateNewItem"));
        workflowRemarks.CreateChildPermission(AppPermissions.Pages_Workflow_WorkflowRemarks_Edit, L("EditItem"));
        workflowRemarks.CreateChildPermission(AppPermissions.Pages_Workflow_WorkflowRemarks_Delete, L("DeleteItem"));

        var wfReadLogs = workflow.CreateChildPermission(AppPermissions.Pages_Workflow_WFReadLogs, L("WFReadLogs"));
        wfReadLogs.CreateChildPermission(AppPermissions.Pages_Workflow_WFReadLogs_Create, L("CreateNewItem"));
        wfReadLogs.CreateChildPermission(AppPermissions.Pages_Workflow_WFReadLogs_Delete, L("DeleteItem"));

        var reportManagement = pages.CreateChildPermission(AppPermissions.Pages_ReportManagement, L("ReportManagement"));

        var domainTypes = reportManagement.CreateChildPermission(AppPermissions.Pages_ReportManagement_DomainTypes, L("DomainTypes"));
        domainTypes.CreateChildPermission(AppPermissions.Pages_ReportManagement_DomainTypes_Create, L("CreateNewItem"));
        domainTypes.CreateChildPermission(AppPermissions.Pages_ReportManagement_DomainTypes_Edit, L("EditItem"));
        domainTypes.CreateChildPermission(AppPermissions.Pages_ReportManagement_DomainTypes_Delete, L("DeleteItem"));

        var reportTypes = reportManagement.CreateChildPermission(AppPermissions.Pages_ReportManagement_ReportTypes, L("ReportTypes"));
        reportTypes.CreateChildPermission(AppPermissions.Pages_ReportManagement_ReportTypes_Create, L("CreateNewItem"));
        reportTypes.CreateChildPermission(AppPermissions.Pages_ReportManagement_ReportTypes_Edit, L("EditItem"));
        reportTypes.CreateChildPermission(AppPermissions.Pages_ReportManagement_ReportTypes_Delete, L("DeleteItem"));

        var printReports = reportManagement.CreateChildPermission(AppPermissions.Pages_ReportManagement_PrintReports, L("PrintReports"));
        printReports.CreateChildPermission(AppPermissions.Pages_ReportManagement_PrintReports_Create, L("CreateNewItem"));
        printReports.CreateChildPermission(AppPermissions.Pages_ReportManagement_PrintReports_Edit, L("EditItem"));
        printReports.CreateChildPermission(AppPermissions.Pages_ReportManagement_PrintReports_Delete, L("DeleteItem"));

        var printReportPermissions = reportManagement.CreateChildPermission(AppPermissions.Pages_ReportManagement_PrintReportPermissions, L("PrintReportPermissions"));
        printReportPermissions.CreateChildPermission(AppPermissions.Pages_ReportManagement_PrintReportPermissions_Create, L("CreateNewItem"));
        printReportPermissions.CreateChildPermission(AppPermissions.Pages_ReportManagement_PrintReportPermissions_Edit, L("EditItem"));
        printReportPermissions.CreateChildPermission(AppPermissions.Pages_ReportManagement_PrintReportPermissions_Delete, L("DeleteItem"));

        var printReportPermissionDetails = reportManagement.CreateChildPermission(AppPermissions.Pages_ReportManagement_PrintReportPermissionDetails, L("PrintReportPermissionDetails"));
        printReportPermissionDetails.CreateChildPermission(AppPermissions.Pages_ReportManagement_PrintReportPermissionDetails_Create, L("CreateNewItem"));
        printReportPermissionDetails.CreateChildPermission(AppPermissions.Pages_ReportManagement_PrintReportPermissionDetails_Edit, L("EditItem"));
        printReportPermissionDetails.CreateChildPermission(AppPermissions.Pages_ReportManagement_PrintReportPermissionDetails_Delete, L("DeleteItem"));

        var reportMasters = reportManagement.CreateChildPermission(AppPermissions.Pages_ReportManagement_ReportMasters, L("ReportMasters"));
        reportMasters.CreateChildPermission(AppPermissions.Pages_ReportManagement_ReportMasters_Create, L("CreateNewItem"));
        reportMasters.CreateChildPermission(AppPermissions.Pages_ReportManagement_ReportMasters_Edit, L("EditItem"));
        reportMasters.CreateChildPermission(AppPermissions.Pages_ReportManagement_ReportMasters_Delete, L("DeleteItem"));

        var reportDetails = reportManagement.CreateChildPermission(AppPermissions.Pages_ReportManagement_ReportDetails, L("ReportDetails"));
        reportDetails.CreateChildPermission(AppPermissions.Pages_ReportManagement_ReportDetails_Create, L("CreateNewItem"));
        reportDetails.CreateChildPermission(AppPermissions.Pages_ReportManagement_ReportDetails_Edit, L("EditItem"));
        reportDetails.CreateChildPermission(AppPermissions.Pages_ReportManagement_ReportDetails_Delete, L("DeleteItem"));

        var reportSettings = reportManagement.CreateChildPermission(AppPermissions.Pages_ReportManagement_ReportSettings, L("ReportSettings"));
        reportSettings.CreateChildPermission(AppPermissions.Pages_ReportManagement_ReportSettings_Create, L("CreateNewItem"));
        reportSettings.CreateChildPermission(AppPermissions.Pages_ReportManagement_ReportSettings_Edit, L("EditItem"));
        reportSettings.CreateChildPermission(AppPermissions.Pages_ReportManagement_ReportSettings_Delete, L("DeleteItem"));

        var reportSettingDetails = reportManagement.CreateChildPermission(AppPermissions.Pages_ReportManagement_ReportSettingDetails, L("ReportSettingDetails"));
        reportSettingDetails.CreateChildPermission(AppPermissions.Pages_ReportManagement_ReportSettingDetails_Create, L("CreateNewItem"));
        reportSettingDetails.CreateChildPermission(AppPermissions.Pages_ReportManagement_ReportSettingDetails_Edit, L("EditItem"));
        reportSettingDetails.CreateChildPermission(AppPermissions.Pages_ReportManagement_ReportSettingDetails_Delete, L("DeleteItem"));

        var reportInfos = reportManagement.CreateChildPermission(AppPermissions.Pages_ReportManagement_ReportInfos, L("ReportInfos"));
        reportInfos.CreateChildPermission(AppPermissions.Pages_ReportManagement_ReportInfos_Create, L("CreateNewItem"));
        reportInfos.CreateChildPermission(AppPermissions.Pages_ReportManagement_ReportInfos_Edit, L("EditItem"));
        reportInfos.CreateChildPermission(AppPermissions.Pages_ReportManagement_ReportInfos_Delete, L("DeleteItem"));

        var reportInfoDetails = reportManagement.CreateChildPermission(AppPermissions.Pages_ReportManagement_ReportInfoDetails, L("ReportInfoDetails"));
        reportInfoDetails.CreateChildPermission(AppPermissions.Pages_ReportManagement_ReportInfoDetails_Create, L("CreateNewItem"));
        reportInfoDetails.CreateChildPermission(AppPermissions.Pages_ReportManagement_ReportInfoDetails_Edit, L("EditItem"));
        reportInfoDetails.CreateChildPermission(AppPermissions.Pages_ReportManagement_ReportInfoDetails_Delete, L("DeleteItem"));

        var competencyDevelopment = pages.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment, L("CompetencyDevelopment"));

        var competencyTypes = competencyDevelopment.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_CompetencyTypes, L("CompetencyTypes"));
        competencyTypes.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_CompetencyTypes_Create, L("CreateNewItem"));
        competencyTypes.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_CompetencyTypes_Edit, L("EditItem"));
        competencyTypes.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_CompetencyTypes_Delete, L("DeleteItem"));

        var competencies = competencyDevelopment.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_Competencies, L("Competencies"));
        competencies.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_Competencies_Create, L("CreateNewItem"));
        competencies.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_Competencies_Edit, L("EditItem"));
        competencies.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_Competencies_Delete, L("DeleteItem"));

        var competencyByJobTitles = competencyDevelopment.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_CompetencyByJobTitles, L("CompetencyByJobTitles"));
        competencyByJobTitles.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_CompetencyByJobTitles_Create, L("CreateNewItem"));
        competencyByJobTitles.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_CompetencyByJobTitles_Edit, L("EditItem"));
        competencyByJobTitles.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_CompetencyByJobTitles_Delete, L("DeleteItem"));

        var competencyByJobTitleDetails = competencyDevelopment.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_CompetencyByJobTitleDetails, L("CompetencyByJobTitleDetails"));
        competencyByJobTitleDetails.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_CompetencyByJobTitleDetails_Create, L("CreateNewItem"));
        competencyByJobTitleDetails.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_CompetencyByJobTitleDetails_Edit, L("EditItem"));
        competencyByJobTitleDetails.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_CompetencyByJobTitleDetails_Delete, L("DeleteItem"));

        var cdPeriods = competencyDevelopment.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_CDPeriods, L("CDPeriods"));
        cdPeriods.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_CDPeriods_Create, L("CreateNewItem"));
        cdPeriods.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_CDPeriods_Edit, L("EditItem"));
        cdPeriods.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_CDPeriods_Delete, L("DeleteItem"));

        var cdForms = competencyDevelopment.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_CDForms, L("CDForms"));
        cdForms.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_CDForms_Create, L("CreateNewItem"));
        cdForms.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_CDForms_Edit, L("EditItem"));
        cdForms.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_CDForms_Delete, L("DeleteItem"));

        var cdPlans = competencyDevelopment.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_CDPlans, L("CDPlans"));
        cdPlans.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_CDPlans_Create, L("CreateNewItem"));
        cdPlans.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_CDPlans_Edit, L("EditItem"));
        cdPlans.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_CDPlans_Delete, L("DeleteItem"));

        var cdMidYearReviews = competencyDevelopment.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_CDMidYearReviews, L("CDMidYearReviews"));
        cdMidYearReviews.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_CDMidYearReviews_Create, L("CreateNewItem"));
        cdMidYearReviews.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_CDMidYearReviews_Edit, L("EditItem"));
        cdMidYearReviews.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_CDMidYearReviews_Delete, L("DeleteItem"));

        var cdAnnualYearReviews = competencyDevelopment.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_CDAnnualYearReviews, L("CDAnnualYearReviews"));
        cdAnnualYearReviews.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_CDAnnualYearReviews_Create, L("CreateNewItem"));
        cdAnnualYearReviews.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_CDAnnualYearReviews_Edit, L("EditItem"));
        cdAnnualYearReviews.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_CDAnnualYearReviews_Delete, L("DeleteItem"));

        var cdRatings = competencyDevelopment.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_CDRatings, L("CDRatings"));
        cdRatings.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_CDRatings_Create, L("CreateNewItem"));
        cdRatings.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_CDRatings_Edit, L("EditItem"));
        cdRatings.CreateChildPermission(AppPermissions.Pages_CompetencyDevelopment_CDRatings_Delete, L("DeleteItem"));

        var recruitment = pages.CreateChildPermission(AppPermissions.Pages_Recruitment, L("Recruitment"));

        var applicants = recruitment.CreateChildPermission(AppPermissions.Pages_Recruitment_Applicants, L("Applicants"));
        applicants.CreateChildPermission(AppPermissions.Pages_Recruitment_Applicants_Create, L("CreateNewItem"));
        applicants.CreateChildPermission(AppPermissions.Pages_Recruitment_Applicants_Edit, L("EditItem"));
        applicants.CreateChildPermission(AppPermissions.Pages_Recruitment_Applicants_Delete, L("DeleteItem"));

        var applicantCVs = recruitment.CreateChildPermission(AppPermissions.Pages_Recruitment_ApplicantCVs, L("ApplicantCVs"));
        applicantCVs.CreateChildPermission(AppPermissions.Pages_Recruitment_ApplicantCVs_Create, L("CreateNewItem"));
        applicantCVs.CreateChildPermission(AppPermissions.Pages_Recruitment_ApplicantCVs_Edit, L("EditItem"));
        applicantCVs.CreateChildPermission(AppPermissions.Pages_Recruitment_ApplicantCVs_Delete, L("DeleteItem"));

        var cvQualifications = recruitment.CreateChildPermission(AppPermissions.Pages_Recruitment_CVQualifications, L("CVQualifications"));
        cvQualifications.CreateChildPermission(AppPermissions.Pages_Recruitment_CVQualifications_Create, L("CreateNewItem"));
        cvQualifications.CreateChildPermission(AppPermissions.Pages_Recruitment_CVQualifications_Edit, L("EditItem"));
        cvQualifications.CreateChildPermission(AppPermissions.Pages_Recruitment_CVQualifications_Delete, L("DeleteItem"));

        var cvWorkExps = recruitment.CreateChildPermission(AppPermissions.Pages_Recruitment_CVWorkExps, L("CVWorkExps"));
        cvWorkExps.CreateChildPermission(AppPermissions.Pages_Recruitment_CVWorkExps_Create, L("CreateNewItem"));
        cvWorkExps.CreateChildPermission(AppPermissions.Pages_Recruitment_CVWorkExps_Edit, L("EditItem"));
        cvWorkExps.CreateChildPermission(AppPermissions.Pages_Recruitment_CVWorkExps_Delete, L("DeleteItem"));

        var cvSkills = recruitment.CreateChildPermission(AppPermissions.Pages_Recruitment_CVSkills, L("CVSkills"));
        cvSkills.CreateChildPermission(AppPermissions.Pages_Recruitment_CVSkills_Create, L("CreateNewItem"));
        cvSkills.CreateChildPermission(AppPermissions.Pages_Recruitment_CVSkills_Edit, L("EditItem"));
        cvSkills.CreateChildPermission(AppPermissions.Pages_Recruitment_CVSkills_Delete, L("DeleteItem"));

        var cvLanguages = recruitment.CreateChildPermission(AppPermissions.Pages_Recruitment_CVLanguages, L("CVLanguages"));
        cvLanguages.CreateChildPermission(AppPermissions.Pages_Recruitment_CVLanguages_Create, L("CreateNewItem"));
        cvLanguages.CreateChildPermission(AppPermissions.Pages_Recruitment_CVLanguages_Edit, L("EditItem"));
        cvLanguages.CreateChildPermission(AppPermissions.Pages_Recruitment_CVLanguages_Delete, L("DeleteItem"));

        var vacancies = recruitment.CreateChildPermission(AppPermissions.Pages_Recruitment_Vacancies, L("Vacancies"));
        vacancies.CreateChildPermission(AppPermissions.Pages_Recruitment_Vacancies_Create, L("CreateNewItem"));
        vacancies.CreateChildPermission(AppPermissions.Pages_Recruitment_Vacancies_Edit, L("EditItem"));
        vacancies.CreateChildPermission(AppPermissions.Pages_Recruitment_Vacancies_Delete, L("DeleteItem"));

        var applicationStatuses = recruitment.CreateChildPermission(AppPermissions.Pages_Recruitment_ApplicationStatuses, L("ApplicationStatuses"));
        applicationStatuses.CreateChildPermission(AppPermissions.Pages_Recruitment_ApplicationStatuses_Create, L("CreateNewItem"));
        applicationStatuses.CreateChildPermission(AppPermissions.Pages_Recruitment_ApplicationStatuses_Edit, L("EditItem"));
        applicationStatuses.CreateChildPermission(AppPermissions.Pages_Recruitment_ApplicationStatuses_Delete, L("DeleteItem"));

        var interviewDetails = recruitment.CreateChildPermission(AppPermissions.Pages_Recruitment_InterviewDetails, L("InterviewDetails"));
        interviewDetails.CreateChildPermission(AppPermissions.Pages_Recruitment_InterviewDetails_Create, L("CreateNewItem"));
        interviewDetails.CreateChildPermission(AppPermissions.Pages_Recruitment_InterviewDetails_Edit, L("EditItem"));
        interviewDetails.CreateChildPermission(AppPermissions.Pages_Recruitment_InterviewDetails_Delete, L("DeleteItem"));

        var onbGroups = recruitment.CreateChildPermission(AppPermissions.Pages_Recruitment_OnbGroups, L("OnbGroups"));
        onbGroups.CreateChildPermission(AppPermissions.Pages_Recruitment_OnbGroups_Create, L("CreateNewItem"));
        onbGroups.CreateChildPermission(AppPermissions.Pages_Recruitment_OnbGroups_Edit, L("EditItem"));
        onbGroups.CreateChildPermission(AppPermissions.Pages_Recruitment_OnbGroups_Delete, L("DeleteItem"));

        var onbGroupDetails = recruitment.CreateChildPermission(AppPermissions.Pages_Recruitment_OnbGroupDetails, L("OnbGroupDetails"));
        onbGroupDetails.CreateChildPermission(AppPermissions.Pages_Recruitment_OnbGroupDetails_Create, L("CreateNewItem"));
        onbGroupDetails.CreateChildPermission(AppPermissions.Pages_Recruitment_OnbGroupDetails_Edit, L("EditItem"));
        onbGroupDetails.CreateChildPermission(AppPermissions.Pages_Recruitment_OnbGroupDetails_Delete, L("DeleteItem"));

        var onbTasks = recruitment.CreateChildPermission(AppPermissions.Pages_Recruitment_OnbTasks, L("OnbTasks"));
        onbTasks.CreateChildPermission(AppPermissions.Pages_Recruitment_OnbTasks_Create, L("CreateNewItem"));
        onbTasks.CreateChildPermission(AppPermissions.Pages_Recruitment_OnbTasks_Edit, L("EditItem"));
        onbTasks.CreateChildPermission(AppPermissions.Pages_Recruitment_OnbTasks_Delete, L("DeleteItem"));

        var onbTemplates = recruitment.CreateChildPermission(AppPermissions.Pages_Recruitment_OnbTemplates, L("OnbTemplates"));
        onbTemplates.CreateChildPermission(AppPermissions.Pages_Recruitment_OnbTemplates_Create, L("CreateNewItem"));
        onbTemplates.CreateChildPermission(AppPermissions.Pages_Recruitment_OnbTemplates_Edit, L("EditItem"));
        onbTemplates.CreateChildPermission(AppPermissions.Pages_Recruitment_OnbTemplates_Delete, L("DeleteItem"));

        var onbTemplateDetails = recruitment.CreateChildPermission(AppPermissions.Pages_Recruitment_OnbTemplateDetails, L("OnbTemplateDetails"));
        onbTemplateDetails.CreateChildPermission(AppPermissions.Pages_Recruitment_OnbTemplateDetails_Create, L("CreateNewItem"));
        onbTemplateDetails.CreateChildPermission(AppPermissions.Pages_Recruitment_OnbTemplateDetails_Edit, L("EditItem"));
        onbTemplateDetails.CreateChildPermission(AppPermissions.Pages_Recruitment_OnbTemplateDetails_Delete, L("DeleteItem"));

        var onbApplicantTasks = recruitment.CreateChildPermission(AppPermissions.Pages_Recruitment_OnbApplicantTasks, L("OnbApplicantTasks"));
        onbApplicantTasks.CreateChildPermission(AppPermissions.Pages_Recruitment_OnbApplicantTasks_Create, L("CreateNewItem"));
        onbApplicantTasks.CreateChildPermission(AppPermissions.Pages_Recruitment_OnbApplicantTasks_Edit, L("EditItem"));
        onbApplicantTasks.CreateChildPermission(AppPermissions.Pages_Recruitment_OnbApplicantTasks_Delete, L("DeleteItem"));

        var onbAppTaskComments = recruitment.CreateChildPermission(AppPermissions.Pages_Recruitment_OnbAppTaskComments, L("OnbAppTaskComments"));
        onbAppTaskComments.CreateChildPermission(AppPermissions.Pages_Recruitment_OnbAppTaskComments_Create, L("CreateNewItem"));
        onbAppTaskComments.CreateChildPermission(AppPermissions.Pages_Recruitment_OnbAppTaskComments_Edit, L("EditItem"));
        onbAppTaskComments.CreateChildPermission(AppPermissions.Pages_Recruitment_OnbAppTaskComments_Delete, L("DeleteItem"));

        var onbVideoLinks = recruitment.CreateChildPermission(AppPermissions.Pages_Recruitment_OnbVideoLinks, L("OnbVideoLinks"));
        onbVideoLinks.CreateChildPermission(AppPermissions.Pages_Recruitment_OnbVideoLinks_Create, L("CreateNewItem"));
        onbVideoLinks.CreateChildPermission(AppPermissions.Pages_Recruitment_OnbVideoLinks_Edit, L("EditItem"));
        onbVideoLinks.CreateChildPermission(AppPermissions.Pages_Recruitment_OnbVideoLinks_Delete, L("DeleteItem"));

        var insurancePlans = pages.CreateChildPermission(AppPermissions.Pages_BenefitsManagement_InsurancePlans, L("InsurancePlans"));
        insurancePlans.CreateChildPermission(AppPermissions.Pages_BenefitsManagement_InsurancePlans_Create, L("CreateNewItem"));
        insurancePlans.CreateChildPermission(AppPermissions.Pages_BenefitsManagement_InsurancePlans_Edit, L("EditItem"));
        insurancePlans.CreateChildPermission(AppPermissions.Pages_BenefitsManagement_InsurancePlans_Delete, L("DeleteItem"));

        var insuranceProviders = pages.CreateChildPermission(AppPermissions.Pages_BenefitsManagement_InsuranceProviders, L("InsuranceProviders"));
        insuranceProviders.CreateChildPermission(AppPermissions.Pages_BenefitsManagement_InsuranceProviders_Create, L("CreateNewItem"));
        insuranceProviders.CreateChildPermission(AppPermissions.Pages_BenefitsManagement_InsuranceProviders_Edit, L("EditItem"));
        insuranceProviders.CreateChildPermission(AppPermissions.Pages_BenefitsManagement_InsuranceProviders_Delete, L("DeleteItem"));

        var employeeInsurances = pages.CreateChildPermission(AppPermissions.Pages_BenefitsManagement_EmployeeInsurances, L("EmployeeInsurances"));
        employeeInsurances.CreateChildPermission(AppPermissions.Pages_BenefitsManagement_EmployeeInsurances_Create, L("CreateNewItem"));
        employeeInsurances.CreateChildPermission(AppPermissions.Pages_BenefitsManagement_EmployeeInsurances_Edit, L("EditItem"));
        employeeInsurances.CreateChildPermission(AppPermissions.Pages_BenefitsManagement_EmployeeInsurances_Delete, L("DeleteItem"));

        var employeeInsuranceDetails = pages.CreateChildPermission(AppPermissions.Pages_BenefitsManagement_EmployeeInsuranceDetails, L("EmployeeInsuranceDetails"));
        employeeInsuranceDetails.CreateChildPermission(AppPermissions.Pages_BenefitsManagement_EmployeeInsuranceDetails_Create, L("CreateNewItem"));
        employeeInsuranceDetails.CreateChildPermission(AppPermissions.Pages_BenefitsManagement_EmployeeInsuranceDetails_Edit, L("EditItem"));
        employeeInsuranceDetails.CreateChildPermission(AppPermissions.Pages_BenefitsManagement_EmployeeInsuranceDetails_Delete, L("DeleteItem"));

        var companyInsuranceDetails = pages.CreateChildPermission(AppPermissions.Pages_BenefitsManagement_CompanyInsuranceDetails, L("CompanyInsuranceDetails"));
        companyInsuranceDetails.CreateChildPermission(AppPermissions.Pages_BenefitsManagement_CompanyInsuranceDetails_Create, L("CreateNewItem"));
        companyInsuranceDetails.CreateChildPermission(AppPermissions.Pages_BenefitsManagement_CompanyInsuranceDetails_Edit, L("EditItem"));
        companyInsuranceDetails.CreateChildPermission(AppPermissions.Pages_BenefitsManagement_CompanyInsuranceDetails_Delete, L("DeleteItem"));

        var surveyManagement = pages.CreateChildPermission(AppPermissions.Pages_SurveyManagement, L("SurveyManagement"));

        var surveys = surveyManagement.CreateChildPermission(AppPermissions.Pages_SurveyManagement_Surveys, L("Surveys"));
        surveys.CreateChildPermission(AppPermissions.Pages_SurveyManagement_Surveys_Create, L("CreateNewItem"));
        surveys.CreateChildPermission(AppPermissions.Pages_SurveyManagement_Surveys_Edit, L("EditItem"));
        surveys.CreateChildPermission(AppPermissions.Pages_SurveyManagement_Surveys_Delete, L("DeleteItem"));

        var surveyDetails = surveyManagement.CreateChildPermission(AppPermissions.Pages_SurveyManagement_SurveyDetails, L("SurveyDetails"));
        surveyDetails.CreateChildPermission(AppPermissions.Pages_SurveyManagement_SurveyDetails_Create, L("CreateNewItem"));
        surveyDetails.CreateChildPermission(AppPermissions.Pages_SurveyManagement_SurveyDetails_Edit, L("EditItem"));
        surveyDetails.CreateChildPermission(AppPermissions.Pages_SurveyManagement_SurveyDetails_Delete, L("DeleteItem"));

        var employeeSurveys = surveyManagement.CreateChildPermission(AppPermissions.Pages_SurveyManagement_EmployeeSurveys, L("EmployeeSurveys"));
        employeeSurveys.CreateChildPermission(AppPermissions.Pages_SurveyManagement_EmployeeSurveys_Create, L("CreateNewItem"));
        employeeSurveys.CreateChildPermission(AppPermissions.Pages_SurveyManagement_EmployeeSurveys_Edit, L("EditItem"));
        employeeSurveys.CreateChildPermission(AppPermissions.Pages_SurveyManagement_EmployeeSurveys_Delete, L("DeleteItem"));

        var employeeSurveyDetails = surveyManagement.CreateChildPermission(AppPermissions.Pages_SurveyManagement_EmployeeSurveyDetails, L("EmployeeSurveyDetails"));
        employeeSurveyDetails.CreateChildPermission(AppPermissions.Pages_SurveyManagement_EmployeeSurveyDetails_Create, L("CreateNewItem"));
        employeeSurveyDetails.CreateChildPermission(AppPermissions.Pages_SurveyManagement_EmployeeSurveyDetails_Edit, L("EditItem"));
        employeeSurveyDetails.CreateChildPermission(AppPermissions.Pages_SurveyManagement_EmployeeSurveyDetails_Delete, L("DeleteItem"));

        var healthAndSafetyManagement = pages.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement, L("HealthAndSafetyManagement"));

        var actionStatuses = healthAndSafetyManagement.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_ActionStatuses, L("ActionStatuses"));
        actionStatuses.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_ActionStatuses_Create, L("CreateNewItem"));
        actionStatuses.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_ActionStatuses_Edit, L("EditItem"));
        actionStatuses.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_ActionStatuses_Delete, L("DeleteItem"));

        var firstAidMedicines = healthAndSafetyManagement.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_FirstAidMedicines, L("FirstAidMedicines"));
        firstAidMedicines.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_FirstAidMedicines_Create, L("CreateNewItem"));
        firstAidMedicines.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_FirstAidMedicines_Edit, L("EditItem"));
        firstAidMedicines.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_FirstAidMedicines_Delete, L("DeleteItem"));

        var incidentClaimStatuses = healthAndSafetyManagement.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_IncidentClaimStatuses, L("IncidentClaimStatuses"));
        incidentClaimStatuses.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_IncidentClaimStatuses_Create, L("CreateNewItem"));
        incidentClaimStatuses.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_IncidentClaimStatuses_Edit, L("EditItem"));
        incidentClaimStatuses.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_IncidentClaimStatuses_Delete, L("DeleteItem"));

        var incidentNatures = healthAndSafetyManagement.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_IncidentNatures, L("IncidentNatures"));
        incidentNatures.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_IncidentNatures_Create, L("CreateNewItem"));
        incidentNatures.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_IncidentNatures_Edit, L("EditItem"));
        incidentNatures.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_IncidentNatures_Delete, L("DeleteItem"));

        var incidentSeverities = healthAndSafetyManagement.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_IncidentSeverities, L("IncidentSeverities"));
        incidentSeverities.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_IncidentSeverities_Create, L("CreateNewItem"));
        incidentSeverities.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_IncidentSeverities_Edit, L("EditItem"));
        incidentSeverities.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_IncidentSeverities_Delete, L("DeleteItem"));

        var incidentStatuses = healthAndSafetyManagement.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_IncidentStatuses, L("IncidentStatuses"));
        incidentStatuses.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_IncidentStatuses_Create, L("CreateNewItem"));
        incidentStatuses.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_IncidentStatuses_Edit, L("EditItem"));
        incidentStatuses.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_IncidentStatuses_Delete, L("DeleteItem"));

        var incidentTypes = healthAndSafetyManagement.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_IncidentTypes, L("IncidentTypes"));
        incidentTypes.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_IncidentTypes_Create, L("CreateNewItem"));
        incidentTypes.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_IncidentTypes_Edit, L("EditItem"));
        incidentTypes.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_IncidentTypes_Delete, L("DeleteItem"));

        var investigationRootCauses = healthAndSafetyManagement.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_InvestigationRootCauses, L("InvestigationRootCauses"));
        investigationRootCauses.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_InvestigationRootCauses_Create, L("CreateNewItem"));
        investigationRootCauses.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_InvestigationRootCauses_Edit, L("EditItem"));
        investigationRootCauses.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_InvestigationRootCauses_Delete, L("DeleteItem"));

        var riskActions = healthAndSafetyManagement.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_RiskActions, L("RiskActions"));
        riskActions.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_RiskActions_Create, L("CreateNewItem"));
        riskActions.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_RiskActions_Edit, L("EditItem"));
        riskActions.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_RiskActions_Delete, L("DeleteItem"));

        var riskImpacts = healthAndSafetyManagement.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_RiskImpacts, L("RiskImpacts"));
        riskImpacts.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_RiskImpacts_Create, L("CreateNewItem"));
        riskImpacts.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_RiskImpacts_Edit, L("EditItem"));
        riskImpacts.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_RiskImpacts_Delete, L("DeleteItem"));

        var riskPossibilities = healthAndSafetyManagement.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_RiskPossibilities, L("RiskPossibilities"));
        riskPossibilities.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_RiskPossibilities_Create, L("CreateNewItem"));
        riskPossibilities.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_RiskPossibilities_Edit, L("EditItem"));
        riskPossibilities.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_RiskPossibilities_Delete, L("DeleteItem"));

        var riskStatuses = healthAndSafetyManagement.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_RiskStatuses, L("RiskStatuses"));
        riskStatuses.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_RiskStatuses_Create, L("CreateNewItem"));
        riskStatuses.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_RiskStatuses_Edit, L("EditItem"));
        riskStatuses.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_RiskStatuses_Delete, L("DeleteItem"));

        var riskTypes = healthAndSafetyManagement.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_RiskTypes, L("RiskTypes"));
        riskTypes.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_RiskTypes_Create, L("CreateNewItem"));
        riskTypes.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_RiskTypes_Edit, L("EditItem"));
        riskTypes.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_RiskTypes_Delete, L("DeleteItem"));

        var safetyAreaAssets = healthAndSafetyManagement.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_SafetyAreaAssets, L("SafetyAreaAssets"));
        safetyAreaAssets.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_SafetyAreaAssets_Create, L("CreateNewItem"));
        safetyAreaAssets.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_SafetyAreaAssets_Edit, L("EditItem"));
        safetyAreaAssets.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_SafetyAreaAssets_Delete, L("DeleteItem"));

        var actionRegisters = healthAndSafetyManagement.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_ActionRegisters, L("ActionRegisters"));
        actionRegisters.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_ActionRegisters_Create, L("CreateNewItem"));
        actionRegisters.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_ActionRegisters_Edit, L("EditItem"));
        actionRegisters.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_ActionRegisters_Delete, L("DeleteItem"));

        var firstAidBoxes = healthAndSafetyManagement.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_FirstAidBoxes, L("FirstAidBoxes"));
        firstAidBoxes.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_FirstAidBoxes_Create, L("CreateNewItem"));
        firstAidBoxes.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_FirstAidBoxes_Edit, L("EditItem"));
        firstAidBoxes.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_FirstAidBoxes_Delete, L("DeleteItem"));

        var firstAidBoxDetails = healthAndSafetyManagement.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_FirstAidBoxDetails, L("FirstAidBoxDetails"));
        firstAidBoxDetails.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_FirstAidBoxDetails_Create, L("CreateNewItem"));
        firstAidBoxDetails.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_FirstAidBoxDetails_Edit, L("EditItem"));
        firstAidBoxDetails.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_FirstAidBoxDetails_Delete, L("DeleteItem"));

        var incidentClaims = healthAndSafetyManagement.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_IncidentClaims, L("IncidentClaims"));
        incidentClaims.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_IncidentClaims_Create, L("CreateNewItem"));
        incidentClaims.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_IncidentClaims_Edit, L("EditItem"));
        incidentClaims.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_IncidentClaims_Delete, L("DeleteItem"));

        var incidentInvestigations = healthAndSafetyManagement.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_IncidentInvestigations, L("IncidentInvestigations"));
        incidentInvestigations.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_IncidentInvestigations_Create, L("CreateNewItem"));
        incidentInvestigations.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_IncidentInvestigations_Edit, L("EditItem"));
        incidentInvestigations.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_IncidentInvestigations_Delete, L("DeleteItem"));

        var incidentRegisters = healthAndSafetyManagement.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_IncidentRegisters, L("IncidentRegisters"));
        incidentRegisters.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_IncidentRegisters_Create, L("CreateNewItem"));
        incidentRegisters.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_IncidentRegisters_Edit, L("EditItem"));
        incidentRegisters.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_IncidentRegisters_Delete, L("DeleteItem"));

        var riskRegisters = healthAndSafetyManagement.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_RiskRegisters, L("RiskRegisters"));
        riskRegisters.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_RiskRegisters_Create, L("CreateNewItem"));
        riskRegisters.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_RiskRegisters_Edit, L("EditItem"));
        riskRegisters.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_RiskRegisters_Delete, L("DeleteItem"));

        var safetyChecklists = healthAndSafetyManagement.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_SafetyChecklists, L("SafetyChecklists"));
        safetyChecklists.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_SafetyChecklists_Create, L("CreateNewItem"));
        safetyChecklists.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_SafetyChecklists_Edit, L("EditItem"));
        safetyChecklists.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_SafetyChecklists_Delete, L("DeleteItem"));

        var siteAudits = healthAndSafetyManagement.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_SiteAudits, L("SiteAudits"));
        siteAudits.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_SiteAudits_Create, L("CreateNewItem"));
        siteAudits.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_SiteAudits_Edit, L("EditItem"));
        siteAudits.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_SiteAudits_Delete, L("DeleteItem"));

        var siteAuditDetails = healthAndSafetyManagement.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_SiteAuditDetails, L("SiteAuditDetails"));
        siteAuditDetails.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_SiteAuditDetails_Create, L("CreateNewItem"));
        siteAuditDetails.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_SiteAuditDetails_Edit, L("EditItem"));
        siteAuditDetails.CreateChildPermission(AppPermissions.Pages_HealthAndSafetyManagement_SiteAuditDetails_Delete, L("DeleteItem"));

        //TENANT-SPECIFIC PERMISSIONS

        pages.CreateChildPermission(AppPermissions.Pages_Tenant_Dashboard, L("Dashboard"), multiTenancySides: MultiTenancySides.Tenant);

        administration.CreateChildPermission(AppPermissions.Pages_Administration_Tenant_Settings, L("Settings"), multiTenancySides: MultiTenancySides.Tenant);
        administration.CreateChildPermission(AppPermissions.Pages_Administration_Tenant_SubscriptionManagement, L("Subscription"), multiTenancySides: MultiTenancySides.Tenant);

        //HOST-SPECIFIC PERMISSIONS

        var editions = pages.CreateChildPermission(AppPermissions.Pages_Editions, L("Editions"), multiTenancySides: MultiTenancySides.Host);
        editions.CreateChildPermission(AppPermissions.Pages_Editions_Create, L("CreatingNewEdition"), multiTenancySides: MultiTenancySides.Host);
        editions.CreateChildPermission(AppPermissions.Pages_Editions_Edit, L("EditingEdition"), multiTenancySides: MultiTenancySides.Host);
        editions.CreateChildPermission(AppPermissions.Pages_Editions_Delete, L("DeletingEdition"), multiTenancySides: MultiTenancySides.Host);
        editions.CreateChildPermission(AppPermissions.Pages_Editions_MoveTenantsToAnotherEdition, L("MoveTenantsToAnotherEdition"), multiTenancySides: MultiTenancySides.Host);

        var tenants = pages.CreateChildPermission(AppPermissions.Pages_Tenants, L("Tenants"), multiTenancySides: MultiTenancySides.Host);
        tenants.CreateChildPermission(AppPermissions.Pages_Tenants_Create, L("CreatingNewTenant"), multiTenancySides: MultiTenancySides.Host);
        tenants.CreateChildPermission(AppPermissions.Pages_Tenants_Edit, L("EditingTenant"), multiTenancySides: MultiTenancySides.Host);
        tenants.CreateChildPermission(AppPermissions.Pages_Tenants_ChangeFeatures, L("ChangingFeatures"), multiTenancySides: MultiTenancySides.Host);
        tenants.CreateChildPermission(AppPermissions.Pages_Tenants_Delete, L("DeletingTenant"), multiTenancySides: MultiTenancySides.Host);
        tenants.CreateChildPermission(AppPermissions.Pages_Tenants_Impersonation, L("LoginForTenants"), multiTenancySides: MultiTenancySides.Host);

        administration.CreateChildPermission(AppPermissions.Pages_Administration_Host_Settings, L("Settings"), multiTenancySides: MultiTenancySides.Host);

        var maintenance = administration.CreateChildPermission(AppPermissions.Pages_Administration_Host_Maintenance, L("Maintenance"), multiTenancySides: _isMultiTenancyEnabled ? MultiTenancySides.Host : MultiTenancySides.Tenant);
        maintenance.CreateChildPermission(AppPermissions.Pages_Administration_NewVersion_Create, L("SendNewVersionNotification"));

        administration.CreateChildPermission(AppPermissions.Pages_Administration_HangfireDashboard, L("HangfireDashboard"), multiTenancySides: _isMultiTenancyEnabled ? MultiTenancySides.Host : MultiTenancySides.Tenant);
        administration.CreateChildPermission(AppPermissions.Pages_Administration_Host_Dashboard, L("Dashboard"), multiTenancySides: MultiTenancySides.Host);
        administration.CreateChildPermission(AppPermissions.Pages_Administration_Host_HealthCheck, L("HealthCheck"), multiTenancySides: _isMultiTenancyEnabled ? MultiTenancySides.Host : MultiTenancySides.Tenant);

        var trainingManagement = pages.CreateChildPermission(AppPermissions.Pages_TrainingManagement_TrainingCourses, L("TrainingCourses"));
        trainingManagement.CreateChildPermission(AppPermissions.Pages_TrainingManagement_TrainingCourses_Create, L("CreateNewItem"));
        trainingManagement.CreateChildPermission(AppPermissions.Pages_TrainingManagement_TrainingCourses_Edit, L("EditItem"));
        trainingManagement.CreateChildPermission(AppPermissions.Pages_TrainingManagement_TrainingCourses_Delete, L("DeleteItem"));

        var trainingSchedules = pages.CreateChildPermission(AppPermissions.Pages_TrainingManagement_TrainingSchedules, L("TrainingSchedules"));
        trainingSchedules.CreateChildPermission(AppPermissions.Pages_TrainingManagement_TrainingSchedules_Create, L("CreateNewItem"));
        trainingSchedules.CreateChildPermission(AppPermissions.Pages_TrainingManagement_TrainingSchedules_Edit, L("EditItem"));
        trainingSchedules.CreateChildPermission(AppPermissions.Pages_TrainingManagement_TrainingSchedules_Delete, L("DeleteItem"));

        var employeeTrainings = pages.CreateChildPermission(AppPermissions.Pages_TrainingManagement_EmployeeTrainings, L("EmployeeTrainings"));
        employeeTrainings.CreateChildPermission(AppPermissions.Pages_TrainingManagement_EmployeeTrainings_Create, L("CreateNewItem"));
        employeeTrainings.CreateChildPermission(AppPermissions.Pages_TrainingManagement_EmployeeTrainings_Edit, L("EditItem"));
        employeeTrainings.CreateChildPermission(AppPermissions.Pages_TrainingManagement_EmployeeTrainings_Delete, L("DeleteItem"));

        var trainingAttendances = pages.CreateChildPermission(AppPermissions.Pages_TrainingManagement_TrainingAttendances, L("TrainingAttendances"));
        trainingAttendances.CreateChildPermission(AppPermissions.Pages_TrainingManagement_TrainingAttendances_Create, L("CreateNewItem"));
        trainingAttendances.CreateChildPermission(AppPermissions.Pages_TrainingManagement_TrainingAttendances_Edit, L("EditItem"));
        trainingAttendances.CreateChildPermission(AppPermissions.Pages_TrainingManagement_TrainingAttendances_Delete, L("DeleteItem"));

        var jobProfileManagement = pages.CreateChildPermission(AppPermissions.Pages_JobProfileManagement, L("JobProfileManagement"));

        var jobFamilies = jobProfileManagement.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobFamilies, L("JobFamilies"));
        jobFamilies.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobFamilies_Create, L("CreateNewItem"));
        jobFamilies.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobFamilies_Edit, L("EditItem"));
        jobFamilies.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobFamilies_Delete, L("DeleteItem"));

        var jobFunctions = jobProfileManagement.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobFunctions, L("JobFunctions"));
        jobFunctions.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobFunctions_Create, L("CreateNewItem"));
        jobFunctions.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobFunctions_Edit, L("EditItem"));
        jobFunctions.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobFunctions_Delete, L("DeleteItem"));

        var jobResponsibilities = jobProfileManagement.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobResponsibilities, L("JobResponsibilities"));
        jobResponsibilities.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobResponsibilities_Create, L("CreateNewItem"));
        jobResponsibilities.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobResponsibilities_Edit, L("EditItem"));
        jobResponsibilities.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobResponsibilities_Delete, L("DeleteItem"));

        var educationCertificates = jobProfileManagement.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_EducationCertificates, L("EducationCertificates"));
        educationCertificates.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_EducationCertificates_Create, L("CreateNewItem"));
        educationCertificates.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_EducationCertificates_Edit, L("EditItem"));
        educationCertificates.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_EducationCertificates_Delete, L("DeleteItem"));

        var educationDegrees = jobProfileManagement.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_EducationDegrees, L("EducationDegrees"));
        educationDegrees.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_EducationDegrees_Create, L("CreateNewItem"));
        educationDegrees.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_EducationDegrees_Edit, L("EditItem"));
        educationDegrees.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_EducationDegrees_Delete, L("DeleteItem"));

        var interviewQuestions = jobProfileManagement.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_InterviewQuestions, L("InterviewQuestions"));
        interviewQuestions.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_InterviewQuestions_Create, L("CreateNewItem"));
        interviewQuestions.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_InterviewQuestions_Edit, L("EditItem"));
        interviewQuestions.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_InterviewQuestions_Delete, L("DeleteItem"));

        var jobProfileSkills = jobProfileManagement.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_Skills, L("Skills"));
        jobProfileSkills.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_Skills_Create, L("CreateNewItem"));
        jobProfileSkills.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_Skills_Edit, L("EditItem"));
        jobProfileSkills.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_Skills_Delete, L("DeleteItem"));

        var jobDescriptions = jobProfileManagement.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobDescriptions, L("JobDescriptions"));
        jobDescriptions.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobDescriptions_Create, L("CreateNewItem"));
        jobDescriptions.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobDescriptions_Edit, L("EditItem"));
        jobDescriptions.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobDescriptions_Delete, L("DeleteItem"));

        var jobDescriptionCertificates = jobProfileManagement.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobDescriptionCertificates, L("JobDescriptionCertificates"));
        jobDescriptionCertificates.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobDescriptionCertificates_Create, L("CreateNewItem"));
        jobDescriptionCertificates.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobDescriptionCertificates_Edit, L("EditItem"));
        jobDescriptionCertificates.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobDescriptionCertificates_Delete, L("DeleteItem"));

        var jobDescriptionDegrees = jobProfileManagement.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobDescriptionDegrees, L("JobDescriptionDegrees"));
        jobDescriptionDegrees.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobDescriptionDegrees_Create, L("CreateNewItem"));
        jobDescriptionDegrees.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobDescriptionDegrees_Edit, L("EditItem"));
        jobDescriptionDegrees.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobDescriptionDegrees_Delete, L("DeleteItem"));

        var jobDescriptionInterviewQuestions = jobProfileManagement.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobDescriptionInterviewQuestions, L("JobDescriptionInterviewQuestions"));
        jobDescriptionInterviewQuestions.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobDescriptionInterviewQuestions_Create, L("CreateNewItem"));
        jobDescriptionInterviewQuestions.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobDescriptionInterviewQuestions_Edit, L("EditItem"));
        jobDescriptionInterviewQuestions.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobDescriptionInterviewQuestions_Delete, L("DeleteItem"));

        var jobDescriptionResponsibilities = jobProfileManagement.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobDescriptionResponsibilities, L("JobDescriptionResponsibilities"));
        jobDescriptionResponsibilities.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobDescriptionResponsibilities_Create, L("CreateNewItem"));
        jobDescriptionResponsibilities.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobDescriptionResponsibilities_Edit, L("EditItem"));
        jobDescriptionResponsibilities.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobDescriptionResponsibilities_Delete, L("DeleteItem"));

        var jobDescriptionSkills = jobProfileManagement.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobDescriptionSkills, L("JobDescriptionSkills"));
        jobDescriptionSkills.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobDescriptionSkills_Create, L("CreateNewItem"));
        jobDescriptionSkills.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobDescriptionSkills_Edit, L("EditItem"));
        jobDescriptionSkills.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobDescriptionSkills_Delete, L("DeleteItem"));

        var jobDescriptionCompetencies = jobProfileManagement.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobDescriptionCompetencies, L("JobDescriptionCompetencies"));
        jobDescriptionCompetencies.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobDescriptionCompetencies_Create, L("CreateNewItem"));
        jobDescriptionCompetencies.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobDescriptionCompetencies_Edit, L("EditItem"));
        jobDescriptionCompetencies.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobDescriptionCompetencies_Delete, L("DeleteItem"));

        var jobDescriptionAcknowledgements = jobProfileManagement.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobDescriptionAcknowledgements, L("JobDescriptionAcknowledgements"));
        jobDescriptionAcknowledgements.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobDescriptionAcknowledgements_Create, L("CreateNewItem"));
        jobDescriptionAcknowledgements.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobDescriptionAcknowledgements_Edit, L("EditItem"));
        jobDescriptionAcknowledgements.CreateChildPermission(AppPermissions.Pages_JobProfileManagement_JobDescriptionAcknowledgements_Delete, L("DeleteItem"));

        var systemSettings = pages.CreateChildPermission(AppPermissions.Pages_SystemSettings, L("SystemSettings"));

        var appraisalSettings = systemSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_AppraisalSettings, L("AppraisalSettings"));
        appraisalSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_AppraisalSettings_Create, L("CreateNewItem"));
        appraisalSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_AppraisalSettings_Edit, L("EditItem"));
        appraisalSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_AppraisalSettings_Delete, L("DeleteItem"));

        var documentSettings = systemSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_DocumentSettings, L("DocumentSettings"));
        documentSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_DocumentSettings_Create, L("CreateNewItem"));
        documentSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_DocumentSettings_Edit, L("EditItem"));
        documentSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_DocumentSettings_Delete, L("DeleteItem"));

        var fiPostingSettings = systemSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_FIPostingSettings, L("FIPostingSettings"));
        fiPostingSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_FIPostingSettings_Create, L("CreateNewItem"));
        fiPostingSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_FIPostingSettings_Edit, L("EditItem"));
        fiPostingSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_FIPostingSettings_Delete, L("DeleteItem"));

        var generalSettings = systemSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_GeneralSettings, L("GeneralSettings"));
        generalSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_GeneralSettings_Create, L("CreateNewItem"));
        generalSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_GeneralSettings_Edit, L("EditItem"));
        generalSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_GeneralSettings_Delete, L("DeleteItem"));

        var notificationSettings = systemSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_NotificationSettings, L("NotificationSettings"));
        notificationSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_NotificationSettings_Create, L("CreateNewItem"));
        notificationSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_NotificationSettings_Edit, L("EditItem"));
        notificationSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_NotificationSettings_Delete, L("DeleteItem"));

        var orgSettings = systemSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_OrgSettings, L("OrgSettings"));
        orgSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_OrgSettings_Create, L("CreateNewItem"));
        orgSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_OrgSettings_Edit, L("EditItem"));
        orgSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_OrgSettings_Delete, L("DeleteItem"));

        var payrollSettings = systemSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_PayrollSettings, L("PayrollSettings"));
        payrollSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_PayrollSettings_Create, L("CreateNewItem"));
        payrollSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_PayrollSettings_Edit, L("EditItem"));
        payrollSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_PayrollSettings_Delete, L("DeleteItem"));

        var personnelSettings = systemSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_PersonnelSettings, L("PersonnelSettings"));
        personnelSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_PersonnelSettings_Create, L("CreateNewItem"));
        personnelSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_PersonnelSettings_Edit, L("EditItem"));
        personnelSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_PersonnelSettings_Delete, L("DeleteItem"));

        var projectCostingSettings = systemSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_ProjectCostingSettings, L("ProjectCostingSettings"));
        projectCostingSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_ProjectCostingSettings_Create, L("CreateNewItem"));
        projectCostingSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_ProjectCostingSettings_Edit, L("EditItem"));
        projectCostingSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_ProjectCostingSettings_Delete, L("DeleteItem"));

        var systemReportSettings = systemSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_ReportSettings, L("ReportSettings"));
        systemReportSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_ReportSettings_Create, L("CreateNewItem"));
        systemReportSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_ReportSettings_Edit, L("EditItem"));
        systemReportSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_ReportSettings_Delete, L("DeleteItem"));

        var securitySettings = systemSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_SecuritySettings, L("SecuritySettings"));
        securitySettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_SecuritySettings_Create, L("CreateNewItem"));
        securitySettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_SecuritySettings_Edit, L("EditItem"));
        securitySettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_SecuritySettings_Delete, L("DeleteItem"));

        var selfServiceSettings = systemSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_SelfServiceSettings, L("SelfServiceSettings"));
        selfServiceSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_SelfServiceSettings_Create, L("CreateNewItem"));
        selfServiceSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_SelfServiceSettings_Edit, L("EditItem"));
        selfServiceSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_SelfServiceSettings_Delete, L("DeleteItem"));

        var timeSettings = systemSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_TimeSettings, L("TimeSettings"));
        timeSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_TimeSettings_Create, L("CreateNewItem"));
        timeSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_TimeSettings_Edit, L("EditItem"));
        timeSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_TimeSettings_Delete, L("DeleteItem"));

        var workflowSettings = systemSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_WorkflowSettings, L("WorkflowSettings"));
        workflowSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_WorkflowSettings_Create, L("CreateNewItem"));
        workflowSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_WorkflowSettings_Edit, L("EditItem"));
        workflowSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_WorkflowSettings_Delete, L("DeleteItem"));

        var payrollDrivers = systemSettings.CreateChildPermission(AppPermissions.Pages_SystemSettings_PayrollDrivers, L("PayrollDrivers"));
        payrollDrivers.CreateChildPermission(AppPermissions.Pages_SystemSettings_PayrollDrivers_Create, L("CreateNewItem"));
        payrollDrivers.CreateChildPermission(AppPermissions.Pages_SystemSettings_PayrollDrivers_Edit, L("EditItem"));
        payrollDrivers.CreateChildPermission(AppPermissions.Pages_SystemSettings_PayrollDrivers_Delete, L("DeleteItem"));

        var sysUserOrgUnitData = pages.CreateChildPermission(AppPermissions.Pages_SystemSecurity_SysUserOrgUnitData, L("SysUserOrgUnitData"));
        sysUserOrgUnitData.CreateChildPermission(AppPermissions.Pages_SystemSecurity_SysUserOrgUnitData_Create, L("CreateNewItem"));
        sysUserOrgUnitData.CreateChildPermission(AppPermissions.Pages_SystemSecurity_SysUserOrgUnitData_Edit, L("EditItem"));
        sysUserOrgUnitData.CreateChildPermission(AppPermissions.Pages_SystemSecurity_SysUserOrgUnitData_Delete, L("DeleteItem"));

        var emailTemplates = pages.CreateChildPermission(AppPermissions.Pages_SystemSecurity_EmailTemplates, L("EmailTemplates"));
        emailTemplates.CreateChildPermission(AppPermissions.Pages_SystemSecurity_EmailTemplates_Create, L("CreateNewItem"));
        emailTemplates.CreateChildPermission(AppPermissions.Pages_SystemSecurity_EmailTemplates_Edit, L("EditItem"));
        emailTemplates.CreateChildPermission(AppPermissions.Pages_SystemSecurity_EmailTemplates_Delete, L("DeleteItem"));

        var emailTokens = pages.CreateChildPermission(AppPermissions.Pages_SystemSecurity_EmailTokens, L("EmailTokens"));
        emailTokens.CreateChildPermission(AppPermissions.Pages_SystemSecurity_EmailTokens_Create, L("CreateNewItem"));
        emailTokens.CreateChildPermission(AppPermissions.Pages_SystemSecurity_EmailTokens_Edit, L("EditItem"));
        emailTokens.CreateChildPermission(AppPermissions.Pages_SystemSecurity_EmailTokens_Delete, L("DeleteItem"));

        var payslipConfigs = pages.CreateChildPermission(AppPermissions.Pages_SystemSecurity_PayslipConfigs, L("PayslipConfigs"));
        payslipConfigs.CreateChildPermission(AppPermissions.Pages_SystemSecurity_PayslipConfigs_Create, L("CreateNewItem"));
        payslipConfigs.CreateChildPermission(AppPermissions.Pages_SystemSecurity_PayslipConfigs_Edit, L("EditItem"));
        payslipConfigs.CreateChildPermission(AppPermissions.Pages_SystemSecurity_PayslipConfigs_Delete, L("DeleteItem"));

        var notificationMasters = pages.CreateChildPermission(AppPermissions.Pages_SystemSecurity_NotificationMasters, L("NotificationMasters"));
        notificationMasters.CreateChildPermission(AppPermissions.Pages_SystemSecurity_NotificationMasters_Create, L("CreateNewItem"));
        notificationMasters.CreateChildPermission(AppPermissions.Pages_SystemSecurity_NotificationMasters_Edit, L("EditItem"));
        notificationMasters.CreateChildPermission(AppPermissions.Pages_SystemSecurity_NotificationMasters_Delete, L("DeleteItem"));

        var systemNotifications = pages.CreateChildPermission(AppPermissions.Pages_SystemSecurity_SystemNotifications, L("SystemNotifications"));
        systemNotifications.CreateChildPermission(AppPermissions.Pages_SystemSecurity_SystemNotifications_Create, L("CreateNewItem"));
        systemNotifications.CreateChildPermission(AppPermissions.Pages_SystemSecurity_SystemNotifications_Edit, L("EditItem"));
        systemNotifications.CreateChildPermission(AppPermissions.Pages_SystemSecurity_SystemNotifications_Delete, L("DeleteItem"));

        var sysObjects = pages.CreateChildPermission(AppPermissions.Pages_SystemSecurity_SysObjects, L("SysObjects"));
        sysObjects.CreateChildPermission(AppPermissions.Pages_SystemSecurity_SysObjects_Create, L("CreateNewItem"));
        sysObjects.CreateChildPermission(AppPermissions.Pages_SystemSecurity_SysObjects_Edit, L("EditItem"));
        sysObjects.CreateChildPermission(AppPermissions.Pages_SystemSecurity_SysObjects_Delete, L("DeleteItem"));

        var retroReasonLogs = pages.CreateChildPermission(AppPermissions.Pages_SystemSecurity_RetroReasonLogs, L("RetroReasonLogs"));
        retroReasonLogs.CreateChildPermission(AppPermissions.Pages_SystemSecurity_RetroReasonLogs_Create, L("CreateNewItem"));
        retroReasonLogs.CreateChildPermission(AppPermissions.Pages_SystemSecurity_RetroReasonLogs_Edit, L("EditItem"));
        retroReasonLogs.CreateChildPermission(AppPermissions.Pages_SystemSecurity_RetroReasonLogs_Delete, L("DeleteItem"));

        var punchNotificationLogs = pages.CreateChildPermission(AppPermissions.Pages_SystemSecurity_PunchNotificationLogs, L("PunchNotificationLogs"));
        punchNotificationLogs.CreateChildPermission(AppPermissions.Pages_SystemSecurity_PunchNotificationLogs_Create, L("CreateNewItem"));
        punchNotificationLogs.CreateChildPermission(AppPermissions.Pages_SystemSecurity_PunchNotificationLogs_Edit, L("EditItem"));
        punchNotificationLogs.CreateChildPermission(AppPermissions.Pages_SystemSecurity_PunchNotificationLogs_Delete, L("DeleteItem"));

        var taskEvents = pages.CreateChildPermission(AppPermissions.Pages_SystemSecurity_TaskEvents, L("TaskEvents"));
        taskEvents.CreateChildPermission(AppPermissions.Pages_SystemSecurity_TaskEvents_Create, L("CreateNewItem"));
        taskEvents.CreateChildPermission(AppPermissions.Pages_SystemSecurity_TaskEvents_Edit, L("EditItem"));
        taskEvents.CreateChildPermission(AppPermissions.Pages_SystemSecurity_TaskEvents_Delete, L("DeleteItem"));

        var employeesNotifications = pages.CreateChildPermission(AppPermissions.Pages_SystemSecurity_EmployeesNotifications, L("EmployeesNotifications"));
        employeesNotifications.CreateChildPermission(AppPermissions.Pages_SystemSecurity_EmployeesNotifications_Create, L("CreateNewItem"));
        employeesNotifications.CreateChildPermission(AppPermissions.Pages_SystemSecurity_EmployeesNotifications_Edit, L("EditItem"));
        employeesNotifications.CreateChildPermission(AppPermissions.Pages_SystemSecurity_EmployeesNotifications_Delete, L("DeleteItem"));

        var employeesNotificationLogs = pages.CreateChildPermission(AppPermissions.Pages_SystemSecurity_EmployeesNotificationLogs, L("EmployeesNotificationLogs"));
        employeesNotificationLogs.CreateChildPermission(AppPermissions.Pages_SystemSecurity_EmployeesNotificationLogs_Create, L("CreateNewItem"));
        employeesNotificationLogs.CreateChildPermission(AppPermissions.Pages_SystemSecurity_EmployeesNotificationLogs_Edit, L("EditItem"));
        employeesNotificationLogs.CreateChildPermission(AppPermissions.Pages_SystemSecurity_EmployeesNotificationLogs_Delete, L("DeleteItem"));

        var bulkUploadLogs = pages.CreateChildPermission(AppPermissions.Pages_SystemSecurity_BulkUploadLogs, L("BulkUploadLogs"));
        bulkUploadLogs.CreateChildPermission(AppPermissions.Pages_SystemSecurity_BulkUploadLogs_Create, L("CreateNewItem"));
        bulkUploadLogs.CreateChildPermission(AppPermissions.Pages_SystemSecurity_BulkUploadLogs_Edit, L("EditItem"));
        bulkUploadLogs.CreateChildPermission(AppPermissions.Pages_SystemSecurity_BulkUploadLogs_Delete, L("DeleteItem"));

        var executionLogs = pages.CreateChildPermission(AppPermissions.Pages_SystemSecurity_ExecutionLogs, L("ExecutionLogs"));
        executionLogs.CreateChildPermission(AppPermissions.Pages_SystemSecurity_ExecutionLogs_Create, L("CreateNewItem"));
        executionLogs.CreateChildPermission(AppPermissions.Pages_SystemSecurity_ExecutionLogs_Edit, L("EditItem"));
        executionLogs.CreateChildPermission(AppPermissions.Pages_SystemSecurity_ExecutionLogs_Delete, L("DeleteItem"));

        var emailNotificationLogs = pages.CreateChildPermission(AppPermissions.Pages_SystemSecurity_EmailNotificationLogs, L("EmailNotificationLogs"));
        emailNotificationLogs.CreateChildPermission(AppPermissions.Pages_SystemSecurity_EmailNotificationLogs_Create, L("CreateNewItem"));
        emailNotificationLogs.CreateChildPermission(AppPermissions.Pages_SystemSecurity_EmailNotificationLogs_Edit, L("EditItem"));
        emailNotificationLogs.CreateChildPermission(AppPermissions.Pages_SystemSecurity_EmailNotificationLogs_Delete, L("DeleteItem"));

        var sysTenantConfigs = pages.CreateChildPermission(AppPermissions.Pages_SystemSecurity_SysTenantConfigs, L("SysTenantConfigs"));
        sysTenantConfigs.CreateChildPermission(AppPermissions.Pages_SystemSecurity_SysTenantConfigs_Create, L("CreateNewItem"));
        sysTenantConfigs.CreateChildPermission(AppPermissions.Pages_SystemSecurity_SysTenantConfigs_Edit, L("EditItem"));
        sysTenantConfigs.CreateChildPermission(AppPermissions.Pages_SystemSecurity_SysTenantConfigs_Delete, L("DeleteItem"));
        //REPORTS
        pages.CreateChildPermission(AppPermissions.Pages_Reports_DesignerDevExpress, L("ReportDesignerDevExpress"));
        pages.CreateChildPermission(AppPermissions.Pages_Reports_PrintReports, L("PrintReportsLauncher"));

        //REPORT DISTRIBUTION (scheduling + bursting)
        var reportDistribution = pages.CreateChildPermission(AppPermissions.Pages_Reports_Distribution, L("ReportDistribution"));
        reportDistribution.CreateChildPermission(AppPermissions.Pages_Reports_Distribution_Create, L("CreateNewItem"));
        reportDistribution.CreateChildPermission(AppPermissions.Pages_Reports_Distribution_Edit, L("EditItem"));
        reportDistribution.CreateChildPermission(AppPermissions.Pages_Reports_Distribution_Delete, L("DeleteItem"));
        reportDistribution.CreateChildPermission(AppPermissions.Pages_Reports_Distribution_RunNow, L("RunScheduleNow"));
        reportDistribution.CreateChildPermission(
            AppPermissions.Pages_Reports_Distribution_BypassRecipientScope,
            L("BypassRecipientScope"));

        //ANALYSIS GRID REPORTS (ported from legacy HCM10.Web/UserControls/Reports/Analysis)
        var analysisReports = pages.CreateChildPermission(AppPermissions.Pages_Reports_Analysis, L("AnalysisReports"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeContract, L("EmployeeContractReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeAction, L("EmployeeActionReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeActionChange, L("EmployeeActionChangeReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeAsset, L("EmployeeAssetReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeAttendance, L("EmployeeAttendanceReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeBank, L("EmployeeBankReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeBasicPay, L("EmployeeBasicPayReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeBonus, L("EmployeeBonusReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeCTC, L("EmployeeCTCReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeDeduction, L("EmployeeDeductionReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeHistory, L("EmployeeHistoryReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeIncentive, L("EmployeeIncentiveReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeInfo, L("EmployeeInfoReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeManager, L("EmployeeManagerReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeePostWise, L("EmployeePostWiseReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeRejoin, L("EmployeeRejoinReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeSalaryChange, L("EmployeeSalaryChangeReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeSponsor, L("EmployeeSponsorReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeStatus, L("EmployeeStatusReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeTransfer, L("EmployeeTransferReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeWorkLocation, L("EmployeeWorkLocationReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeWorkflowRole, L("EmployeeWorkflowRoleReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_SalaryBenefits, L("SalaryBenefitsReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_SalaryByNationality, L("SalaryByNationalityReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_StaffHeadcount, L("StaffHeadcountReport"));
        //PayrollReport grouping (legacy HCM10.Web/UserControls/Reports/Analysis/PayrollReport).
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_BasicSalaryRegister, L("BasicSalaryRegisterReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_SalaryRegister, L("SalaryRegisterReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PayrollAnalysis, L("PayrollAnalysisReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PayrollAnalysisByDept, L("PayrollAnalysisByDeptReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PayrollSheet, L("PayrollSheetReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PayrollSheetCustom023, L("PayrollSheetCustom023Report"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom016LoanFormatBmc, L("PayrollJVCustom016LoanFormatBmcReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom016LoanFormatMyb, L("PayrollJVCustom016LoanFormatMybReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom016MainFormatBmc, L("PayrollJVCustom016MainFormatBmcReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom016MainFormatMyb, L("PayrollJVCustom016MainFormatMybReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom016Detail, L("PayrollJVCustom016DetailReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom016LeaveProvision, L("PayrollJVCustom016LeaveProvisionReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom016IndemnityProvision, L("PayrollJVCustom016IndemnityProvisionReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom017, L("PayrollJVCustom017Report"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom018, L("PayrollJVCustom018Report"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom019, L("PayrollJVCustom019Report"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom023, L("PayrollJVCustom023Report"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom030, L("PayrollJVCustom030Report"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom035, L("PayrollJVCustom035Report"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom045, L("PayrollJVCustom045Report"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom051, L("PayrollJVCustom051Report"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom042Salary, L("PayrollJVCustom042SalaryReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom042AdvanceSalary, L("PayrollJVCustom042AdvanceSalaryReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom042AnnualLeaveProvision, L("PayrollJVCustom042AnnualLeaveProvisionReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom042PublicHolidayLeaveProvision, L("PayrollJVCustom042PublicHolidayLeaveProvisionReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom042RamadanTimeBackLeaveProvision, L("PayrollJVCustom042RamadanTimeBackLeaveProvisionReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom042LieuDayLeaveProvision, L("PayrollJVCustom042LieuDayLeaveProvisionReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom042IndemnityProvision, L("PayrollJVCustom042IndemnityProvisionReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom042Overtime, L("PayrollJVCustom042OvertimeReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom042MissingAccounts, L("PayrollJVCustom042MissingAccountsReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom042EmployeeWiseBreakDown, L("PayrollJVCustom042EmployeeWiseBreakDownReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PayrollCTC, L("PayrollCTCReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PayrollNetPayContractDiff, L("PayrollNetPayContractDiffReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PrePayrollReport, L("PrePayrollReportReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_SalaryDeduction, L("SalaryDeductionReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_SalaryForecast, L("SalaryForecastReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_SalaryRetroResult, L("SalaryRetroResultReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_ServiceBonus, L("ServiceBonusReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_StaffNotinPayroll, L("StaffNotinPayrollReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_StaffOvertime, L("StaffOvertimeReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_StaffTurnover, L("StaffTurnoverReport"));
        //TimeReport grouping (legacy HCM10.Web/UserControls/Reports/Analysis/TimeReport).
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_MonthlyAttWithTolerance, L("MonthlyAttWithToleranceReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_MonthlyTimeAttendance, L("MonthlyTimeAttendanceReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_TimeAnalysisByDept, L("TimeAnalysisByDeptReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_TimeRegister, L("TimeRegisterReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_TimeRegisterApproval, L("TimeRegisterApprovalReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeDutyPeriod, L("EmployeeDutyPeriodReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_LateEarlyLog, L("LateEarlyLogReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_LateEarlyLogWithPhoto, L("LateEarlyLogWithPhotoReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_TimeSchedule, L("TimeScheduleReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_AttendanceLog, L("AttendanceLogReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmpAttendanceLog, L("EmpAttendanceLogReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_DailyAttendanceSummary, L("DailyAttendanceSummaryReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_DailyManHoursSummary, L("DailyManHoursSummaryReport"));
        //Per-customer TimeReport variants (legacy usc*_Custom0NN controls).
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_LateEarlyLogCustom040, L("LateEarlyLogCustom040Report"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_LateEarlyLogCustom045, L("LateEarlyLogCustom045Report"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_LateEarlyLogCustom053, L("LateEarlyLogCustom053Report"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_MonthlyAttWithToleranceCustom019, L("MonthlyAttWithToleranceCustom019Report"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_LateEarlyLogCustom019, L("LateEarlyLogCustom019Report"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_LateEarlyLogCustom023, L("LateEarlyLogCustom023Report"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_LateEarlyLogCustom033, L("LateEarlyLogCustom033Report"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_LateEarlyLogCustom036, L("LateEarlyLogCustom036Report"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_Asset, L("AssetReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeTravel, L("EmployeeTravelReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeDocMovement, L("EmployeeDocMovementReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeFnFPayment, L("EmployeeFnFPaymentReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeNewJoiner, L("EmployeeNewJoinerReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeOnHold, L("EmployeeOnHoldReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeePassport, L("EmployeePassportReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeProbation, L("EmployeeProbationReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeResidency, L("EmployeeResidencyReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeSeparation, L("EmployeeSeparationReport"));
        //Transaction analysis reports (legacy HCM10.Web/UserControls/Reports/Analysis/Transaction).
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_News, L("NewsReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeAssetInvoice, L("EmployeeAssetInvoiceReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeDisciplinary, L("EmployeeDisciplinaryReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeDocument, L("EmployeeDocumentReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeDocumentCustom033, L("EmployeeDocumentCustom033Report"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeHealthReimburse, L("EmployeeHealthReimburseReport"));
        //Organization/Workflow analysis reports (legacy HCM10.Web/UserControls/Reports/Analysis/Organization, /Workflow).
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_OrgUnitTree, L("OrgUnitTreeReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_OrganizationDocument, L("OrganizationDocumentReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeRequests, L("EmployeeRequestsReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_LeaveRequestAnalysis, L("LeaveRequestAnalysisReport"));
        //Transaction analysis reports (legacy HCM10.Web/UserControls/Reports/Analysis/Transaction).
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeLeaveSalary, L("EmployeeLeaveSalaryReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeLoan, L("EmployeeLoanReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeNonPayroll, L("EmployeeNonPayrollReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeePayment, L("EmployeePaymentReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeSocialSecurity, L("EmployeeSocialSecurityReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmpOvertime, L("EmpOvertimeReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmpOvertimeByDay, L("EmpOvertimeByDayReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_MonthlyPayrollTransaction, L("MonthlyPayrollTransactionReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_AirTicketQuotaBalance, L("AirTicketQuotaBalanceReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_AnnualLeaveSheet, L("AnnualLeaveSheetReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EndOfService, L("EndOfServiceReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_IndemnityProvision, L("IndemnityProvisionReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_IndemnityProvisionAsOnDate, L("IndemnityProvisionAsOnDateReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_LeaveProvision, L("LeaveProvisionReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_LeaveQuotaBalance, L("LeaveQuotaBalanceReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_ProvisionChange, L("ProvisionChangeReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_QuotaProvisionAsOnDate, L("QuotaProvisionAsOnDateReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_YearlyLeaveIndemnityDetailCustom016, L("YearlyLeaveIndemnityDetailCustom016Report"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_YearlyLeaveIndemnityDetailCustom023, L("YearlyLeaveIndemnityDetailCustom023Report"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PAEmployee, L("PAEmployeeReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PAEmployeeScorecard, L("PAEmployeeScorecardReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PASummary, L("PASummaryReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PerformanceAppraisal, L("PerformanceAppraisalReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PerformanceAppraisalReviewerSummary, L("PerformanceAppraisalReviewerSummaryReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_PerformanceAppraisalManagerEvaluation, L("PerformanceAppraisalManagerEvaluationReport"));

        //ADMINISTRATION TOOLS (ported from legacy HCM10.Web/UserControls/Administration)
        var administrationTools = pages.CreateChildPermission(AppPermissions.Pages_Administration_Tools, L("AdministrationTools"));
        administrationTools.CreateChildPermission(AppPermissions.Pages_Administration_Tools_GeneratePayrollCalendar, L("GeneratingPayrollCalendar"));
        administrationTools.CreateChildPermission(AppPermissions.Pages_Administration_Tools_DeleteSalaryRegister, L("DeletingSalaryRegisters"));
        administrationTools.CreateChildPermission(AppPermissions.Pages_Administration_Tools_UnlockSalaryRegister, L("UnlockingSalaryRegisters"));
        administrationTools.CreateChildPermission(AppPermissions.Pages_Administration_Tools_FixForfeitQuota, L("FixingForfeitQuota"));
        administrationTools.CreateChildPermission(AppPermissions.Pages_Administration_Tools_FixPayrollProvisions, L("FixingPayrollProvisions"));
        administrationTools.CreateChildPermission(AppPermissions.Pages_Administration_Tools_RecalculateProbation, L("RecalculatingProbationEndDates"));
        administrationTools.CreateChildPermission(AppPermissions.Pages_Administration_Tools_TimeAttendanceResync, L("ResyncingTimeAttendanceData"));
        administrationTools.CreateChildPermission(AppPermissions.Pages_Administration_Tools_TransferApprovals, L("TransferringPendingApprovals"));
        administrationTools.CreateChildPermission(AppPermissions.Pages_Administration_Tools_BulkPasswordReset, L("BulkResettingPasswords"));
        administrationTools.CreateChildPermission(AppPermissions.Pages_Administration_Tools_CopyWorkflowSetup, L("CopyingWorkflowSetup"));

        // Legacy HCM10.Web/UserControls/ImportExport/. See AppPermissions for why Import and Export are
        // separate children.
        var importExport = pages.CreateChildPermission(AppPermissions.Pages_ImportExport, L("ImportExport"));

        var bulkImportExport = importExport.CreateChildPermission(AppPermissions.Pages_ImportExport_BulkImportExport, L("BulkImportExport"));
        bulkImportExport.CreateChildPermission(AppPermissions.Pages_ImportExport_BulkImportExport_Import, L("RunningBulkImport"));
        bulkImportExport.CreateChildPermission(AppPermissions.Pages_ImportExport_BulkImportExport_Export, L("RunningBulkExport"));

        importExport.CreateChildPermission(AppPermissions.Pages_ImportExport_ProfilePhotoImport, L("RunningProfilePhotoImport"));
        importExport.CreateChildPermission(AppPermissions.Pages_ImportExport_TimesheetImport, L("RunningTimesheetImport"));

        // ProjectReport / Recruitment / AccommodationReport analysis grid reports (legacy
        // HCM10.Web/UserControls/Reports/Analysis/ProjectReport, /Recruitment, /AccommodationReport).
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_ProjectCosting, L("ProjectCostingReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_ProjectCostingCustom052, L("ProjectCostingCustom052Report"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_ProjectMonthwiseSummary, L("ProjectMonthwiseSummaryReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_ProjectWiseAccrual, L("ProjectWiseAccrualReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_ApplicantSearch, L("ApplicantSearchReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_CandidateOnbInit, L("CandidateOnbInitReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_VacancyStatus, L("VacancyStatusReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeAccommodation, L("EmployeeAccommodationReport"));

        // TrainingReport / SurveyReport / Administration / GovtRelations analysis grid reports.
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeTraining, L("EmployeeTrainingReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_TrainingCourse, L("TrainingCourseReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_TrainingSchedule, L("TrainingScheduleReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_EmployeeSurvey, L("EmployeeSurveyReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_SurveyStatus, L("SurveyStatusReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_AuditReport, L("AuditReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_ExecutionLogReport, L("ExecutionLogReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_Sponsor, L("SponsorReport"));
        analysisReports.CreateChildPermission(AppPermissions.Pages_Reports_Analysis_SponsorJobTitleQuota, L("SponsorJobTitleQuotaReport"));
    }

    private static ILocalizableString L(string name)
    {
        return new LocalizableString(name, HCMv3Consts.LocalizationSourceName);
    }
}

