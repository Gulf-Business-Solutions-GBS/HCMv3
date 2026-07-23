namespace HCMv3.Authorization;

/// <summary>
/// Defines string constants for application's permission names.
/// <see cref="AppAuthorizationProvider"/> for permission definitions.
/// </summary>
public static class AppPermissions
{
    //COMMON PERMISSIONS (FOR BOTH OF TENANTS AND HOST)

    public const string Pages = "Pages";

    public const string Pages_DemoUiComponents = "Pages.DemoUiComponents";
    public const string Pages_Administration = "Pages.Administration";

    //GENERAL MASTER INFO
    public const string Pages_GeneralMasterInfo = "Pages.GeneralMasterInfo";

    public const string Pages_GeneralMasterInfo_AccessUnits = "Pages.GeneralMasterInfo.AccessUnits";
    public const string Pages_GeneralMasterInfo_AccessUnits_Create = "Pages.GeneralMasterInfo.AccessUnits.Create";
    public const string Pages_GeneralMasterInfo_AccessUnits_Edit = "Pages.GeneralMasterInfo.AccessUnits.Edit";
    public const string Pages_GeneralMasterInfo_AccessUnits_Delete = "Pages.GeneralMasterInfo.AccessUnits.Delete";

    public const string Pages_GeneralMasterInfo_Countries = "Pages.GeneralMasterInfo.Countries";
    public const string Pages_GeneralMasterInfo_Countries_Create = "Pages.GeneralMasterInfo.Countries.Create";
    public const string Pages_GeneralMasterInfo_Countries_Edit = "Pages.GeneralMasterInfo.Countries.Edit";
    public const string Pages_GeneralMasterInfo_Countries_Delete = "Pages.GeneralMasterInfo.Countries.Delete";
    public const string Pages_GeneralMasterInfo_BankFiles = "Pages.GeneralMasterInfo.BankFiles";
    public const string Pages_GeneralMasterInfo_BankFiles_Create = "Pages.GeneralMasterInfo.BankFiles.Create";
    public const string Pages_GeneralMasterInfo_BankFiles_Edit = "Pages.GeneralMasterInfo.BankFiles.Edit";
    public const string Pages_GeneralMasterInfo_BankFiles_Delete = "Pages.GeneralMasterInfo.BankFiles.Delete";
    public const string Pages_GeneralMasterInfo_BenefitTypes = "Pages.GeneralMasterInfo.BenefitTypes";
    public const string Pages_GeneralMasterInfo_BenefitTypes_Create = "Pages.GeneralMasterInfo.BenefitTypes.Create";
    public const string Pages_GeneralMasterInfo_BenefitTypes_Edit = "Pages.GeneralMasterInfo.BenefitTypes.Edit";
    public const string Pages_GeneralMasterInfo_BenefitTypes_Delete = "Pages.GeneralMasterInfo.BenefitTypes.Delete";
    public const string Pages_GeneralMasterInfo_ContractTypes = "Pages.GeneralMasterInfo.ContractTypes";
    public const string Pages_GeneralMasterInfo_ContractTypes_Create = "Pages.GeneralMasterInfo.ContractTypes.Create";
    public const string Pages_GeneralMasterInfo_ContractTypes_Edit = "Pages.GeneralMasterInfo.ContractTypes.Edit";
    public const string Pages_GeneralMasterInfo_ContractTypes_Delete = "Pages.GeneralMasterInfo.ContractTypes.Delete";
    public const string Pages_GeneralMasterInfo_DateTypes = "Pages.GeneralMasterInfo.DateTypes";
    public const string Pages_GeneralMasterInfo_DateTypes_Create = "Pages.GeneralMasterInfo.DateTypes.Create";
    public const string Pages_GeneralMasterInfo_DateTypes_Edit = "Pages.GeneralMasterInfo.DateTypes.Edit";
    public const string Pages_GeneralMasterInfo_DateTypes_Delete = "Pages.GeneralMasterInfo.DateTypes.Delete";
    public const string Pages_GeneralMasterInfo_DocumentPlaces = "Pages.GeneralMasterInfo.DocumentPlaces";
    public const string Pages_GeneralMasterInfo_DocumentPlaces_Create = "Pages.GeneralMasterInfo.DocumentPlaces.Create";
    public const string Pages_GeneralMasterInfo_DocumentPlaces_Edit = "Pages.GeneralMasterInfo.DocumentPlaces.Edit";
    public const string Pages_GeneralMasterInfo_DocumentPlaces_Delete = "Pages.GeneralMasterInfo.DocumentPlaces.Delete";
    public const string Pages_GeneralMasterInfo_Educations = "Pages.GeneralMasterInfo.Educations";
    public const string Pages_GeneralMasterInfo_Educations_Create = "Pages.GeneralMasterInfo.Educations.Create";
    public const string Pages_GeneralMasterInfo_Educations_Edit = "Pages.GeneralMasterInfo.Educations.Edit";
    public const string Pages_GeneralMasterInfo_Educations_Delete = "Pages.GeneralMasterInfo.Educations.Delete";
    public const string Pages_GeneralMasterInfo_EducationBranches = "Pages.GeneralMasterInfo.EducationBranches";
    public const string Pages_GeneralMasterInfo_EducationBranches_Create = "Pages.GeneralMasterInfo.EducationBranches.Create";
    public const string Pages_GeneralMasterInfo_EducationBranches_Edit = "Pages.GeneralMasterInfo.EducationBranches.Edit";
    public const string Pages_GeneralMasterInfo_EducationBranches_Delete = "Pages.GeneralMasterInfo.EducationBranches.Delete";
    public const string Pages_GeneralMasterInfo_Governorates = "Pages.GeneralMasterInfo.Governorates";
    public const string Pages_GeneralMasterInfo_Governorates_Create = "Pages.GeneralMasterInfo.Governorates.Create";
    public const string Pages_GeneralMasterInfo_Governorates_Edit = "Pages.GeneralMasterInfo.Governorates.Edit";
    public const string Pages_GeneralMasterInfo_Governorates_Delete = "Pages.GeneralMasterInfo.Governorates.Delete";
    public const string Pages_GeneralMasterInfo_GrievanceTypes = "Pages.GeneralMasterInfo.GrievanceTypes";
    public const string Pages_GeneralMasterInfo_GrievanceTypes_Create = "Pages.GeneralMasterInfo.GrievanceTypes.Create";
    public const string Pages_GeneralMasterInfo_GrievanceTypes_Edit = "Pages.GeneralMasterInfo.GrievanceTypes.Edit";
    public const string Pages_GeneralMasterInfo_GrievanceTypes_Delete = "Pages.GeneralMasterInfo.GrievanceTypes.Delete";
    public const string Pages_GeneralMasterInfo_HRServiceTypes = "Pages.GeneralMasterInfo.HRServiceTypes";
    public const string Pages_GeneralMasterInfo_HRServiceTypes_Create = "Pages.GeneralMasterInfo.HRServiceTypes.Create";
    public const string Pages_GeneralMasterInfo_HRServiceTypes_Edit = "Pages.GeneralMasterInfo.HRServiceTypes.Edit";
    public const string Pages_GeneralMasterInfo_HRServiceTypes_Delete = "Pages.GeneralMasterInfo.HRServiceTypes.Delete";
    public const string Pages_GeneralMasterInfo_ITServiceTypes = "Pages.GeneralMasterInfo.ITServiceTypes";
    public const string Pages_GeneralMasterInfo_ITServiceTypes_Create = "Pages.GeneralMasterInfo.ITServiceTypes.Create";
    public const string Pages_GeneralMasterInfo_ITServiceTypes_Edit = "Pages.GeneralMasterInfo.ITServiceTypes.Edit";
    public const string Pages_GeneralMasterInfo_ITServiceTypes_Delete = "Pages.GeneralMasterInfo.ITServiceTypes.Delete";
    public const string Pages_GeneralMasterInfo_LanguageInfos = "Pages.GeneralMasterInfo.LanguageInfos";
    public const string Pages_GeneralMasterInfo_LanguageInfos_Create = "Pages.GeneralMasterInfo.LanguageInfos.Create";
    public const string Pages_GeneralMasterInfo_LanguageInfos_Edit = "Pages.GeneralMasterInfo.LanguageInfos.Edit";
    public const string Pages_GeneralMasterInfo_LanguageInfos_Delete = "Pages.GeneralMasterInfo.LanguageInfos.Delete";
    public const string Pages_GeneralMasterInfo_MaritalStatuses = "Pages.GeneralMasterInfo.MaritalStatuses";
    public const string Pages_GeneralMasterInfo_MaritalStatuses_Create = "Pages.GeneralMasterInfo.MaritalStatuses.Create";
    public const string Pages_GeneralMasterInfo_MaritalStatuses_Edit = "Pages.GeneralMasterInfo.MaritalStatuses.Edit";
    public const string Pages_GeneralMasterInfo_MaritalStatuses_Delete = "Pages.GeneralMasterInfo.MaritalStatuses.Delete";
    public const string Pages_GeneralMasterInfo_NationalityGroups = "Pages.GeneralMasterInfo.NationalityGroups";
    public const string Pages_GeneralMasterInfo_NationalityGroups_Create = "Pages.GeneralMasterInfo.NationalityGroups.Create";
    public const string Pages_GeneralMasterInfo_NationalityGroups_Edit = "Pages.GeneralMasterInfo.NationalityGroups.Edit";
    public const string Pages_GeneralMasterInfo_NationalityGroups_Delete = "Pages.GeneralMasterInfo.NationalityGroups.Delete";
    public const string Pages_GeneralMasterInfo_NewsTypes = "Pages.GeneralMasterInfo.NewsTypes";
    public const string Pages_GeneralMasterInfo_NewsTypes_Create = "Pages.GeneralMasterInfo.NewsTypes.Create";
    public const string Pages_GeneralMasterInfo_NewsTypes_Edit = "Pages.GeneralMasterInfo.NewsTypes.Edit";
    public const string Pages_GeneralMasterInfo_NewsTypes_Delete = "Pages.GeneralMasterInfo.NewsTypes.Delete";
    public const string Pages_GeneralMasterInfo_PassportTypes = "Pages.GeneralMasterInfo.PassportTypes";
    public const string Pages_GeneralMasterInfo_PassportTypes_Create = "Pages.GeneralMasterInfo.PassportTypes.Create";
    public const string Pages_GeneralMasterInfo_PassportTypes_Edit = "Pages.GeneralMasterInfo.PassportTypes.Edit";
    public const string Pages_GeneralMasterInfo_PassportTypes_Delete = "Pages.GeneralMasterInfo.PassportTypes.Delete";
    public const string Pages_GeneralMasterInfo_PostingGroups = "Pages.GeneralMasterInfo.PostingGroups";
    public const string Pages_GeneralMasterInfo_PostingGroups_Create = "Pages.GeneralMasterInfo.PostingGroups.Create";
    public const string Pages_GeneralMasterInfo_PostingGroups_Edit = "Pages.GeneralMasterInfo.PostingGroups.Edit";
    public const string Pages_GeneralMasterInfo_PostingGroups_Delete = "Pages.GeneralMasterInfo.PostingGroups.Delete";
    public const string Pages_GeneralMasterInfo_Qualifications = "Pages.GeneralMasterInfo.Qualifications";
    public const string Pages_GeneralMasterInfo_Qualifications_Create = "Pages.GeneralMasterInfo.Qualifications.Create";
    public const string Pages_GeneralMasterInfo_Qualifications_Edit = "Pages.GeneralMasterInfo.Qualifications.Edit";
    public const string Pages_GeneralMasterInfo_Qualifications_Delete = "Pages.GeneralMasterInfo.Qualifications.Delete";
    public const string Pages_GeneralMasterInfo_RecruitmentTypes = "Pages.GeneralMasterInfo.RecruitmentTypes";
    public const string Pages_GeneralMasterInfo_RecruitmentTypes_Create = "Pages.GeneralMasterInfo.RecruitmentTypes.Create";
    public const string Pages_GeneralMasterInfo_RecruitmentTypes_Edit = "Pages.GeneralMasterInfo.RecruitmentTypes.Edit";
    public const string Pages_GeneralMasterInfo_RecruitmentTypes_Delete = "Pages.GeneralMasterInfo.RecruitmentTypes.Delete";
    public const string Pages_GeneralMasterInfo_RelationshipTypes = "Pages.GeneralMasterInfo.RelationshipTypes";
    public const string Pages_GeneralMasterInfo_RelationshipTypes_Create = "Pages.GeneralMasterInfo.RelationshipTypes.Create";
    public const string Pages_GeneralMasterInfo_RelationshipTypes_Edit = "Pages.GeneralMasterInfo.RelationshipTypes.Edit";
    public const string Pages_GeneralMasterInfo_RelationshipTypes_Delete = "Pages.GeneralMasterInfo.RelationshipTypes.Delete";
    public const string Pages_GeneralMasterInfo_Religions = "Pages.GeneralMasterInfo.Religions";
    public const string Pages_GeneralMasterInfo_Religions_Create = "Pages.GeneralMasterInfo.Religions.Create";
    public const string Pages_GeneralMasterInfo_Religions_Edit = "Pages.GeneralMasterInfo.Religions.Edit";
    public const string Pages_GeneralMasterInfo_Religions_Delete = "Pages.GeneralMasterInfo.Religions.Delete";
    public const string Pages_GeneralMasterInfo_States = "Pages.GeneralMasterInfo.States";
    public const string Pages_GeneralMasterInfo_States_Create = "Pages.GeneralMasterInfo.States.Create";
    public const string Pages_GeneralMasterInfo_States_Edit = "Pages.GeneralMasterInfo.States.Edit";
    public const string Pages_GeneralMasterInfo_States_Delete = "Pages.GeneralMasterInfo.States.Delete";
    public const string Pages_GeneralMasterInfo_TaskTypes = "Pages.GeneralMasterInfo.TaskTypes";
    public const string Pages_GeneralMasterInfo_TaskTypes_Create = "Pages.GeneralMasterInfo.TaskTypes.Create";
    public const string Pages_GeneralMasterInfo_TaskTypes_Edit = "Pages.GeneralMasterInfo.TaskTypes.Edit";
    public const string Pages_GeneralMasterInfo_TaskTypes_Delete = "Pages.GeneralMasterInfo.TaskTypes.Delete";
    public const string Pages_GeneralMasterInfo_Titles = "Pages.GeneralMasterInfo.Titles";
    public const string Pages_GeneralMasterInfo_Titles_Create = "Pages.GeneralMasterInfo.Titles.Create";
    public const string Pages_GeneralMasterInfo_Titles_Edit = "Pages.GeneralMasterInfo.Titles.Edit";
    public const string Pages_GeneralMasterInfo_Titles_Delete = "Pages.GeneralMasterInfo.Titles.Delete";
    public const string Pages_GeneralMasterInfo_TrainingTypes = "Pages.GeneralMasterInfo.TrainingTypes";
    public const string Pages_GeneralMasterInfo_TrainingTypes_Create = "Pages.GeneralMasterInfo.TrainingTypes.Create";
    public const string Pages_GeneralMasterInfo_TrainingTypes_Edit = "Pages.GeneralMasterInfo.TrainingTypes.Edit";
    public const string Pages_GeneralMasterInfo_TrainingTypes_Delete = "Pages.GeneralMasterInfo.TrainingTypes.Delete";
    public const string Pages_GeneralMasterInfo_Universities = "Pages.GeneralMasterInfo.Universities";
    public const string Pages_GeneralMasterInfo_Universities_Create = "Pages.GeneralMasterInfo.Universities.Create";
    public const string Pages_GeneralMasterInfo_Universities_Edit = "Pages.GeneralMasterInfo.Universities.Edit";
    public const string Pages_GeneralMasterInfo_Universities_Delete = "Pages.GeneralMasterInfo.Universities.Delete";
    public const string Pages_GeneralMasterInfo_VisaTypes = "Pages.GeneralMasterInfo.VisaTypes";
    public const string Pages_GeneralMasterInfo_VisaTypes_Create = "Pages.GeneralMasterInfo.VisaTypes.Create";
    public const string Pages_GeneralMasterInfo_VisaTypes_Edit = "Pages.GeneralMasterInfo.VisaTypes.Edit";
    public const string Pages_GeneralMasterInfo_VisaTypes_Delete = "Pages.GeneralMasterInfo.VisaTypes.Delete";
    public const string Pages_GeneralMasterInfo_WorkVisaTypes = "Pages.GeneralMasterInfo.WorkVisaTypes";
    public const string Pages_GeneralMasterInfo_WorkVisaTypes_Create = "Pages.GeneralMasterInfo.WorkVisaTypes.Create";
    public const string Pages_GeneralMasterInfo_WorkVisaTypes_Edit = "Pages.GeneralMasterInfo.WorkVisaTypes.Edit";
    public const string Pages_GeneralMasterInfo_WorkVisaTypes_Delete = "Pages.GeneralMasterInfo.WorkVisaTypes.Delete";
    public const string Pages_GeneralMasterInfo_AssetTypes = "Pages.GeneralMasterInfo.AssetTypes";
    public const string Pages_GeneralMasterInfo_AssetTypes_Create = "Pages.GeneralMasterInfo.AssetTypes.Create";
    public const string Pages_GeneralMasterInfo_AssetTypes_Edit = "Pages.GeneralMasterInfo.AssetTypes.Edit";
    public const string Pages_GeneralMasterInfo_AssetTypes_Delete = "Pages.GeneralMasterInfo.AssetTypes.Delete";
    public const string Pages_GeneralMasterInfo_Cities = "Pages.GeneralMasterInfo.Cities";
    public const string Pages_GeneralMasterInfo_Cities_Create = "Pages.GeneralMasterInfo.Cities.Create";
    public const string Pages_GeneralMasterInfo_Cities_Edit = "Pages.GeneralMasterInfo.Cities.Edit";
    public const string Pages_GeneralMasterInfo_Cities_Delete = "Pages.GeneralMasterInfo.Cities.Delete";
    public const string Pages_GeneralMasterInfo_Institutions = "Pages.GeneralMasterInfo.Institutions";
    public const string Pages_GeneralMasterInfo_Institutions_Create = "Pages.GeneralMasterInfo.Institutions.Create";
    public const string Pages_GeneralMasterInfo_Institutions_Edit = "Pages.GeneralMasterInfo.Institutions.Edit";
    public const string Pages_GeneralMasterInfo_Institutions_Delete = "Pages.GeneralMasterInfo.Institutions.Delete";
    public const string Pages_GeneralMasterInfo_Nationalities = "Pages.GeneralMasterInfo.Nationalities";
    public const string Pages_GeneralMasterInfo_Nationalities_Create = "Pages.GeneralMasterInfo.Nationalities.Create";
    public const string Pages_GeneralMasterInfo_Nationalities_Edit = "Pages.GeneralMasterInfo.Nationalities.Edit";
    public const string Pages_GeneralMasterInfo_Nationalities_Delete = "Pages.GeneralMasterInfo.Nationalities.Delete";
    public const string Pages_GeneralMasterInfo_DependantTypes = "Pages.GeneralMasterInfo.DependantTypes";
    public const string Pages_GeneralMasterInfo_DependantTypes_Create = "Pages.GeneralMasterInfo.DependantTypes.Create";
    public const string Pages_GeneralMasterInfo_DependantTypes_Edit = "Pages.GeneralMasterInfo.DependantTypes.Edit";
    public const string Pages_GeneralMasterInfo_DependantTypes_Delete = "Pages.GeneralMasterInfo.DependantTypes.Delete";
    public const string Pages_GeneralMasterInfo_Currencies = "Pages.GeneralMasterInfo.Currencies";
    public const string Pages_GeneralMasterInfo_Currencies_Create = "Pages.GeneralMasterInfo.Currencies.Create";
    public const string Pages_GeneralMasterInfo_Currencies_Edit = "Pages.GeneralMasterInfo.Currencies.Edit";
    public const string Pages_GeneralMasterInfo_Currencies_Delete = "Pages.GeneralMasterInfo.Currencies.Delete";
    public const string Pages_GeneralMasterInfo_SeparationReasons = "Pages.GeneralMasterInfo.SeparationReasons";
    public const string Pages_GeneralMasterInfo_SeparationReasons_Create = "Pages.GeneralMasterInfo.SeparationReasons.Create";
    public const string Pages_GeneralMasterInfo_SeparationReasons_Edit = "Pages.GeneralMasterInfo.SeparationReasons.Edit";
    public const string Pages_GeneralMasterInfo_SeparationReasons_Delete = "Pages.GeneralMasterInfo.SeparationReasons.Delete";
    public const string Pages_GeneralMasterInfo_DisciplinaryDeductionTypes = "Pages.GeneralMasterInfo.DisciplinaryDeductionTypes";
    public const string Pages_GeneralMasterInfo_DisciplinaryDeductionTypes_Create = "Pages.GeneralMasterInfo.DisciplinaryDeductionTypes.Create";
    public const string Pages_GeneralMasterInfo_DisciplinaryDeductionTypes_Edit = "Pages.GeneralMasterInfo.DisciplinaryDeductionTypes.Edit";
    public const string Pages_GeneralMasterInfo_DisciplinaryDeductionTypes_Delete = "Pages.GeneralMasterInfo.DisciplinaryDeductionTypes.Delete";
    public const string Pages_GeneralMasterInfo_PersonalActions = "Pages.GeneralMasterInfo.PersonalActions";
    public const string Pages_GeneralMasterInfo_PersonalActions_Create = "Pages.GeneralMasterInfo.PersonalActions.Create";
    public const string Pages_GeneralMasterInfo_PersonalActions_Edit = "Pages.GeneralMasterInfo.PersonalActions.Edit";
    public const string Pages_GeneralMasterInfo_PersonalActions_Delete = "Pages.GeneralMasterInfo.PersonalActions.Delete";
    public const string Pages_GeneralMasterInfo_PersonalActionReasons = "Pages.GeneralMasterInfo.PersonalActionReasons";
    public const string Pages_GeneralMasterInfo_PersonalActionReasons_Create = "Pages.GeneralMasterInfo.PersonalActionReasons.Create";
    public const string Pages_GeneralMasterInfo_PersonalActionReasons_Edit = "Pages.GeneralMasterInfo.PersonalActionReasons.Edit";
    public const string Pages_GeneralMasterInfo_PersonalActionReasons_Delete = "Pages.GeneralMasterInfo.PersonalActionReasons.Delete";
    public const string Pages_GeneralMasterInfo_Banks = "Pages.GeneralMasterInfo.Banks";
    public const string Pages_GeneralMasterInfo_Banks_Create = "Pages.GeneralMasterInfo.Banks.Create";
    public const string Pages_GeneralMasterInfo_Banks_Edit = "Pages.GeneralMasterInfo.Banks.Edit";
    public const string Pages_GeneralMasterInfo_Banks_Delete = "Pages.GeneralMasterInfo.Banks.Delete";
    public const string Pages_GeneralMasterInfo_PayeeBankDetails = "Pages.GeneralMasterInfo.PayeeBankDetails";
    public const string Pages_GeneralMasterInfo_PayeeBankDetails_Create = "Pages.GeneralMasterInfo.PayeeBankDetails.Create";
    public const string Pages_GeneralMasterInfo_PayeeBankDetails_Edit = "Pages.GeneralMasterInfo.PayeeBankDetails.Edit";
    public const string Pages_GeneralMasterInfo_PayeeBankDetails_Delete = "Pages.GeneralMasterInfo.PayeeBankDetails.Delete";
    public const string Pages_GeneralMasterInfo_BudgetingGroups = "Pages.GeneralMasterInfo.BudgetingGroups";
    public const string Pages_GeneralMasterInfo_BudgetingGroups_Create = "Pages.GeneralMasterInfo.BudgetingGroups.Create";
    public const string Pages_GeneralMasterInfo_BudgetingGroups_Edit = "Pages.GeneralMasterInfo.BudgetingGroups.Edit";
    public const string Pages_GeneralMasterInfo_BudgetingGroups_Delete = "Pages.GeneralMasterInfo.BudgetingGroups.Delete";
    public const string Pages_GeneralMasterInfo_BudgetingGroupRateDetails = "Pages.GeneralMasterInfo.BudgetingGroupRateDetails";
    public const string Pages_GeneralMasterInfo_BudgetingGroupRateDetails_Create = "Pages.GeneralMasterInfo.BudgetingGroupRateDetails.Create";
    public const string Pages_GeneralMasterInfo_BudgetingGroupRateDetails_Edit = "Pages.GeneralMasterInfo.BudgetingGroupRateDetails.Edit";
    public const string Pages_GeneralMasterInfo_BudgetingGroupRateDetails_Delete = "Pages.GeneralMasterInfo.BudgetingGroupRateDetails.Delete";
    public const string Pages_GeneralMasterInfo_ClearanceRequestTypes = "Pages.GeneralMasterInfo.ClearanceRequestTypes";
    public const string Pages_GeneralMasterInfo_ClearanceRequestTypes_Create = "Pages.GeneralMasterInfo.ClearanceRequestTypes.Create";
    public const string Pages_GeneralMasterInfo_ClearanceRequestTypes_Edit = "Pages.GeneralMasterInfo.ClearanceRequestTypes.Edit";
    public const string Pages_GeneralMasterInfo_ClearanceRequestTypes_Delete = "Pages.GeneralMasterInfo.ClearanceRequestTypes.Delete";
    public const string Pages_GeneralMasterInfo_ClearanceRequestFields = "Pages.GeneralMasterInfo.ClearanceRequestFields";
    public const string Pages_GeneralMasterInfo_ClearanceRequestFields_Create = "Pages.GeneralMasterInfo.ClearanceRequestFields.Create";
    public const string Pages_GeneralMasterInfo_ClearanceRequestFields_Edit = "Pages.GeneralMasterInfo.ClearanceRequestFields.Edit";
    public const string Pages_GeneralMasterInfo_ClearanceRequestFields_Delete = "Pages.GeneralMasterInfo.ClearanceRequestFields.Delete";
    public const string Pages_GeneralMasterInfo_ClearanceRequestFieldDetails = "Pages.GeneralMasterInfo.ClearanceRequestFieldDetails";
    public const string Pages_GeneralMasterInfo_ClearanceRequestFieldDetails_Create = "Pages.GeneralMasterInfo.ClearanceRequestFieldDetails.Create";
    public const string Pages_GeneralMasterInfo_ClearanceRequestFieldDetails_Edit = "Pages.GeneralMasterInfo.ClearanceRequestFieldDetails.Edit";
    public const string Pages_GeneralMasterInfo_ClearanceRequestFieldDetails_Delete = "Pages.GeneralMasterInfo.ClearanceRequestFieldDetails.Delete";
    public const string Pages_GeneralMasterInfo_DisciplinaryTypes = "Pages.GeneralMasterInfo.DisciplinaryTypes";
    public const string Pages_GeneralMasterInfo_DisciplinaryTypes_Create = "Pages.GeneralMasterInfo.DisciplinaryTypes.Create";
    public const string Pages_GeneralMasterInfo_DisciplinaryTypes_Edit = "Pages.GeneralMasterInfo.DisciplinaryTypes.Edit";
    public const string Pages_GeneralMasterInfo_DisciplinaryTypes_Delete = "Pages.GeneralMasterInfo.DisciplinaryTypes.Delete";
    public const string Pages_GeneralMasterInfo_DisciplinaryTypeDetails = "Pages.GeneralMasterInfo.DisciplinaryTypeDetails";
    public const string Pages_GeneralMasterInfo_DisciplinaryTypeDetails_Create = "Pages.GeneralMasterInfo.DisciplinaryTypeDetails.Create";
    public const string Pages_GeneralMasterInfo_DisciplinaryTypeDetails_Edit = "Pages.GeneralMasterInfo.DisciplinaryTypeDetails.Edit";
    public const string Pages_GeneralMasterInfo_DisciplinaryTypeDetails_Delete = "Pages.GeneralMasterInfo.DisciplinaryTypeDetails.Delete";
    public const string Pages_GeneralMasterInfo_NotificationConfigs = "Pages.GeneralMasterInfo.NotificationConfigs";
    public const string Pages_GeneralMasterInfo_NotificationConfigs_Create = "Pages.GeneralMasterInfo.NotificationConfigs.Create";
    public const string Pages_GeneralMasterInfo_NotificationConfigs_Edit = "Pages.GeneralMasterInfo.NotificationConfigs.Edit";
    public const string Pages_GeneralMasterInfo_NotificationConfigs_Delete = "Pages.GeneralMasterInfo.NotificationConfigs.Delete";
    public const string Pages_GeneralMasterInfo_NotificationGroups = "Pages.GeneralMasterInfo.NotificationGroups";
    public const string Pages_GeneralMasterInfo_NotificationGroups_Create = "Pages.GeneralMasterInfo.NotificationGroups.Create";
    public const string Pages_GeneralMasterInfo_NotificationGroups_Edit = "Pages.GeneralMasterInfo.NotificationGroups.Edit";
    public const string Pages_GeneralMasterInfo_NotificationGroups_Delete = "Pages.GeneralMasterInfo.NotificationGroups.Delete";
    public const string Pages_GeneralMasterInfo_DocumentTypes = "Pages.GeneralMasterInfo.DocumentTypes";
    public const string Pages_GeneralMasterInfo_DocumentTypes_Create = "Pages.GeneralMasterInfo.DocumentTypes.Create";
    public const string Pages_GeneralMasterInfo_DocumentTypes_Edit = "Pages.GeneralMasterInfo.DocumentTypes.Edit";
    public const string Pages_GeneralMasterInfo_DocumentTypes_Delete = "Pages.GeneralMasterInfo.DocumentTypes.Delete";
    public const string Pages_GeneralMasterInfo_DocumentPrintForms = "Pages.GeneralMasterInfo.DocumentPrintForms";
    public const string Pages_GeneralMasterInfo_DocumentPrintForms_Create = "Pages.GeneralMasterInfo.DocumentPrintForms.Create";
    public const string Pages_GeneralMasterInfo_DocumentPrintForms_Edit = "Pages.GeneralMasterInfo.DocumentPrintForms.Edit";
    public const string Pages_GeneralMasterInfo_DocumentPrintForms_Delete = "Pages.GeneralMasterInfo.DocumentPrintForms.Delete";
    public const string Pages_GeneralMasterInfo_LoanTypes = "Pages.GeneralMasterInfo.LoanTypes";
    public const string Pages_GeneralMasterInfo_LoanTypes_Create = "Pages.GeneralMasterInfo.LoanTypes.Create";
    public const string Pages_GeneralMasterInfo_LoanTypes_Edit = "Pages.GeneralMasterInfo.LoanTypes.Edit";
    public const string Pages_GeneralMasterInfo_LoanTypes_Delete = "Pages.GeneralMasterInfo.LoanTypes.Delete";
    public const string Pages_GeneralMasterInfo_LoanTypeDetails = "Pages.GeneralMasterInfo.LoanTypeDetails";
    public const string Pages_GeneralMasterInfo_LoanTypeDetails_Create = "Pages.GeneralMasterInfo.LoanTypeDetails.Create";
    public const string Pages_GeneralMasterInfo_LoanTypeDetails_Edit = "Pages.GeneralMasterInfo.LoanTypeDetails.Edit";
    public const string Pages_GeneralMasterInfo_LoanTypeDetails_Delete = "Pages.GeneralMasterInfo.LoanTypeDetails.Delete";
    public const string Pages_GeneralMasterInfo_OvertimeTypes = "Pages.GeneralMasterInfo.OvertimeTypes";
    public const string Pages_GeneralMasterInfo_OvertimeTypes_Create = "Pages.GeneralMasterInfo.OvertimeTypes.Create";
    public const string Pages_GeneralMasterInfo_OvertimeTypes_Edit = "Pages.GeneralMasterInfo.OvertimeTypes.Edit";
    public const string Pages_GeneralMasterInfo_OvertimeTypes_Delete = "Pages.GeneralMasterInfo.OvertimeTypes.Delete";
    public const string Pages_GeneralMasterInfo_OvertimeTypeDetails = "Pages.GeneralMasterInfo.OvertimeTypeDetails";
    public const string Pages_GeneralMasterInfo_OvertimeTypeDetails_Create = "Pages.GeneralMasterInfo.OvertimeTypeDetails.Create";
    public const string Pages_GeneralMasterInfo_OvertimeTypeDetails_Edit = "Pages.GeneralMasterInfo.OvertimeTypeDetails.Edit";
    public const string Pages_GeneralMasterInfo_OvertimeTypeDetails_Delete = "Pages.GeneralMasterInfo.OvertimeTypeDetails.Delete";
    public const string Pages_GeneralMasterInfo_Assets = "Pages.GeneralMasterInfo.Assets";
    public const string Pages_GeneralMasterInfo_Assets_Create = "Pages.GeneralMasterInfo.Assets.Create";
    public const string Pages_GeneralMasterInfo_Assets_Edit = "Pages.GeneralMasterInfo.Assets.Edit";
    public const string Pages_GeneralMasterInfo_Assets_Delete = "Pages.GeneralMasterInfo.Assets.Delete";
    public const string Pages_GeneralMasterInfo_NumberSeries = "Pages.GeneralMasterInfo.NumberSeries";
    public const string Pages_GeneralMasterInfo_NumberSeries_Create = "Pages.GeneralMasterInfo.NumberSeries.Create";
    public const string Pages_GeneralMasterInfo_NumberSeries_Edit = "Pages.GeneralMasterInfo.NumberSeries.Edit";
    public const string Pages_GeneralMasterInfo_NumberSeries_Delete = "Pages.GeneralMasterInfo.NumberSeries.Delete";
    public const string Pages_GeneralMasterInfo_CurrencyConvertors = "Pages.GeneralMasterInfo.CurrencyConvertors";
    public const string Pages_GeneralMasterInfo_CurrencyConvertors_Create = "Pages.GeneralMasterInfo.CurrencyConvertors.Create";
    public const string Pages_GeneralMasterInfo_CurrencyConvertors_Edit = "Pages.GeneralMasterInfo.CurrencyConvertors.Edit";
    public const string Pages_GeneralMasterInfo_CurrencyConvertors_Delete = "Pages.GeneralMasterInfo.CurrencyConvertors.Delete";

