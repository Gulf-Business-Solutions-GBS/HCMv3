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
    }

    private static ILocalizableString L(string name)
    {
        return new LocalizableString(name, HCMv3Consts.LocalizationSourceName);
    }
}