    //ORGANIZATION MANAGEMENT
    public const string Pages_OrganizationManagement = "Pages.OrganizationManagement";

    public const string Pages_OrganizationManagement_CostCenterGroups = "Pages.OrganizationManagement.CostCenterGroups";
    public const string Pages_OrganizationManagement_CostCenterGroups_Create = "Pages.OrganizationManagement.CostCenterGroups.Create";
    public const string Pages_OrganizationManagement_CostCenterGroups_Edit = "Pages.OrganizationManagement.CostCenterGroups.Edit";
    public const string Pages_OrganizationManagement_CostCenterGroups_Delete = "Pages.OrganizationManagement.CostCenterGroups.Delete";

    public const string Pages_OrganizationManagement_CostCenters = "Pages.OrganizationManagement.CostCenters";
    public const string Pages_OrganizationManagement_CostCenters_Create = "Pages.OrganizationManagement.CostCenters.Create";
    public const string Pages_OrganizationManagement_CostCenters_Edit = "Pages.OrganizationManagement.CostCenters.Edit";
    public const string Pages_OrganizationManagement_CostCenters_Delete = "Pages.OrganizationManagement.CostCenters.Delete";

    public const string Pages_OrganizationManagement_CostElementTypes = "Pages.OrganizationManagement.CostElementTypes";
    public const string Pages_OrganizationManagement_CostElementTypes_Create = "Pages.OrganizationManagement.CostElementTypes.Create";
    public const string Pages_OrganizationManagement_CostElementTypes_Edit = "Pages.OrganizationManagement.CostElementTypes.Edit";
    public const string Pages_OrganizationManagement_CostElementTypes_Delete = "Pages.OrganizationManagement.CostElementTypes.Delete";

    public const string Pages_OrganizationManagement_CostElements = "Pages.OrganizationManagement.CostElements";
    public const string Pages_OrganizationManagement_CostElements_Create = "Pages.OrganizationManagement.CostElements.Create";
    public const string Pages_OrganizationManagement_CostElements_Edit = "Pages.OrganizationManagement.CostElements.Edit";
    public const string Pages_OrganizationManagement_CostElements_Delete = "Pages.OrganizationManagement.CostElements.Delete";

    public const string Pages_OrganizationManagement_JobTitles = "Pages.OrganizationManagement.JobTitles";
    public const string Pages_OrganizationManagement_JobTitles_Create = "Pages.OrganizationManagement.JobTitles.Create";
    public const string Pages_OrganizationManagement_JobTitles_Edit = "Pages.OrganizationManagement.JobTitles.Edit";
    public const string Pages_OrganizationManagement_JobTitles_Delete = "Pages.OrganizationManagement.JobTitles.Delete";

    public const string Pages_OrganizationManagement_OrganizationUnitTypes = "Pages.OrganizationManagement.OrganizationUnitTypes";
    public const string Pages_OrganizationManagement_OrganizationUnitTypes_Create = "Pages.OrganizationManagement.OrganizationUnitTypes.Create";
    public const string Pages_OrganizationManagement_OrganizationUnitTypes_Edit = "Pages.OrganizationManagement.OrganizationUnitTypes.Edit";
    public const string Pages_OrganizationManagement_OrganizationUnitTypes_Delete = "Pages.OrganizationManagement.OrganizationUnitTypes.Delete";

    public const string Pages_OrganizationManagement_OrgUnits = "Pages.OrganizationManagement.OrgUnits";
    public const string Pages_OrganizationManagement_OrgUnits_Create = "Pages.OrganizationManagement.OrgUnits.Create";
    public const string Pages_OrganizationManagement_OrgUnits_Edit = "Pages.OrganizationManagement.OrgUnits.Edit";
    public const string Pages_OrganizationManagement_OrgUnits_Delete = "Pages.OrganizationManagement.OrgUnits.Delete";

    public const string Pages_OrganizationManagement_OrganizationBankInfos = "Pages.OrganizationManagement.OrganizationBankInfos";
    public const string Pages_OrganizationManagement_OrganizationBankInfos_Create = "Pages.OrganizationManagement.OrganizationBankInfos.Create";
    public const string Pages_OrganizationManagement_OrganizationBankInfos_Edit = "Pages.OrganizationManagement.OrganizationBankInfos.Edit";
    public const string Pages_OrganizationManagement_OrganizationBankInfos_Delete = "Pages.OrganizationManagement.OrganizationBankInfos.Delete";

    public const string Pages_OrganizationManagement_WorkLocations = "Pages.OrganizationManagement.WorkLocations";
    public const string Pages_OrganizationManagement_WorkLocations_Create = "Pages.OrganizationManagement.WorkLocations.Create";
    public const string Pages_OrganizationManagement_WorkLocations_Edit = "Pages.OrganizationManagement.WorkLocations.Edit";
    public const string Pages_OrganizationManagement_WorkLocations_Delete = "Pages.OrganizationManagement.WorkLocations.Delete";

    //PERSONNEL ADMINISTRATION (WAVE A)
    public const string Pages_PersonnelAdministration = "Pages.PersonnelAdministration";

    public const string Pages_PersonnelAdministration_Employees = "Pages.PersonnelAdministration.Employees";
    public const string Pages_PersonnelAdministration_Employees_Create = "Pages.PersonnelAdministration.Employees.Create";
    public const string Pages_PersonnelAdministration_Employees_Edit = "Pages.PersonnelAdministration.Employees.Edit";
    public const string Pages_PersonnelAdministration_Employees_Delete = "Pages.PersonnelAdministration.Employees.Delete";

    public const string Pages_PersonnelAdministration_EmployeeDates = "Pages.PersonnelAdministration.EmployeeDates";
    public const string Pages_PersonnelAdministration_EmployeeDates_Create = "Pages.PersonnelAdministration.EmployeeDates.Create";
    public const string Pages_PersonnelAdministration_EmployeeDates_Edit = "Pages.PersonnelAdministration.EmployeeDates.Edit";
    public const string Pages_PersonnelAdministration_EmployeeDates_Delete = "Pages.PersonnelAdministration.EmployeeDates.Delete";

    public const string Pages_PersonnelAdministration_EmployeeManagers = "Pages.PersonnelAdministration.EmployeeManagers";
    public const string Pages_PersonnelAdministration_EmployeeManagers_Create = "Pages.PersonnelAdministration.EmployeeManagers.Create";
    public const string Pages_PersonnelAdministration_EmployeeManagers_Edit = "Pages.PersonnelAdministration.EmployeeManagers.Edit";
    public const string Pages_PersonnelAdministration_EmployeeManagers_Delete = "Pages.PersonnelAdministration.EmployeeManagers.Delete";

    public const string Pages_PersonnelAdministration_EmployeeDottedLineManagers = "Pages.PersonnelAdministration.EmployeeDottedLineManagers";
    public const string Pages_PersonnelAdministration_EmployeeDottedLineManagers_Create = "Pages.PersonnelAdministration.EmployeeDottedLineManagers.Create";
    public const string Pages_PersonnelAdministration_EmployeeDottedLineManagers_Edit = "Pages.PersonnelAdministration.EmployeeDottedLineManagers.Edit";
    public const string Pages_PersonnelAdministration_EmployeeDottedLineManagers_Delete = "Pages.PersonnelAdministration.EmployeeDottedLineManagers.Delete";

    public const string Pages_PersonnelAdministration_EmployeeDelegations = "Pages.PersonnelAdministration.EmployeeDelegations";
    public const string Pages_PersonnelAdministration_EmployeeDelegations_Create = "Pages.PersonnelAdministration.EmployeeDelegations.Create";
    public const string Pages_PersonnelAdministration_EmployeeDelegations_Edit = "Pages.PersonnelAdministration.EmployeeDelegations.Edit";
    public const string Pages_PersonnelAdministration_EmployeeDelegations_Delete = "Pages.PersonnelAdministration.EmployeeDelegations.Delete";

    public const string Pages_PersonnelAdministration_EmployeeRelationships = "Pages.PersonnelAdministration.EmployeeRelationships";
    public const string Pages_PersonnelAdministration_EmployeeRelationships_Create = "Pages.PersonnelAdministration.EmployeeRelationships.Create";
    public const string Pages_PersonnelAdministration_EmployeeRelationships_Edit = "Pages.PersonnelAdministration.EmployeeRelationships.Edit";
    public const string Pages_PersonnelAdministration_EmployeeRelationships_Delete = "Pages.PersonnelAdministration.EmployeeRelationships.Delete";

    public const string Pages_PersonnelAdministration_EmployeeSignatures = "Pages.PersonnelAdministration.EmployeeSignatures";
    public const string Pages_PersonnelAdministration_EmployeeSignatures_Create = "Pages.PersonnelAdministration.EmployeeSignatures.Create";
    public const string Pages_PersonnelAdministration_EmployeeSignatures_Edit = "Pages.PersonnelAdministration.EmployeeSignatures.Edit";
    public const string Pages_PersonnelAdministration_EmployeeSignatures_Delete = "Pages.PersonnelAdministration.EmployeeSignatures.Delete";

    public const string Pages_PersonnelAdministration_EmployeeEducations = "Pages.PersonnelAdministration.EmployeeEducations";
    public const string Pages_PersonnelAdministration_EmployeeEducations_Create = "Pages.PersonnelAdministration.EmployeeEducations.Create";
    public const string Pages_PersonnelAdministration_EmployeeEducations_Edit = "Pages.PersonnelAdministration.EmployeeEducations.Edit";
    public const string Pages_PersonnelAdministration_EmployeeEducations_Delete = "Pages.PersonnelAdministration.EmployeeEducations.Delete";

    public const string Pages_PersonnelAdministration_EmployeeCertifications = "Pages.PersonnelAdministration.EmployeeCertifications";
    public const string Pages_PersonnelAdministration_EmployeeCertifications_Create = "Pages.PersonnelAdministration.EmployeeCertifications.Create";
    public const string Pages_PersonnelAdministration_EmployeeCertifications_Edit = "Pages.PersonnelAdministration.EmployeeCertifications.Edit";
    public const string Pages_PersonnelAdministration_EmployeeCertifications_Delete = "Pages.PersonnelAdministration.EmployeeCertifications.Delete";

    public const string Pages_PersonnelAdministration_EmployeeWorkExperiences = "Pages.PersonnelAdministration.EmployeeWorkExperiences";
    public const string Pages_PersonnelAdministration_EmployeeWorkExperiences_Create = "Pages.PersonnelAdministration.EmployeeWorkExperiences.Create";
    public const string Pages_PersonnelAdministration_EmployeeWorkExperiences_Edit = "Pages.PersonnelAdministration.EmployeeWorkExperiences.Edit";
    public const string Pages_PersonnelAdministration_EmployeeWorkExperiences_Delete = "Pages.PersonnelAdministration.EmployeeWorkExperiences.Delete";

    public const string Pages_PersonnelAdministration_EmployeeDocuments = "Pages.PersonnelAdministration.EmployeeDocuments";
    public const string Pages_PersonnelAdministration_EmployeeDocuments_Create = "Pages.PersonnelAdministration.EmployeeDocuments.Create";
    public const string Pages_PersonnelAdministration_EmployeeDocuments_Edit = "Pages.PersonnelAdministration.EmployeeDocuments.Edit";
    public const string Pages_PersonnelAdministration_EmployeeDocuments_Delete = "Pages.PersonnelAdministration.EmployeeDocuments.Delete";

    public const string Pages_PersonnelAdministration_EmployeeDocumentMvmts = "Pages.PersonnelAdministration.EmployeeDocumentMvmts";
    public const string Pages_PersonnelAdministration_EmployeeDocumentMvmts_Create = "Pages.PersonnelAdministration.EmployeeDocumentMvmts.Create";
    public const string Pages_PersonnelAdministration_EmployeeDocumentMvmts_Edit = "Pages.PersonnelAdministration.EmployeeDocumentMvmts.Edit";
    public const string Pages_PersonnelAdministration_EmployeeDocumentMvmts_Delete = "Pages.PersonnelAdministration.EmployeeDocumentMvmts.Delete";

    public const string Pages_PersonnelAdministration_EmployeeVisaInfos = "Pages.PersonnelAdministration.EmployeeVisaInfos";
    public const string Pages_PersonnelAdministration_EmployeeVisaInfos_Create = "Pages.PersonnelAdministration.EmployeeVisaInfos.Create";
    public const string Pages_PersonnelAdministration_EmployeeVisaInfos_Edit = "Pages.PersonnelAdministration.EmployeeVisaInfos.Edit";
    public const string Pages_PersonnelAdministration_EmployeeVisaInfos_Delete = "Pages.PersonnelAdministration.EmployeeVisaInfos.Delete";

    // Personnel Administration (Wave B)
    public const string Pages_PersonnelAdministration_EmployeeOrgAssignments = "Pages.PersonnelAdministration.EmployeeOrgAssignments";
    public const string Pages_PersonnelAdministration_EmployeeOrgAssignments_Create = "Pages.PersonnelAdministration.EmployeeOrgAssignments.Create";
    public const string Pages_PersonnelAdministration_EmployeeOrgAssignments_Edit = "Pages.PersonnelAdministration.EmployeeOrgAssignments.Edit";
    public const string Pages_PersonnelAdministration_EmployeeOrgAssignments_Delete = "Pages.PersonnelAdministration.EmployeeOrgAssignments.Delete";

    public const string Pages_PersonnelAdministration_EmployeeContracts = "Pages.PersonnelAdministration.EmployeeContracts";
    public const string Pages_PersonnelAdministration_EmployeeContracts_Create = "Pages.PersonnelAdministration.EmployeeContracts.Create";
    public const string Pages_PersonnelAdministration_EmployeeContracts_Edit = "Pages.PersonnelAdministration.EmployeeContracts.Edit";
    public const string Pages_PersonnelAdministration_EmployeeContracts_Delete = "Pages.PersonnelAdministration.EmployeeContracts.Delete";

    public const string Pages_PersonnelAdministration_EmployeeActions = "Pages.PersonnelAdministration.EmployeeActions";
    public const string Pages_PersonnelAdministration_EmployeeActions_Create = "Pages.PersonnelAdministration.EmployeeActions.Create";
    public const string Pages_PersonnelAdministration_EmployeeActions_Edit = "Pages.PersonnelAdministration.EmployeeActions.Edit";
    public const string Pages_PersonnelAdministration_EmployeeActions_Delete = "Pages.PersonnelAdministration.EmployeeActions.Delete";

    public const string Pages_PersonnelAdministration_EmployeeSeparations = "Pages.PersonnelAdministration.EmployeeSeparations";
    public const string Pages_PersonnelAdministration_EmployeeSeparations_Create = "Pages.PersonnelAdministration.EmployeeSeparations.Create";
    public const string Pages_PersonnelAdministration_EmployeeSeparations_Edit = "Pages.PersonnelAdministration.EmployeeSeparations.Edit";
    public const string Pages_PersonnelAdministration_EmployeeSeparations_Delete = "Pages.PersonnelAdministration.EmployeeSeparations.Delete";

    public const string Pages_PersonnelAdministration_EmployeeRejoins = "Pages.PersonnelAdministration.EmployeeRejoins";
    public const string Pages_PersonnelAdministration_EmployeeRejoins_Create = "Pages.PersonnelAdministration.EmployeeRejoins.Create";
    public const string Pages_PersonnelAdministration_EmployeeRejoins_Edit = "Pages.PersonnelAdministration.EmployeeRejoins.Edit";
    public const string Pages_PersonnelAdministration_EmployeeRejoins_Delete = "Pages.PersonnelAdministration.EmployeeRejoins.Delete";

    public const string Pages_Administration_Roles = "Pages.Administration.Roles";
    public const string Pages_Administration_Roles_Create = "Pages.Administration.Roles.Create";
    public const string Pages_Administration_Roles_Edit = "Pages.Administration.Roles.Edit";
    public const string Pages_Administration_Roles_Delete = "Pages.Administration.Roles.Delete";

    public const string Pages_Administration_Users = "Pages.Administration.Users";
    public const string Pages_Administration_Users_Create = "Pages.Administration.Users.Create";
    public const string Pages_Administration_Users_Edit = "Pages.Administration.Users.Edit";
    public const string Pages_Administration_Users_Delete = "Pages.Administration.Users.Delete";
    public const string Pages_Administration_Users_ChangePermissions = "Pages.Administration.Users.ChangePermissions";
    public const string Pages_Administration_Users_Impersonation = "Pages.Administration.Users.Impersonation";
    public const string Pages_Administration_Users_Unlock = "Pages.Administration.Users.Unlock";
    public const string Pages_Administration_Users_ChangeProfilePicture = "Pages.Administration.Users.ChangeProfilePicture";

    public const string Pages_Administration_Languages = "Pages.Administration.Languages";
    public const string Pages_Administration_Languages_Create = "Pages.Administration.Languages.Create";
    public const string Pages_Administration_Languages_Edit = "Pages.Administration.Languages.Edit";
    public const string Pages_Administration_Languages_Delete = "Pages.Administration.Languages.Delete";
    public const string Pages_Administration_Languages_ChangeTexts = "Pages.Administration.Languages.ChangeTexts";
    public const string Pages_Administration_Languages_ChangeDefaultLanguage = "Pages.Administration.Languages.ChangeDefaultLanguage";

    public const string Pages_Administration_AuditLogs = "Pages.Administration.AuditLogs";

    public const string Pages_Administration_ActiveSessions = "Pages.Administration.ActiveSessions";

    public const string Pages_Administration_OrganizationUnits = "Pages.Administration.OrganizationUnits";
    public const string Pages_Administration_OrganizationUnits_ManageOrganizationTree = "Pages.Administration.OrganizationUnits.ManageOrganizationTree";
    public const string Pages_Administration_OrganizationUnits_ManageMembers = "Pages.Administration.OrganizationUnits.ManageMembers";
    public const string Pages_Administration_OrganizationUnits_ManageRoles = "Pages.Administration.OrganizationUnits.ManageRoles";

    public const string Pages_Administration_HangfireDashboard = "Pages.Administration.HangfireDashboard";

    public const string Pages_Administration_UiCustomization = "Pages.Administration.UiCustomization";

    public const string Pages_Administration_WebhookSubscription = "Pages.Administration.WebhookSubscription";
    public const string Pages_Administration_WebhookSubscription_Create = "Pages.Administration.WebhookSubscription.Create";
    public const string Pages_Administration_WebhookSubscription_Edit = "Pages.Administration.WebhookSubscription.Edit";
    public const string Pages_Administration_WebhookSubscription_ChangeActivity = "Pages.Administration.WebhookSubscription.ChangeActivity";
    public const string Pages_Administration_WebhookSubscription_Detail = "Pages.Administration.WebhookSubscription.Detail";
    public const string Pages_Administration_Webhook_ListSendAttempts = "Pages.Administration.Webhook.ListSendAttempts";
    public const string Pages_Administration_Webhook_ResendWebhook = "Pages.Administration.Webhook.ResendWebhook";

    public const string Pages_Administration_DynamicProperties = "Pages.Administration.DynamicProperties";
    public const string Pages_Administration_DynamicProperties_Create = "Pages.Administration.DynamicProperties.Create";
    public const string Pages_Administration_DynamicProperties_Edit = "Pages.Administration.DynamicProperties.Edit";
    public const string Pages_Administration_DynamicProperties_Delete = "Pages.Administration.DynamicProperties.Delete";

    public const string Pages_Administration_DynamicPropertyValue = "Pages.Administration.DynamicPropertyValue";
    public const string Pages_Administration_DynamicPropertyValue_Create = "Pages.Administration.DynamicPropertyValue.Create";
    public const string Pages_Administration_DynamicPropertyValue_Edit = "Pages.Administration.DynamicPropertyValue.Edit";
    public const string Pages_Administration_DynamicPropertyValue_Delete = "Pages.Administration.DynamicPropertyValue.Delete";

    public const string Pages_Administration_DynamicEntityProperties = "Pages.Administration.DynamicEntityProperties";
    public const string Pages_Administration_DynamicEntityProperties_Create = "Pages.Administration.DynamicEntityProperties.Create";
    public const string Pages_Administration_DynamicEntityProperties_Edit = "Pages.Administration.DynamicEntityProperties.Edit";
    public const string Pages_Administration_DynamicEntityProperties_Delete = "Pages.Administration.DynamicEntityProperties.Delete";

    public const string Pages_Administration_DynamicEntityPropertyValue = "Pages.Administration.DynamicEntityPropertyValue";
    public const string Pages_Administration_DynamicEntityPropertyValue_Create = "Pages.Administration.DynamicEntityPropertyValue.Create";
    public const string Pages_Administration_DynamicEntityPropertyValue_Edit = "Pages.Administration.DynamicEntityPropertyValue.Edit";
    public const string Pages_Administration_DynamicEntityPropertyValue_Delete = "Pages.Administration.DynamicEntityPropertyValue.Delete";

    public const string Pages_Administration_MassNotification = "Pages.Administration.MassNotification";
    public const string Pages_Administration_MassNotification_Create = "Pages.Administration.MassNotification.Create";

    public const string Pages_Administration_NewVersion_Create = "Pages.Administration.NewVersion.Create";

    public const string Pages_Administration_EntityChanges_FullHistory = "Pages.Administration.EntityChanges.FullHistory";

    //TIME MANAGEMENT (WAVE 1)
    public const string Pages_TimeManagement = "Pages.TimeManagement";

    public const string Pages_TimeManagement_TimeTypes = "Pages.TimeManagement.TimeTypes";
    public const string Pages_TimeManagement_TimeTypes_Create = "Pages.TimeManagement.TimeTypes.Create";
    public const string Pages_TimeManagement_TimeTypes_Edit = "Pages.TimeManagement.TimeTypes.Edit";
    public const string Pages_TimeManagement_TimeTypes_Delete = "Pages.TimeManagement.TimeTypes.Delete";

    public const string Pages_TimeManagement_TransTypes = "Pages.TimeManagement.TransTypes";
    public const string Pages_TimeManagement_TransTypes_Create = "Pages.TimeManagement.TransTypes.Create";
    public const string Pages_TimeManagement_TransTypes_Edit = "Pages.TimeManagement.TransTypes.Edit";
    public const string Pages_TimeManagement_TransTypes_Delete = "Pages.TimeManagement.TransTypes.Delete";

    public const string Pages_TimeManagement_TimeTransferTypes = "Pages.TimeManagement.TimeTransferTypes";
    public const string Pages_TimeManagement_TimeTransferTypes_Create = "Pages.TimeManagement.TimeTransferTypes.Create";
    public const string Pages_TimeManagement_TimeTransferTypes_Edit = "Pages.TimeManagement.TimeTransferTypes.Edit";
    public const string Pages_TimeManagement_TimeTransferTypes_Delete = "Pages.TimeManagement.TimeTransferTypes.Delete";

    public const string Pages_TimeManagement_BreakShifts = "Pages.TimeManagement.BreakShifts";
    public const string Pages_TimeManagement_BreakShifts_Create = "Pages.TimeManagement.BreakShifts.Create";
    public const string Pages_TimeManagement_BreakShifts_Edit = "Pages.TimeManagement.BreakShifts.Edit";
    public const string Pages_TimeManagement_BreakShifts_Delete = "Pages.TimeManagement.BreakShifts.Delete";

    public const string Pages_TimeManagement_Duties = "Pages.TimeManagement.Duties";
    public const string Pages_TimeManagement_Duties_Create = "Pages.TimeManagement.Duties.Create";
    public const string Pages_TimeManagement_Duties_Edit = "Pages.TimeManagement.Duties.Edit";
    public const string Pages_TimeManagement_Duties_Delete = "Pages.TimeManagement.Duties.Delete";

    public const string Pages_TimeManagement_DailyDuties = "Pages.TimeManagement.DailyDuties";
    public const string Pages_TimeManagement_DailyDuties_Create = "Pages.TimeManagement.DailyDuties.Create";
    public const string Pages_TimeManagement_DailyDuties_Edit = "Pages.TimeManagement.DailyDuties.Edit";
    public const string Pages_TimeManagement_DailyDuties_Delete = "Pages.TimeManagement.DailyDuties.Delete";

    public const string Pages_TimeManagement_HolidayCalendars = "Pages.TimeManagement.HolidayCalendars";
    public const string Pages_TimeManagement_HolidayCalendars_Create = "Pages.TimeManagement.HolidayCalendars.Create";
    public const string Pages_TimeManagement_HolidayCalendars_Edit = "Pages.TimeManagement.HolidayCalendars.Edit";
    public const string Pages_TimeManagement_HolidayCalendars_Delete = "Pages.TimeManagement.HolidayCalendars.Delete";

    public const string Pages_TimeManagement_Holidays = "Pages.TimeManagement.Holidays";
    public const string Pages_TimeManagement_Holidays_Create = "Pages.TimeManagement.Holidays.Create";
    public const string Pages_TimeManagement_Holidays_Edit = "Pages.TimeManagement.Holidays.Edit";
    public const string Pages_TimeManagement_Holidays_Delete = "Pages.TimeManagement.Holidays.Delete";

    public const string Pages_TimeManagement_HolidayPeriods = "Pages.TimeManagement.HolidayPeriods";
    public const string Pages_TimeManagement_HolidayPeriods_Create = "Pages.TimeManagement.HolidayPeriods.Create";
    public const string Pages_TimeManagement_HolidayPeriods_Edit = "Pages.TimeManagement.HolidayPeriods.Edit";
    public const string Pages_TimeManagement_HolidayPeriods_Delete = "Pages.TimeManagement.HolidayPeriods.Delete";

    public const string Pages_TimeManagement_TimePolicies = "Pages.TimeManagement.TimePolicies";
    public const string Pages_TimeManagement_TimePolicies_Create = "Pages.TimeManagement.TimePolicies.Create";
    public const string Pages_TimeManagement_TimePolicies_Edit = "Pages.TimeManagement.TimePolicies.Edit";
    public const string Pages_TimeManagement_TimePolicies_Delete = "Pages.TimeManagement.TimePolicies.Delete";

    //TIME MANAGEMENT (WAVE 2)
    public const string Pages_TimeManagement_DutyPeriods = "Pages.TimeManagement.DutyPeriods";
    public const string Pages_TimeManagement_DutyPeriods_Create = "Pages.TimeManagement.DutyPeriods.Create";
    public const string Pages_TimeManagement_DutyPeriods_Edit = "Pages.TimeManagement.DutyPeriods.Edit";
    public const string Pages_TimeManagement_DutyPeriods_Delete = "Pages.TimeManagement.DutyPeriods.Delete";

    public const string Pages_TimeManagement_DutyPeriodDetails = "Pages.TimeManagement.DutyPeriodDetails";
    public const string Pages_TimeManagement_DutyPeriodDetails_Create = "Pages.TimeManagement.DutyPeriodDetails.Create";
    public const string Pages_TimeManagement_DutyPeriodDetails_Edit = "Pages.TimeManagement.DutyPeriodDetails.Edit";
    public const string Pages_TimeManagement_DutyPeriodDetails_Delete = "Pages.TimeManagement.DutyPeriodDetails.Delete";

    public const string Pages_TimeManagement_RosterDutyPeriods = "Pages.TimeManagement.RosterDutyPeriods";
    public const string Pages_TimeManagement_RosterDutyPeriods_Create = "Pages.TimeManagement.RosterDutyPeriods.Create";
    public const string Pages_TimeManagement_RosterDutyPeriods_Edit = "Pages.TimeManagement.RosterDutyPeriods.Edit";
    public const string Pages_TimeManagement_RosterDutyPeriods_Delete = "Pages.TimeManagement.RosterDutyPeriods.Delete";

    public const string Pages_TimeManagement_RosterDutyPeriodDetails = "Pages.TimeManagement.RosterDutyPeriodDetails";
    public const string Pages_TimeManagement_RosterDutyPeriodDetails_Create = "Pages.TimeManagement.RosterDutyPeriodDetails.Create";
    public const string Pages_TimeManagement_RosterDutyPeriodDetails_Edit = "Pages.TimeManagement.RosterDutyPeriodDetails.Edit";
    public const string Pages_TimeManagement_RosterDutyPeriodDetails_Delete = "Pages.TimeManagement.RosterDutyPeriodDetails.Delete";

    public const string Pages_TimeManagement_RosterDutySchedules = "Pages.TimeManagement.RosterDutySchedules";
    public const string Pages_TimeManagement_RosterDutySchedules_Create = "Pages.TimeManagement.RosterDutySchedules.Create";
    public const string Pages_TimeManagement_RosterDutySchedules_Edit = "Pages.TimeManagement.RosterDutySchedules.Edit";
    public const string Pages_TimeManagement_RosterDutySchedules_Delete = "Pages.TimeManagement.RosterDutySchedules.Delete";

    public const string Pages_TimeManagement_RosterGroups = "Pages.TimeManagement.RosterGroups";
    public const string Pages_TimeManagement_RosterGroups_Create = "Pages.TimeManagement.RosterGroups.Create";
    public const string Pages_TimeManagement_RosterGroups_Edit = "Pages.TimeManagement.RosterGroups.Edit";
    public const string Pages_TimeManagement_RosterGroups_Delete = "Pages.TimeManagement.RosterGroups.Delete";

    public const string Pages_TimeManagement_RosterGroupDetails = "Pages.TimeManagement.RosterGroupDetails";
    public const string Pages_TimeManagement_RosterGroupDetails_Create = "Pages.TimeManagement.RosterGroupDetails.Create";
    public const string Pages_TimeManagement_RosterGroupDetails_Edit = "Pages.TimeManagement.RosterGroupDetails.Edit";
    public const string Pages_TimeManagement_RosterGroupDetails_Delete = "Pages.TimeManagement.RosterGroupDetails.Delete";

    public const string Pages_TimeManagement_RosterGroupSchedules = "Pages.TimeManagement.RosterGroupSchedules";
    public const string Pages_TimeManagement_RosterGroupSchedules_Create = "Pages.TimeManagement.RosterGroupSchedules.Create";
    public const string Pages_TimeManagement_RosterGroupSchedules_Edit = "Pages.TimeManagement.RosterGroupSchedules.Edit";
    public const string Pages_TimeManagement_RosterGroupSchedules_Delete = "Pages.TimeManagement.RosterGroupSchedules.Delete";

    public const string Pages_TimeManagement_RosterDailyDuties = "Pages.TimeManagement.RosterDailyDuties";
    public const string Pages_TimeManagement_RosterDailyDuties_Create = "Pages.TimeManagement.RosterDailyDuties.Create";
    public const string Pages_TimeManagement_RosterDailyDuties_Edit = "Pages.TimeManagement.RosterDailyDuties.Edit";
    public const string Pages_TimeManagement_RosterDailyDuties_Delete = "Pages.TimeManagement.RosterDailyDuties.Delete";

    // Wave 3 (employee-assignment records)
    public const string Pages_TimeManagement_EmployeeDutyPeriods = "Pages.TimeManagement.EmployeeDutyPeriods";
    public const string Pages_TimeManagement_EmployeeDutyPeriods_Create = "Pages.TimeManagement.EmployeeDutyPeriods.Create";
    public const string Pages_TimeManagement_EmployeeDutyPeriods_Edit = "Pages.TimeManagement.EmployeeDutyPeriods.Edit";
    public const string Pages_TimeManagement_EmployeeDutyPeriods_Delete = "Pages.TimeManagement.EmployeeDutyPeriods.Delete";

    public const string Pages_TimeManagement_EmployeeTimePolicies = "Pages.TimeManagement.EmployeeTimePolicies";
    public const string Pages_TimeManagement_EmployeeTimePolicies_Create = "Pages.TimeManagement.EmployeeTimePolicies.Create";
    public const string Pages_TimeManagement_EmployeeTimePolicies_Edit = "Pages.TimeManagement.EmployeeTimePolicies.Edit";
    public const string Pages_TimeManagement_EmployeeTimePolicies_Delete = "Pages.TimeManagement.EmployeeTimePolicies.Delete";

    public const string Pages_TimeManagement_EmployeeSubstituteDuties = "Pages.TimeManagement.EmployeeSubstituteDuties";
    public const string Pages_TimeManagement_EmployeeSubstituteDuties_Create = "Pages.TimeManagement.EmployeeSubstituteDuties.Create";
    public const string Pages_TimeManagement_EmployeeSubstituteDuties_Edit = "Pages.TimeManagement.EmployeeSubstituteDuties.Edit";
    public const string Pages_TimeManagement_EmployeeSubstituteDuties_Delete = "Pages.TimeManagement.EmployeeSubstituteDuties.Delete";

    public const string Pages_TimeManagement_EmployeeDutySchedules = "Pages.TimeManagement.EmployeeDutySchedules";
    public const string Pages_TimeManagement_EmployeeDutySchedules_Create = "Pages.TimeManagement.EmployeeDutySchedules.Create";
    public const string Pages_TimeManagement_EmployeeDutySchedules_Edit = "Pages.TimeManagement.EmployeeDutySchedules.Edit";
    public const string Pages_TimeManagement_EmployeeDutySchedules_Delete = "Pages.TimeManagement.EmployeeDutySchedules.Delete";

    public const string Pages_TimeManagement_WorkLocationDutyPeriods = "Pages.TimeManagement.WorkLocationDutyPeriods";
    public const string Pages_TimeManagement_WorkLocationDutyPeriods_Create = "Pages.TimeManagement.WorkLocationDutyPeriods.Create";
    public const string Pages_TimeManagement_WorkLocationDutyPeriods_Edit = "Pages.TimeManagement.WorkLocationDutyPeriods.Edit";
    public const string Pages_TimeManagement_WorkLocationDutyPeriods_Delete = "Pages.TimeManagement.WorkLocationDutyPeriods.Delete";

    public const string Pages_TimeManagement_EmployeeTimesheets = "Pages.TimeManagement.EmployeeTimesheets";
    public const string Pages_TimeManagement_EmployeeTimesheets_Create = "Pages.TimeManagement.EmployeeTimesheets.Create";
    public const string Pages_TimeManagement_EmployeeTimesheets_Edit = "Pages.TimeManagement.EmployeeTimesheets.Edit";
    public const string Pages_TimeManagement_EmployeeTimesheets_Delete = "Pages.TimeManagement.EmployeeTimesheets.Delete";

    //TIME MANAGEMENT (WAVE 4 - raw event/log data + calculated register data)
    public const string Pages_TimeManagement_TimeEvents = "Pages.TimeManagement.TimeEvents";
    public const string Pages_TimeManagement_TimeEvents_Create = "Pages.TimeManagement.TimeEvents.Create";
    public const string Pages_TimeManagement_TimeEvents_Edit = "Pages.TimeManagement.TimeEvents.Edit";
    public const string Pages_TimeManagement_TimeEvents_Delete = "Pages.TimeManagement.TimeEvents.Delete";

    public const string Pages_TimeManagement_TimeAttAPIs = "Pages.TimeManagement.TimeAttAPIs";
    public const string Pages_TimeManagement_TimeAttAPIs_Create = "Pages.TimeManagement.TimeAttAPIs.Create";
    public const string Pages_TimeManagement_TimeAttAPIs_Edit = "Pages.TimeManagement.TimeAttAPIs.Edit";
    public const string Pages_TimeManagement_TimeAttAPIs_Delete = "Pages.TimeManagement.TimeAttAPIs.Delete";

    public const string Pages_TimeManagement_TimeLogs = "Pages.TimeManagement.TimeLogs";
    public const string Pages_TimeManagement_TimeLogs_Create = "Pages.TimeManagement.TimeLogs.Create";
    public const string Pages_TimeManagement_TimeLogs_Edit = "Pages.TimeManagement.TimeLogs.Edit";
    public const string Pages_TimeManagement_TimeLogs_Delete = "Pages.TimeManagement.TimeLogs.Delete";

    public const string Pages_TimeManagement_TimeRegisters = "Pages.TimeManagement.TimeRegisters";
    public const string Pages_TimeManagement_TimeRegisters_Create = "Pages.TimeManagement.TimeRegisters.Create";
    public const string Pages_TimeManagement_TimeRegisters_Edit = "Pages.TimeManagement.TimeRegisters.Edit";
    public const string Pages_TimeManagement_TimeRegisters_Delete = "Pages.TimeManagement.TimeRegisters.Delete";

    public const string Pages_TimeManagement_MonthlyTimeRegisters = "Pages.TimeManagement.MonthlyTimeRegisters";
    public const string Pages_TimeManagement_MonthlyTimeRegisters_Create = "Pages.TimeManagement.MonthlyTimeRegisters.Create";
    public const string Pages_TimeManagement_MonthlyTimeRegisters_Edit = "Pages.TimeManagement.MonthlyTimeRegisters.Edit";
    public const string Pages_TimeManagement_MonthlyTimeRegisters_Delete = "Pages.TimeManagement.MonthlyTimeRegisters.Delete";

    public const string Pages_TimeManagement_MonthlyTimeRegisterDetails = "Pages.TimeManagement.MonthlyTimeRegisterDetails";
    public const string Pages_TimeManagement_MonthlyTimeRegisterDetails_Create = "Pages.TimeManagement.MonthlyTimeRegisterDetails.Create";
    public const string Pages_TimeManagement_MonthlyTimeRegisterDetails_Edit = "Pages.TimeManagement.MonthlyTimeRegisterDetails.Edit";
    public const string Pages_TimeManagement_MonthlyTimeRegisterDetails_Delete = "Pages.TimeManagement.MonthlyTimeRegisterDetails.Delete";

    public const string Pages_TimeManagement_YearlyTimeRegisters = "Pages.TimeManagement.YearlyTimeRegisters";
    public const string Pages_TimeManagement_YearlyTimeRegisters_Create = "Pages.TimeManagement.YearlyTimeRegisters.Create";
    public const string Pages_TimeManagement_YearlyTimeRegisters_Edit = "Pages.TimeManagement.YearlyTimeRegisters.Edit";
    public const string Pages_TimeManagement_YearlyTimeRegisters_Delete = "Pages.TimeManagement.YearlyTimeRegisters.Delete";

    public const string Pages_TimeManagement_YearlyTimeRegisterDetails = "Pages.TimeManagement.YearlyTimeRegisterDetails";
    public const string Pages_TimeManagement_YearlyTimeRegisterDetails_Create = "Pages.TimeManagement.YearlyTimeRegisterDetails.Create";
    public const string Pages_TimeManagement_YearlyTimeRegisterDetails_Edit = "Pages.TimeManagement.YearlyTimeRegisterDetails.Edit";
    public const string Pages_TimeManagement_YearlyTimeRegisterDetails_Delete = "Pages.TimeManagement.YearlyTimeRegisterDetails.Delete";

    public const string Pages_TimeManagement_SimulationTimeRegisters = "Pages.TimeManagement.SimulationTimeRegisters";
    public const string Pages_TimeManagement_SimulationTimeRegisters_Create = "Pages.TimeManagement.SimulationTimeRegisters.Create";
    public const string Pages_TimeManagement_SimulationTimeRegisters_Edit = "Pages.TimeManagement.SimulationTimeRegisters.Edit";
    public const string Pages_TimeManagement_SimulationTimeRegisters_Delete = "Pages.TimeManagement.SimulationTimeRegisters.Delete";

    public const string Pages_TimeManagement_SimulationTimeRegisterDetails = "Pages.TimeManagement.SimulationTimeRegisterDetails";
    public const string Pages_TimeManagement_SimulationTimeRegisterDetails_Create = "Pages.TimeManagement.SimulationTimeRegisterDetails.Create";
    public const string Pages_TimeManagement_SimulationTimeRegisterDetails_Edit = "Pages.TimeManagement.SimulationTimeRegisterDetails.Edit";
    public const string Pages_TimeManagement_SimulationTimeRegisterDetails_Delete = "Pages.TimeManagement.SimulationTimeRegisterDetails.Delete";

    public const string Pages_TimeManagement_TimeTransfers = "Pages.TimeManagement.TimeTransfers";
    public const string Pages_TimeManagement_TimeTransfers_Create = "Pages.TimeManagement.TimeTransfers.Create";
    public const string Pages_TimeManagement_TimeTransfers_Edit = "Pages.TimeManagement.TimeTransfers.Edit";
    public const string Pages_TimeManagement_TimeTransfers_Delete = "Pages.TimeManagement.TimeTransfers.Delete";

    //TENANT-SPECIFIC PERMISSIONS

    public const string Pages_Tenant_Dashboard = "Pages.Tenant.Dashboard";

    public const string Pages_Administration_Tenant_Settings = "Pages.Administration.Tenant.Settings";

    public const string Pages_Administration_Tenant_SubscriptionManagement = "Pages.Administration.Tenant.SubscriptionManagement";

    //HOST-SPECIFIC PERMISSIONS

    public const string Pages_Editions = "Pages.Editions";
    public const string Pages_Editions_Create = "Pages.Editions.Create";
    public const string Pages_Editions_Edit = "Pages.Editions.Edit";
    public const string Pages_Editions_Delete = "Pages.Editions.Delete";
    public const string Pages_Editions_MoveTenantsToAnotherEdition = "Pages.Editions.MoveTenantsToAnotherEdition";

    public const string Pages_Tenants = "Pages.Tenants";
    public const string Pages_Tenants_Create = "Pages.Tenants.Create";
    public const string Pages_Tenants_Edit = "Pages.Tenants.Edit";
    public const string Pages_Tenants_ChangeFeatures = "Pages.Tenants.ChangeFeatures";
    public const string Pages_Tenants_Delete = "Pages.Tenants.Delete";
    public const string Pages_Tenants_Impersonation = "Pages.Tenants.Impersonation";

    public const string Pages_Administration_Host_Maintenance = "Pages.Administration.Host.Maintenance";
    public const string Pages_Administration_Host_Settings = "Pages.Administration.Host.Settings";
    public const string Pages_Administration_Host_Dashboard = "Pages.Administration.Host.Dashboard";
    public const string Pages_Administration_Host_HealthCheck = "Pages.Administration.Host.HealthCheck";
}

