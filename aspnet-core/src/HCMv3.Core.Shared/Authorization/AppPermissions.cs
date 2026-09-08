namespace HCMv3.Authorization;

/// <summary>
/// Defines string constants for application's permission names.
/// <see cref="AppAuthorizationProvider"/> for permission definitions.
/// </summary>
public static class AppPermissions
{
    //COMMON PERMISSIONS (FOR BOTH OF TENANTS AND HOST)

    public const string Pages = "Pages";


    /// <summary>
    /// Go-Live Setup -- the guided go-live setup (questions, bulk sheets, readiness board).
    /// Granted to whoever configures a tenant for its first payroll: the customer's own admin in the
    /// self-serve tier, a GBS consultant in the consultant-led tier.
    /// </summary>
    public const string Pages_GoLiveSetup = "Pages.GoLiveSetup";

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

    /// <summary>
    /// Read-only Company Structure Overview chart. Separate from <see cref="Pages_OrganizationManagement_OrgUnits"/>
    /// on purpose: viewing the org structure is a far broader entitlement than administering it, so the
    /// chart must be grantable without also granting create/edit/delete on the unit master.
    /// </summary>
    public const string Pages_OrganizationManagement_CompanyStructureChart = "Pages.OrganizationManagement.CompanyStructureChart";

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
    public const string Pages_PersonnelAdministration_Employees_ManageUserLink = "Pages.PersonnelAdministration.Employees.ManageUserLink";

    /// <summary>
    /// Read-only Employee Org Chart. Separate from <see cref="Pages_PersonnelAdministration_EmployeeManagers"/>
    /// on purpose: the chart shows the company's whole reporting structure, which is a far broader thing to
    /// hand out than the right to edit who reports to whom.
    /// </summary>
    public const string Pages_PersonnelAdministration_EmployeeOrgChart = "Pages.PersonnelAdministration.EmployeeOrgChart";

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

    public const string Pages_PersonnelAdministration_EmployeeAbsences = "Pages.PersonnelAdministration.EmployeeAbsences";
    public const string Pages_PersonnelAdministration_EmployeeAbsences_Create = "Pages.PersonnelAdministration.EmployeeAbsences.Create";
    public const string Pages_PersonnelAdministration_EmployeeAbsences_Edit = "Pages.PersonnelAdministration.EmployeeAbsences.Edit";
    public const string Pages_PersonnelAdministration_EmployeeAbsences_Delete = "Pages.PersonnelAdministration.EmployeeAbsences.Delete";

    public const string Pages_PersonnelAdministration_EmployeeAbsenceQuotas = "Pages.PersonnelAdministration.EmployeeAbsenceQuotas";
    public const string Pages_PersonnelAdministration_EmployeeAbsenceQuotas_Create = "Pages.PersonnelAdministration.EmployeeAbsenceQuotas.Create";
    public const string Pages_PersonnelAdministration_EmployeeAbsenceQuotas_Edit = "Pages.PersonnelAdministration.EmployeeAbsenceQuotas.Edit";
    public const string Pages_PersonnelAdministration_EmployeeAbsenceQuotas_Delete = "Pages.PersonnelAdministration.EmployeeAbsenceQuotas.Delete";

    public const string Pages_PersonnelAdministration_EmployeeAbsAdjustments = "Pages.PersonnelAdministration.EmployeeAbsAdjustments";
    public const string Pages_PersonnelAdministration_EmployeeAbsAdjustments_Create = "Pages.PersonnelAdministration.EmployeeAbsAdjustments.Create";
    public const string Pages_PersonnelAdministration_EmployeeAbsAdjustments_Edit = "Pages.PersonnelAdministration.EmployeeAbsAdjustments.Edit";
    public const string Pages_PersonnelAdministration_EmployeeAbsAdjustments_Delete = "Pages.PersonnelAdministration.EmployeeAbsAdjustments.Delete";

    public const string Pages_PersonnelAdministration_EmployeeAbsQuotaForfeits = "Pages.PersonnelAdministration.EmployeeAbsQuotaForfeits";
    public const string Pages_PersonnelAdministration_EmployeeAbsQuotaForfeits_Create = "Pages.PersonnelAdministration.EmployeeAbsQuotaForfeits.Create";
    public const string Pages_PersonnelAdministration_EmployeeAbsQuotaForfeits_Edit = "Pages.PersonnelAdministration.EmployeeAbsQuotaForfeits.Edit";
    public const string Pages_PersonnelAdministration_EmployeeAbsQuotaForfeits_Delete = "Pages.PersonnelAdministration.EmployeeAbsQuotaForfeits.Delete";

    public const string Pages_PersonnelAdministration_EmployeeAttendances = "Pages.PersonnelAdministration.EmployeeAttendances";
    public const string Pages_PersonnelAdministration_EmployeeAttendances_Create = "Pages.PersonnelAdministration.EmployeeAttendances.Create";
    public const string Pages_PersonnelAdministration_EmployeeAttendances_Edit = "Pages.PersonnelAdministration.EmployeeAttendances.Edit";
    public const string Pages_PersonnelAdministration_EmployeeAttendances_Delete = "Pages.PersonnelAdministration.EmployeeAttendances.Delete";

    public const string Pages_PersonnelAdministration_EmployeeAttendanceQuotas = "Pages.PersonnelAdministration.EmployeeAttendanceQuotas";
    public const string Pages_PersonnelAdministration_EmployeeAttendanceQuotas_Create = "Pages.PersonnelAdministration.EmployeeAttendanceQuotas.Create";
    public const string Pages_PersonnelAdministration_EmployeeAttendanceQuotas_Edit = "Pages.PersonnelAdministration.EmployeeAttendanceQuotas.Edit";
    public const string Pages_PersonnelAdministration_EmployeeAttendanceQuotas_Delete = "Pages.PersonnelAdministration.EmployeeAttendanceQuotas.Delete";

    public const string Pages_PersonnelAdministration_EmployeeBasicPays = "Pages.PersonnelAdministration.EmployeeBasicPays";
    public const string Pages_PersonnelAdministration_EmployeeBasicPays_Create = "Pages.PersonnelAdministration.EmployeeBasicPays.Create";
    public const string Pages_PersonnelAdministration_EmployeeBasicPays_Edit = "Pages.PersonnelAdministration.EmployeeBasicPays.Edit";
    public const string Pages_PersonnelAdministration_EmployeeBasicPays_Delete = "Pages.PersonnelAdministration.EmployeeBasicPays.Delete";

    public const string Pages_PersonnelAdministration_EmployeeBasicPayDetails = "Pages.PersonnelAdministration.EmployeeBasicPayDetails";
    public const string Pages_PersonnelAdministration_EmployeeBasicPayDetails_Create = "Pages.PersonnelAdministration.EmployeeBasicPayDetails.Create";
    public const string Pages_PersonnelAdministration_EmployeeBasicPayDetails_Edit = "Pages.PersonnelAdministration.EmployeeBasicPayDetails.Edit";
    public const string Pages_PersonnelAdministration_EmployeeBasicPayDetails_Delete = "Pages.PersonnelAdministration.EmployeeBasicPayDetails.Delete";

    public const string Pages_PersonnelAdministration_EmployeeOnHolds = "Pages.PersonnelAdministration.EmployeeOnHolds";
    public const string Pages_PersonnelAdministration_EmployeeOnHolds_Create = "Pages.PersonnelAdministration.EmployeeOnHolds.Create";
    public const string Pages_PersonnelAdministration_EmployeeOnHolds_Edit = "Pages.PersonnelAdministration.EmployeeOnHolds.Edit";
    public const string Pages_PersonnelAdministration_EmployeeOnHolds_Delete = "Pages.PersonnelAdministration.EmployeeOnHolds.Delete";

    public const string Pages_PersonnelAdministration_EmployeeGratuities = "Pages.PersonnelAdministration.EmployeeGratuities";
    public const string Pages_PersonnelAdministration_EmployeeGratuities_Create = "Pages.PersonnelAdministration.EmployeeGratuities.Create";
    public const string Pages_PersonnelAdministration_EmployeeGratuities_Edit = "Pages.PersonnelAdministration.EmployeeGratuities.Edit";
    public const string Pages_PersonnelAdministration_EmployeeGratuities_Delete = "Pages.PersonnelAdministration.EmployeeGratuities.Delete";

    public const string Pages_PersonnelAdministration_EmployeeGratuityQuotas = "Pages.PersonnelAdministration.EmployeeGratuityQuotas";
    public const string Pages_PersonnelAdministration_EmployeeGratuityQuotas_Create = "Pages.PersonnelAdministration.EmployeeGratuityQuotas.Create";
    public const string Pages_PersonnelAdministration_EmployeeGratuityQuotas_Edit = "Pages.PersonnelAdministration.EmployeeGratuityQuotas.Edit";
    public const string Pages_PersonnelAdministration_EmployeeGratuityQuotas_Delete = "Pages.PersonnelAdministration.EmployeeGratuityQuotas.Delete";

    public const string Pages_PersonnelAdministration_EmployeeLoans = "Pages.PersonnelAdministration.EmployeeLoans";
    public const string Pages_PersonnelAdministration_EmployeeLoans_Create = "Pages.PersonnelAdministration.EmployeeLoans.Create";
    public const string Pages_PersonnelAdministration_EmployeeLoans_Edit = "Pages.PersonnelAdministration.EmployeeLoans.Edit";
    public const string Pages_PersonnelAdministration_EmployeeLoans_Delete = "Pages.PersonnelAdministration.EmployeeLoans.Delete";

    public const string Pages_PersonnelAdministration_EmployeeLoanExemptions = "Pages.PersonnelAdministration.EmployeeLoanExemptions";
    public const string Pages_PersonnelAdministration_EmployeeLoanExemptions_Create = "Pages.PersonnelAdministration.EmployeeLoanExemptions.Create";
    public const string Pages_PersonnelAdministration_EmployeeLoanExemptions_Edit = "Pages.PersonnelAdministration.EmployeeLoanExemptions.Edit";
    public const string Pages_PersonnelAdministration_EmployeeLoanExemptions_Delete = "Pages.PersonnelAdministration.EmployeeLoanExemptions.Delete";

    public const string Pages_PersonnelAdministration_EmployeeLoanRecoveries = "Pages.PersonnelAdministration.EmployeeLoanRecoveries";
    public const string Pages_PersonnelAdministration_EmployeeLoanRecoveries_Create = "Pages.PersonnelAdministration.EmployeeLoanRecoveries.Create";
    public const string Pages_PersonnelAdministration_EmployeeLoanRecoveries_Edit = "Pages.PersonnelAdministration.EmployeeLoanRecoveries.Edit";
    public const string Pages_PersonnelAdministration_EmployeeLoanRecoveries_Delete = "Pages.PersonnelAdministration.EmployeeLoanRecoveries.Delete";

    public const string Pages_PersonnelAdministration_LoanInstallments = "Pages.PersonnelAdministration.LoanInstallments";
    public const string Pages_PersonnelAdministration_LoanInstallments_Create = "Pages.PersonnelAdministration.LoanInstallments.Create";
    public const string Pages_PersonnelAdministration_LoanInstallments_Edit = "Pages.PersonnelAdministration.LoanInstallments.Edit";
    public const string Pages_PersonnelAdministration_LoanInstallments_Delete = "Pages.PersonnelAdministration.LoanInstallments.Delete";

    public const string Pages_PersonnelAdministration_EmployeeFunds = "Pages.PersonnelAdministration.EmployeeFunds";
    public const string Pages_PersonnelAdministration_EmployeeFunds_Create = "Pages.PersonnelAdministration.EmployeeFunds.Create";
    public const string Pages_PersonnelAdministration_EmployeeFunds_Edit = "Pages.PersonnelAdministration.EmployeeFunds.Edit";
    public const string Pages_PersonnelAdministration_EmployeeFunds_Delete = "Pages.PersonnelAdministration.EmployeeFunds.Delete";

    public const string Pages_PersonnelAdministration_EmployeeFundDetails = "Pages.PersonnelAdministration.EmployeeFundDetails";
    public const string Pages_PersonnelAdministration_EmployeeFundDetails_Create = "Pages.PersonnelAdministration.EmployeeFundDetails.Create";
    public const string Pages_PersonnelAdministration_EmployeeFundDetails_Edit = "Pages.PersonnelAdministration.EmployeeFundDetails.Edit";
    public const string Pages_PersonnelAdministration_EmployeeFundDetails_Delete = "Pages.PersonnelAdministration.EmployeeFundDetails.Delete";

    public const string Pages_PersonnelAdministration_EmployeeFundExemptions = "Pages.PersonnelAdministration.EmployeeFundExemptions";
    public const string Pages_PersonnelAdministration_EmployeeFundExemptions_Create = "Pages.PersonnelAdministration.EmployeeFundExemptions.Create";
    public const string Pages_PersonnelAdministration_EmployeeFundExemptions_Edit = "Pages.PersonnelAdministration.EmployeeFundExemptions.Edit";
    public const string Pages_PersonnelAdministration_EmployeeFundExemptions_Delete = "Pages.PersonnelAdministration.EmployeeFundExemptions.Delete";

    // PersonnelAdministration (Wave 3 deferred bucket)
    public const string Pages_PersonnelAdministration_EmployeeTickets = "Pages.PersonnelAdministration.EmployeeTickets";
    public const string Pages_PersonnelAdministration_EmployeeTickets_Create = "Pages.PersonnelAdministration.EmployeeTickets.Create";
    public const string Pages_PersonnelAdministration_EmployeeTickets_Edit = "Pages.PersonnelAdministration.EmployeeTickets.Edit";
    public const string Pages_PersonnelAdministration_EmployeeTickets_Delete = "Pages.PersonnelAdministration.EmployeeTickets.Delete";

    public const string Pages_PersonnelAdministration_EmployeeTicketQuotas = "Pages.PersonnelAdministration.EmployeeTicketQuotas";
    public const string Pages_PersonnelAdministration_EmployeeTicketQuotas_Create = "Pages.PersonnelAdministration.EmployeeTicketQuotas.Create";
    public const string Pages_PersonnelAdministration_EmployeeTicketQuotas_Edit = "Pages.PersonnelAdministration.EmployeeTicketQuotas.Edit";
    public const string Pages_PersonnelAdministration_EmployeeTicketQuotas_Delete = "Pages.PersonnelAdministration.EmployeeTicketQuotas.Delete";

    public const string Pages_PersonnelAdministration_EmployeeTicketRoutes = "Pages.PersonnelAdministration.EmployeeTicketRoutes";
    public const string Pages_PersonnelAdministration_EmployeeTicketRoutes_Create = "Pages.PersonnelAdministration.EmployeeTicketRoutes.Create";
    public const string Pages_PersonnelAdministration_EmployeeTicketRoutes_Edit = "Pages.PersonnelAdministration.EmployeeTicketRoutes.Edit";
    public const string Pages_PersonnelAdministration_EmployeeTicketRoutes_Delete = "Pages.PersonnelAdministration.EmployeeTicketRoutes.Delete";

    public const string Pages_PersonnelAdministration_EmployeeTktAdjustments = "Pages.PersonnelAdministration.EmployeeTktAdjustments";
    public const string Pages_PersonnelAdministration_EmployeeTktAdjustments_Create = "Pages.PersonnelAdministration.EmployeeTktAdjustments.Create";
    public const string Pages_PersonnelAdministration_EmployeeTktAdjustments_Edit = "Pages.PersonnelAdministration.EmployeeTktAdjustments.Edit";
    public const string Pages_PersonnelAdministration_EmployeeTktAdjustments_Delete = "Pages.PersonnelAdministration.EmployeeTktAdjustments.Delete";

    public const string Pages_PersonnelAdministration_EmployeeSocialSecurities = "Pages.PersonnelAdministration.EmployeeSocialSecurities";
    public const string Pages_PersonnelAdministration_EmployeeSocialSecurities_Create = "Pages.PersonnelAdministration.EmployeeSocialSecurities.Create";
    public const string Pages_PersonnelAdministration_EmployeeSocialSecurities_Edit = "Pages.PersonnelAdministration.EmployeeSocialSecurities.Edit";
    public const string Pages_PersonnelAdministration_EmployeeSocialSecurities_Delete = "Pages.PersonnelAdministration.EmployeeSocialSecurities.Delete";

    public const string Pages_PersonnelAdministration_EmployeeSocialSecurityClaims = "Pages.PersonnelAdministration.EmployeeSocialSecurityClaims";
    public const string Pages_PersonnelAdministration_EmployeeSocialSecurityClaims_Create = "Pages.PersonnelAdministration.EmployeeSocialSecurityClaims.Create";
    public const string Pages_PersonnelAdministration_EmployeeSocialSecurityClaims_Edit = "Pages.PersonnelAdministration.EmployeeSocialSecurityClaims.Edit";
    public const string Pages_PersonnelAdministration_EmployeeSocialSecurityClaims_Delete = "Pages.PersonnelAdministration.EmployeeSocialSecurityClaims.Delete";

    public const string Pages_PersonnelAdministration_EmployeeDependants = "Pages.PersonnelAdministration.EmployeeDependants";
    public const string Pages_PersonnelAdministration_EmployeeDependants_Create = "Pages.PersonnelAdministration.EmployeeDependants.Create";
    public const string Pages_PersonnelAdministration_EmployeeDependants_Edit = "Pages.PersonnelAdministration.EmployeeDependants.Edit";
    public const string Pages_PersonnelAdministration_EmployeeDependants_Delete = "Pages.PersonnelAdministration.EmployeeDependants.Delete";

    public const string Pages_PersonnelAdministration_EmployeeDependantExceptions = "Pages.PersonnelAdministration.EmployeeDependantExceptions";
    public const string Pages_PersonnelAdministration_EmployeeDependantExceptions_Create = "Pages.PersonnelAdministration.EmployeeDependantExceptions.Create";
    public const string Pages_PersonnelAdministration_EmployeeDependantExceptions_Edit = "Pages.PersonnelAdministration.EmployeeDependantExceptions.Edit";
    public const string Pages_PersonnelAdministration_EmployeeDependantExceptions_Delete = "Pages.PersonnelAdministration.EmployeeDependantExceptions.Delete";

    public const string Pages_PersonnelAdministration_EmployeeSponsors = "Pages.PersonnelAdministration.EmployeeSponsors";
    public const string Pages_PersonnelAdministration_EmployeeSponsors_Create = "Pages.PersonnelAdministration.EmployeeSponsors.Create";
    public const string Pages_PersonnelAdministration_EmployeeSponsors_Edit = "Pages.PersonnelAdministration.EmployeeSponsors.Edit";
    public const string Pages_PersonnelAdministration_EmployeeSponsors_Delete = "Pages.PersonnelAdministration.EmployeeSponsors.Delete";

    public const string Pages_PersonnelAdministration_EmployeeFnFPayments = "Pages.PersonnelAdministration.EmployeeFnFPayments";
    public const string Pages_PersonnelAdministration_EmployeeFnFPayments_Create = "Pages.PersonnelAdministration.EmployeeFnFPayments.Create";
    public const string Pages_PersonnelAdministration_EmployeeFnFPayments_Edit = "Pages.PersonnelAdministration.EmployeeFnFPayments.Edit";
    public const string Pages_PersonnelAdministration_EmployeeFnFPayments_Delete = "Pages.PersonnelAdministration.EmployeeFnFPayments.Delete";

    public const string Pages_PersonnelAdministration_EmployeeBanks = "Pages.PersonnelAdministration.EmployeeBanks";
    public const string Pages_PersonnelAdministration_EmployeeBanks_Create = "Pages.PersonnelAdministration.EmployeeBanks.Create";
    public const string Pages_PersonnelAdministration_EmployeeBanks_Edit = "Pages.PersonnelAdministration.EmployeeBanks.Edit";
    public const string Pages_PersonnelAdministration_EmployeeBanks_Delete = "Pages.PersonnelAdministration.EmployeeBanks.Delete";

    public const string Pages_PersonnelAdministration_EmployeeAssets = "Pages.PersonnelAdministration.EmployeeAssets";
    public const string Pages_PersonnelAdministration_EmployeeAssets_Create = "Pages.PersonnelAdministration.EmployeeAssets.Create";
    public const string Pages_PersonnelAdministration_EmployeeAssets_Edit = "Pages.PersonnelAdministration.EmployeeAssets.Edit";
    public const string Pages_PersonnelAdministration_EmployeeAssets_Delete = "Pages.PersonnelAdministration.EmployeeAssets.Delete";

    public const string Pages_PersonnelAdministration_EmployeeHRServices = "Pages.PersonnelAdministration.EmployeeHRServices";
    public const string Pages_PersonnelAdministration_EmployeeHRServices_Create = "Pages.PersonnelAdministration.EmployeeHRServices.Create";
    public const string Pages_PersonnelAdministration_EmployeeHRServices_Edit = "Pages.PersonnelAdministration.EmployeeHRServices.Edit";
    public const string Pages_PersonnelAdministration_EmployeeHRServices_Delete = "Pages.PersonnelAdministration.EmployeeHRServices.Delete";

    public const string Pages_PersonnelAdministration_EmployeeLetterRequests = "Pages.PersonnelAdministration.EmployeeLetterRequests";
    public const string Pages_PersonnelAdministration_EmployeeLetterRequests_Create = "Pages.PersonnelAdministration.EmployeeLetterRequests.Create";
    public const string Pages_PersonnelAdministration_EmployeeLetterRequests_Edit = "Pages.PersonnelAdministration.EmployeeLetterRequests.Edit";
    public const string Pages_PersonnelAdministration_EmployeeLetterRequests_Delete = "Pages.PersonnelAdministration.EmployeeLetterRequests.Delete";

    public const string Pages_PersonnelAdministration_EmployeeITServices = "Pages.PersonnelAdministration.EmployeeITServices";
    public const string Pages_PersonnelAdministration_EmployeeITServices_Create = "Pages.PersonnelAdministration.EmployeeITServices.Create";
    public const string Pages_PersonnelAdministration_EmployeeITServices_Edit = "Pages.PersonnelAdministration.EmployeeITServices.Edit";
    public const string Pages_PersonnelAdministration_EmployeeITServices_Delete = "Pages.PersonnelAdministration.EmployeeITServices.Delete";

    public const string Pages_PersonnelAdministration_EmployeeAssetInvoices = "Pages.PersonnelAdministration.EmployeeAssetInvoices";
    public const string Pages_PersonnelAdministration_EmployeeAssetInvoices_Create = "Pages.PersonnelAdministration.EmployeeAssetInvoices.Create";
    public const string Pages_PersonnelAdministration_EmployeeAssetInvoices_Edit = "Pages.PersonnelAdministration.EmployeeAssetInvoices.Edit";
    public const string Pages_PersonnelAdministration_EmployeeAssetInvoices_Delete = "Pages.PersonnelAdministration.EmployeeAssetInvoices.Delete";

    public const string Pages_PersonnelAdministration_EmployeeDisciplinaries = "Pages.PersonnelAdministration.EmployeeDisciplinaries";
    public const string Pages_PersonnelAdministration_EmployeeDisciplinaries_Create = "Pages.PersonnelAdministration.EmployeeDisciplinaries.Create";
    public const string Pages_PersonnelAdministration_EmployeeDisciplinaries_Edit = "Pages.PersonnelAdministration.EmployeeDisciplinaries.Edit";
    public const string Pages_PersonnelAdministration_EmployeeDisciplinaries_Delete = "Pages.PersonnelAdministration.EmployeeDisciplinaries.Delete";

    public const string Pages_PersonnelAdministration_EmployeeGrievances = "Pages.PersonnelAdministration.EmployeeGrievances";
    public const string Pages_PersonnelAdministration_EmployeeGrievances_Create = "Pages.PersonnelAdministration.EmployeeGrievances.Create";
    public const string Pages_PersonnelAdministration_EmployeeGrievances_Edit = "Pages.PersonnelAdministration.EmployeeGrievances.Edit";
    public const string Pages_PersonnelAdministration_EmployeeGrievances_Delete = "Pages.PersonnelAdministration.EmployeeGrievances.Delete";

    public const string Pages_PersonnelAdministration_EmployeeHealthReimburses = "Pages.PersonnelAdministration.EmployeeHealthReimburses";
    public const string Pages_PersonnelAdministration_EmployeeHealthReimburses_Create = "Pages.PersonnelAdministration.EmployeeHealthReimburses.Create";
    public const string Pages_PersonnelAdministration_EmployeeHealthReimburses_Edit = "Pages.PersonnelAdministration.EmployeeHealthReimburses.Edit";
    public const string Pages_PersonnelAdministration_EmployeeHealthReimburses_Delete = "Pages.PersonnelAdministration.EmployeeHealthReimburses.Delete";

    public const string Pages_PersonnelAdministration_EmployeeOvertimes = "Pages.PersonnelAdministration.EmployeeOvertimes";
    public const string Pages_PersonnelAdministration_EmployeeOvertimes_Create = "Pages.PersonnelAdministration.EmployeeOvertimes.Create";
    public const string Pages_PersonnelAdministration_EmployeeOvertimes_Edit = "Pages.PersonnelAdministration.EmployeeOvertimes.Edit";
    public const string Pages_PersonnelAdministration_EmployeeOvertimes_Delete = "Pages.PersonnelAdministration.EmployeeOvertimes.Delete";

    public const string Pages_PersonnelAdministration_EmployeePayments = "Pages.PersonnelAdministration.EmployeePayments";
    public const string Pages_PersonnelAdministration_EmployeePayments_Create = "Pages.PersonnelAdministration.EmployeePayments.Create";
    public const string Pages_PersonnelAdministration_EmployeePayments_Edit = "Pages.PersonnelAdministration.EmployeePayments.Edit";
    public const string Pages_PersonnelAdministration_EmployeePayments_Delete = "Pages.PersonnelAdministration.EmployeePayments.Delete";

    // The HR payment BATCH -- one wage type, many employees, one approval. Separate from EmployeePayments
    // above on purpose: granting somebody the right to key in a single payment is not the same as granting
    // them the right to put a whole payroll run's worth of money in front of an approver.
    public const string Pages_PersonnelAdministration_EmployeePaymentBatches = "Pages.PersonnelAdministration.EmployeePaymentBatches";
    public const string Pages_PersonnelAdministration_EmployeePaymentBatches_Create = "Pages.PersonnelAdministration.EmployeePaymentBatches.Create";
    public const string Pages_PersonnelAdministration_EmployeePaymentBatches_Edit = "Pages.PersonnelAdministration.EmployeePaymentBatches.Edit";
    public const string Pages_PersonnelAdministration_EmployeePaymentBatches_Delete = "Pages.PersonnelAdministration.EmployeePaymentBatches.Delete";

    public const string Pages_PersonnelAdministration_EmployeeRecurringPayments = "Pages.PersonnelAdministration.EmployeeRecurringPayments";
    public const string Pages_PersonnelAdministration_EmployeeRecurringPayments_Create = "Pages.PersonnelAdministration.EmployeeRecurringPayments.Create";
    public const string Pages_PersonnelAdministration_EmployeeRecurringPayments_Edit = "Pages.PersonnelAdministration.EmployeeRecurringPayments.Edit";
    public const string Pages_PersonnelAdministration_EmployeeRecurringPayments_Delete = "Pages.PersonnelAdministration.EmployeeRecurringPayments.Delete";

    public const string Pages_PersonnelAdministration_EmployeeRemunerations = "Pages.PersonnelAdministration.EmployeeRemunerations";
    public const string Pages_PersonnelAdministration_EmployeeRemunerations_Create = "Pages.PersonnelAdministration.EmployeeRemunerations.Create";
    public const string Pages_PersonnelAdministration_EmployeeRemunerations_Edit = "Pages.PersonnelAdministration.EmployeeRemunerations.Edit";
    public const string Pages_PersonnelAdministration_EmployeeRemunerations_Delete = "Pages.PersonnelAdministration.EmployeeRemunerations.Delete";

    public const string Pages_PersonnelAdministration_EmployeeRewards = "Pages.PersonnelAdministration.EmployeeRewards";
    public const string Pages_PersonnelAdministration_EmployeeRewards_Create = "Pages.PersonnelAdministration.EmployeeRewards.Create";
    public const string Pages_PersonnelAdministration_EmployeeRewards_Edit = "Pages.PersonnelAdministration.EmployeeRewards.Edit";
    public const string Pages_PersonnelAdministration_EmployeeRewards_Delete = "Pages.PersonnelAdministration.EmployeeRewards.Delete";

    public const string Pages_PersonnelAdministration_EmployeeCostDistributions = "Pages.PersonnelAdministration.EmployeeCostDistributions";
    public const string Pages_PersonnelAdministration_EmployeeCostDistributions_Create = "Pages.PersonnelAdministration.EmployeeCostDistributions.Create";
    public const string Pages_PersonnelAdministration_EmployeeCostDistributions_Edit = "Pages.PersonnelAdministration.EmployeeCostDistributions.Edit";
    public const string Pages_PersonnelAdministration_EmployeeCostDistributions_Delete = "Pages.PersonnelAdministration.EmployeeCostDistributions.Delete";

    public const string Pages_PersonnelAdministration_EmployeeAccountAssignments = "Pages.PersonnelAdministration.EmployeeAccountAssignments";
    public const string Pages_PersonnelAdministration_EmployeeAccountAssignments_Create = "Pages.PersonnelAdministration.EmployeeAccountAssignments.Create";
    public const string Pages_PersonnelAdministration_EmployeeAccountAssignments_Edit = "Pages.PersonnelAdministration.EmployeeAccountAssignments.Edit";
    public const string Pages_PersonnelAdministration_EmployeeAccountAssignments_Delete = "Pages.PersonnelAdministration.EmployeeAccountAssignments.Delete";

    public const string Pages_PersonnelAdministration_EmployeeExtdWorkLocations = "Pages.PersonnelAdministration.EmployeeExtdWorkLocations";
    public const string Pages_PersonnelAdministration_EmployeeExtdWorkLocations_Create = "Pages.PersonnelAdministration.EmployeeExtdWorkLocations.Create";
    public const string Pages_PersonnelAdministration_EmployeeExtdWorkLocations_Edit = "Pages.PersonnelAdministration.EmployeeExtdWorkLocations.Edit";
    public const string Pages_PersonnelAdministration_EmployeeExtdWorkLocations_Delete = "Pages.PersonnelAdministration.EmployeeExtdWorkLocations.Delete";

    public const string Pages_PersonnelAdministration_EmployeeBenefitExceptions = "Pages.PersonnelAdministration.EmployeeBenefitExceptions";
    public const string Pages_PersonnelAdministration_EmployeeBenefitExceptions_Create = "Pages.PersonnelAdministration.EmployeeBenefitExceptions.Create";
    public const string Pages_PersonnelAdministration_EmployeeBenefitExceptions_Edit = "Pages.PersonnelAdministration.EmployeeBenefitExceptions.Edit";
    public const string Pages_PersonnelAdministration_EmployeeBenefitExceptions_Delete = "Pages.PersonnelAdministration.EmployeeBenefitExceptions.Delete";

    public const string Pages_PersonnelAdministration_EmployeeNonPayrollTrans = "Pages.PersonnelAdministration.EmployeeNonPayrollTrans";
    public const string Pages_PersonnelAdministration_EmployeeNonPayrollTrans_Create = "Pages.PersonnelAdministration.EmployeeNonPayrollTrans.Create";
    public const string Pages_PersonnelAdministration_EmployeeNonPayrollTrans_Edit = "Pages.PersonnelAdministration.EmployeeNonPayrollTrans.Edit";
    public const string Pages_PersonnelAdministration_EmployeeNonPayrollTrans_Delete = "Pages.PersonnelAdministration.EmployeeNonPayrollTrans.Delete";

    public const string Pages_PersonnelAdministration_EmployeeTasks = "Pages.PersonnelAdministration.EmployeeTasks";
    public const string Pages_PersonnelAdministration_EmployeeTasks_Create = "Pages.PersonnelAdministration.EmployeeTasks.Create";
    public const string Pages_PersonnelAdministration_EmployeeTasks_Edit = "Pages.PersonnelAdministration.EmployeeTasks.Edit";
    public const string Pages_PersonnelAdministration_EmployeeTasks_Delete = "Pages.PersonnelAdministration.EmployeeTasks.Delete";

    // Work ASSIGNED to an employee (legacy TaskRequest). A separate permission set from EmployeeTasks above,
    // because it guards a separate thing: EmployeeTasks is the HR reminder/monitoring record, this is an
    // assignment that routes for approval. Sharing one permission would mean granting HR's diary in order to
    // let a manager hand out work.
    public const string Pages_PersonnelAdministration_EmployeeAssignedTasks = "Pages.PersonnelAdministration.EmployeeAssignedTasks";
    public const string Pages_PersonnelAdministration_EmployeeAssignedTasks_Create = "Pages.PersonnelAdministration.EmployeeAssignedTasks.Create";
    public const string Pages_PersonnelAdministration_EmployeeAssignedTasks_Edit = "Pages.PersonnelAdministration.EmployeeAssignedTasks.Edit";
    public const string Pages_PersonnelAdministration_EmployeeAssignedTasks_Delete = "Pages.PersonnelAdministration.EmployeeAssignedTasks.Delete";

    public const string Pages_PersonnelAdministration_News = "Pages.PersonnelAdministration.News";
    public const string Pages_PersonnelAdministration_News_Create = "Pages.PersonnelAdministration.News.Create";
    public const string Pages_PersonnelAdministration_News_Edit = "Pages.PersonnelAdministration.News.Edit";
    public const string Pages_PersonnelAdministration_News_Delete = "Pages.PersonnelAdministration.News.Delete";

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

    /// <summary>
    /// Re-map a Screen Code. Codes are how people refer to screens to each other, so changing one
    /// changes shared vocabulary -- an administrator's decision, not a personal preference.
    /// </summary>
    public const string Pages_Administration_Navigation_ManageScreenCodes = "Pages.Administration.Navigation.ManageScreenCodes";

    /// <summary>Publish tenant-wide quick links onto everyone's dashboard.</summary>
    public const string Pages_Administration_Navigation_ManageQuickLinks = "Pages.Administration.Navigation.ManageQuickLinks";

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

    /// <summary>
    /// Read-only roster schedule viewer (ported legacy uscRosterDutySchedule screen). View-only, so there is
    /// deliberately no Create/Edit/Delete child permission.
    /// </summary>
    public const string Pages_TimeManagement_RosterScheduleViewer = "Pages.TimeManagement.RosterScheduleViewer";

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

    // Batch approval of calculated timesheet days (legacy TimeLogRequest). A manager/HR grid, not self
    // service -- there is deliberately no Ess_* counterpart, exactly as for personnel actions.
    public const string Pages_TimeManagement_TimeLogApprovals = "Pages.TimeManagement.TimeLogApprovals";
    public const string Pages_TimeManagement_TimeLogApprovals_Create = "Pages.TimeManagement.TimeLogApprovals.Create";

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

    // Wave T4 (Time Engine execution). Legacy gated this behind the uscTimeEngine screen's own screen
    // permission; there is no CRUD here -- the only action is "run".
    public const string Pages_TimeManagement_TimeEngine = "Pages.TimeManagement.TimeEngine";
    public const string Pages_TimeManagement_TimeEngine_Run = "Pages.TimeManagement.TimeEngine.Run";

    public const string Pages_PayrollManagement = "Pages.PayrollManagement";

    // Cost allocation file screen (legacy uscCostingFile.ascx). Run = execute the cost engine,
    // Export = download the costing spreadsheet, View = read-only preview/validate.
    public const string Pages_PayrollManagement_CostingFile = "Pages.PayrollManagement.CostingFile";
    public const string Pages_PayrollManagement_CostingFile_Run = "Pages.PayrollManagement.CostingFile.Run";
    public const string Pages_PayrollManagement_CostingFile_Export = "Pages.PayrollManagement.CostingFile.Export";

    // Journal voucher posting screen (legacy uscPostJV.ascx). Post is separated from View because
    // posting writes irreversibly to the external finance system.
    public const string Pages_PayrollManagement_PostJv = "Pages.PayrollManagement.PostJv";
    public const string Pages_PayrollManagement_PostJv_Post = "Pages.PayrollManagement.PostJv.Post";

    public const string Pages_PayrollManagement_AXBatches = "Pages.PayrollManagement.AXBatches";
    public const string Pages_PayrollManagement_AXBatches_Create = "Pages.PayrollManagement.AXBatches.Create";
    public const string Pages_PayrollManagement_AXBatches_Edit = "Pages.PayrollManagement.AXBatches.Edit";
    public const string Pages_PayrollManagement_AXBatches_Delete = "Pages.PayrollManagement.AXBatches.Delete";

    public const string Pages_PayrollManagement_AbsenceTypes = "Pages.PayrollManagement.AbsenceTypes";
    public const string Pages_PayrollManagement_AbsenceTypes_Create = "Pages.PayrollManagement.AbsenceTypes.Create";
    public const string Pages_PayrollManagement_AbsenceTypes_Edit = "Pages.PayrollManagement.AbsenceTypes.Edit";
    public const string Pages_PayrollManagement_AbsenceTypes_Delete = "Pages.PayrollManagement.AbsenceTypes.Delete";

    public const string Pages_PayrollManagement_AbsenceTypeExceptions = "Pages.PayrollManagement.AbsenceTypeExceptions";
    public const string Pages_PayrollManagement_AbsenceTypeExceptions_Create = "Pages.PayrollManagement.AbsenceTypeExceptions.Create";
    public const string Pages_PayrollManagement_AbsenceTypeExceptions_Edit = "Pages.PayrollManagement.AbsenceTypeExceptions.Edit";
    public const string Pages_PayrollManagement_AbsenceTypeExceptions_Delete = "Pages.PayrollManagement.AbsenceTypeExceptions.Delete";

    public const string Pages_PayrollManagement_AbsenceDeductionRules = "Pages.PayrollManagement.AbsenceDeductionRules";
    public const string Pages_PayrollManagement_AbsenceDeductionRules_Create = "Pages.PayrollManagement.AbsenceDeductionRules.Create";
    public const string Pages_PayrollManagement_AbsenceDeductionRules_Edit = "Pages.PayrollManagement.AbsenceDeductionRules.Edit";
    public const string Pages_PayrollManagement_AbsenceDeductionRules_Delete = "Pages.PayrollManagement.AbsenceDeductionRules.Delete";

    public const string Pages_PayrollManagement_AbsenceDedRuleDetails = "Pages.PayrollManagement.AbsenceDedRuleDetails";
    public const string Pages_PayrollManagement_AbsenceDedRuleDetails_Create = "Pages.PayrollManagement.AbsenceDedRuleDetails.Create";
    public const string Pages_PayrollManagement_AbsenceDedRuleDetails_Edit = "Pages.PayrollManagement.AbsenceDedRuleDetails.Edit";
    public const string Pages_PayrollManagement_AbsenceDedRuleDetails_Delete = "Pages.PayrollManagement.AbsenceDedRuleDetails.Delete";

    public const string Pages_PayrollManagement_AbsenceViolationRules = "Pages.PayrollManagement.AbsenceViolationRules";
    public const string Pages_PayrollManagement_AbsenceViolationRules_Create = "Pages.PayrollManagement.AbsenceViolationRules.Create";
    public const string Pages_PayrollManagement_AbsenceViolationRules_Edit = "Pages.PayrollManagement.AbsenceViolationRules.Edit";
    public const string Pages_PayrollManagement_AbsenceViolationRules_Delete = "Pages.PayrollManagement.AbsenceViolationRules.Delete";

    public const string Pages_PayrollManagement_AbsenceViolationRuleDetails = "Pages.PayrollManagement.AbsenceViolationRuleDetails";
    public const string Pages_PayrollManagement_AbsenceViolationRuleDetails_Create = "Pages.PayrollManagement.AbsenceViolationRuleDetails.Create";
    public const string Pages_PayrollManagement_AbsenceViolationRuleDetails_Edit = "Pages.PayrollManagement.AbsenceViolationRuleDetails.Edit";
    public const string Pages_PayrollManagement_AbsenceViolationRuleDetails_Delete = "Pages.PayrollManagement.AbsenceViolationRuleDetails.Delete";

    public const string Pages_PayrollManagement_AbsenceViolationRuleExceptions = "Pages.PayrollManagement.AbsenceViolationRuleExceptions";
    public const string Pages_PayrollManagement_AbsenceViolationRuleExceptions_Create = "Pages.PayrollManagement.AbsenceViolationRuleExceptions.Create";
    public const string Pages_PayrollManagement_AbsenceViolationRuleExceptions_Edit = "Pages.PayrollManagement.AbsenceViolationRuleExceptions.Edit";
    public const string Pages_PayrollManagement_AbsenceViolationRuleExceptions_Delete = "Pages.PayrollManagement.AbsenceViolationRuleExceptions.Delete";

    public const string Pages_PayrollManagement_AttendanceTypes = "Pages.PayrollManagement.AttendanceTypes";
    public const string Pages_PayrollManagement_AttendanceTypes_Create = "Pages.PayrollManagement.AttendanceTypes.Create";
    public const string Pages_PayrollManagement_AttendanceTypes_Edit = "Pages.PayrollManagement.AttendanceTypes.Edit";
    public const string Pages_PayrollManagement_AttendanceTypes_Delete = "Pages.PayrollManagement.AttendanceTypes.Delete";

    public const string Pages_PayrollManagement_AccountTypes = "Pages.PayrollManagement.AccountTypes";
    public const string Pages_PayrollManagement_AccountTypes_Create = "Pages.PayrollManagement.AccountTypes.Create";
    public const string Pages_PayrollManagement_AccountTypes_Edit = "Pages.PayrollManagement.AccountTypes.Edit";
    public const string Pages_PayrollManagement_AccountTypes_Delete = "Pages.PayrollManagement.AccountTypes.Delete";

    public const string Pages_PayrollManagement_AccountInfos = "Pages.PayrollManagement.AccountInfos";
    public const string Pages_PayrollManagement_AccountInfos_Create = "Pages.PayrollManagement.AccountInfos.Create";
    public const string Pages_PayrollManagement_AccountInfos_Edit = "Pages.PayrollManagement.AccountInfos.Edit";
    public const string Pages_PayrollManagement_AccountInfos_Delete = "Pages.PayrollManagement.AccountInfos.Delete";

    public const string Pages_PayrollManagement_EmployeeGroups = "Pages.PayrollManagement.EmployeeGroups";
    public const string Pages_PayrollManagement_EmployeeGroups_Create = "Pages.PayrollManagement.EmployeeGroups.Create";
    public const string Pages_PayrollManagement_EmployeeGroups_Edit = "Pages.PayrollManagement.EmployeeGroups.Edit";
    public const string Pages_PayrollManagement_EmployeeGroups_Delete = "Pages.PayrollManagement.EmployeeGroups.Delete";

    public const string Pages_PayrollManagement_PayrollReasonTypes = "Pages.PayrollManagement.PayrollReasonTypes";
    public const string Pages_PayrollManagement_PayrollReasonTypes_Create = "Pages.PayrollManagement.PayrollReasonTypes.Create";
    public const string Pages_PayrollManagement_PayrollReasonTypes_Edit = "Pages.PayrollManagement.PayrollReasonTypes.Edit";
    public const string Pages_PayrollManagement_PayrollReasonTypes_Delete = "Pages.PayrollManagement.PayrollReasonTypes.Delete";

    public const string Pages_PayrollManagement_EmployeeSubGroups = "Pages.PayrollManagement.EmployeeSubGroups";
    public const string Pages_PayrollManagement_EmployeeSubGroups_Create = "Pages.PayrollManagement.EmployeeSubGroups.Create";
    public const string Pages_PayrollManagement_EmployeeSubGroups_Edit = "Pages.PayrollManagement.EmployeeSubGroups.Edit";
    public const string Pages_PayrollManagement_EmployeeSubGroups_Delete = "Pages.PayrollManagement.EmployeeSubGroups.Delete";

    public const string Pages_PayrollManagement_EmployeeSubGroupAbsences = "Pages.PayrollManagement.EmployeeSubGroupAbsences";
    public const string Pages_PayrollManagement_EmployeeSubGroupAbsences_Create = "Pages.PayrollManagement.EmployeeSubGroupAbsences.Create";
    public const string Pages_PayrollManagement_EmployeeSubGroupAbsences_Edit = "Pages.PayrollManagement.EmployeeSubGroupAbsences.Edit";
    public const string Pages_PayrollManagement_EmployeeSubGroupAbsences_Delete = "Pages.PayrollManagement.EmployeeSubGroupAbsences.Delete";

    public const string Pages_PayrollManagement_EmployeeSubGroupAbsenceTypes = "Pages.PayrollManagement.EmployeeSubGroupAbsenceTypes";
    public const string Pages_PayrollManagement_EmployeeSubGroupAbsenceTypes_Create = "Pages.PayrollManagement.EmployeeSubGroupAbsenceTypes.Create";
    public const string Pages_PayrollManagement_EmployeeSubGroupAbsenceTypes_Edit = "Pages.PayrollManagement.EmployeeSubGroupAbsenceTypes.Edit";
    public const string Pages_PayrollManagement_EmployeeSubGroupAbsenceTypes_Delete = "Pages.PayrollManagement.EmployeeSubGroupAbsenceTypes.Delete";

    public const string Pages_PayrollManagement_EmployeeSubGroupAssetAllowances = "Pages.PayrollManagement.EmployeeSubGroupAssetAllowances";
    public const string Pages_PayrollManagement_EmployeeSubGroupAssetAllowances_Create = "Pages.PayrollManagement.EmployeeSubGroupAssetAllowances.Create";
    public const string Pages_PayrollManagement_EmployeeSubGroupAssetAllowances_Edit = "Pages.PayrollManagement.EmployeeSubGroupAssetAllowances.Edit";
    public const string Pages_PayrollManagement_EmployeeSubGroupAssetAllowances_Delete = "Pages.PayrollManagement.EmployeeSubGroupAssetAllowances.Delete";

    public const string Pages_PayrollManagement_EmployeeSubGroupBenefits = "Pages.PayrollManagement.EmployeeSubGroupBenefits";
    public const string Pages_PayrollManagement_EmployeeSubGroupBenefits_Create = "Pages.PayrollManagement.EmployeeSubGroupBenefits.Create";
    public const string Pages_PayrollManagement_EmployeeSubGroupBenefits_Edit = "Pages.PayrollManagement.EmployeeSubGroupBenefits.Edit";
    public const string Pages_PayrollManagement_EmployeeSubGroupBenefits_Delete = "Pages.PayrollManagement.EmployeeSubGroupBenefits.Delete";

    public const string Pages_PayrollManagement_EmployeeSubGroupBenefitDetails = "Pages.PayrollManagement.EmployeeSubGroupBenefitDetails";
    public const string Pages_PayrollManagement_EmployeeSubGroupBenefitDetails_Create = "Pages.PayrollManagement.EmployeeSubGroupBenefitDetails.Create";
    public const string Pages_PayrollManagement_EmployeeSubGroupBenefitDetails_Edit = "Pages.PayrollManagement.EmployeeSubGroupBenefitDetails.Edit";
    public const string Pages_PayrollManagement_EmployeeSubGroupBenefitDetails_Delete = "Pages.PayrollManagement.EmployeeSubGroupBenefitDetails.Delete";

    public const string Pages_PayrollManagement_EmployeeSubGroupBonuses = "Pages.PayrollManagement.EmployeeSubGroupBonuses";
    public const string Pages_PayrollManagement_EmployeeSubGroupBonuses_Create = "Pages.PayrollManagement.EmployeeSubGroupBonuses.Create";
    public const string Pages_PayrollManagement_EmployeeSubGroupBonuses_Edit = "Pages.PayrollManagement.EmployeeSubGroupBonuses.Edit";
    public const string Pages_PayrollManagement_EmployeeSubGroupBonuses_Delete = "Pages.PayrollManagement.EmployeeSubGroupBonuses.Delete";

    public const string Pages_PayrollManagement_EmployeeSubGroupCommissions = "Pages.PayrollManagement.EmployeeSubGroupCommissions";
    public const string Pages_PayrollManagement_EmployeeSubGroupCommissions_Create = "Pages.PayrollManagement.EmployeeSubGroupCommissions.Create";
    public const string Pages_PayrollManagement_EmployeeSubGroupCommissions_Edit = "Pages.PayrollManagement.EmployeeSubGroupCommissions.Edit";
    public const string Pages_PayrollManagement_EmployeeSubGroupCommissions_Delete = "Pages.PayrollManagement.EmployeeSubGroupCommissions.Delete";

    public const string Pages_PayrollManagement_EmployeeSubGroupFunds = "Pages.PayrollManagement.EmployeeSubGroupFunds";
    public const string Pages_PayrollManagement_EmployeeSubGroupFunds_Create = "Pages.PayrollManagement.EmployeeSubGroupFunds.Create";
    public const string Pages_PayrollManagement_EmployeeSubGroupFunds_Edit = "Pages.PayrollManagement.EmployeeSubGroupFunds.Edit";
    public const string Pages_PayrollManagement_EmployeeSubGroupFunds_Delete = "Pages.PayrollManagement.EmployeeSubGroupFunds.Delete";

    public const string Pages_PayrollManagement_EmployeeSubGroupGratuities = "Pages.PayrollManagement.EmployeeSubGroupGratuities";
    public const string Pages_PayrollManagement_EmployeeSubGroupGratuities_Create = "Pages.PayrollManagement.EmployeeSubGroupGratuities.Create";
    public const string Pages_PayrollManagement_EmployeeSubGroupGratuities_Edit = "Pages.PayrollManagement.EmployeeSubGroupGratuities.Edit";
    public const string Pages_PayrollManagement_EmployeeSubGroupGratuities_Delete = "Pages.PayrollManagement.EmployeeSubGroupGratuities.Delete";

    public const string Pages_PayrollManagement_EmployeeSubGroupHealthReimburses = "Pages.PayrollManagement.EmployeeSubGroupHealthReimburses";
    public const string Pages_PayrollManagement_EmployeeSubGroupHealthReimburses_Create = "Pages.PayrollManagement.EmployeeSubGroupHealthReimburses.Create";
    public const string Pages_PayrollManagement_EmployeeSubGroupHealthReimburses_Edit = "Pages.PayrollManagement.EmployeeSubGroupHealthReimburses.Edit";
    public const string Pages_PayrollManagement_EmployeeSubGroupHealthReimburses_Delete = "Pages.PayrollManagement.EmployeeSubGroupHealthReimburses.Delete";

    public const string Pages_PayrollManagement_EmployeeSubGroupPayments = "Pages.PayrollManagement.EmployeeSubGroupPayments";
    public const string Pages_PayrollManagement_EmployeeSubGroupPayments_Create = "Pages.PayrollManagement.EmployeeSubGroupPayments.Create";
    public const string Pages_PayrollManagement_EmployeeSubGroupPayments_Edit = "Pages.PayrollManagement.EmployeeSubGroupPayments.Edit";
    public const string Pages_PayrollManagement_EmployeeSubGroupPayments_Delete = "Pages.PayrollManagement.EmployeeSubGroupPayments.Delete";

    public const string Pages_PayrollManagement_EmployeeSubGroupTickets = "Pages.PayrollManagement.EmployeeSubGroupTickets";
    public const string Pages_PayrollManagement_EmployeeSubGroupTickets_Create = "Pages.PayrollManagement.EmployeeSubGroupTickets.Create";
    public const string Pages_PayrollManagement_EmployeeSubGroupTickets_Edit = "Pages.PayrollManagement.EmployeeSubGroupTickets.Edit";
    public const string Pages_PayrollManagement_EmployeeSubGroupTickets_Delete = "Pages.PayrollManagement.EmployeeSubGroupTickets.Delete";

    public const string Pages_PayrollManagement_EmployeeSubGroupWageTypes = "Pages.PayrollManagement.EmployeeSubGroupWageTypes";
    public const string Pages_PayrollManagement_EmployeeSubGroupWageTypes_Create = "Pages.PayrollManagement.EmployeeSubGroupWageTypes.Create";
    public const string Pages_PayrollManagement_EmployeeSubGroupWageTypes_Edit = "Pages.PayrollManagement.EmployeeSubGroupWageTypes.Edit";
    public const string Pages_PayrollManagement_EmployeeSubGroupWageTypes_Delete = "Pages.PayrollManagement.EmployeeSubGroupWageTypes.Delete";

    public const string Pages_PayrollManagement_PayrollPeriods = "Pages.PayrollManagement.PayrollPeriods";
    public const string Pages_PayrollManagement_PayrollPeriods_Create = "Pages.PayrollManagement.PayrollPeriods.Create";
    public const string Pages_PayrollManagement_PayrollPeriods_Edit = "Pages.PayrollManagement.PayrollPeriods.Edit";
    public const string Pages_PayrollManagement_PayrollPeriods_Delete = "Pages.PayrollManagement.PayrollPeriods.Delete";

    public const string Pages_PayrollManagement_PayrollAreas = "Pages.PayrollManagement.PayrollAreas";
    public const string Pages_PayrollManagement_PayrollAreas_Create = "Pages.PayrollManagement.PayrollAreas.Create";
    public const string Pages_PayrollManagement_PayrollAreas_Edit = "Pages.PayrollManagement.PayrollAreas.Edit";
    public const string Pages_PayrollManagement_PayrollAreas_Delete = "Pages.PayrollManagement.PayrollAreas.Delete";

    public const string Pages_PayrollManagement_PayrollCalendars = "Pages.PayrollManagement.PayrollCalendars";
    public const string Pages_PayrollManagement_PayrollCalendars_Create = "Pages.PayrollManagement.PayrollCalendars.Create";
    public const string Pages_PayrollManagement_PayrollCalendars_Edit = "Pages.PayrollManagement.PayrollCalendars.Edit";
    public const string Pages_PayrollManagement_PayrollCalendars_Delete = "Pages.PayrollManagement.PayrollCalendars.Delete";

    public const string Pages_PayrollManagement_PayrollControlRecords = "Pages.PayrollManagement.PayrollControlRecords";
    public const string Pages_PayrollManagement_PayrollControlRecords_Create = "Pages.PayrollManagement.PayrollControlRecords.Create";
    public const string Pages_PayrollManagement_PayrollControlRecords_Edit = "Pages.PayrollManagement.PayrollControlRecords.Edit";
    public const string Pages_PayrollManagement_PayrollControlRecords_Delete = "Pages.PayrollManagement.PayrollControlRecords.Delete";

    public const string Pages_PayrollManagement_IndemnityPayRules = "Pages.PayrollManagement.IndemnityPayRules";
    public const string Pages_PayrollManagement_IndemnityPayRules_Create = "Pages.PayrollManagement.IndemnityPayRules.Create";
    public const string Pages_PayrollManagement_IndemnityPayRules_Edit = "Pages.PayrollManagement.IndemnityPayRules.Edit";
    public const string Pages_PayrollManagement_IndemnityPayRules_Delete = "Pages.PayrollManagement.IndemnityPayRules.Delete";

    public const string Pages_PayrollManagement_IndemnityPayRuleDetails = "Pages.PayrollManagement.IndemnityPayRuleDetails";
    public const string Pages_PayrollManagement_IndemnityPayRuleDetails_Create = "Pages.PayrollManagement.IndemnityPayRuleDetails.Create";
    public const string Pages_PayrollManagement_IndemnityPayRuleDetails_Edit = "Pages.PayrollManagement.IndemnityPayRuleDetails.Edit";
    public const string Pages_PayrollManagement_IndemnityPayRuleDetails_Delete = "Pages.PayrollManagement.IndemnityPayRuleDetails.Delete";

    public const string Pages_PayrollManagement_OrgUnitWageAccGroups = "Pages.PayrollManagement.OrgUnitWageAccGroups";
    public const string Pages_PayrollManagement_OrgUnitWageAccGroups_Create = "Pages.PayrollManagement.OrgUnitWageAccGroups.Create";
    public const string Pages_PayrollManagement_OrgUnitWageAccGroups_Edit = "Pages.PayrollManagement.OrgUnitWageAccGroups.Edit";
    public const string Pages_PayrollManagement_OrgUnitWageAccGroups_Delete = "Pages.PayrollManagement.OrgUnitWageAccGroups.Delete";

    public const string Pages_PayrollManagement_WageAccGroups = "Pages.PayrollManagement.WageAccGroups";
    public const string Pages_PayrollManagement_WageAccGroups_Create = "Pages.PayrollManagement.WageAccGroups.Create";
    public const string Pages_PayrollManagement_WageAccGroups_Edit = "Pages.PayrollManagement.WageAccGroups.Edit";
    public const string Pages_PayrollManagement_WageAccGroups_Delete = "Pages.PayrollManagement.WageAccGroups.Delete";

    public const string Pages_PayrollManagement_WageAccGroupDetails = "Pages.PayrollManagement.WageAccGroupDetails";
    public const string Pages_PayrollManagement_WageAccGroupDetails_Create = "Pages.PayrollManagement.WageAccGroupDetails.Create";
    public const string Pages_PayrollManagement_WageAccGroupDetails_Edit = "Pages.PayrollManagement.WageAccGroupDetails.Edit";
    public const string Pages_PayrollManagement_WageAccGroupDetails_Delete = "Pages.PayrollManagement.WageAccGroupDetails.Delete";

    public const string Pages_PayrollManagement_WageTypes = "Pages.PayrollManagement.WageTypes";
    public const string Pages_PayrollManagement_WageTypes_Create = "Pages.PayrollManagement.WageTypes.Create";
    public const string Pages_PayrollManagement_WageTypes_Edit = "Pages.PayrollManagement.WageTypes.Edit";
    public const string Pages_PayrollManagement_WageTypes_Delete = "Pages.PayrollManagement.WageTypes.Delete";

    public const string Pages_PayrollManagement_WageTypeExceptions = "Pages.PayrollManagement.WageTypeExceptions";
    public const string Pages_PayrollManagement_WageTypeExceptions_Create = "Pages.PayrollManagement.WageTypeExceptions.Create";
    public const string Pages_PayrollManagement_WageTypeExceptions_Edit = "Pages.PayrollManagement.WageTypeExceptions.Edit";
    public const string Pages_PayrollManagement_WageTypeExceptions_Delete = "Pages.PayrollManagement.WageTypeExceptions.Delete";

    // Wave P1b (Payroll Engine execution). Legacy gated this behind the uscPayrollEngine screen's own screen
    // permission; there is no CRUD here -- the only action is "run".
    public const string Pages_PayrollManagement_PayrollEngine = "Pages.PayrollManagement.PayrollEngine";
    public const string Pages_PayrollManagement_PayrollEngine_Run = "Pages.PayrollManagement.PayrollEngine.Run";

    public const string Pages_PayrollManagement_WageTypeLeaves = "Pages.PayrollManagement.WageTypeLeaves";
    public const string Pages_PayrollManagement_WageTypeLeaves_Create = "Pages.PayrollManagement.WageTypeLeaves.Create";
    public const string Pages_PayrollManagement_WageTypeLeaves_Edit = "Pages.PayrollManagement.WageTypeLeaves.Edit";
    public const string Pages_PayrollManagement_WageTypeLeaves_Delete = "Pages.PayrollManagement.WageTypeLeaves.Delete";

    public const string Pages_PayrollManagement_WageTypeSecurityRoles = "Pages.PayrollManagement.WageTypeSecurityRoles";
    public const string Pages_PayrollManagement_WageTypeSecurityRoles_Create = "Pages.PayrollManagement.WageTypeSecurityRoles.Create";
    public const string Pages_PayrollManagement_WageTypeSecurityRoles_Edit = "Pages.PayrollManagement.WageTypeSecurityRoles.Edit";
    public const string Pages_PayrollManagement_WageTypeSecurityRoles_Delete = "Pages.PayrollManagement.WageTypeSecurityRoles.Delete";

    public const string Pages_PayrollManagement_WageAccounts = "Pages.PayrollManagement.WageAccounts";
    public const string Pages_PayrollManagement_WageAccounts_Create = "Pages.PayrollManagement.WageAccounts.Create";
    public const string Pages_PayrollManagement_WageAccounts_Edit = "Pages.PayrollManagement.WageAccounts.Edit";
    public const string Pages_PayrollManagement_WageAccounts_Delete = "Pages.PayrollManagement.WageAccounts.Delete";

    public const string Pages_PayrollManagement_SocialSecurities = "Pages.PayrollManagement.SocialSecurities";
    public const string Pages_PayrollManagement_SocialSecurities_Create = "Pages.PayrollManagement.SocialSecurities.Create";
    public const string Pages_PayrollManagement_SocialSecurities_Edit = "Pages.PayrollManagement.SocialSecurities.Edit";
    public const string Pages_PayrollManagement_SocialSecurities_Delete = "Pages.PayrollManagement.SocialSecurities.Delete";

    public const string Pages_PayrollManagement_SocialSecurityDetails = "Pages.PayrollManagement.SocialSecurityDetails";
    public const string Pages_PayrollManagement_SocialSecurityDetails_Create = "Pages.PayrollManagement.SocialSecurityDetails.Create";
    public const string Pages_PayrollManagement_SocialSecurityDetails_Edit = "Pages.PayrollManagement.SocialSecurityDetails.Edit";
    public const string Pages_PayrollManagement_SocialSecurityDetails_Delete = "Pages.PayrollManagement.SocialSecurityDetails.Delete";

    public const string Pages_PayrollManagement_SocialSecurityMaritalAllowances = "Pages.PayrollManagement.SocialSecurityMaritalAllowances";
    public const string Pages_PayrollManagement_SocialSecurityMaritalAllowances_Create = "Pages.PayrollManagement.SocialSecurityMaritalAllowances.Create";
    public const string Pages_PayrollManagement_SocialSecurityMaritalAllowances_Edit = "Pages.PayrollManagement.SocialSecurityMaritalAllowances.Edit";
    public const string Pages_PayrollManagement_SocialSecurityMaritalAllowances_Delete = "Pages.PayrollManagement.SocialSecurityMaritalAllowances.Delete";

    public const string Pages_PayrollManagement_SocialSecurityEducationAllowances = "Pages.PayrollManagement.SocialSecurityEducationAllowances";
    public const string Pages_PayrollManagement_SocialSecurityEducationAllowances_Create = "Pages.PayrollManagement.SocialSecurityEducationAllowances.Create";
    public const string Pages_PayrollManagement_SocialSecurityEducationAllowances_Edit = "Pages.PayrollManagement.SocialSecurityEducationAllowances.Edit";
    public const string Pages_PayrollManagement_SocialSecurityEducationAllowances_Delete = "Pages.PayrollManagement.SocialSecurityEducationAllowances.Delete";

    public const string Pages_PayrollManagement_SocialSecurityRules = "Pages.PayrollManagement.SocialSecurityRules";
    public const string Pages_PayrollManagement_SocialSecurityRules_Create = "Pages.PayrollManagement.SocialSecurityRules.Create";
    public const string Pages_PayrollManagement_SocialSecurityRules_Edit = "Pages.PayrollManagement.SocialSecurityRules.Edit";
    public const string Pages_PayrollManagement_SocialSecurityRules_Delete = "Pages.PayrollManagement.SocialSecurityRules.Delete";

    public const string Pages_PayrollManagement_TicketClasses = "Pages.PayrollManagement.TicketClasses";
    public const string Pages_PayrollManagement_TicketClasses_Create = "Pages.PayrollManagement.TicketClasses.Create";
    public const string Pages_PayrollManagement_TicketClasses_Edit = "Pages.PayrollManagement.TicketClasses.Edit";
    public const string Pages_PayrollManagement_TicketClasses_Delete = "Pages.PayrollManagement.TicketClasses.Delete";

    public const string Pages_PayrollManagement_TicketPaxTypes = "Pages.PayrollManagement.TicketPaxTypes";
    public const string Pages_PayrollManagement_TicketPaxTypes_Create = "Pages.PayrollManagement.TicketPaxTypes.Create";
    public const string Pages_PayrollManagement_TicketPaxTypes_Edit = "Pages.PayrollManagement.TicketPaxTypes.Edit";
    public const string Pages_PayrollManagement_TicketPaxTypes_Delete = "Pages.PayrollManagement.TicketPaxTypes.Delete";

    public const string Pages_PayrollManagement_TicketRoutes = "Pages.PayrollManagement.TicketRoutes";
    public const string Pages_PayrollManagement_TicketRoutes_Create = "Pages.PayrollManagement.TicketRoutes.Create";
    public const string Pages_PayrollManagement_TicketRoutes_Edit = "Pages.PayrollManagement.TicketRoutes.Edit";
    public const string Pages_PayrollManagement_TicketRoutes_Delete = "Pages.PayrollManagement.TicketRoutes.Delete";

    public const string Pages_PayrollManagement_TicketFares = "Pages.PayrollManagement.TicketFares";
    public const string Pages_PayrollManagement_TicketFares_Create = "Pages.PayrollManagement.TicketFares.Create";
    public const string Pages_PayrollManagement_TicketFares_Edit = "Pages.PayrollManagement.TicketFares.Edit";
    public const string Pages_PayrollManagement_TicketFares_Delete = "Pages.PayrollManagement.TicketFares.Delete";

    public const string Pages_PayrollManagement_TicketTypes = "Pages.PayrollManagement.TicketTypes";
    public const string Pages_PayrollManagement_TicketTypes_Create = "Pages.PayrollManagement.TicketTypes.Create";
    public const string Pages_PayrollManagement_TicketTypes_Edit = "Pages.PayrollManagement.TicketTypes.Edit";
    public const string Pages_PayrollManagement_TicketTypes_Delete = "Pages.PayrollManagement.TicketTypes.Delete";

    public const string Pages_PayrollManagement_EmployeeWageAccGroups = "Pages.PayrollManagement.EmployeeWageAccGroups";
    public const string Pages_PayrollManagement_EmployeeWageAccGroups_Create = "Pages.PayrollManagement.EmployeeWageAccGroups.Create";
    public const string Pages_PayrollManagement_EmployeeWageAccGroups_Edit = "Pages.PayrollManagement.EmployeeWageAccGroups.Edit";
    public const string Pages_PayrollManagement_EmployeeWageAccGroups_Delete = "Pages.PayrollManagement.EmployeeWageAccGroups.Delete";

    public const string Pages_PayrollManagement_EmployeeWageAccounts = "Pages.PayrollManagement.EmployeeWageAccounts";
    public const string Pages_PayrollManagement_EmployeeWageAccounts_Create = "Pages.PayrollManagement.EmployeeWageAccounts.Create";
    public const string Pages_PayrollManagement_EmployeeWageAccounts_Edit = "Pages.PayrollManagement.EmployeeWageAccounts.Edit";
    public const string Pages_PayrollManagement_EmployeeWageAccounts_Delete = "Pages.PayrollManagement.EmployeeWageAccounts.Delete";

    public const string Pages_PayrollManagement_EmployeePayrollStatuses = "Pages.PayrollManagement.EmployeePayrollStatuses";
    public const string Pages_PayrollManagement_EmployeePayrollStatuses_Create = "Pages.PayrollManagement.EmployeePayrollStatuses.Create";
    public const string Pages_PayrollManagement_EmployeePayrollStatuses_Edit = "Pages.PayrollManagement.EmployeePayrollStatuses.Edit";
    public const string Pages_PayrollManagement_EmployeePayrollStatuses_Delete = "Pages.PayrollManagement.EmployeePayrollStatuses.Delete";

    public const string Pages_PayrollManagement_AbsencePayments = "Pages.PayrollManagement.AbsencePayments";
    public const string Pages_PayrollManagement_AbsencePayments_Create = "Pages.PayrollManagement.AbsencePayments.Create";
    public const string Pages_PayrollManagement_AbsencePayments_Edit = "Pages.PayrollManagement.AbsencePayments.Edit";
    public const string Pages_PayrollManagement_AbsencePayments_Delete = "Pages.PayrollManagement.AbsencePayments.Delete";

    public const string Pages_PayrollManagement_TempBenefitTypes = "Pages.PayrollManagement.TempBenefitTypes";
    public const string Pages_PayrollManagement_TempBenefitTypes_Create = "Pages.PayrollManagement.TempBenefitTypes.Create";
    public const string Pages_PayrollManagement_TempBenefitTypes_Edit = "Pages.PayrollManagement.TempBenefitTypes.Edit";
    public const string Pages_PayrollManagement_TempBenefitTypes_Delete = "Pages.PayrollManagement.TempBenefitTypes.Delete";

    public const string Pages_PayrollManagement_TempEmployeeBenefits = "Pages.PayrollManagement.TempEmployeeBenefits";
    public const string Pages_PayrollManagement_TempEmployeeBenefits_Create = "Pages.PayrollManagement.TempEmployeeBenefits.Create";
    public const string Pages_PayrollManagement_TempEmployeeBenefits_Edit = "Pages.PayrollManagement.TempEmployeeBenefits.Edit";
    public const string Pages_PayrollManagement_TempEmployeeBenefits_Delete = "Pages.PayrollManagement.TempEmployeeBenefits.Delete";

    public const string Pages_PayrollManagement_WageTypeRegisters = "Pages.PayrollManagement.WageTypeRegisters";
    public const string Pages_PayrollManagement_WageTypeRegisters_Create = "Pages.PayrollManagement.WageTypeRegisters.Create";
    public const string Pages_PayrollManagement_WageTypeRegisters_Edit = "Pages.PayrollManagement.WageTypeRegisters.Edit";
    public const string Pages_PayrollManagement_WageTypeRegisters_Delete = "Pages.PayrollManagement.WageTypeRegisters.Delete";

    public const string Pages_PayrollManagement_PayrollLogs = "Pages.PayrollManagement.PayrollLogs";
    public const string Pages_PayrollManagement_PayrollLogs_Create = "Pages.PayrollManagement.PayrollLogs.Create";
    public const string Pages_PayrollManagement_PayrollLogs_Edit = "Pages.PayrollManagement.PayrollLogs.Edit";
    public const string Pages_PayrollManagement_PayrollLogs_Delete = "Pages.PayrollManagement.PayrollLogs.Delete";

    public const string Pages_PayrollManagement_CostRegisters = "Pages.PayrollManagement.CostRegisters";
    public const string Pages_PayrollManagement_CostRegisters_Create = "Pages.PayrollManagement.CostRegisters.Create";
    public const string Pages_PayrollManagement_CostRegisters_Edit = "Pages.PayrollManagement.CostRegisters.Edit";
    public const string Pages_PayrollManagement_CostRegisters_Delete = "Pages.PayrollManagement.CostRegisters.Delete";

    public const string Pages_PayrollManagement_CostRegisterDetails = "Pages.PayrollManagement.CostRegisterDetails";
    public const string Pages_PayrollManagement_CostRegisterDetails_Create = "Pages.PayrollManagement.CostRegisterDetails.Create";
    public const string Pages_PayrollManagement_CostRegisterDetails_Edit = "Pages.PayrollManagement.CostRegisterDetails.Edit";
    public const string Pages_PayrollManagement_CostRegisterDetails_Delete = "Pages.PayrollManagement.CostRegisterDetails.Delete";

    public const string Pages_PayrollManagement_BankFiles = "Pages.PayrollManagement.BankFiles";
    public const string Pages_PayrollManagement_BankFiles_Generate = "Pages.PayrollManagement.BankFiles.Generate";

    public const string Pages_PayrollManagement_SalaryRegisters = "Pages.PayrollManagement.SalaryRegisters";
    public const string Pages_PayrollManagement_SalaryRegisters_Create = "Pages.PayrollManagement.SalaryRegisters.Create";
    public const string Pages_PayrollManagement_SalaryRegisters_Edit = "Pages.PayrollManagement.SalaryRegisters.Edit";
    public const string Pages_PayrollManagement_SalaryRegisters_Delete = "Pages.PayrollManagement.SalaryRegisters.Delete";

    public const string Pages_PayrollManagement_SalaryRegisterDetails = "Pages.PayrollManagement.SalaryRegisterDetails";
    public const string Pages_PayrollManagement_SalaryRegisterDetails_Create = "Pages.PayrollManagement.SalaryRegisterDetails.Create";
    public const string Pages_PayrollManagement_SalaryRegisterDetails_Edit = "Pages.PayrollManagement.SalaryRegisterDetails.Edit";
    public const string Pages_PayrollManagement_SalaryRegisterDetails_Delete = "Pages.PayrollManagement.SalaryRegisterDetails.Delete";

    public const string Pages_PayrollManagement_SalaryRegisterDaysDetails = "Pages.PayrollManagement.SalaryRegisterDaysDetails";
    public const string Pages_PayrollManagement_SalaryRegisterDaysDetails_Create = "Pages.PayrollManagement.SalaryRegisterDaysDetails.Create";
    public const string Pages_PayrollManagement_SalaryRegisterDaysDetails_Edit = "Pages.PayrollManagement.SalaryRegisterDaysDetails.Edit";
    public const string Pages_PayrollManagement_SalaryRegisterDaysDetails_Delete = "Pages.PayrollManagement.SalaryRegisterDaysDetails.Delete";

    public const string Pages_PayrollManagement_SimulationSalaryRegisters = "Pages.PayrollManagement.SimulationSalaryRegisters";
    public const string Pages_PayrollManagement_SimulationSalaryRegisters_Create = "Pages.PayrollManagement.SimulationSalaryRegisters.Create";
    public const string Pages_PayrollManagement_SimulationSalaryRegisters_Edit = "Pages.PayrollManagement.SimulationSalaryRegisters.Edit";
    public const string Pages_PayrollManagement_SimulationSalaryRegisters_Delete = "Pages.PayrollManagement.SimulationSalaryRegisters.Delete";

    public const string Pages_PayrollManagement_SimulationSalaryRegisterDetails = "Pages.PayrollManagement.SimulationSalaryRegisterDetails";
    public const string Pages_PayrollManagement_SimulationSalaryRegisterDetails_Create = "Pages.PayrollManagement.SimulationSalaryRegisterDetails.Create";
    public const string Pages_PayrollManagement_SimulationSalaryRegisterDetails_Edit = "Pages.PayrollManagement.SimulationSalaryRegisterDetails.Edit";
    public const string Pages_PayrollManagement_SimulationSalaryRegisterDetails_Delete = "Pages.PayrollManagement.SimulationSalaryRegisterDetails.Delete";

    public const string Pages_PayrollManagement_SimulationSalaryRegisterDaysDetails = "Pages.PayrollManagement.SimulationSalaryRegisterDaysDetails";
    public const string Pages_PayrollManagement_SimulationSalaryRegisterDaysDetails_Create = "Pages.PayrollManagement.SimulationSalaryRegisterDaysDetails.Create";
    public const string Pages_PayrollManagement_SimulationSalaryRegisterDaysDetails_Edit = "Pages.PayrollManagement.SimulationSalaryRegisterDaysDetails.Edit";
    public const string Pages_PayrollManagement_SimulationSalaryRegisterDaysDetails_Delete = "Pages.PayrollManagement.SimulationSalaryRegisterDaysDetails.Delete";

    public const string Pages_PayrollManagement_AbsenceViolationActionRecords = "Pages.PayrollManagement.AbsenceViolationActionRecords";
    public const string Pages_PayrollManagement_AbsenceViolationActionRecords_Create = "Pages.PayrollManagement.AbsenceViolationActionRecords.Create";
    public const string Pages_PayrollManagement_AbsenceViolationActionRecords_Edit = "Pages.PayrollManagement.AbsenceViolationActionRecords.Edit";
    public const string Pages_PayrollManagement_AbsenceViolationActionRecords_Delete = "Pages.PayrollManagement.AbsenceViolationActionRecords.Delete";

    public const string Pages_ProjectManagement = "Pages.ProjectManagement";

    public const string Pages_ProjectManagement_ProjectTypes = "Pages.ProjectManagement.ProjectTypes";
    public const string Pages_ProjectManagement_ProjectTypes_Create = "Pages.ProjectManagement.ProjectTypes.Create";
    public const string Pages_ProjectManagement_ProjectTypes_Edit = "Pages.ProjectManagement.ProjectTypes.Edit";
    public const string Pages_ProjectManagement_ProjectTypes_Delete = "Pages.ProjectManagement.ProjectTypes.Delete";

    public const string Pages_ProjectManagement_Projects = "Pages.ProjectManagement.Projects";
    public const string Pages_ProjectManagement_Projects_Create = "Pages.ProjectManagement.Projects.Create";
    public const string Pages_ProjectManagement_Projects_Edit = "Pages.ProjectManagement.Projects.Edit";
    public const string Pages_ProjectManagement_Projects_Delete = "Pages.ProjectManagement.Projects.Delete";

    public const string Pages_ProjectManagement_ProjectWageTypes = "Pages.ProjectManagement.ProjectWageTypes";
    public const string Pages_ProjectManagement_ProjectWageTypes_Create = "Pages.ProjectManagement.ProjectWageTypes.Create";
    public const string Pages_ProjectManagement_ProjectWageTypes_Edit = "Pages.ProjectManagement.ProjectWageTypes.Edit";
    public const string Pages_ProjectManagement_ProjectWageTypes_Delete = "Pages.ProjectManagement.ProjectWageTypes.Delete";

    public const string Pages_ProjectManagement_EmployeeProjects = "Pages.ProjectManagement.EmployeeProjects";
    public const string Pages_ProjectManagement_EmployeeProjects_Create = "Pages.ProjectManagement.EmployeeProjects.Create";
    public const string Pages_ProjectManagement_EmployeeProjects_Edit = "Pages.ProjectManagement.EmployeeProjects.Edit";
    public const string Pages_ProjectManagement_EmployeeProjects_Delete = "Pages.ProjectManagement.EmployeeProjects.Delete";

    public const string Pages_ProjectManagement_EmployeeProjectDetails = "Pages.ProjectManagement.EmployeeProjectDetails";
    public const string Pages_ProjectManagement_EmployeeProjectDetails_Create = "Pages.ProjectManagement.EmployeeProjectDetails.Create";
    public const string Pages_ProjectManagement_EmployeeProjectDetails_Edit = "Pages.ProjectManagement.EmployeeProjectDetails.Edit";
    public const string Pages_ProjectManagement_EmployeeProjectDetails_Delete = "Pages.ProjectManagement.EmployeeProjectDetails.Delete";

    public const string Pages_ProjectManagement_ProjectRegisters = "Pages.ProjectManagement.ProjectRegisters";
    public const string Pages_ProjectManagement_ProjectRegisters_Create = "Pages.ProjectManagement.ProjectRegisters.Create";
    public const string Pages_ProjectManagement_ProjectRegisters_Edit = "Pages.ProjectManagement.ProjectRegisters.Edit";
    public const string Pages_ProjectManagement_ProjectRegisters_Delete = "Pages.ProjectManagement.ProjectRegisters.Delete";

    public const string Pages_ProjectManagement_ProjectRegisterDetails = "Pages.ProjectManagement.ProjectRegisterDetails";
    public const string Pages_ProjectManagement_ProjectRegisterDetails_Create = "Pages.ProjectManagement.ProjectRegisterDetails.Create";
    public const string Pages_ProjectManagement_ProjectRegisterDetails_Edit = "Pages.ProjectManagement.ProjectRegisterDetails.Edit";
    public const string Pages_ProjectManagement_ProjectRegisterDetails_Delete = "Pages.ProjectManagement.ProjectRegisterDetails.Delete";

    public const string Pages_ProjectManagement_ProjectLogs = "Pages.ProjectManagement.ProjectLogs";
    public const string Pages_ProjectManagement_ProjectLogs_Create = "Pages.ProjectManagement.ProjectLogs.Create";
    public const string Pages_ProjectManagement_ProjectLogs_Edit = "Pages.ProjectManagement.ProjectLogs.Edit";
    public const string Pages_ProjectManagement_ProjectLogs_Delete = "Pages.ProjectManagement.ProjectLogs.Delete";

    // Project Cost Engine execution. Legacy gated this behind the uscProjectCosting screen's own screen
    // permission; here it is a first-class permission pair (view status / run).
    public const string Pages_ProjectManagement_ProjectCostEngine = "Pages.ProjectManagement.ProjectCostEngine";
    public const string Pages_ProjectManagement_ProjectCostEngine_Run = "Pages.ProjectManagement.ProjectCostEngine.Run";

    public const string Pages_TravelManagement = "Pages.TravelManagement";

    public const string Pages_TravelManagement_ExpenseTypes = "Pages.TravelManagement.ExpenseTypes";
    public const string Pages_TravelManagement_ExpenseTypes_Create = "Pages.TravelManagement.ExpenseTypes.Create";
    public const string Pages_TravelManagement_ExpenseTypes_Edit = "Pages.TravelManagement.ExpenseTypes.Edit";
    public const string Pages_TravelManagement_ExpenseTypes_Delete = "Pages.TravelManagement.ExpenseTypes.Delete";

    public const string Pages_TravelManagement_Hotels = "Pages.TravelManagement.Hotels";
    public const string Pages_TravelManagement_Hotels_Create = "Pages.TravelManagement.Hotels.Create";
    public const string Pages_TravelManagement_Hotels_Edit = "Pages.TravelManagement.Hotels.Edit";
    public const string Pages_TravelManagement_Hotels_Delete = "Pages.TravelManagement.Hotels.Delete";

    public const string Pages_TravelManagement_TravelAirlines = "Pages.TravelManagement.TravelAirlines";
    public const string Pages_TravelManagement_TravelAirlines_Create = "Pages.TravelManagement.TravelAirlines.Create";
    public const string Pages_TravelManagement_TravelAirlines_Edit = "Pages.TravelManagement.TravelAirlines.Edit";
    public const string Pages_TravelManagement_TravelAirlines_Delete = "Pages.TravelManagement.TravelAirlines.Delete";

    public const string Pages_TravelManagement_TravelCancelReasons = "Pages.TravelManagement.TravelCancelReasons";
    public const string Pages_TravelManagement_TravelCancelReasons_Create = "Pages.TravelManagement.TravelCancelReasons.Create";
    public const string Pages_TravelManagement_TravelCancelReasons_Edit = "Pages.TravelManagement.TravelCancelReasons.Edit";
    public const string Pages_TravelManagement_TravelCancelReasons_Delete = "Pages.TravelManagement.TravelCancelReasons.Delete";

    public const string Pages_TravelManagement_TravelDestinations = "Pages.TravelManagement.TravelDestinations";
    public const string Pages_TravelManagement_TravelDestinations_Create = "Pages.TravelManagement.TravelDestinations.Create";
    public const string Pages_TravelManagement_TravelDestinations_Edit = "Pages.TravelManagement.TravelDestinations.Edit";
    public const string Pages_TravelManagement_TravelDestinations_Delete = "Pages.TravelManagement.TravelDestinations.Delete";

    public const string Pages_TravelManagement_TravelTypes = "Pages.TravelManagement.TravelTypes";
    public const string Pages_TravelManagement_TravelTypes_Create = "Pages.TravelManagement.TravelTypes.Create";
    public const string Pages_TravelManagement_TravelTypes_Edit = "Pages.TravelManagement.TravelTypes.Edit";
    public const string Pages_TravelManagement_TravelTypes_Delete = "Pages.TravelManagement.TravelTypes.Delete";

    public const string Pages_TravelManagement_TravelPerDiems = "Pages.TravelManagement.TravelPerDiems";
    public const string Pages_TravelManagement_TravelPerDiems_Create = "Pages.TravelManagement.TravelPerDiems.Create";
    public const string Pages_TravelManagement_TravelPerDiems_Edit = "Pages.TravelManagement.TravelPerDiems.Edit";
    public const string Pages_TravelManagement_TravelPerDiems_Delete = "Pages.TravelManagement.TravelPerDiems.Delete";

    public const string Pages_TravelManagement_Travels = "Pages.TravelManagement.Travels";
    public const string Pages_TravelManagement_Travels_Create = "Pages.TravelManagement.Travels.Create";
    public const string Pages_TravelManagement_Travels_Edit = "Pages.TravelManagement.Travels.Edit";
    public const string Pages_TravelManagement_Travels_Delete = "Pages.TravelManagement.Travels.Delete";

    public const string Pages_TravelManagement_TravelDetails = "Pages.TravelManagement.TravelDetails";
    public const string Pages_TravelManagement_TravelDetails_Create = "Pages.TravelManagement.TravelDetails.Create";
    public const string Pages_TravelManagement_TravelDetails_Edit = "Pages.TravelManagement.TravelDetails.Edit";
    public const string Pages_TravelManagement_TravelDetails_Delete = "Pages.TravelManagement.TravelDetails.Delete";

    public const string Pages_TravelManagement_TravelDetailCosts = "Pages.TravelManagement.TravelDetailCosts";
    public const string Pages_TravelManagement_TravelDetailCosts_Create = "Pages.TravelManagement.TravelDetailCosts.Create";
    public const string Pages_TravelManagement_TravelDetailCosts_Edit = "Pages.TravelManagement.TravelDetailCosts.Edit";
    public const string Pages_TravelManagement_TravelDetailCosts_Delete = "Pages.TravelManagement.TravelDetailCosts.Delete";

    public const string Pages_TravelManagement_TravelFeedbacks = "Pages.TravelManagement.TravelFeedbacks";
    public const string Pages_TravelManagement_TravelFeedbacks_Create = "Pages.TravelManagement.TravelFeedbacks.Create";
    public const string Pages_TravelManagement_TravelFeedbacks_Edit = "Pages.TravelManagement.TravelFeedbacks.Edit";
    public const string Pages_TravelManagement_TravelFeedbacks_Delete = "Pages.TravelManagement.TravelFeedbacks.Delete";

    public const string Pages_PayrollManagement_AbsenceViolationActionLogs = "Pages.PayrollManagement.AbsenceViolationActionLogs";
    public const string Pages_PayrollManagement_AbsenceViolationActionLogs_Create = "Pages.PayrollManagement.AbsenceViolationActionLogs.Create";
    public const string Pages_PayrollManagement_AbsenceViolationActionLogs_Edit = "Pages.PayrollManagement.AbsenceViolationActionLogs.Edit";
    public const string Pages_PayrollManagement_AbsenceViolationActionLogs_Delete = "Pages.PayrollManagement.AbsenceViolationActionLogs.Delete";

    public const string Pages_PayrollManagement_GratuityAccrualSummaries = "Pages.PayrollManagement.GratuityAccrualSummaries";
    public const string Pages_PayrollManagement_GratuityAccrualSummaries_Create = "Pages.PayrollManagement.GratuityAccrualSummaries.Create";
    public const string Pages_PayrollManagement_GratuityAccrualSummaries_Edit = "Pages.PayrollManagement.GratuityAccrualSummaries.Edit";
    public const string Pages_PayrollManagement_GratuityAccrualSummaries_Delete = "Pages.PayrollManagement.GratuityAccrualSummaries.Delete";

    public const string Pages_PayrollManagement_LeaveAccrualSummaries = "Pages.PayrollManagement.LeaveAccrualSummaries";
    public const string Pages_PayrollManagement_LeaveAccrualSummaries_Create = "Pages.PayrollManagement.LeaveAccrualSummaries.Create";
    public const string Pages_PayrollManagement_LeaveAccrualSummaries_Edit = "Pages.PayrollManagement.LeaveAccrualSummaries.Edit";
    public const string Pages_PayrollManagement_LeaveAccrualSummaries_Delete = "Pages.PayrollManagement.LeaveAccrualSummaries.Delete";

    public const string Pages_PayrollManagement_TicketAccrualSummaries = "Pages.PayrollManagement.TicketAccrualSummaries";
    public const string Pages_PayrollManagement_TicketAccrualSummaries_Create = "Pages.PayrollManagement.TicketAccrualSummaries.Create";
    public const string Pages_PayrollManagement_TicketAccrualSummaries_Edit = "Pages.PayrollManagement.TicketAccrualSummaries.Edit";
    public const string Pages_PayrollManagement_TicketAccrualSummaries_Delete = "Pages.PayrollManagement.TicketAccrualSummaries.Delete";

    public const string Pages_GovtRelationsManagement = "Pages.GovtRelationsManagement";

    public const string Pages_GovtRelationsManagement_BusinessTypes = "Pages.GovtRelationsManagement.BusinessTypes";
    public const string Pages_GovtRelationsManagement_BusinessTypes_Create = "Pages.GovtRelationsManagement.BusinessTypes.Create";
    public const string Pages_GovtRelationsManagement_BusinessTypes_Edit = "Pages.GovtRelationsManagement.BusinessTypes.Edit";
    public const string Pages_GovtRelationsManagement_BusinessTypes_Delete = "Pages.GovtRelationsManagement.BusinessTypes.Delete";

    public const string Pages_GovtRelationsManagement_GovtJobCategories = "Pages.GovtRelationsManagement.GovtJobCategories";
    public const string Pages_GovtRelationsManagement_GovtJobCategories_Create = "Pages.GovtRelationsManagement.GovtJobCategories.Create";
    public const string Pages_GovtRelationsManagement_GovtJobCategories_Edit = "Pages.GovtRelationsManagement.GovtJobCategories.Edit";
    public const string Pages_GovtRelationsManagement_GovtJobCategories_Delete = "Pages.GovtRelationsManagement.GovtJobCategories.Delete";

    public const string Pages_GovtRelationsManagement_GovtJobTitles = "Pages.GovtRelationsManagement.GovtJobTitles";
    public const string Pages_GovtRelationsManagement_GovtJobTitles_Create = "Pages.GovtRelationsManagement.GovtJobTitles.Create";
    public const string Pages_GovtRelationsManagement_GovtJobTitles_Edit = "Pages.GovtRelationsManagement.GovtJobTitles.Edit";
    public const string Pages_GovtRelationsManagement_GovtJobTitles_Delete = "Pages.GovtRelationsManagement.GovtJobTitles.Delete";

    public const string Pages_GovtRelationsManagement_Signatories = "Pages.GovtRelationsManagement.Signatories";
    public const string Pages_GovtRelationsManagement_Signatories_Create = "Pages.GovtRelationsManagement.Signatories.Create";
    public const string Pages_GovtRelationsManagement_Signatories_Edit = "Pages.GovtRelationsManagement.Signatories.Edit";
    public const string Pages_GovtRelationsManagement_Signatories_Delete = "Pages.GovtRelationsManagement.Signatories.Delete";

    public const string Pages_GovtRelationsManagement_Sponsors = "Pages.GovtRelationsManagement.Sponsors";
    public const string Pages_GovtRelationsManagement_Sponsors_Create = "Pages.GovtRelationsManagement.Sponsors.Create";
    public const string Pages_GovtRelationsManagement_Sponsors_Edit = "Pages.GovtRelationsManagement.Sponsors.Edit";
    public const string Pages_GovtRelationsManagement_Sponsors_Delete = "Pages.GovtRelationsManagement.Sponsors.Delete";

    public const string Pages_GovtRelationsManagement_SponsorJobTitles = "Pages.GovtRelationsManagement.SponsorJobTitles";
    public const string Pages_GovtRelationsManagement_SponsorJobTitles_Create = "Pages.GovtRelationsManagement.SponsorJobTitles.Create";
    public const string Pages_GovtRelationsManagement_SponsorJobTitles_Edit = "Pages.GovtRelationsManagement.SponsorJobTitles.Edit";
    public const string Pages_GovtRelationsManagement_SponsorJobTitles_Delete = "Pages.GovtRelationsManagement.SponsorJobTitles.Delete";

    public const string Pages_GovtRelationsManagement_SponsorSignatories = "Pages.GovtRelationsManagement.SponsorSignatories";
    public const string Pages_GovtRelationsManagement_SponsorSignatories_Create = "Pages.GovtRelationsManagement.SponsorSignatories.Create";
    public const string Pages_GovtRelationsManagement_SponsorSignatories_Edit = "Pages.GovtRelationsManagement.SponsorSignatories.Edit";
    public const string Pages_GovtRelationsManagement_SponsorSignatories_Delete = "Pages.GovtRelationsManagement.SponsorSignatories.Delete";

    public const string Pages_GovtRelationsManagement_SponsorGovtRepresentatives = "Pages.GovtRelationsManagement.SponsorGovtRepresentatives";
    public const string Pages_GovtRelationsManagement_SponsorGovtRepresentatives_Create = "Pages.GovtRelationsManagement.SponsorGovtRepresentatives.Create";
    public const string Pages_GovtRelationsManagement_SponsorGovtRepresentatives_Edit = "Pages.GovtRelationsManagement.SponsorGovtRepresentatives.Edit";
    public const string Pages_GovtRelationsManagement_SponsorGovtRepresentatives_Delete = "Pages.GovtRelationsManagement.SponsorGovtRepresentatives.Delete";

    public const string Pages_GovtRelationsManagement_SponsorLicenseInfos = "Pages.GovtRelationsManagement.SponsorLicenseInfos";
    public const string Pages_GovtRelationsManagement_SponsorLicenseInfos_Create = "Pages.GovtRelationsManagement.SponsorLicenseInfos.Create";
    public const string Pages_GovtRelationsManagement_SponsorLicenseInfos_Edit = "Pages.GovtRelationsManagement.SponsorLicenseInfos.Edit";
    public const string Pages_GovtRelationsManagement_SponsorLicenseInfos_Delete = "Pages.GovtRelationsManagement.SponsorLicenseInfos.Delete";

    public const string Pages_GovtRelationsManagement_SponsorBanks = "Pages.GovtRelationsManagement.SponsorBanks";
    public const string Pages_GovtRelationsManagement_SponsorBanks_Create = "Pages.GovtRelationsManagement.SponsorBanks.Create";
    public const string Pages_GovtRelationsManagement_SponsorBanks_Edit = "Pages.GovtRelationsManagement.SponsorBanks.Edit";
    public const string Pages_GovtRelationsManagement_SponsorBanks_Delete = "Pages.GovtRelationsManagement.SponsorBanks.Delete";

    public const string Pages_GovtRelationsManagement_OrganizationDocuments = "Pages.GovtRelationsManagement.OrganizationDocuments";
    public const string Pages_GovtRelationsManagement_OrganizationDocuments_Create = "Pages.GovtRelationsManagement.OrganizationDocuments.Create";
    public const string Pages_GovtRelationsManagement_OrganizationDocuments_Edit = "Pages.GovtRelationsManagement.OrganizationDocuments.Edit";
    public const string Pages_GovtRelationsManagement_OrganizationDocuments_Delete = "Pages.GovtRelationsManagement.OrganizationDocuments.Delete";

    public const string Pages_GovtRelationsManagement_EmployeeExitPermits = "Pages.GovtRelationsManagement.EmployeeExitPermits";
    public const string Pages_GovtRelationsManagement_EmployeeExitPermits_Create = "Pages.GovtRelationsManagement.EmployeeExitPermits.Create";
    public const string Pages_GovtRelationsManagement_EmployeeExitPermits_Edit = "Pages.GovtRelationsManagement.EmployeeExitPermits.Edit";
    public const string Pages_GovtRelationsManagement_EmployeeExitPermits_Delete = "Pages.GovtRelationsManagement.EmployeeExitPermits.Delete";

    public const string Pages_AccommodationManagement = "Pages.AccommodationManagement";

    public const string Pages_AccommodationManagement_Locations = "Pages.AccommodationManagement.Locations";
    public const string Pages_AccommodationManagement_Locations_Create = "Pages.AccommodationManagement.Locations.Create";
    public const string Pages_AccommodationManagement_Locations_Edit = "Pages.AccommodationManagement.Locations.Edit";
    public const string Pages_AccommodationManagement_Locations_Delete = "Pages.AccommodationManagement.Locations.Delete";

    public const string Pages_AccommodationManagement_Buildings = "Pages.AccommodationManagement.Buildings";
    public const string Pages_AccommodationManagement_Buildings_Create = "Pages.AccommodationManagement.Buildings.Create";
    public const string Pages_AccommodationManagement_Buildings_Edit = "Pages.AccommodationManagement.Buildings.Edit";
    public const string Pages_AccommodationManagement_Buildings_Delete = "Pages.AccommodationManagement.Buildings.Delete";

    public const string Pages_AccommodationManagement_BuildingContracts = "Pages.AccommodationManagement.BuildingContracts";
    public const string Pages_AccommodationManagement_BuildingContracts_Create = "Pages.AccommodationManagement.BuildingContracts.Create";
    public const string Pages_AccommodationManagement_BuildingContracts_Edit = "Pages.AccommodationManagement.BuildingContracts.Edit";
    public const string Pages_AccommodationManagement_BuildingContracts_Delete = "Pages.AccommodationManagement.BuildingContracts.Delete";

    public const string Pages_AccommodationManagement_Floors = "Pages.AccommodationManagement.Floors";
    public const string Pages_AccommodationManagement_Floors_Create = "Pages.AccommodationManagement.Floors.Create";
    public const string Pages_AccommodationManagement_Floors_Edit = "Pages.AccommodationManagement.Floors.Edit";
    public const string Pages_AccommodationManagement_Floors_Delete = "Pages.AccommodationManagement.Floors.Delete";

    public const string Pages_AccommodationManagement_Flats = "Pages.AccommodationManagement.Flats";
    public const string Pages_AccommodationManagement_Flats_Create = "Pages.AccommodationManagement.Flats.Create";
    public const string Pages_AccommodationManagement_Flats_Edit = "Pages.AccommodationManagement.Flats.Edit";
    public const string Pages_AccommodationManagement_Flats_Delete = "Pages.AccommodationManagement.Flats.Delete";

    public const string Pages_AccommodationManagement_Rooms = "Pages.AccommodationManagement.Rooms";
    public const string Pages_AccommodationManagement_Rooms_Create = "Pages.AccommodationManagement.Rooms.Create";
    public const string Pages_AccommodationManagement_Rooms_Edit = "Pages.AccommodationManagement.Rooms.Edit";
    public const string Pages_AccommodationManagement_Rooms_Delete = "Pages.AccommodationManagement.Rooms.Delete";

    public const string Pages_AccommodationManagement_Beds = "Pages.AccommodationManagement.Beds";
    public const string Pages_AccommodationManagement_Beds_Create = "Pages.AccommodationManagement.Beds.Create";
    public const string Pages_AccommodationManagement_Beds_Edit = "Pages.AccommodationManagement.Beds.Edit";
    public const string Pages_AccommodationManagement_Beds_Delete = "Pages.AccommodationManagement.Beds.Delete";

    public const string Pages_AccommodationManagement_AccommodationTypes = "Pages.AccommodationManagement.AccommodationTypes";
    public const string Pages_AccommodationManagement_AccommodationTypes_Create = "Pages.AccommodationManagement.AccommodationTypes.Create";
    public const string Pages_AccommodationManagement_AccommodationTypes_Edit = "Pages.AccommodationManagement.AccommodationTypes.Edit";
    public const string Pages_AccommodationManagement_AccommodationTypes_Delete = "Pages.AccommodationManagement.AccommodationTypes.Delete";

    public const string Pages_AccommodationManagement_AccommodationServiceTypes = "Pages.AccommodationManagement.AccommodationServiceTypes";
    public const string Pages_AccommodationManagement_AccommodationServiceTypes_Create = "Pages.AccommodationManagement.AccommodationServiceTypes.Create";
    public const string Pages_AccommodationManagement_AccommodationServiceTypes_Edit = "Pages.AccommodationManagement.AccommodationServiceTypes.Edit";
    public const string Pages_AccommodationManagement_AccommodationServiceTypes_Delete = "Pages.AccommodationManagement.AccommodationServiceTypes.Delete";

    public const string Pages_AccommodationManagement_EmployeeAccommodations = "Pages.AccommodationManagement.EmployeeAccommodations";
    public const string Pages_AccommodationManagement_EmployeeAccommodations_Create = "Pages.AccommodationManagement.EmployeeAccommodations.Create";
    public const string Pages_AccommodationManagement_EmployeeAccommodations_Edit = "Pages.AccommodationManagement.EmployeeAccommodations.Edit";
    public const string Pages_AccommodationManagement_EmployeeAccommodations_Delete = "Pages.AccommodationManagement.EmployeeAccommodations.Delete";

    public const string Pages_AccommodationManagement_AccommodationServices = "Pages.AccommodationManagement.AccommodationServices";
    public const string Pages_AccommodationManagement_AccommodationServices_Create = "Pages.AccommodationManagement.AccommodationServices.Create";
    public const string Pages_AccommodationManagement_AccommodationServices_Edit = "Pages.AccommodationManagement.AccommodationServices.Edit";
    public const string Pages_AccommodationManagement_AccommodationServices_Delete = "Pages.AccommodationManagement.AccommodationServices.Delete";

    public const string Pages_PerformanceManagement = "Pages.PerformanceManagement";

    public const string Pages_PerformanceManagement_FormTypes = "Pages.PerformanceManagement.FormTypes";
    public const string Pages_PerformanceManagement_FormTypes_Create = "Pages.PerformanceManagement.FormTypes.Create";
    public const string Pages_PerformanceManagement_FormTypes_Edit = "Pages.PerformanceManagement.FormTypes.Edit";
    public const string Pages_PerformanceManagement_FormTypes_Delete = "Pages.PerformanceManagement.FormTypes.Delete";

    public const string Pages_PerformanceManagement_KpiCategories = "Pages.PerformanceManagement.KpiCategories";
    public const string Pages_PerformanceManagement_KpiCategories_Create = "Pages.PerformanceManagement.KpiCategories.Create";
    public const string Pages_PerformanceManagement_KpiCategories_Edit = "Pages.PerformanceManagement.KpiCategories.Edit";
    public const string Pages_PerformanceManagement_KpiCategories_Delete = "Pages.PerformanceManagement.KpiCategories.Delete";

    public const string Pages_PerformanceManagement_KpiInfos = "Pages.PerformanceManagement.KpiInfos";
    public const string Pages_PerformanceManagement_KpiInfos_Create = "Pages.PerformanceManagement.KpiInfos.Create";
    public const string Pages_PerformanceManagement_KpiInfos_Edit = "Pages.PerformanceManagement.KpiInfos.Edit";
    public const string Pages_PerformanceManagement_KpiInfos_Delete = "Pages.PerformanceManagement.KpiInfos.Delete";

    public const string Pages_PerformanceManagement_PARatings = "Pages.PerformanceManagement.PARatings";
    public const string Pages_PerformanceManagement_PARatings_Create = "Pages.PerformanceManagement.PARatings.Create";
    public const string Pages_PerformanceManagement_PARatings_Edit = "Pages.PerformanceManagement.PARatings.Edit";
    public const string Pages_PerformanceManagement_PARatings_Delete = "Pages.PerformanceManagement.PARatings.Delete";

    public const string Pages_PerformanceManagement_PACycles = "Pages.PerformanceManagement.PACycles";
    public const string Pages_PerformanceManagement_PACycles_Create = "Pages.PerformanceManagement.PACycles.Create";
    public const string Pages_PerformanceManagement_PACycles_Edit = "Pages.PerformanceManagement.PACycles.Edit";
    public const string Pages_PerformanceManagement_PACycles_Delete = "Pages.PerformanceManagement.PACycles.Delete";

    public const string Pages_PerformanceManagement_PAPeriods = "Pages.PerformanceManagement.PAPeriods";
    public const string Pages_PerformanceManagement_PAPeriods_Create = "Pages.PerformanceManagement.PAPeriods.Create";
    public const string Pages_PerformanceManagement_PAPeriods_Edit = "Pages.PerformanceManagement.PAPeriods.Edit";
    public const string Pages_PerformanceManagement_PAPeriods_Delete = "Pages.PerformanceManagement.PAPeriods.Delete";

    public const string Pages_PerformanceManagement_PATimelines = "Pages.PerformanceManagement.PATimelines";
    public const string Pages_PerformanceManagement_PATimelines_Create = "Pages.PerformanceManagement.PATimelines.Create";
    public const string Pages_PerformanceManagement_PATimelines_Edit = "Pages.PerformanceManagement.PATimelines.Edit";
    public const string Pages_PerformanceManagement_PATimelines_Delete = "Pages.PerformanceManagement.PATimelines.Delete";

    public const string Pages_PerformanceManagement_PATimelineExceptions = "Pages.PerformanceManagement.PATimelineExceptions";
    public const string Pages_PerformanceManagement_PATimelineExceptions_Create = "Pages.PerformanceManagement.PATimelineExceptions.Create";
    public const string Pages_PerformanceManagement_PATimelineExceptions_Edit = "Pages.PerformanceManagement.PATimelineExceptions.Edit";
    public const string Pages_PerformanceManagement_PATimelineExceptions_Delete = "Pages.PerformanceManagement.PATimelineExceptions.Delete";

    public const string Pages_PerformanceManagement_PAPeerNominees = "Pages.PerformanceManagement.PAPeerNominees";
    public const string Pages_PerformanceManagement_PAPeerNominees_Create = "Pages.PerformanceManagement.PAPeerNominees.Create";
    public const string Pages_PerformanceManagement_PAPeerNominees_Edit = "Pages.PerformanceManagement.PAPeerNominees.Edit";
    public const string Pages_PerformanceManagement_PAPeerNominees_Delete = "Pages.PerformanceManagement.PAPeerNominees.Delete";

    public const string Pages_PerformanceManagement_KpiByJobTitles = "Pages.PerformanceManagement.KpiByJobTitles";
    public const string Pages_PerformanceManagement_KpiByJobTitles_Create = "Pages.PerformanceManagement.KpiByJobTitles.Create";
    public const string Pages_PerformanceManagement_KpiByJobTitles_Edit = "Pages.PerformanceManagement.KpiByJobTitles.Edit";
    public const string Pages_PerformanceManagement_KpiByJobTitles_Delete = "Pages.PerformanceManagement.KpiByJobTitles.Delete";

    public const string Pages_PerformanceManagement_KpiByOrgUnits = "Pages.PerformanceManagement.KpiByOrgUnits";
    public const string Pages_PerformanceManagement_KpiByOrgUnits_Create = "Pages.PerformanceManagement.KpiByOrgUnits.Create";
    public const string Pages_PerformanceManagement_KpiByOrgUnits_Edit = "Pages.PerformanceManagement.KpiByOrgUnits.Edit";
    public const string Pages_PerformanceManagement_KpiByOrgUnits_Delete = "Pages.PerformanceManagement.KpiByOrgUnits.Delete";

    public const string Pages_PerformanceManagement_PAForms = "Pages.PerformanceManagement.PAForms";
    public const string Pages_PerformanceManagement_PAForms_Create = "Pages.PerformanceManagement.PAForms.Create";
    public const string Pages_PerformanceManagement_PAForms_Edit = "Pages.PerformanceManagement.PAForms.Edit";
    public const string Pages_PerformanceManagement_PAForms_Delete = "Pages.PerformanceManagement.PAForms.Delete";

    public const string Pages_PerformanceManagement_PAEmployees = "Pages.PerformanceManagement.PAEmployees";
    public const string Pages_PerformanceManagement_PAEmployees_Create = "Pages.PerformanceManagement.PAEmployees.Create";
    public const string Pages_PerformanceManagement_PAEmployees_Edit = "Pages.PerformanceManagement.PAEmployees.Edit";
    public const string Pages_PerformanceManagement_PAEmployees_Delete = "Pages.PerformanceManagement.PAEmployees.Delete";

    public const string Pages_SuccessionManagement = "Pages.SuccessionManagement";

    public const string Pages_SuccessionManagement_ImpactOfLosses = "Pages.SuccessionManagement.ImpactOfLosses";
    public const string Pages_SuccessionManagement_ImpactOfLosses_Create = "Pages.SuccessionManagement.ImpactOfLosses.Create";
    public const string Pages_SuccessionManagement_ImpactOfLosses_Edit = "Pages.SuccessionManagement.ImpactOfLosses.Edit";
    public const string Pages_SuccessionManagement_ImpactOfLosses_Delete = "Pages.SuccessionManagement.ImpactOfLosses.Delete";

    public const string Pages_SuccessionManagement_NineBoxRatings = "Pages.SuccessionManagement.NineBoxRatings";
    public const string Pages_SuccessionManagement_NineBoxRatings_Create = "Pages.SuccessionManagement.NineBoxRatings.Create";
    public const string Pages_SuccessionManagement_NineBoxRatings_Edit = "Pages.SuccessionManagement.NineBoxRatings.Edit";
    public const string Pages_SuccessionManagement_NineBoxRatings_Delete = "Pages.SuccessionManagement.NineBoxRatings.Delete";

    public const string Pages_SuccessionManagement_Rankings = "Pages.SuccessionManagement.Rankings";
    public const string Pages_SuccessionManagement_Rankings_Create = "Pages.SuccessionManagement.Rankings.Create";
    public const string Pages_SuccessionManagement_Rankings_Edit = "Pages.SuccessionManagement.Rankings.Edit";
    public const string Pages_SuccessionManagement_Rankings_Delete = "Pages.SuccessionManagement.Rankings.Delete";

    public const string Pages_SuccessionManagement_Readinesses = "Pages.SuccessionManagement.Readinesses";
    public const string Pages_SuccessionManagement_Readinesses_Create = "Pages.SuccessionManagement.Readinesses.Create";
    public const string Pages_SuccessionManagement_Readinesses_Edit = "Pages.SuccessionManagement.Readinesses.Edit";
    public const string Pages_SuccessionManagement_Readinesses_Delete = "Pages.SuccessionManagement.Readinesses.Delete";

    public const string Pages_SuccessionManagement_ReasonForLeavings = "Pages.SuccessionManagement.ReasonForLeavings";
    public const string Pages_SuccessionManagement_ReasonForLeavings_Create = "Pages.SuccessionManagement.ReasonForLeavings.Create";
    public const string Pages_SuccessionManagement_ReasonForLeavings_Edit = "Pages.SuccessionManagement.ReasonForLeavings.Edit";
    public const string Pages_SuccessionManagement_ReasonForLeavings_Delete = "Pages.SuccessionManagement.ReasonForLeavings.Delete";

    public const string Pages_SuccessionManagement_RiskOfLosses = "Pages.SuccessionManagement.RiskOfLosses";
    public const string Pages_SuccessionManagement_RiskOfLosses_Create = "Pages.SuccessionManagement.RiskOfLosses.Create";
    public const string Pages_SuccessionManagement_RiskOfLosses_Edit = "Pages.SuccessionManagement.RiskOfLosses.Edit";
    public const string Pages_SuccessionManagement_RiskOfLosses_Delete = "Pages.SuccessionManagement.RiskOfLosses.Delete";

    public const string Pages_SuccessionManagement_SuccessionRatings = "Pages.SuccessionManagement.SuccessionRatings";
    public const string Pages_SuccessionManagement_SuccessionRatings_Create = "Pages.SuccessionManagement.SuccessionRatings.Create";
    public const string Pages_SuccessionManagement_SuccessionRatings_Edit = "Pages.SuccessionManagement.SuccessionRatings.Edit";
    public const string Pages_SuccessionManagement_SuccessionRatings_Delete = "Pages.SuccessionManagement.SuccessionRatings.Delete";

    public const string Pages_SuccessionManagement_TalentPools = "Pages.SuccessionManagement.TalentPools";
    public const string Pages_SuccessionManagement_TalentPools_Create = "Pages.SuccessionManagement.TalentPools.Create";
    public const string Pages_SuccessionManagement_TalentPools_Edit = "Pages.SuccessionManagement.TalentPools.Edit";
    public const string Pages_SuccessionManagement_TalentPools_Delete = "Pages.SuccessionManagement.TalentPools.Delete";

    public const string Pages_SuccessionManagement_NineBoxMappings = "Pages.SuccessionManagement.NineBoxMappings";
    public const string Pages_SuccessionManagement_NineBoxMappings_Create = "Pages.SuccessionManagement.NineBoxMappings.Create";
    public const string Pages_SuccessionManagement_NineBoxMappings_Edit = "Pages.SuccessionManagement.NineBoxMappings.Edit";
    public const string Pages_SuccessionManagement_NineBoxMappings_Delete = "Pages.SuccessionManagement.NineBoxMappings.Delete";

    public const string Pages_SuccessionManagement_EmpPerformanceHistories = "Pages.SuccessionManagement.EmpPerformanceHistories";
    public const string Pages_SuccessionManagement_EmpPerformanceHistories_Create = "Pages.SuccessionManagement.EmpPerformanceHistories.Create";
    public const string Pages_SuccessionManagement_EmpPerformanceHistories_Edit = "Pages.SuccessionManagement.EmpPerformanceHistories.Edit";
    public const string Pages_SuccessionManagement_EmpPerformanceHistories_Delete = "Pages.SuccessionManagement.EmpPerformanceHistories.Delete";

    public const string Pages_SuccessionManagement_EmpPotentialAssessments = "Pages.SuccessionManagement.EmpPotentialAssessments";
    public const string Pages_SuccessionManagement_EmpPotentialAssessments_Create = "Pages.SuccessionManagement.EmpPotentialAssessments.Create";
    public const string Pages_SuccessionManagement_EmpPotentialAssessments_Edit = "Pages.SuccessionManagement.EmpPotentialAssessments.Edit";
    public const string Pages_SuccessionManagement_EmpPotentialAssessments_Delete = "Pages.SuccessionManagement.EmpPotentialAssessments.Delete";

    public const string Pages_SuccessionManagement_EmpTalentMatrixDatas = "Pages.SuccessionManagement.EmpTalentMatrixDatas";
    public const string Pages_SuccessionManagement_EmpTalentMatrixDatas_Create = "Pages.SuccessionManagement.EmpTalentMatrixDatas.Create";
    public const string Pages_SuccessionManagement_EmpTalentMatrixDatas_Edit = "Pages.SuccessionManagement.EmpTalentMatrixDatas.Edit";
    public const string Pages_SuccessionManagement_EmpTalentMatrixDatas_Delete = "Pages.SuccessionManagement.EmpTalentMatrixDatas.Delete";

    public const string Pages_SuccessionManagement_EmployeeTalentFlags = "Pages.SuccessionManagement.EmployeeTalentFlags";
    public const string Pages_SuccessionManagement_EmployeeTalentFlags_Create = "Pages.SuccessionManagement.EmployeeTalentFlags.Create";
    public const string Pages_SuccessionManagement_EmployeeTalentFlags_Edit = "Pages.SuccessionManagement.EmployeeTalentFlags.Edit";
    public const string Pages_SuccessionManagement_EmployeeTalentFlags_Delete = "Pages.SuccessionManagement.EmployeeTalentFlags.Delete";

    public const string Pages_SuccessionManagement_SuccessorNominees = "Pages.SuccessionManagement.SuccessorNominees";
    public const string Pages_SuccessionManagement_SuccessorNominees_Create = "Pages.SuccessionManagement.SuccessorNominees.Create";
    public const string Pages_SuccessionManagement_SuccessorNominees_Edit = "Pages.SuccessionManagement.SuccessorNominees.Edit";
    public const string Pages_SuccessionManagement_SuccessorNominees_Delete = "Pages.SuccessionManagement.SuccessorNominees.Delete";

    public const string Pages_ManpowerPlanningManagement = "Pages.ManpowerPlanningManagement";

    public const string Pages_ManpowerPlanningManagement_ManpowerPlanningPeriods = "Pages.ManpowerPlanningManagement.ManpowerPlanningPeriods";
    public const string Pages_ManpowerPlanningManagement_ManpowerPlanningPeriods_Create = "Pages.ManpowerPlanningManagement.ManpowerPlanningPeriods.Create";
    public const string Pages_ManpowerPlanningManagement_ManpowerPlanningPeriods_Edit = "Pages.ManpowerPlanningManagement.ManpowerPlanningPeriods.Edit";
    public const string Pages_ManpowerPlanningManagement_ManpowerPlanningPeriods_Delete = "Pages.ManpowerPlanningManagement.ManpowerPlanningPeriods.Delete";

    public const string Pages_ManpowerPlanningManagement_ManpowerPlannings = "Pages.ManpowerPlanningManagement.ManpowerPlannings";
    public const string Pages_ManpowerPlanningManagement_ManpowerPlannings_Create = "Pages.ManpowerPlanningManagement.ManpowerPlannings.Create";
    public const string Pages_ManpowerPlanningManagement_ManpowerPlannings_Edit = "Pages.ManpowerPlanningManagement.ManpowerPlannings.Edit";
    public const string Pages_ManpowerPlanningManagement_ManpowerPlannings_Delete = "Pages.ManpowerPlanningManagement.ManpowerPlannings.Delete";

    // No ...ManpowerPlannings.Approve permission. Approving a manpower plan is a workflow act now: who may
    // do it is decided by the plan's approval chain (RequestMaster/WorkflowRole), not by a page permission,
    // and the app-service methods it used to guard no longer exist.

    public const string Pages_ManpowerPlanningManagement_ManpowerPlanningDetails = "Pages.ManpowerPlanningManagement.ManpowerPlanningDetails";
    public const string Pages_ManpowerPlanningManagement_ManpowerPlanningDetails_Create = "Pages.ManpowerPlanningManagement.ManpowerPlanningDetails.Create";
    public const string Pages_ManpowerPlanningManagement_ManpowerPlanningDetails_Edit = "Pages.ManpowerPlanningManagement.ManpowerPlanningDetails.Edit";
    public const string Pages_ManpowerPlanningManagement_ManpowerPlanningDetails_Delete = "Pages.ManpowerPlanningManagement.ManpowerPlanningDetails.Delete";

    public const string Pages_ManpowerPlanningManagement_Positions = "Pages.ManpowerPlanningManagement.Positions";
    public const string Pages_ManpowerPlanningManagement_Positions_Create = "Pages.ManpowerPlanningManagement.Positions.Create";
    public const string Pages_ManpowerPlanningManagement_Positions_Edit = "Pages.ManpowerPlanningManagement.Positions.Edit";
    public const string Pages_ManpowerPlanningManagement_Positions_Delete = "Pages.ManpowerPlanningManagement.Positions.Delete";

    public const string Pages_ManpowerPlanningManagement_PositionRelationships = "Pages.ManpowerPlanningManagement.PositionRelationships";
    public const string Pages_ManpowerPlanningManagement_PositionRelationships_Create = "Pages.ManpowerPlanningManagement.PositionRelationships.Create";
    public const string Pages_ManpowerPlanningManagement_PositionRelationships_Edit = "Pages.ManpowerPlanningManagement.PositionRelationships.Edit";
    public const string Pages_ManpowerPlanningManagement_PositionRelationships_Delete = "Pages.ManpowerPlanningManagement.PositionRelationships.Delete";

    public const string Pages_Workflow = "Pages.Workflow";

    public const string Pages_Workflow_WorkflowRoles = "Pages.Workflow.WorkflowRoles";
    public const string Pages_Workflow_WorkflowRoles_Create = "Pages.Workflow.WorkflowRoles.Create";
    public const string Pages_Workflow_WorkflowRoles_Edit = "Pages.Workflow.WorkflowRoles.Edit";
    public const string Pages_Workflow_WorkflowRoles_Delete = "Pages.Workflow.WorkflowRoles.Delete";

    public const string Pages_Workflow_EmployeeWorkflowRoles = "Pages.Workflow.EmployeeWorkflowRoles";
    public const string Pages_Workflow_EmployeeWorkflowRoles_Create = "Pages.Workflow.EmployeeWorkflowRoles.Create";
    public const string Pages_Workflow_EmployeeWorkflowRoles_Edit = "Pages.Workflow.EmployeeWorkflowRoles.Edit";
    public const string Pages_Workflow_EmployeeWorkflowRoles_Delete = "Pages.Workflow.EmployeeWorkflowRoles.Delete";

    public const string Pages_Workflow_WorkflowGroups = "Pages.Workflow.WorkflowGroups";
    public const string Pages_Workflow_WorkflowGroups_Create = "Pages.Workflow.WorkflowGroups.Create";
    public const string Pages_Workflow_WorkflowGroups_Edit = "Pages.Workflow.WorkflowGroups.Edit";
    public const string Pages_Workflow_WorkflowGroups_Delete = "Pages.Workflow.WorkflowGroups.Delete";

    public const string Pages_Workflow_WorkflowGroupDetails = "Pages.Workflow.WorkflowGroupDetails";
    public const string Pages_Workflow_WorkflowGroupDetails_Create = "Pages.Workflow.WorkflowGroupDetails.Create";
    public const string Pages_Workflow_WorkflowGroupDetails_Edit = "Pages.Workflow.WorkflowGroupDetails.Edit";
    public const string Pages_Workflow_WorkflowGroupDetails_Delete = "Pages.Workflow.WorkflowGroupDetails.Delete";

    public const string Pages_Workflow_WorkflowGroupFieldAccesses = "Pages.Workflow.WorkflowGroupFieldAccesses";
    public const string Pages_Workflow_WorkflowGroupFieldAccesses_Create = "Pages.Workflow.WorkflowGroupFieldAccesses.Create";
    public const string Pages_Workflow_WorkflowGroupFieldAccesses_Edit = "Pages.Workflow.WorkflowGroupFieldAccesses.Edit";
    public const string Pages_Workflow_WorkflowGroupFieldAccesses_Delete = "Pages.Workflow.WorkflowGroupFieldAccesses.Delete";

    public const string Pages_Workflow_RequestMasters = "Pages.Workflow.RequestMasters";
    public const string Pages_Workflow_RequestMasters_Create = "Pages.Workflow.RequestMasters.Create";
    public const string Pages_Workflow_RequestMasters_Edit = "Pages.Workflow.RequestMasters.Edit";
    public const string Pages_Workflow_RequestMasters_Delete = "Pages.Workflow.RequestMasters.Delete";
    public const string Pages_Workflow_WorkflowTransactionTypes = "Pages.Workflow.WorkflowTransactionTypes";
    public const string Pages_Workflow_WorkflowTransactionTypes_Create = "Pages.Workflow.WorkflowTransactionTypes.Create";
    public const string Pages_Workflow_WorkflowTransactionTypes_Edit = "Pages.Workflow.WorkflowTransactionTypes.Edit";
    public const string Pages_Workflow_WorkflowTransactionTypes_Delete = "Pages.Workflow.WorkflowTransactionTypes.Delete";

    #region Business Rules

    // The rule engine is administrative configuration, not a data screen: a rule authored here runs inside
    // other people's saves. Authoring is therefore separated from ASSIGNING -- writing a rule changes
    // nothing until somebody binds it to a base object and an event, and that binding is its own permission
    // so a business analyst can draft rules without being able to arm them in production.
    public const string Pages_BusinessRules = "Pages.BusinessRules";
    public const string Pages_BusinessRules_Create = "Pages.BusinessRules.Create";
    public const string Pages_BusinessRules_Edit = "Pages.BusinessRules.Edit";
    public const string Pages_BusinessRules_Delete = "Pages.BusinessRules.Delete";

    /// <summary>Dry-run a rule against sample values. Read-only: the test harness never persists.</summary>
    public const string Pages_BusinessRules_Test = "Pages.BusinessRules.Test";

    /// <summary>Bind a rule to a base object and event, or unbind it. This is what makes a rule live.</summary>
    public const string Pages_BusinessRules_Assign = "Pages.BusinessRules.Assign";

    /// <summary>Read the execution log -- which rule fired, on which row, with what outcome.</summary>
    public const string Pages_BusinessRules_ExecutionLogs = "Pages.BusinessRules.ExecutionLogs";

    #endregion

    #region Employee Self Service

    // A SECOND permission tree, deliberately rooted outside "Pages".
    //
    // Everything under Pages.* is an administrative screen: it can read and write any employee's data. ESS
    // is the opposite -- an employee acting on their own records, scoped by IEmployeeScopeProvider. Keeping
    // the two trees apart is what lets an "Employee" role be granted self-service without any possibility
    // of it inheriting an admin screen, and it is why HCMv3 needs no separate ESS application the way
    // legacy HCM10.ESS did: the ~575-item admin menu is already permission-filtered, so an employee with no
    // Pages.* permission simply never sees it.
    //
    // Hiding the menu is cosmetic. The real guarantee is [AbpAuthorize(AppPermissions.Ess_*)] plus the
    // employee scope filter on every endpoint.
    public const string Ess = "Ess";

    /// <summary>Requests waiting on me to approve, across every transaction type.</summary>
    public const string Ess_Inbox = "Ess.Inbox";

    /// <summary>My own requests and their status.</summary>
    public const string Ess_MyRequests = "Ess.MyRequests";

    public const string Ess_MyProfile = "Ess.MyProfile";

    /// <summary>Team views for a manager -- scoped to direct reports, not the whole company.</summary>
    public const string Ess_MyTeam = "Ess.MyTeam";

    // One per ESS-enabled transaction type. These strings are what WorkflowTransactionType.EssPermissionName
    // holds, so the catalog row and the permission must agree or the menu entry silently never renders.
    public const string Ess_MyLeave = "Ess.MyLeave";
    public const string Ess_MyAssets = "Ess.MyAssets";
    public const string Ess_MyDocuments = "Ess.MyDocuments";
    public const string Ess_MyLoans = "Ess.MyLoans";

    // Separate from Ess_MyLoans on purpose: asking to skip a month's recovery is a different act from
    // borrowing, and a company may well let everyone see their loans while letting nobody request an
    // exemption without HR raising it.
    public const string Ess_MyLoanExemptions = "Ess.MyLoanExemptions";
    public const string Ess_MyOvertime = "Ess.MyOvertime";
    public const string Ess_MyTickets = "Ess.MyTickets";
    public const string Ess_MyAttendance = "Ess.MyAttendance";
    public const string Ess_MyBank = "Ess.MyBank";
    /// <summary>
    /// Asking to GO on a trip. Separate from Ess.MyTravelClaims, which is claiming back what a trip cost:
    /// requesting travel commits the company to the expense, claiming it back merely reports one, and a
    /// company may well let everyone claim while letting only some people request.
    /// </summary>
    public const string Ess_MyTravel = "Ess.MyTravel";
    public const string Ess_MyTravelClaims = "Ess.MyTravelClaims";
    public const string Ess_MyPassportWithdrawals = "Ess.MyPassportWithdrawals";
    public const string Ess_MyExitPermits = "Ess.MyExitPermits";

    /// <summary>Certificates, letters and card replacements the employee asks for. There is deliberately no
    /// Ess.MyITServices: an IT service is raised on the employee's behalf and has no self-service screen.</summary>
    public const string Ess_MyHRServices = "Ess.MyHRServices";

    /// <summary>
    /// Employment letters the employee asks for -- salary certificate, experience certificate,
    /// salary-continuity letter. Distinct from Ess.MyHRServices, which is the fee-bearing service catalogue
    /// (card replacements and the like); a letter is a document the company writes and stamps, and approval
    /// on it grants nothing except the right to print it.
    /// </summary>
    public const string Ess_MyLetterRequests = "Ess.MyLetterRequests";
    /// <summary>Missing clock punches (legacy "skip punch"). Separate from Ess.MyAttendance on purpose: this
    /// one adds a raw clock event and lets the Time Engine recompute the day, whereas an attendance
    /// correction states a paid attendance type outright.</summary>
    public const string Ess_MyPunches = "Ess.MyPunches";

    /// <summary>
    /// Exit clearance -- the checklist IT, Finance and HR sign off before somebody leaves. Held by the
    /// leaving employee AND by the approvers, because the same service serves both: the employee raises the
    /// clearance, and each department's approver answers their own lines of it.
    /// </summary>
    public const string Ess_MyClearances = "Ess.MyClearances";

    /// <summary>
    /// Health reimbursement -- claiming a medical expense back against the employee's own entitlement. Held
    /// by the claiming employee. Note it also gates the balance endpoint, which is why an employee who cannot
    /// claim cannot read their entitlement figures either.
    /// </summary>
    public const string Ess_MyHealthReimburses = "Ess.MyHealthReimburses";

    /// <summary>
    /// Delegation of duties -- asking a colleague to cover the employee's approvals and notifications while
    /// they are away. Held by the employee going away, not by the person covering: the delegator is the one
    /// giving authority up.
    /// </summary>
    public const string Ess_MyDelegations = "Ess.MyDelegations";

    /// <summary>
    /// Registering the handset the mobile app may send attendance punches from. Separate from
    /// Ess.MyAttendance and Ess.MyPunches on purpose: those are claims ABOUT attendance that an approver
    /// reads and judges, whereas this grants a device the standing to record attendance in the first place,
    /// and a company may well let everyone correct a punch while letting nobody enrol their own phone.
    /// </summary>
    public const string Ess_MyMobileDevices = "Ess.MyMobileDevices";

    #endregion

    public const string Pages_Workflow_RequestDetails = "Pages.Workflow.RequestDetails";
    public const string Pages_Workflow_RequestDetails_Create = "Pages.Workflow.RequestDetails.Create";
    public const string Pages_Workflow_RequestDetails_Edit = "Pages.Workflow.RequestDetails.Edit";
    public const string Pages_Workflow_RequestDetails_Delete = "Pages.Workflow.RequestDetails.Delete";

    public const string Pages_Workflow_RequestDetailParallels = "Pages.Workflow.RequestDetailParallels";
    public const string Pages_Workflow_RequestDetailParallels_Create = "Pages.Workflow.RequestDetailParallels.Create";
    public const string Pages_Workflow_RequestDetailParallels_Edit = "Pages.Workflow.RequestDetailParallels.Edit";
    public const string Pages_Workflow_RequestDetailParallels_Delete = "Pages.Workflow.RequestDetailParallels.Delete";

    public const string Pages_Workflow_WorkflowRequests = "Pages.Workflow.WorkflowRequests";
    public const string Pages_Workflow_WorkflowRequests_Create = "Pages.Workflow.WorkflowRequests.Create";
    public const string Pages_Workflow_WorkflowRequests_Edit = "Pages.Workflow.WorkflowRequests.Edit";
    public const string Pages_Workflow_WorkflowRequests_Delete = "Pages.Workflow.WorkflowRequests.Delete";
    public const string Pages_Workflow_WorkflowRequests_Submit = "Pages.Workflow.WorkflowRequests.Submit";
    public const string Pages_Workflow_WorkflowRequests_Approve = "Pages.Workflow.WorkflowRequests.Approve";
    public const string Pages_Workflow_WorkflowRequests_Reject = "Pages.Workflow.WorkflowRequests.Reject";
    public const string Pages_Workflow_WorkflowRequests_SendBack = "Pages.Workflow.WorkflowRequests.SendBack";
    public const string Pages_Workflow_WorkflowRequests_Reassign = "Pages.Workflow.WorkflowRequests.Reassign";

    public const string Pages_Workflow_RequestLogs = "Pages.Workflow.RequestLogs";
    public const string Pages_Workflow_RequestLogs_Create = "Pages.Workflow.RequestLogs.Create";
    public const string Pages_Workflow_RequestLogs_Delete = "Pages.Workflow.RequestLogs.Delete";

    public const string Pages_Workflow_WorkFlowApprovers = "Pages.Workflow.WorkFlowApprovers";
    public const string Pages_Workflow_WorkFlowApprovers_Create = "Pages.Workflow.WorkFlowApprovers.Create";
    public const string Pages_Workflow_WorkFlowApprovers_Edit = "Pages.Workflow.WorkFlowApprovers.Edit";
    public const string Pages_Workflow_WorkFlowApprovers_Delete = "Pages.Workflow.WorkFlowApprovers.Delete";

    public const string Pages_Workflow_WorkflowRemarks = "Pages.Workflow.WorkflowRemarks";
    public const string Pages_Workflow_WorkflowRemarks_Create = "Pages.Workflow.WorkflowRemarks.Create";
    public const string Pages_Workflow_WorkflowRemarks_Edit = "Pages.Workflow.WorkflowRemarks.Edit";
    public const string Pages_Workflow_WorkflowRemarks_Delete = "Pages.Workflow.WorkflowRemarks.Delete";

    public const string Pages_Workflow_WFReadLogs = "Pages.Workflow.WFReadLogs";
    public const string Pages_Workflow_WFReadLogs_Create = "Pages.Workflow.WFReadLogs.Create";
    public const string Pages_Workflow_WFReadLogs_Delete = "Pages.Workflow.WFReadLogs.Delete";

    public const string Pages_ReportManagement = "Pages.ReportManagement";

    public const string Pages_ReportManagement_DomainTypes = "Pages.ReportManagement.DomainTypes";
    public const string Pages_ReportManagement_DomainTypes_Create = "Pages.ReportManagement.DomainTypes.Create";
    public const string Pages_ReportManagement_DomainTypes_Edit = "Pages.ReportManagement.DomainTypes.Edit";
    public const string Pages_ReportManagement_DomainTypes_Delete = "Pages.ReportManagement.DomainTypes.Delete";

    public const string Pages_ReportManagement_ReportTypes = "Pages.ReportManagement.ReportTypes";
    public const string Pages_ReportManagement_ReportTypes_Create = "Pages.ReportManagement.ReportTypes.Create";
    public const string Pages_ReportManagement_ReportTypes_Edit = "Pages.ReportManagement.ReportTypes.Edit";
    public const string Pages_ReportManagement_ReportTypes_Delete = "Pages.ReportManagement.ReportTypes.Delete";

    public const string Pages_ReportManagement_PrintReports = "Pages.ReportManagement.PrintReports";
    public const string Pages_ReportManagement_PrintReports_Create = "Pages.ReportManagement.PrintReports.Create";
    public const string Pages_ReportManagement_PrintReports_Edit = "Pages.ReportManagement.PrintReports.Edit";
    public const string Pages_ReportManagement_PrintReports_Delete = "Pages.ReportManagement.PrintReports.Delete";

    public const string Pages_ReportManagement_PrintReportPermissions = "Pages.ReportManagement.PrintReportPermissions";
    public const string Pages_ReportManagement_PrintReportPermissions_Create = "Pages.ReportManagement.PrintReportPermissions.Create";
    public const string Pages_ReportManagement_PrintReportPermissions_Edit = "Pages.ReportManagement.PrintReportPermissions.Edit";
    public const string Pages_ReportManagement_PrintReportPermissions_Delete = "Pages.ReportManagement.PrintReportPermissions.Delete";

    public const string Pages_ReportManagement_PrintReportPermissionDetails = "Pages.ReportManagement.PrintReportPermissionDetails";
    public const string Pages_ReportManagement_PrintReportPermissionDetails_Create = "Pages.ReportManagement.PrintReportPermissionDetails.Create";
    public const string Pages_ReportManagement_PrintReportPermissionDetails_Edit = "Pages.ReportManagement.PrintReportPermissionDetails.Edit";
    public const string Pages_ReportManagement_PrintReportPermissionDetails_Delete = "Pages.ReportManagement.PrintReportPermissionDetails.Delete";

    public const string Pages_ReportManagement_ReportMasters = "Pages.ReportManagement.ReportMasters";
    public const string Pages_ReportManagement_ReportMasters_Create = "Pages.ReportManagement.ReportMasters.Create";
    public const string Pages_ReportManagement_ReportMasters_Edit = "Pages.ReportManagement.ReportMasters.Edit";
    public const string Pages_ReportManagement_ReportMasters_Delete = "Pages.ReportManagement.ReportMasters.Delete";

    public const string Pages_ReportManagement_ReportDetails = "Pages.ReportManagement.ReportDetails";
    public const string Pages_ReportManagement_ReportDetails_Create = "Pages.ReportManagement.ReportDetails.Create";
    public const string Pages_ReportManagement_ReportDetails_Edit = "Pages.ReportManagement.ReportDetails.Edit";
    public const string Pages_ReportManagement_ReportDetails_Delete = "Pages.ReportManagement.ReportDetails.Delete";

    public const string Pages_ReportManagement_ReportSettings = "Pages.ReportManagement.ReportSettings";
    public const string Pages_ReportManagement_ReportSettings_Create = "Pages.ReportManagement.ReportSettings.Create";
    public const string Pages_ReportManagement_ReportSettings_Edit = "Pages.ReportManagement.ReportSettings.Edit";
    public const string Pages_ReportManagement_ReportSettings_Delete = "Pages.ReportManagement.ReportSettings.Delete";

    public const string Pages_ReportManagement_ReportSettingDetails = "Pages.ReportManagement.ReportSettingDetails";
    public const string Pages_ReportManagement_ReportSettingDetails_Create = "Pages.ReportManagement.ReportSettingDetails.Create";
    public const string Pages_ReportManagement_ReportSettingDetails_Edit = "Pages.ReportManagement.ReportSettingDetails.Edit";
    public const string Pages_ReportManagement_ReportSettingDetails_Delete = "Pages.ReportManagement.ReportSettingDetails.Delete";

    public const string Pages_ReportManagement_ReportInfos = "Pages.ReportManagement.ReportInfos";
    public const string Pages_ReportManagement_ReportInfos_Create = "Pages.ReportManagement.ReportInfos.Create";
    public const string Pages_ReportManagement_ReportInfos_Edit = "Pages.ReportManagement.ReportInfos.Edit";
    public const string Pages_ReportManagement_ReportInfos_Delete = "Pages.ReportManagement.ReportInfos.Delete";

    public const string Pages_ReportManagement_ReportInfoDetails = "Pages.ReportManagement.ReportInfoDetails";
    public const string Pages_ReportManagement_ReportInfoDetails_Create = "Pages.ReportManagement.ReportInfoDetails.Create";
    public const string Pages_ReportManagement_ReportInfoDetails_Edit = "Pages.ReportManagement.ReportInfoDetails.Edit";
    public const string Pages_ReportManagement_ReportInfoDetails_Delete = "Pages.ReportManagement.ReportInfoDetails.Delete";

    public const string Pages_CompetencyDevelopment = "Pages.CompetencyDevelopment";

    public const string Pages_CompetencyDevelopment_CompetencyTypes = "Pages.CompetencyDevelopment.CompetencyTypes";
    public const string Pages_CompetencyDevelopment_CompetencyTypes_Create = "Pages.CompetencyDevelopment.CompetencyTypes.Create";
    public const string Pages_CompetencyDevelopment_CompetencyTypes_Edit = "Pages.CompetencyDevelopment.CompetencyTypes.Edit";
    public const string Pages_CompetencyDevelopment_CompetencyTypes_Delete = "Pages.CompetencyDevelopment.CompetencyTypes.Delete";

    public const string Pages_CompetencyDevelopment_Competencies = "Pages.CompetencyDevelopment.Competencies";
    public const string Pages_CompetencyDevelopment_Competencies_Create = "Pages.CompetencyDevelopment.Competencies.Create";
    public const string Pages_CompetencyDevelopment_Competencies_Edit = "Pages.CompetencyDevelopment.Competencies.Edit";
    public const string Pages_CompetencyDevelopment_Competencies_Delete = "Pages.CompetencyDevelopment.Competencies.Delete";

    public const string Pages_CompetencyDevelopment_CompetencyByJobTitles = "Pages.CompetencyDevelopment.CompetencyByJobTitles";
    public const string Pages_CompetencyDevelopment_CompetencyByJobTitles_Create = "Pages.CompetencyDevelopment.CompetencyByJobTitles.Create";
    public const string Pages_CompetencyDevelopment_CompetencyByJobTitles_Edit = "Pages.CompetencyDevelopment.CompetencyByJobTitles.Edit";
    public const string Pages_CompetencyDevelopment_CompetencyByJobTitles_Delete = "Pages.CompetencyDevelopment.CompetencyByJobTitles.Delete";

    public const string Pages_CompetencyDevelopment_CompetencyByJobTitleDetails = "Pages.CompetencyDevelopment.CompetencyByJobTitleDetails";
    public const string Pages_CompetencyDevelopment_CompetencyByJobTitleDetails_Create = "Pages.CompetencyDevelopment.CompetencyByJobTitleDetails.Create";
    public const string Pages_CompetencyDevelopment_CompetencyByJobTitleDetails_Edit = "Pages.CompetencyDevelopment.CompetencyByJobTitleDetails.Edit";
    public const string Pages_CompetencyDevelopment_CompetencyByJobTitleDetails_Delete = "Pages.CompetencyDevelopment.CompetencyByJobTitleDetails.Delete";

    public const string Pages_CompetencyDevelopment_CDPeriods = "Pages.CompetencyDevelopment.CDPeriods";
    public const string Pages_CompetencyDevelopment_CDPeriods_Create = "Pages.CompetencyDevelopment.CDPeriods.Create";
    public const string Pages_CompetencyDevelopment_CDPeriods_Edit = "Pages.CompetencyDevelopment.CDPeriods.Edit";
    public const string Pages_CompetencyDevelopment_CDPeriods_Delete = "Pages.CompetencyDevelopment.CDPeriods.Delete";

    public const string Pages_CompetencyDevelopment_CDForms = "Pages.CompetencyDevelopment.CDForms";
    public const string Pages_CompetencyDevelopment_CDForms_Create = "Pages.CompetencyDevelopment.CDForms.Create";
    public const string Pages_CompetencyDevelopment_CDForms_Edit = "Pages.CompetencyDevelopment.CDForms.Edit";
    public const string Pages_CompetencyDevelopment_CDForms_Delete = "Pages.CompetencyDevelopment.CDForms.Delete";

    public const string Pages_CompetencyDevelopment_CDPlans = "Pages.CompetencyDevelopment.CDPlans";
    public const string Pages_CompetencyDevelopment_CDPlans_Create = "Pages.CompetencyDevelopment.CDPlans.Create";
    public const string Pages_CompetencyDevelopment_CDPlans_Edit = "Pages.CompetencyDevelopment.CDPlans.Edit";
    public const string Pages_CompetencyDevelopment_CDPlans_Delete = "Pages.CompetencyDevelopment.CDPlans.Delete";

    public const string Pages_CompetencyDevelopment_CDMidYearReviews = "Pages.CompetencyDevelopment.CDMidYearReviews";
    public const string Pages_CompetencyDevelopment_CDMidYearReviews_Create = "Pages.CompetencyDevelopment.CDMidYearReviews.Create";
    public const string Pages_CompetencyDevelopment_CDMidYearReviews_Edit = "Pages.CompetencyDevelopment.CDMidYearReviews.Edit";
    public const string Pages_CompetencyDevelopment_CDMidYearReviews_Delete = "Pages.CompetencyDevelopment.CDMidYearReviews.Delete";

    public const string Pages_CompetencyDevelopment_CDAnnualYearReviews = "Pages.CompetencyDevelopment.CDAnnualYearReviews";
    public const string Pages_CompetencyDevelopment_CDAnnualYearReviews_Create = "Pages.CompetencyDevelopment.CDAnnualYearReviews.Create";
    public const string Pages_CompetencyDevelopment_CDAnnualYearReviews_Edit = "Pages.CompetencyDevelopment.CDAnnualYearReviews.Edit";
    public const string Pages_CompetencyDevelopment_CDAnnualYearReviews_Delete = "Pages.CompetencyDevelopment.CDAnnualYearReviews.Delete";

    public const string Pages_CompetencyDevelopment_CDRatings = "Pages.CompetencyDevelopment.CDRatings";
    public const string Pages_CompetencyDevelopment_CDRatings_Create = "Pages.CompetencyDevelopment.CDRatings.Create";
    public const string Pages_CompetencyDevelopment_CDRatings_Edit = "Pages.CompetencyDevelopment.CDRatings.Edit";
    public const string Pages_CompetencyDevelopment_CDRatings_Delete = "Pages.CompetencyDevelopment.CDRatings.Delete";

    public const string Pages_Recruitment = "Pages.Recruitment";

    public const string Pages_Recruitment_Applicants = "Pages.Recruitment.Applicants";
    public const string Pages_Recruitment_Applicants_Create = "Pages.Recruitment.Applicants.Create";
    public const string Pages_Recruitment_Applicants_Edit = "Pages.Recruitment.Applicants.Edit";
    public const string Pages_Recruitment_Applicants_Delete = "Pages.Recruitment.Applicants.Delete";

    public const string Pages_Recruitment_ApplicantCVs = "Pages.Recruitment.ApplicantCVs";
    public const string Pages_Recruitment_ApplicantCVs_Create = "Pages.Recruitment.ApplicantCVs.Create";
    public const string Pages_Recruitment_ApplicantCVs_Edit = "Pages.Recruitment.ApplicantCVs.Edit";
    public const string Pages_Recruitment_ApplicantCVs_Delete = "Pages.Recruitment.ApplicantCVs.Delete";

    public const string Pages_Recruitment_CVQualifications = "Pages.Recruitment.CVQualifications";
    public const string Pages_Recruitment_CVQualifications_Create = "Pages.Recruitment.CVQualifications.Create";
    public const string Pages_Recruitment_CVQualifications_Edit = "Pages.Recruitment.CVQualifications.Edit";
    public const string Pages_Recruitment_CVQualifications_Delete = "Pages.Recruitment.CVQualifications.Delete";

    public const string Pages_Recruitment_CVWorkExps = "Pages.Recruitment.CVWorkExps";
    public const string Pages_Recruitment_CVWorkExps_Create = "Pages.Recruitment.CVWorkExps.Create";
    public const string Pages_Recruitment_CVWorkExps_Edit = "Pages.Recruitment.CVWorkExps.Edit";
    public const string Pages_Recruitment_CVWorkExps_Delete = "Pages.Recruitment.CVWorkExps.Delete";

    public const string Pages_Recruitment_CVSkills = "Pages.Recruitment.CVSkills";
    public const string Pages_Recruitment_CVSkills_Create = "Pages.Recruitment.CVSkills.Create";
    public const string Pages_Recruitment_CVSkills_Edit = "Pages.Recruitment.CVSkills.Edit";
    public const string Pages_Recruitment_CVSkills_Delete = "Pages.Recruitment.CVSkills.Delete";

    public const string Pages_Recruitment_CVLanguages = "Pages.Recruitment.CVLanguages";
    public const string Pages_Recruitment_CVLanguages_Create = "Pages.Recruitment.CVLanguages.Create";
    public const string Pages_Recruitment_CVLanguages_Edit = "Pages.Recruitment.CVLanguages.Edit";
    public const string Pages_Recruitment_CVLanguages_Delete = "Pages.Recruitment.CVLanguages.Delete";

    public const string Pages_Recruitment_Vacancies = "Pages.Recruitment.Vacancies";
    public const string Pages_Recruitment_Vacancies_Create = "Pages.Recruitment.Vacancies.Create";
    public const string Pages_Recruitment_Vacancies_Edit = "Pages.Recruitment.Vacancies.Edit";
    public const string Pages_Recruitment_Vacancies_Delete = "Pages.Recruitment.Vacancies.Delete";

    public const string Pages_Recruitment_ApplicationStatuses = "Pages.Recruitment.ApplicationStatuses";
    public const string Pages_Recruitment_ApplicationStatuses_Create = "Pages.Recruitment.ApplicationStatuses.Create";
    public const string Pages_Recruitment_ApplicationStatuses_Edit = "Pages.Recruitment.ApplicationStatuses.Edit";
    public const string Pages_Recruitment_ApplicationStatuses_Delete = "Pages.Recruitment.ApplicationStatuses.Delete";

    public const string Pages_Recruitment_InterviewDetails = "Pages.Recruitment.InterviewDetails";
    public const string Pages_Recruitment_InterviewDetails_Create = "Pages.Recruitment.InterviewDetails.Create";
    public const string Pages_Recruitment_InterviewDetails_Edit = "Pages.Recruitment.InterviewDetails.Edit";
    public const string Pages_Recruitment_InterviewDetails_Delete = "Pages.Recruitment.InterviewDetails.Delete";

    public const string Pages_Recruitment_OnbGroups = "Pages.Recruitment.OnbGroups";
    public const string Pages_Recruitment_OnbGroups_Create = "Pages.Recruitment.OnbGroups.Create";
    public const string Pages_Recruitment_OnbGroups_Edit = "Pages.Recruitment.OnbGroups.Edit";
    public const string Pages_Recruitment_OnbGroups_Delete = "Pages.Recruitment.OnbGroups.Delete";

    public const string Pages_Recruitment_OnbGroupDetails = "Pages.Recruitment.OnbGroupDetails";
    public const string Pages_Recruitment_OnbGroupDetails_Create = "Pages.Recruitment.OnbGroupDetails.Create";
    public const string Pages_Recruitment_OnbGroupDetails_Edit = "Pages.Recruitment.OnbGroupDetails.Edit";
    public const string Pages_Recruitment_OnbGroupDetails_Delete = "Pages.Recruitment.OnbGroupDetails.Delete";

    public const string Pages_Recruitment_OnbTasks = "Pages.Recruitment.OnbTasks";
    public const string Pages_Recruitment_OnbTasks_Create = "Pages.Recruitment.OnbTasks.Create";
    public const string Pages_Recruitment_OnbTasks_Edit = "Pages.Recruitment.OnbTasks.Edit";
    public const string Pages_Recruitment_OnbTasks_Delete = "Pages.Recruitment.OnbTasks.Delete";

    public const string Pages_Recruitment_OnbTemplates = "Pages.Recruitment.OnbTemplates";
    public const string Pages_Recruitment_OnbTemplates_Create = "Pages.Recruitment.OnbTemplates.Create";
    public const string Pages_Recruitment_OnbTemplates_Edit = "Pages.Recruitment.OnbTemplates.Edit";
    public const string Pages_Recruitment_OnbTemplates_Delete = "Pages.Recruitment.OnbTemplates.Delete";

    public const string Pages_Recruitment_OnbTemplateDetails = "Pages.Recruitment.OnbTemplateDetails";
    public const string Pages_Recruitment_OnbTemplateDetails_Create = "Pages.Recruitment.OnbTemplateDetails.Create";
    public const string Pages_Recruitment_OnbTemplateDetails_Edit = "Pages.Recruitment.OnbTemplateDetails.Edit";
    public const string Pages_Recruitment_OnbTemplateDetails_Delete = "Pages.Recruitment.OnbTemplateDetails.Delete";

    public const string Pages_Recruitment_OnbApplicantTasks = "Pages.Recruitment.OnbApplicantTasks";
    public const string Pages_Recruitment_OnbApplicantTasks_Create = "Pages.Recruitment.OnbApplicantTasks.Create";
    public const string Pages_Recruitment_OnbApplicantTasks_Edit = "Pages.Recruitment.OnbApplicantTasks.Edit";
    public const string Pages_Recruitment_OnbApplicantTasks_Delete = "Pages.Recruitment.OnbApplicantTasks.Delete";

    public const string Pages_Recruitment_OnbAppTaskComments = "Pages.Recruitment.OnbAppTaskComments";
    public const string Pages_Recruitment_OnbAppTaskComments_Create = "Pages.Recruitment.OnbAppTaskComments.Create";
    public const string Pages_Recruitment_OnbAppTaskComments_Edit = "Pages.Recruitment.OnbAppTaskComments.Edit";
    public const string Pages_Recruitment_OnbAppTaskComments_Delete = "Pages.Recruitment.OnbAppTaskComments.Delete";

    public const string Pages_Recruitment_OnbVideoLinks = "Pages.Recruitment.OnbVideoLinks";
    public const string Pages_Recruitment_OnbVideoLinks_Create = "Pages.Recruitment.OnbVideoLinks.Create";
    public const string Pages_Recruitment_OnbVideoLinks_Edit = "Pages.Recruitment.OnbVideoLinks.Edit";
    public const string Pages_Recruitment_OnbVideoLinks_Delete = "Pages.Recruitment.OnbVideoLinks.Delete";

    public const string Pages_BenefitsManagement_InsurancePlans = "Pages.BenefitsManagement.InsurancePlans";
    public const string Pages_BenefitsManagement_InsurancePlans_Create = "Pages.BenefitsManagement.InsurancePlans.Create";
    public const string Pages_BenefitsManagement_InsurancePlans_Edit = "Pages.BenefitsManagement.InsurancePlans.Edit";
    public const string Pages_BenefitsManagement_InsurancePlans_Delete = "Pages.BenefitsManagement.InsurancePlans.Delete";

    public const string Pages_BenefitsManagement_InsuranceProviders = "Pages.BenefitsManagement.InsuranceProviders";
    public const string Pages_BenefitsManagement_InsuranceProviders_Create = "Pages.BenefitsManagement.InsuranceProviders.Create";
    public const string Pages_BenefitsManagement_InsuranceProviders_Edit = "Pages.BenefitsManagement.InsuranceProviders.Edit";
    public const string Pages_BenefitsManagement_InsuranceProviders_Delete = "Pages.BenefitsManagement.InsuranceProviders.Delete";

    public const string Pages_BenefitsManagement_EmployeeInsurances = "Pages.BenefitsManagement.EmployeeInsurances";
    public const string Pages_BenefitsManagement_EmployeeInsurances_Create = "Pages.BenefitsManagement.EmployeeInsurances.Create";
    public const string Pages_BenefitsManagement_EmployeeInsurances_Edit = "Pages.BenefitsManagement.EmployeeInsurances.Edit";
    public const string Pages_BenefitsManagement_EmployeeInsurances_Delete = "Pages.BenefitsManagement.EmployeeInsurances.Delete";

    public const string Pages_BenefitsManagement_EmployeeInsuranceDetails = "Pages.BenefitsManagement.EmployeeInsuranceDetails";
    public const string Pages_BenefitsManagement_EmployeeInsuranceDetails_Create = "Pages.BenefitsManagement.EmployeeInsuranceDetails.Create";
    public const string Pages_BenefitsManagement_EmployeeInsuranceDetails_Edit = "Pages.BenefitsManagement.EmployeeInsuranceDetails.Edit";
    public const string Pages_BenefitsManagement_EmployeeInsuranceDetails_Delete = "Pages.BenefitsManagement.EmployeeInsuranceDetails.Delete";

    public const string Pages_BenefitsManagement_CompanyInsuranceDetails = "Pages.BenefitsManagement.CompanyInsuranceDetails";
    public const string Pages_BenefitsManagement_CompanyInsuranceDetails_Create = "Pages.BenefitsManagement.CompanyInsuranceDetails.Create";
    public const string Pages_BenefitsManagement_CompanyInsuranceDetails_Edit = "Pages.BenefitsManagement.CompanyInsuranceDetails.Edit";
    public const string Pages_BenefitsManagement_CompanyInsuranceDetails_Delete = "Pages.BenefitsManagement.CompanyInsuranceDetails.Delete";

    public const string Pages_SurveyManagement = "Pages.SurveyManagement";

    public const string Pages_SurveyManagement_Surveys = "Pages.SurveyManagement.Surveys";
    public const string Pages_SurveyManagement_Surveys_Create = "Pages.SurveyManagement.Surveys.Create";
    public const string Pages_SurveyManagement_Surveys_Edit = "Pages.SurveyManagement.Surveys.Edit";
    public const string Pages_SurveyManagement_Surveys_Delete = "Pages.SurveyManagement.Surveys.Delete";

    public const string Pages_SurveyManagement_SurveyDetails = "Pages.SurveyManagement.SurveyDetails";
    public const string Pages_SurveyManagement_SurveyDetails_Create = "Pages.SurveyManagement.SurveyDetails.Create";
    public const string Pages_SurveyManagement_SurveyDetails_Edit = "Pages.SurveyManagement.SurveyDetails.Edit";
    public const string Pages_SurveyManagement_SurveyDetails_Delete = "Pages.SurveyManagement.SurveyDetails.Delete";

    public const string Pages_SurveyManagement_EmployeeSurveys = "Pages.SurveyManagement.EmployeeSurveys";
    public const string Pages_SurveyManagement_EmployeeSurveys_Create = "Pages.SurveyManagement.EmployeeSurveys.Create";
    public const string Pages_SurveyManagement_EmployeeSurveys_Edit = "Pages.SurveyManagement.EmployeeSurveys.Edit";
    public const string Pages_SurveyManagement_EmployeeSurveys_Delete = "Pages.SurveyManagement.EmployeeSurveys.Delete";

    public const string Pages_SurveyManagement_EmployeeSurveyDetails = "Pages.SurveyManagement.EmployeeSurveyDetails";
    public const string Pages_SurveyManagement_EmployeeSurveyDetails_Create = "Pages.SurveyManagement.EmployeeSurveyDetails.Create";
    public const string Pages_SurveyManagement_EmployeeSurveyDetails_Edit = "Pages.SurveyManagement.EmployeeSurveyDetails.Edit";
    public const string Pages_SurveyManagement_EmployeeSurveyDetails_Delete = "Pages.SurveyManagement.EmployeeSurveyDetails.Delete";

    public const string Pages_GatePassManagement = "Pages.GatePassManagement";

    public const string Pages_GatePassManagement_GatePasses = "Pages.GatePassManagement.GatePasses";
    public const string Pages_GatePassManagement_GatePasses_Create = "Pages.GatePassManagement.GatePasses.Create";
    public const string Pages_GatePassManagement_GatePasses_Edit = "Pages.GatePassManagement.GatePasses.Edit";
    public const string Pages_GatePassManagement_GatePasses_Delete = "Pages.GatePassManagement.GatePasses.Delete";

    public const string Pages_GatePassManagement_GatePassMaterials = "Pages.GatePassManagement.GatePassMaterials";
    public const string Pages_GatePassManagement_GatePassMaterials_Create = "Pages.GatePassManagement.GatePassMaterials.Create";
    public const string Pages_GatePassManagement_GatePassMaterials_Edit = "Pages.GatePassManagement.GatePassMaterials.Edit";
    public const string Pages_GatePassManagement_GatePassMaterials_Delete = "Pages.GatePassManagement.GatePassMaterials.Delete";

    public const string Pages_GatePassManagement_GatePassPersons = "Pages.GatePassManagement.GatePassPersons";
    public const string Pages_GatePassManagement_GatePassPersons_Create = "Pages.GatePassManagement.GatePassPersons.Create";
    public const string Pages_GatePassManagement_GatePassPersons_Edit = "Pages.GatePassManagement.GatePassPersons.Edit";
    public const string Pages_GatePassManagement_GatePassPersons_Delete = "Pages.GatePassManagement.GatePassPersons.Delete";

    public const string Pages_GatePassManagement_GatePassVehicles = "Pages.GatePassManagement.GatePassVehicles";
    public const string Pages_GatePassManagement_GatePassVehicles_Create = "Pages.GatePassManagement.GatePassVehicles.Create";
    public const string Pages_GatePassManagement_GatePassVehicles_Edit = "Pages.GatePassManagement.GatePassVehicles.Edit";
    public const string Pages_GatePassManagement_GatePassVehicles_Delete = "Pages.GatePassManagement.GatePassVehicles.Delete";

    public const string Pages_GatePassManagement_GatePassTypes = "Pages.GatePassManagement.GatePassTypes";
    public const string Pages_GatePassManagement_GatePassTypes_Create = "Pages.GatePassManagement.GatePassTypes.Create";
    public const string Pages_GatePassManagement_GatePassTypes_Edit = "Pages.GatePassManagement.GatePassTypes.Edit";
    public const string Pages_GatePassManagement_GatePassTypes_Delete = "Pages.GatePassManagement.GatePassTypes.Delete";

    public const string Pages_GatePassManagement_GatePassMovements = "Pages.GatePassManagement.GatePassMovements";
    public const string Pages_GatePassManagement_GatePassMovements_Create = "Pages.GatePassManagement.GatePassMovements.Create";
    public const string Pages_GatePassManagement_GatePassMovements_Edit = "Pages.GatePassManagement.GatePassMovements.Edit";
    public const string Pages_GatePassManagement_GatePassMovements_Delete = "Pages.GatePassManagement.GatePassMovements.Delete";

    public const string Pages_GatePassManagement_GatePassStatuses = "Pages.GatePassManagement.GatePassStatuses";
    public const string Pages_GatePassManagement_GatePassStatuses_Create = "Pages.GatePassManagement.GatePassStatuses.Create";
    public const string Pages_GatePassManagement_GatePassStatuses_Edit = "Pages.GatePassManagement.GatePassStatuses.Edit";
    public const string Pages_GatePassManagement_GatePassStatuses_Delete = "Pages.GatePassManagement.GatePassStatuses.Delete";

    public const string Pages_GatePassManagement_GatePassVendors = "Pages.GatePassManagement.GatePassVendors";
    public const string Pages_GatePassManagement_GatePassVendors_Create = "Pages.GatePassManagement.GatePassVendors.Create";
    public const string Pages_GatePassManagement_GatePassVendors_Edit = "Pages.GatePassManagement.GatePassVendors.Edit";
    public const string Pages_GatePassManagement_GatePassVendors_Delete = "Pages.GatePassManagement.GatePassVendors.Delete";

    public const string Pages_GatePassManagement_PersonTypes = "Pages.GatePassManagement.PersonTypes";
    public const string Pages_GatePassManagement_PersonTypes_Create = "Pages.GatePassManagement.PersonTypes.Create";
    public const string Pages_GatePassManagement_PersonTypes_Edit = "Pages.GatePassManagement.PersonTypes.Edit";
    public const string Pages_GatePassManagement_PersonTypes_Delete = "Pages.GatePassManagement.PersonTypes.Delete";

    public const string Pages_GatePassManagement_VehicleTypes = "Pages.GatePassManagement.VehicleTypes";
    public const string Pages_GatePassManagement_VehicleTypes_Create = "Pages.GatePassManagement.VehicleTypes.Create";
    public const string Pages_GatePassManagement_VehicleTypes_Edit = "Pages.GatePassManagement.VehicleTypes.Edit";
    public const string Pages_GatePassManagement_VehicleTypes_Delete = "Pages.GatePassManagement.VehicleTypes.Delete";

    public const string Pages_GatePassManagement_VehicleBrands = "Pages.GatePassManagement.VehicleBrands";
    public const string Pages_GatePassManagement_VehicleBrands_Create = "Pages.GatePassManagement.VehicleBrands.Create";
    public const string Pages_GatePassManagement_VehicleBrands_Edit = "Pages.GatePassManagement.VehicleBrands.Edit";
    public const string Pages_GatePassManagement_VehicleBrands_Delete = "Pages.GatePassManagement.VehicleBrands.Delete";

    public const string Pages_GatePassManagement_VehicleModels = "Pages.GatePassManagement.VehicleModels";
    public const string Pages_GatePassManagement_VehicleModels_Create = "Pages.GatePassManagement.VehicleModels.Create";
    public const string Pages_GatePassManagement_VehicleModels_Edit = "Pages.GatePassManagement.VehicleModels.Edit";
    public const string Pages_GatePassManagement_VehicleModels_Delete = "Pages.GatePassManagement.VehicleModels.Delete";

    public const string Pages_GatePassManagement_PoolCars = "Pages.GatePassManagement.PoolCars";
    public const string Pages_GatePassManagement_PoolCars_Create = "Pages.GatePassManagement.PoolCars.Create";
    public const string Pages_GatePassManagement_PoolCars_Edit = "Pages.GatePassManagement.PoolCars.Edit";
    public const string Pages_GatePassManagement_PoolCars_Delete = "Pages.GatePassManagement.PoolCars.Delete";

    public const string Pages_GatePassManagement_EmployeePoolCars = "Pages.GatePassManagement.EmployeePoolCars";
    public const string Pages_GatePassManagement_EmployeePoolCars_Create = "Pages.GatePassManagement.EmployeePoolCars.Create";
    public const string Pages_GatePassManagement_EmployeePoolCars_Edit = "Pages.GatePassManagement.EmployeePoolCars.Edit";
    public const string Pages_GatePassManagement_EmployeePoolCars_Delete = "Pages.GatePassManagement.EmployeePoolCars.Delete";

    public const string Pages_HealthAndSafetyManagement = "Pages.HealthAndSafetyManagement";

    public const string Pages_HealthAndSafetyManagement_ActionStatuses = "Pages.HealthAndSafetyManagement.ActionStatuses";
    public const string Pages_HealthAndSafetyManagement_ActionStatuses_Create = "Pages.HealthAndSafetyManagement.ActionStatuses.Create";
    public const string Pages_HealthAndSafetyManagement_ActionStatuses_Edit = "Pages.HealthAndSafetyManagement.ActionStatuses.Edit";
    public const string Pages_HealthAndSafetyManagement_ActionStatuses_Delete = "Pages.HealthAndSafetyManagement.ActionStatuses.Delete";

    public const string Pages_HealthAndSafetyManagement_FirstAidMedicines = "Pages.HealthAndSafetyManagement.FirstAidMedicines";
    public const string Pages_HealthAndSafetyManagement_FirstAidMedicines_Create = "Pages.HealthAndSafetyManagement.FirstAidMedicines.Create";
    public const string Pages_HealthAndSafetyManagement_FirstAidMedicines_Edit = "Pages.HealthAndSafetyManagement.FirstAidMedicines.Edit";
    public const string Pages_HealthAndSafetyManagement_FirstAidMedicines_Delete = "Pages.HealthAndSafetyManagement.FirstAidMedicines.Delete";

    public const string Pages_HealthAndSafetyManagement_IncidentClaimStatuses = "Pages.HealthAndSafetyManagement.IncidentClaimStatuses";
    public const string Pages_HealthAndSafetyManagement_IncidentClaimStatuses_Create = "Pages.HealthAndSafetyManagement.IncidentClaimStatuses.Create";
    public const string Pages_HealthAndSafetyManagement_IncidentClaimStatuses_Edit = "Pages.HealthAndSafetyManagement.IncidentClaimStatuses.Edit";
    public const string Pages_HealthAndSafetyManagement_IncidentClaimStatuses_Delete = "Pages.HealthAndSafetyManagement.IncidentClaimStatuses.Delete";

    public const string Pages_HealthAndSafetyManagement_IncidentNatures = "Pages.HealthAndSafetyManagement.IncidentNatures";
    public const string Pages_HealthAndSafetyManagement_IncidentNatures_Create = "Pages.HealthAndSafetyManagement.IncidentNatures.Create";
    public const string Pages_HealthAndSafetyManagement_IncidentNatures_Edit = "Pages.HealthAndSafetyManagement.IncidentNatures.Edit";
    public const string Pages_HealthAndSafetyManagement_IncidentNatures_Delete = "Pages.HealthAndSafetyManagement.IncidentNatures.Delete";

    public const string Pages_HealthAndSafetyManagement_IncidentSeverities = "Pages.HealthAndSafetyManagement.IncidentSeverities";
    public const string Pages_HealthAndSafetyManagement_IncidentSeverities_Create = "Pages.HealthAndSafetyManagement.IncidentSeverities.Create";
    public const string Pages_HealthAndSafetyManagement_IncidentSeverities_Edit = "Pages.HealthAndSafetyManagement.IncidentSeverities.Edit";
    public const string Pages_HealthAndSafetyManagement_IncidentSeverities_Delete = "Pages.HealthAndSafetyManagement.IncidentSeverities.Delete";

    public const string Pages_HealthAndSafetyManagement_IncidentStatuses = "Pages.HealthAndSafetyManagement.IncidentStatuses";
    public const string Pages_HealthAndSafetyManagement_IncidentStatuses_Create = "Pages.HealthAndSafetyManagement.IncidentStatuses.Create";
    public const string Pages_HealthAndSafetyManagement_IncidentStatuses_Edit = "Pages.HealthAndSafetyManagement.IncidentStatuses.Edit";
    public const string Pages_HealthAndSafetyManagement_IncidentStatuses_Delete = "Pages.HealthAndSafetyManagement.IncidentStatuses.Delete";

    public const string Pages_HealthAndSafetyManagement_IncidentTypes = "Pages.HealthAndSafetyManagement.IncidentTypes";
    public const string Pages_HealthAndSafetyManagement_IncidentTypes_Create = "Pages.HealthAndSafetyManagement.IncidentTypes.Create";
    public const string Pages_HealthAndSafetyManagement_IncidentTypes_Edit = "Pages.HealthAndSafetyManagement.IncidentTypes.Edit";
    public const string Pages_HealthAndSafetyManagement_IncidentTypes_Delete = "Pages.HealthAndSafetyManagement.IncidentTypes.Delete";

    public const string Pages_HealthAndSafetyManagement_InvestigationRootCauses = "Pages.HealthAndSafetyManagement.InvestigationRootCauses";
    public const string Pages_HealthAndSafetyManagement_InvestigationRootCauses_Create = "Pages.HealthAndSafetyManagement.InvestigationRootCauses.Create";
    public const string Pages_HealthAndSafetyManagement_InvestigationRootCauses_Edit = "Pages.HealthAndSafetyManagement.InvestigationRootCauses.Edit";
    public const string Pages_HealthAndSafetyManagement_InvestigationRootCauses_Delete = "Pages.HealthAndSafetyManagement.InvestigationRootCauses.Delete";

    public const string Pages_HealthAndSafetyManagement_RiskActions = "Pages.HealthAndSafetyManagement.RiskActions";
    public const string Pages_HealthAndSafetyManagement_RiskActions_Create = "Pages.HealthAndSafetyManagement.RiskActions.Create";
    public const string Pages_HealthAndSafetyManagement_RiskActions_Edit = "Pages.HealthAndSafetyManagement.RiskActions.Edit";
    public const string Pages_HealthAndSafetyManagement_RiskActions_Delete = "Pages.HealthAndSafetyManagement.RiskActions.Delete";

    public const string Pages_HealthAndSafetyManagement_RiskImpacts = "Pages.HealthAndSafetyManagement.RiskImpacts";
    public const string Pages_HealthAndSafetyManagement_RiskImpacts_Create = "Pages.HealthAndSafetyManagement.RiskImpacts.Create";
    public const string Pages_HealthAndSafetyManagement_RiskImpacts_Edit = "Pages.HealthAndSafetyManagement.RiskImpacts.Edit";
    public const string Pages_HealthAndSafetyManagement_RiskImpacts_Delete = "Pages.HealthAndSafetyManagement.RiskImpacts.Delete";

    public const string Pages_HealthAndSafetyManagement_RiskPossibilities = "Pages.HealthAndSafetyManagement.RiskPossibilities";
    public const string Pages_HealthAndSafetyManagement_RiskPossibilities_Create = "Pages.HealthAndSafetyManagement.RiskPossibilities.Create";
    public const string Pages_HealthAndSafetyManagement_RiskPossibilities_Edit = "Pages.HealthAndSafetyManagement.RiskPossibilities.Edit";
    public const string Pages_HealthAndSafetyManagement_RiskPossibilities_Delete = "Pages.HealthAndSafetyManagement.RiskPossibilities.Delete";

    public const string Pages_HealthAndSafetyManagement_RiskStatuses = "Pages.HealthAndSafetyManagement.RiskStatuses";
    public const string Pages_HealthAndSafetyManagement_RiskStatuses_Create = "Pages.HealthAndSafetyManagement.RiskStatuses.Create";
    public const string Pages_HealthAndSafetyManagement_RiskStatuses_Edit = "Pages.HealthAndSafetyManagement.RiskStatuses.Edit";
    public const string Pages_HealthAndSafetyManagement_RiskStatuses_Delete = "Pages.HealthAndSafetyManagement.RiskStatuses.Delete";

    public const string Pages_HealthAndSafetyManagement_RiskTypes = "Pages.HealthAndSafetyManagement.RiskTypes";
    public const string Pages_HealthAndSafetyManagement_RiskTypes_Create = "Pages.HealthAndSafetyManagement.RiskTypes.Create";
    public const string Pages_HealthAndSafetyManagement_RiskTypes_Edit = "Pages.HealthAndSafetyManagement.RiskTypes.Edit";
    public const string Pages_HealthAndSafetyManagement_RiskTypes_Delete = "Pages.HealthAndSafetyManagement.RiskTypes.Delete";

    public const string Pages_HealthAndSafetyManagement_SafetyAreaAssets = "Pages.HealthAndSafetyManagement.SafetyAreaAssets";
    public const string Pages_HealthAndSafetyManagement_SafetyAreaAssets_Create = "Pages.HealthAndSafetyManagement.SafetyAreaAssets.Create";
    public const string Pages_HealthAndSafetyManagement_SafetyAreaAssets_Edit = "Pages.HealthAndSafetyManagement.SafetyAreaAssets.Edit";
    public const string Pages_HealthAndSafetyManagement_SafetyAreaAssets_Delete = "Pages.HealthAndSafetyManagement.SafetyAreaAssets.Delete";

    public const string Pages_HealthAndSafetyManagement_ActionRegisters = "Pages.HealthAndSafetyManagement.ActionRegisters";
    public const string Pages_HealthAndSafetyManagement_ActionRegisters_Create = "Pages.HealthAndSafetyManagement.ActionRegisters.Create";
    public const string Pages_HealthAndSafetyManagement_ActionRegisters_Edit = "Pages.HealthAndSafetyManagement.ActionRegisters.Edit";
    public const string Pages_HealthAndSafetyManagement_ActionRegisters_Delete = "Pages.HealthAndSafetyManagement.ActionRegisters.Delete";

    public const string Pages_HealthAndSafetyManagement_FirstAidBoxes = "Pages.HealthAndSafetyManagement.FirstAidBoxes";
    public const string Pages_HealthAndSafetyManagement_FirstAidBoxes_Create = "Pages.HealthAndSafetyManagement.FirstAidBoxes.Create";
    public const string Pages_HealthAndSafetyManagement_FirstAidBoxes_Edit = "Pages.HealthAndSafetyManagement.FirstAidBoxes.Edit";
    public const string Pages_HealthAndSafetyManagement_FirstAidBoxes_Delete = "Pages.HealthAndSafetyManagement.FirstAidBoxes.Delete";

    public const string Pages_HealthAndSafetyManagement_FirstAidBoxDetails = "Pages.HealthAndSafetyManagement.FirstAidBoxDetails";
    public const string Pages_HealthAndSafetyManagement_FirstAidBoxDetails_Create = "Pages.HealthAndSafetyManagement.FirstAidBoxDetails.Create";
    public const string Pages_HealthAndSafetyManagement_FirstAidBoxDetails_Edit = "Pages.HealthAndSafetyManagement.FirstAidBoxDetails.Edit";
    public const string Pages_HealthAndSafetyManagement_FirstAidBoxDetails_Delete = "Pages.HealthAndSafetyManagement.FirstAidBoxDetails.Delete";

    public const string Pages_HealthAndSafetyManagement_IncidentClaims = "Pages.HealthAndSafetyManagement.IncidentClaims";
    public const string Pages_HealthAndSafetyManagement_IncidentClaims_Create = "Pages.HealthAndSafetyManagement.IncidentClaims.Create";
    public const string Pages_HealthAndSafetyManagement_IncidentClaims_Edit = "Pages.HealthAndSafetyManagement.IncidentClaims.Edit";
    public const string Pages_HealthAndSafetyManagement_IncidentClaims_Delete = "Pages.HealthAndSafetyManagement.IncidentClaims.Delete";

    public const string Pages_HealthAndSafetyManagement_IncidentInvestigations = "Pages.HealthAndSafetyManagement.IncidentInvestigations";
    public const string Pages_HealthAndSafetyManagement_IncidentInvestigations_Create = "Pages.HealthAndSafetyManagement.IncidentInvestigations.Create";
    public const string Pages_HealthAndSafetyManagement_IncidentInvestigations_Edit = "Pages.HealthAndSafetyManagement.IncidentInvestigations.Edit";
    public const string Pages_HealthAndSafetyManagement_IncidentInvestigations_Delete = "Pages.HealthAndSafetyManagement.IncidentInvestigations.Delete";

    public const string Pages_HealthAndSafetyManagement_IncidentRegisters = "Pages.HealthAndSafetyManagement.IncidentRegisters";
    public const string Pages_HealthAndSafetyManagement_IncidentRegisters_Create = "Pages.HealthAndSafetyManagement.IncidentRegisters.Create";
    public const string Pages_HealthAndSafetyManagement_IncidentRegisters_Edit = "Pages.HealthAndSafetyManagement.IncidentRegisters.Edit";
    public const string Pages_HealthAndSafetyManagement_IncidentRegisters_Delete = "Pages.HealthAndSafetyManagement.IncidentRegisters.Delete";

    public const string Pages_HealthAndSafetyManagement_RiskRegisters = "Pages.HealthAndSafetyManagement.RiskRegisters";
    public const string Pages_HealthAndSafetyManagement_RiskRegisters_Create = "Pages.HealthAndSafetyManagement.RiskRegisters.Create";
    public const string Pages_HealthAndSafetyManagement_RiskRegisters_Edit = "Pages.HealthAndSafetyManagement.RiskRegisters.Edit";
    public const string Pages_HealthAndSafetyManagement_RiskRegisters_Delete = "Pages.HealthAndSafetyManagement.RiskRegisters.Delete";

    public const string Pages_HealthAndSafetyManagement_SafetyChecklists = "Pages.HealthAndSafetyManagement.SafetyChecklists";
    public const string Pages_HealthAndSafetyManagement_SafetyChecklists_Create = "Pages.HealthAndSafetyManagement.SafetyChecklists.Create";
    public const string Pages_HealthAndSafetyManagement_SafetyChecklists_Edit = "Pages.HealthAndSafetyManagement.SafetyChecklists.Edit";
    public const string Pages_HealthAndSafetyManagement_SafetyChecklists_Delete = "Pages.HealthAndSafetyManagement.SafetyChecklists.Delete";

    public const string Pages_HealthAndSafetyManagement_SiteAudits = "Pages.HealthAndSafetyManagement.SiteAudits";
    public const string Pages_HealthAndSafetyManagement_SiteAudits_Create = "Pages.HealthAndSafetyManagement.SiteAudits.Create";
    public const string Pages_HealthAndSafetyManagement_SiteAudits_Edit = "Pages.HealthAndSafetyManagement.SiteAudits.Edit";
    public const string Pages_HealthAndSafetyManagement_SiteAudits_Delete = "Pages.HealthAndSafetyManagement.SiteAudits.Delete";

    public const string Pages_HealthAndSafetyManagement_SiteAuditDetails = "Pages.HealthAndSafetyManagement.SiteAuditDetails";
    public const string Pages_HealthAndSafetyManagement_SiteAuditDetails_Create = "Pages.HealthAndSafetyManagement.SiteAuditDetails.Create";
    public const string Pages_HealthAndSafetyManagement_SiteAuditDetails_Edit = "Pages.HealthAndSafetyManagement.SiteAuditDetails.Edit";
    public const string Pages_HealthAndSafetyManagement_SiteAuditDetails_Delete = "Pages.HealthAndSafetyManagement.SiteAuditDetails.Delete";

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

    public const string Pages_TrainingManagement_TrainingCourses = "Pages.TrainingManagement.TrainingCourses";
    public const string Pages_TrainingManagement_TrainingCourses_Create = "Pages.TrainingManagement.TrainingCourses.Create";
    public const string Pages_TrainingManagement_TrainingCourses_Edit = "Pages.TrainingManagement.TrainingCourses.Edit";
    public const string Pages_TrainingManagement_TrainingCourses_Delete = "Pages.TrainingManagement.TrainingCourses.Delete";

    public const string Pages_TrainingManagement_TrainingSchedules = "Pages.TrainingManagement.TrainingSchedules";
    public const string Pages_TrainingManagement_TrainingSchedules_Create = "Pages.TrainingManagement.TrainingSchedules.Create";
    public const string Pages_TrainingManagement_TrainingSchedules_Edit = "Pages.TrainingManagement.TrainingSchedules.Edit";
    public const string Pages_TrainingManagement_TrainingSchedules_Delete = "Pages.TrainingManagement.TrainingSchedules.Delete";

    public const string Pages_TrainingManagement_EmployeeTrainings = "Pages.TrainingManagement.EmployeeTrainings";
    public const string Pages_TrainingManagement_EmployeeTrainings_Create = "Pages.TrainingManagement.EmployeeTrainings.Create";
    public const string Pages_TrainingManagement_EmployeeTrainings_Edit = "Pages.TrainingManagement.EmployeeTrainings.Edit";
    public const string Pages_TrainingManagement_EmployeeTrainings_Delete = "Pages.TrainingManagement.EmployeeTrainings.Delete";

    public const string Pages_TrainingManagement_TrainingAttendances = "Pages.TrainingManagement.TrainingAttendances";
    public const string Pages_TrainingManagement_TrainingAttendances_Create = "Pages.TrainingManagement.TrainingAttendances.Create";
    public const string Pages_TrainingManagement_TrainingAttendances_Edit = "Pages.TrainingManagement.TrainingAttendances.Edit";
    public const string Pages_TrainingManagement_TrainingAttendances_Delete = "Pages.TrainingManagement.TrainingAttendances.Delete";

    public const string Pages_JobProfileManagement = "Pages.JobProfileManagement";

    public const string Pages_JobProfileManagement_JobFamilies = "Pages.JobProfileManagement.JobFamilies";
    public const string Pages_JobProfileManagement_JobFamilies_Create = "Pages.JobProfileManagement.JobFamilies.Create";
    public const string Pages_JobProfileManagement_JobFamilies_Edit = "Pages.JobProfileManagement.JobFamilies.Edit";
    public const string Pages_JobProfileManagement_JobFamilies_Delete = "Pages.JobProfileManagement.JobFamilies.Delete";

    public const string Pages_JobProfileManagement_JobFunctions = "Pages.JobProfileManagement.JobFunctions";
    public const string Pages_JobProfileManagement_JobFunctions_Create = "Pages.JobProfileManagement.JobFunctions.Create";
    public const string Pages_JobProfileManagement_JobFunctions_Edit = "Pages.JobProfileManagement.JobFunctions.Edit";
    public const string Pages_JobProfileManagement_JobFunctions_Delete = "Pages.JobProfileManagement.JobFunctions.Delete";

    public const string Pages_JobProfileManagement_JobResponsibilities = "Pages.JobProfileManagement.JobResponsibilities";
    public const string Pages_JobProfileManagement_JobResponsibilities_Create = "Pages.JobProfileManagement.JobResponsibilities.Create";
    public const string Pages_JobProfileManagement_JobResponsibilities_Edit = "Pages.JobProfileManagement.JobResponsibilities.Edit";
    public const string Pages_JobProfileManagement_JobResponsibilities_Delete = "Pages.JobProfileManagement.JobResponsibilities.Delete";

    public const string Pages_JobProfileManagement_EducationCertificates = "Pages.JobProfileManagement.EducationCertificates";
    public const string Pages_JobProfileManagement_EducationCertificates_Create = "Pages.JobProfileManagement.EducationCertificates.Create";
    public const string Pages_JobProfileManagement_EducationCertificates_Edit = "Pages.JobProfileManagement.EducationCertificates.Edit";
    public const string Pages_JobProfileManagement_EducationCertificates_Delete = "Pages.JobProfileManagement.EducationCertificates.Delete";

    public const string Pages_JobProfileManagement_EducationDegrees = "Pages.JobProfileManagement.EducationDegrees";
    public const string Pages_JobProfileManagement_EducationDegrees_Create = "Pages.JobProfileManagement.EducationDegrees.Create";
    public const string Pages_JobProfileManagement_EducationDegrees_Edit = "Pages.JobProfileManagement.EducationDegrees.Edit";
    public const string Pages_JobProfileManagement_EducationDegrees_Delete = "Pages.JobProfileManagement.EducationDegrees.Delete";

    public const string Pages_JobProfileManagement_InterviewQuestions = "Pages.JobProfileManagement.InterviewQuestions";
    public const string Pages_JobProfileManagement_InterviewQuestions_Create = "Pages.JobProfileManagement.InterviewQuestions.Create";
    public const string Pages_JobProfileManagement_InterviewQuestions_Edit = "Pages.JobProfileManagement.InterviewQuestions.Edit";
    public const string Pages_JobProfileManagement_InterviewQuestions_Delete = "Pages.JobProfileManagement.InterviewQuestions.Delete";

    public const string Pages_JobProfileManagement_Skills = "Pages.JobProfileManagement.Skills";
    public const string Pages_JobProfileManagement_Skills_Create = "Pages.JobProfileManagement.Skills.Create";
    public const string Pages_JobProfileManagement_Skills_Edit = "Pages.JobProfileManagement.Skills.Edit";
    public const string Pages_JobProfileManagement_Skills_Delete = "Pages.JobProfileManagement.Skills.Delete";

    public const string Pages_JobProfileManagement_JobDescriptions = "Pages.JobProfileManagement.JobDescriptions";
    public const string Pages_JobProfileManagement_JobDescriptions_Create = "Pages.JobProfileManagement.JobDescriptions.Create";
    public const string Pages_JobProfileManagement_JobDescriptions_Edit = "Pages.JobProfileManagement.JobDescriptions.Edit";
    public const string Pages_JobProfileManagement_JobDescriptions_Delete = "Pages.JobProfileManagement.JobDescriptions.Delete";

    public const string Pages_JobProfileManagement_JobDescriptionCertificates = "Pages.JobProfileManagement.JobDescriptionCertificates";
    public const string Pages_JobProfileManagement_JobDescriptionCertificates_Create = "Pages.JobProfileManagement.JobDescriptionCertificates.Create";
    public const string Pages_JobProfileManagement_JobDescriptionCertificates_Edit = "Pages.JobProfileManagement.JobDescriptionCertificates.Edit";
    public const string Pages_JobProfileManagement_JobDescriptionCertificates_Delete = "Pages.JobProfileManagement.JobDescriptionCertificates.Delete";

    public const string Pages_JobProfileManagement_JobDescriptionDegrees = "Pages.JobProfileManagement.JobDescriptionDegrees";
    public const string Pages_JobProfileManagement_JobDescriptionDegrees_Create = "Pages.JobProfileManagement.JobDescriptionDegrees.Create";
    public const string Pages_JobProfileManagement_JobDescriptionDegrees_Edit = "Pages.JobProfileManagement.JobDescriptionDegrees.Edit";
    public const string Pages_JobProfileManagement_JobDescriptionDegrees_Delete = "Pages.JobProfileManagement.JobDescriptionDegrees.Delete";

    public const string Pages_JobProfileManagement_JobDescriptionInterviewQuestions = "Pages.JobProfileManagement.JobDescriptionInterviewQuestions";
    public const string Pages_JobProfileManagement_JobDescriptionInterviewQuestions_Create = "Pages.JobProfileManagement.JobDescriptionInterviewQuestions.Create";
    public const string Pages_JobProfileManagement_JobDescriptionInterviewQuestions_Edit = "Pages.JobProfileManagement.JobDescriptionInterviewQuestions.Edit";
    public const string Pages_JobProfileManagement_JobDescriptionInterviewQuestions_Delete = "Pages.JobProfileManagement.JobDescriptionInterviewQuestions.Delete";

    public const string Pages_JobProfileManagement_JobDescriptionResponsibilities = "Pages.JobProfileManagement.JobDescriptionResponsibilities";
    public const string Pages_JobProfileManagement_JobDescriptionResponsibilities_Create = "Pages.JobProfileManagement.JobDescriptionResponsibilities.Create";
    public const string Pages_JobProfileManagement_JobDescriptionResponsibilities_Edit = "Pages.JobProfileManagement.JobDescriptionResponsibilities.Edit";
    public const string Pages_JobProfileManagement_JobDescriptionResponsibilities_Delete = "Pages.JobProfileManagement.JobDescriptionResponsibilities.Delete";

    public const string Pages_JobProfileManagement_JobDescriptionSkills = "Pages.JobProfileManagement.JobDescriptionSkills";
    public const string Pages_JobProfileManagement_JobDescriptionSkills_Create = "Pages.JobProfileManagement.JobDescriptionSkills.Create";
    public const string Pages_JobProfileManagement_JobDescriptionSkills_Edit = "Pages.JobProfileManagement.JobDescriptionSkills.Edit";
    public const string Pages_JobProfileManagement_JobDescriptionSkills_Delete = "Pages.JobProfileManagement.JobDescriptionSkills.Delete";

    public const string Pages_JobProfileManagement_JobDescriptionCompetencies = "Pages.JobProfileManagement.JobDescriptionCompetencies";
    public const string Pages_JobProfileManagement_JobDescriptionCompetencies_Create = "Pages.JobProfileManagement.JobDescriptionCompetencies.Create";
    public const string Pages_JobProfileManagement_JobDescriptionCompetencies_Edit = "Pages.JobProfileManagement.JobDescriptionCompetencies.Edit";
    public const string Pages_JobProfileManagement_JobDescriptionCompetencies_Delete = "Pages.JobProfileManagement.JobDescriptionCompetencies.Delete";

    public const string Pages_JobProfileManagement_JobDescriptionAcknowledgements = "Pages.JobProfileManagement.JobDescriptionAcknowledgements";
    public const string Pages_JobProfileManagement_JobDescriptionAcknowledgements_Create = "Pages.JobProfileManagement.JobDescriptionAcknowledgements.Create";
    public const string Pages_JobProfileManagement_JobDescriptionAcknowledgements_Edit = "Pages.JobProfileManagement.JobDescriptionAcknowledgements.Edit";
    public const string Pages_JobProfileManagement_JobDescriptionAcknowledgements_Delete = "Pages.JobProfileManagement.JobDescriptionAcknowledgements.Delete";

    public const string Pages_SystemSettings = "Pages.SystemSettings";

    public const string Pages_SystemSettings_AppraisalSettings = "Pages.SystemSettings.AppraisalSettings";
    public const string Pages_SystemSettings_AppraisalSettings_Create = "Pages.SystemSettings.AppraisalSettings.Create";
    public const string Pages_SystemSettings_AppraisalSettings_Edit = "Pages.SystemSettings.AppraisalSettings.Edit";
    public const string Pages_SystemSettings_AppraisalSettings_Delete = "Pages.SystemSettings.AppraisalSettings.Delete";

    public const string Pages_SystemSettings_DocumentSettings = "Pages.SystemSettings.DocumentSettings";
    public const string Pages_SystemSettings_DocumentSettings_Create = "Pages.SystemSettings.DocumentSettings.Create";
    public const string Pages_SystemSettings_DocumentSettings_Edit = "Pages.SystemSettings.DocumentSettings.Edit";
    public const string Pages_SystemSettings_DocumentSettings_Delete = "Pages.SystemSettings.DocumentSettings.Delete";

    public const string Pages_SystemSettings_FIPostingSettings = "Pages.SystemSettings.FIPostingSettings";
    public const string Pages_SystemSettings_FIPostingSettings_Create = "Pages.SystemSettings.FIPostingSettings.Create";
    public const string Pages_SystemSettings_FIPostingSettings_Edit = "Pages.SystemSettings.FIPostingSettings.Edit";
    public const string Pages_SystemSettings_FIPostingSettings_Delete = "Pages.SystemSettings.FIPostingSettings.Delete";

    public const string Pages_SystemSettings_GeneralSettings = "Pages.SystemSettings.GeneralSettings";
    public const string Pages_SystemSettings_GeneralSettings_Create = "Pages.SystemSettings.GeneralSettings.Create";
    public const string Pages_SystemSettings_GeneralSettings_Edit = "Pages.SystemSettings.GeneralSettings.Edit";
    public const string Pages_SystemSettings_GeneralSettings_Delete = "Pages.SystemSettings.GeneralSettings.Delete";

    public const string Pages_SystemSettings_NotificationSettings = "Pages.SystemSettings.NotificationSettings";
    public const string Pages_SystemSettings_NotificationSettings_Create = "Pages.SystemSettings.NotificationSettings.Create";
    public const string Pages_SystemSettings_NotificationSettings_Edit = "Pages.SystemSettings.NotificationSettings.Edit";
    public const string Pages_SystemSettings_NotificationSettings_Delete = "Pages.SystemSettings.NotificationSettings.Delete";

    public const string Pages_SystemSettings_OrgSettings = "Pages.SystemSettings.OrgSettings";
    public const string Pages_SystemSettings_OrgSettings_Create = "Pages.SystemSettings.OrgSettings.Create";
    public const string Pages_SystemSettings_OrgSettings_Edit = "Pages.SystemSettings.OrgSettings.Edit";
    public const string Pages_SystemSettings_OrgSettings_Delete = "Pages.SystemSettings.OrgSettings.Delete";

    public const string Pages_SystemSettings_PayrollSettings = "Pages.SystemSettings.PayrollSettings";
    public const string Pages_SystemSettings_PayrollSettings_Create = "Pages.SystemSettings.PayrollSettings.Create";
    public const string Pages_SystemSettings_PayrollSettings_Edit = "Pages.SystemSettings.PayrollSettings.Edit";
    public const string Pages_SystemSettings_PayrollSettings_Delete = "Pages.SystemSettings.PayrollSettings.Delete";

    public const string Pages_SystemSettings_PersonnelSettings = "Pages.SystemSettings.PersonnelSettings";
    public const string Pages_SystemSettings_PersonnelSettings_Create = "Pages.SystemSettings.PersonnelSettings.Create";
    public const string Pages_SystemSettings_PersonnelSettings_Edit = "Pages.SystemSettings.PersonnelSettings.Edit";
    public const string Pages_SystemSettings_PersonnelSettings_Delete = "Pages.SystemSettings.PersonnelSettings.Delete";

    public const string Pages_SystemSettings_ProjectCostingSettings = "Pages.SystemSettings.ProjectCostingSettings";
    public const string Pages_SystemSettings_ProjectCostingSettings_Create = "Pages.SystemSettings.ProjectCostingSettings.Create";
    public const string Pages_SystemSettings_ProjectCostingSettings_Edit = "Pages.SystemSettings.ProjectCostingSettings.Edit";
    public const string Pages_SystemSettings_ProjectCostingSettings_Delete = "Pages.SystemSettings.ProjectCostingSettings.Delete";

    public const string Pages_SystemSettings_ReportSettings = "Pages.SystemSettings.ReportSettings";
    public const string Pages_SystemSettings_ReportSettings_Create = "Pages.SystemSettings.ReportSettings.Create";
    public const string Pages_SystemSettings_ReportSettings_Edit = "Pages.SystemSettings.ReportSettings.Edit";
    public const string Pages_SystemSettings_ReportSettings_Delete = "Pages.SystemSettings.ReportSettings.Delete";

    public const string Pages_SystemSettings_SecuritySettings = "Pages.SystemSettings.SecuritySettings";
    public const string Pages_SystemSettings_SecuritySettings_Create = "Pages.SystemSettings.SecuritySettings.Create";
    public const string Pages_SystemSettings_SecuritySettings_Edit = "Pages.SystemSettings.SecuritySettings.Edit";
    public const string Pages_SystemSettings_SecuritySettings_Delete = "Pages.SystemSettings.SecuritySettings.Delete";

    public const string Pages_SystemSettings_SelfServiceSettings = "Pages.SystemSettings.SelfServiceSettings";
    public const string Pages_SystemSettings_SelfServiceSettings_Create = "Pages.SystemSettings.SelfServiceSettings.Create";
    public const string Pages_SystemSettings_SelfServiceSettings_Edit = "Pages.SystemSettings.SelfServiceSettings.Edit";
    public const string Pages_SystemSettings_SelfServiceSettings_Delete = "Pages.SystemSettings.SelfServiceSettings.Delete";

    public const string Pages_SystemSettings_TimeSettings = "Pages.SystemSettings.TimeSettings";
    public const string Pages_SystemSettings_TimeSettings_Create = "Pages.SystemSettings.TimeSettings.Create";
    public const string Pages_SystemSettings_TimeSettings_Edit = "Pages.SystemSettings.TimeSettings.Edit";
    public const string Pages_SystemSettings_TimeSettings_Delete = "Pages.SystemSettings.TimeSettings.Delete";

    public const string Pages_SystemSettings_WorkflowSettings = "Pages.SystemSettings.WorkflowSettings";
    public const string Pages_SystemSettings_WorkflowSettings_Create = "Pages.SystemSettings.WorkflowSettings.Create";
    public const string Pages_SystemSettings_WorkflowSettings_Edit = "Pages.SystemSettings.WorkflowSettings.Edit";
    public const string Pages_SystemSettings_WorkflowSettings_Delete = "Pages.SystemSettings.WorkflowSettings.Delete";

    public const string Pages_SystemSettings_PayrollDrivers = "Pages.SystemSettings.PayrollDrivers";
    public const string Pages_SystemSettings_PayrollDrivers_Create = "Pages.SystemSettings.PayrollDrivers.Create";
    public const string Pages_SystemSettings_PayrollDrivers_Edit = "Pages.SystemSettings.PayrollDrivers.Edit";
    public const string Pages_SystemSettings_PayrollDrivers_Delete = "Pages.SystemSettings.PayrollDrivers.Delete";

    public const string Pages_SystemSecurity_SysUserOrgUnitData = "Pages.SystemSecurity.SysUserOrgUnitData";
    public const string Pages_SystemSecurity_SysUserOrgUnitData_Create = "Pages.SystemSecurity.SysUserOrgUnitData.Create";
    public const string Pages_SystemSecurity_SysUserOrgUnitData_Edit = "Pages.SystemSecurity.SysUserOrgUnitData.Edit";
    public const string Pages_SystemSecurity_SysUserOrgUnitData_Delete = "Pages.SystemSecurity.SysUserOrgUnitData.Delete";

    public const string Pages_SystemSecurity_EmailTemplates = "Pages.SystemSecurity.EmailTemplates";
    public const string Pages_SystemSecurity_EmailTemplates_Create = "Pages.SystemSecurity.EmailTemplates.Create";
    public const string Pages_SystemSecurity_EmailTemplates_Edit = "Pages.SystemSecurity.EmailTemplates.Edit";
    public const string Pages_SystemSecurity_EmailTemplates_Delete = "Pages.SystemSecurity.EmailTemplates.Delete";

    public const string Pages_SystemSecurity_EmailTokens = "Pages.SystemSecurity.EmailTokens";
    public const string Pages_SystemSecurity_EmailTokens_Create = "Pages.SystemSecurity.EmailTokens.Create";
    public const string Pages_SystemSecurity_EmailTokens_Edit = "Pages.SystemSecurity.EmailTokens.Edit";
    public const string Pages_SystemSecurity_EmailTokens_Delete = "Pages.SystemSecurity.EmailTokens.Delete";

    public const string Pages_SystemSecurity_PayslipConfigs = "Pages.SystemSecurity.PayslipConfigs";
    public const string Pages_SystemSecurity_PayslipConfigs_Create = "Pages.SystemSecurity.PayslipConfigs.Create";
    public const string Pages_SystemSecurity_PayslipConfigs_Edit = "Pages.SystemSecurity.PayslipConfigs.Edit";
    public const string Pages_SystemSecurity_PayslipConfigs_Delete = "Pages.SystemSecurity.PayslipConfigs.Delete";

    public const string Pages_SystemSecurity_NotificationMasters = "Pages.SystemSecurity.NotificationMasters";
    public const string Pages_SystemSecurity_NotificationMasters_Create = "Pages.SystemSecurity.NotificationMasters.Create";
    public const string Pages_SystemSecurity_NotificationMasters_Edit = "Pages.SystemSecurity.NotificationMasters.Edit";
    public const string Pages_SystemSecurity_NotificationMasters_Delete = "Pages.SystemSecurity.NotificationMasters.Delete";

    public const string Pages_SystemSecurity_SystemNotifications = "Pages.SystemSecurity.SystemNotifications";
    public const string Pages_SystemSecurity_SystemNotifications_Create = "Pages.SystemSecurity.SystemNotifications.Create";
    public const string Pages_SystemSecurity_SystemNotifications_Edit = "Pages.SystemSecurity.SystemNotifications.Edit";
    public const string Pages_SystemSecurity_SystemNotifications_Delete = "Pages.SystemSecurity.SystemNotifications.Delete";

    public const string Pages_SystemSecurity_SysObjects = "Pages.SystemSecurity.SysObjects";
    public const string Pages_SystemSecurity_SysObjects_Create = "Pages.SystemSecurity.SysObjects.Create";
    public const string Pages_SystemSecurity_SysObjects_Edit = "Pages.SystemSecurity.SysObjects.Edit";
    public const string Pages_SystemSecurity_SysObjects_Delete = "Pages.SystemSecurity.SysObjects.Delete";

    public const string Pages_SystemSecurity_RetroReasonLogs = "Pages.SystemSecurity.RetroReasonLogs";
    public const string Pages_SystemSecurity_RetroReasonLogs_Create = "Pages.SystemSecurity.RetroReasonLogs.Create";
    public const string Pages_SystemSecurity_RetroReasonLogs_Edit = "Pages.SystemSecurity.RetroReasonLogs.Edit";
    public const string Pages_SystemSecurity_RetroReasonLogs_Delete = "Pages.SystemSecurity.RetroReasonLogs.Delete";

    public const string Pages_SystemSecurity_PunchNotificationLogs = "Pages.SystemSecurity.PunchNotificationLogs";
    public const string Pages_SystemSecurity_PunchNotificationLogs_Create = "Pages.SystemSecurity.PunchNotificationLogs.Create";
    public const string Pages_SystemSecurity_PunchNotificationLogs_Edit = "Pages.SystemSecurity.PunchNotificationLogs.Edit";
    public const string Pages_SystemSecurity_PunchNotificationLogs_Delete = "Pages.SystemSecurity.PunchNotificationLogs.Delete";

    public const string Pages_SystemSecurity_TaskEvents = "Pages.SystemSecurity.TaskEvents";
    public const string Pages_SystemSecurity_TaskEvents_Create = "Pages.SystemSecurity.TaskEvents.Create";
    public const string Pages_SystemSecurity_TaskEvents_Edit = "Pages.SystemSecurity.TaskEvents.Edit";
    public const string Pages_SystemSecurity_TaskEvents_Delete = "Pages.SystemSecurity.TaskEvents.Delete";

    public const string Pages_SystemSecurity_EmployeesNotifications = "Pages.SystemSecurity.EmployeesNotifications";
    public const string Pages_SystemSecurity_EmployeesNotifications_Create = "Pages.SystemSecurity.EmployeesNotifications.Create";
    public const string Pages_SystemSecurity_EmployeesNotifications_Edit = "Pages.SystemSecurity.EmployeesNotifications.Edit";
    public const string Pages_SystemSecurity_EmployeesNotifications_Delete = "Pages.SystemSecurity.EmployeesNotifications.Delete";

    public const string Pages_SystemSecurity_EmployeesNotificationLogs = "Pages.SystemSecurity.EmployeesNotificationLogs";
    public const string Pages_SystemSecurity_EmployeesNotificationLogs_Create = "Pages.SystemSecurity.EmployeesNotificationLogs.Create";
    public const string Pages_SystemSecurity_EmployeesNotificationLogs_Edit = "Pages.SystemSecurity.EmployeesNotificationLogs.Edit";
    public const string Pages_SystemSecurity_EmployeesNotificationLogs_Delete = "Pages.SystemSecurity.EmployeesNotificationLogs.Delete";

    public const string Pages_SystemSecurity_BulkUploadLogs = "Pages.SystemSecurity.BulkUploadLogs";
    public const string Pages_SystemSecurity_BulkUploadLogs_Create = "Pages.SystemSecurity.BulkUploadLogs.Create";
    public const string Pages_SystemSecurity_BulkUploadLogs_Edit = "Pages.SystemSecurity.BulkUploadLogs.Edit";
    public const string Pages_SystemSecurity_BulkUploadLogs_Delete = "Pages.SystemSecurity.BulkUploadLogs.Delete";

    public const string Pages_SystemSecurity_ExecutionLogs = "Pages.SystemSecurity.ExecutionLogs";
    public const string Pages_SystemSecurity_ExecutionLogs_Create = "Pages.SystemSecurity.ExecutionLogs.Create";
    public const string Pages_SystemSecurity_ExecutionLogs_Edit = "Pages.SystemSecurity.ExecutionLogs.Edit";
    public const string Pages_SystemSecurity_ExecutionLogs_Delete = "Pages.SystemSecurity.ExecutionLogs.Delete";

    public const string Pages_SystemSecurity_EmailNotificationLogs = "Pages.SystemSecurity.EmailNotificationLogs";
    public const string Pages_SystemSecurity_EmailNotificationLogs_Create = "Pages.SystemSecurity.EmailNotificationLogs.Create";
    public const string Pages_SystemSecurity_EmailNotificationLogs_Edit = "Pages.SystemSecurity.EmailNotificationLogs.Edit";
    public const string Pages_SystemSecurity_EmailNotificationLogs_Delete = "Pages.SystemSecurity.EmailNotificationLogs.Delete";

    public const string Pages_SystemSecurity_SysTenantConfigs = "Pages.SystemSecurity.SysTenantConfigs";
    public const string Pages_SystemSecurity_SysTenantConfigs_Create = "Pages.SystemSecurity.SysTenantConfigs.Create";
    public const string Pages_SystemSecurity_SysTenantConfigs_Edit = "Pages.SystemSecurity.SysTenantConfigs.Edit";
    public const string Pages_SystemSecurity_SysTenantConfigs_Delete = "Pages.SystemSecurity.SysTenantConfigs.Delete";


    /// <summary>
    /// Bulk payslip mailing (legacy uscRptPaySlip). Separated from the on-screen preview because it mails
    /// money documents to every employee in a payroll period.
    /// </summary>

    /// <summary>
    /// Internal dev-only tool that embeds the standalone Stimulsoft Report Designer (aspnet-core/tools/HCMv3.ReportDesigner)
    /// for reviewing the DevExpress-to-Stimulsoft report migration. Not a production reporting feature.
    /// </summary>

    /// <summary>
    /// Native DevExpress Web Report Designer, served from Web.Host (/DXXRD, /DXXRDV) and hosted by the
    /// Angular dx-report-designer component. Edits the ported XtraReport classes in HCMv3.Reports.
    /// </summary>
    public const string Pages_Reports_DesignerDevExpress = "Pages.Reports.DesignerDevExpress";

    /// <summary>
    /// The "Print Reports" launcher: the end-user list of runnable PrintReport catalog entries
    /// (legacy HCM10's Print Reports screen). Deliberately NOT the same permission as
    /// Pages_ReportManagement_PrintReports, which guards the ADMIN CRUD screen for the catalog --
    /// running a report and maintaining the catalog are different jobs.
    /// </summary>
    public const string Pages_Reports_PrintReports = "Pages.Reports.PrintReports";

    /// <summary>
    /// Report Distribution: creating and maintaining report schedules, and viewing their run history.
    /// </summary>
    public const string Pages_Reports_Distribution = "Pages.Reports.Distribution";

    public const string Pages_Reports_Distribution_Create = "Pages.Reports.Distribution.Create";

    public const string Pages_Reports_Distribution_Edit = "Pages.Reports.Distribution.Edit";

    public const string Pages_Reports_Distribution_Delete = "Pages.Reports.Distribution.Delete";

    /// <summary>Triggering a schedule immediately rather than waiting for its next occurrence.</summary>
    public const string Pages_Reports_Distribution_RunNow = "Pages.Reports.Distribution.RunNow";

    /// <summary>
    /// ELEVATED. Allows setting <c>ReportScheduleRecipient.BypassRecipientScope</c>, which sends a slice
    /// WITHOUT filtering it to the recipient's own row-level-security scope.
    ///
    /// Held separately from Create/Edit precisely because it is the one setting in this module that can
    /// hand somebody data they are not otherwise entitled to. It exists for external recipients (an
    /// auditor with no HCMv3 account and therefore no scope), and granting it to ordinary schedule
    /// authors defeats the point of the module's per-recipient scoping.
    /// </summary>
    public const string Pages_Reports_Distribution_BypassRecipientScope = "Pages.Reports.Distribution.BypassRecipientScope";

    //ANALYSIS GRID REPORTS (ported from legacy HCM10.Web/UserControls/Reports/Analysis/**/usc*.ascx)
    //
    //One parent permission for the whole analysis-report family plus ONE CHILD PER REPORT, so a role can
    //be granted individual reports. Every new report in this wave adds exactly one constant here, one
    //CreateChildPermission line in AppAuthorizationProvider, and one localization key.
    public const string Pages_Reports_Analysis = "Pages.Reports.Analysis";

    public const string Pages_Reports_Analysis_EmployeeContract = "Pages.Reports.Analysis.EmployeeContract";

    // PayrollReport grouping.
    public const string Pages_Reports_Analysis_BasicSalaryRegister = "Pages.Reports.Analysis.BasicSalaryRegister";

    public const string Pages_Reports_Analysis_SalaryRegister = "Pages.Reports.Analysis.SalaryRegister";

    public const string Pages_Reports_Analysis_PayrollAnalysisByDept = "Pages.Reports.Analysis.PayrollAnalysisByDept";

    public const string Pages_Reports_Analysis_PayrollAnalysis = "Pages.Reports.Analysis.PayrollAnalysis";

    public const string Pages_Reports_Analysis_PayrollSheet = "Pages.Reports.Analysis.PayrollSheet";

    public const string Pages_Reports_Analysis_PayrollSheetCustom023 = "Pages.Reports.Analysis.PayrollSheetCustom023";

    // Customer-specific Journal Voucher variants (legacy uscRptPayrollJV_Custom0NN). Custom 016 has ~11
    // live cmbType format variants; only "Loan Format" (split BMC/MYB, different column sets) is ported
    // so far -- see PayrollJVCustom016PostingLineLoader remarks for the rest.
    public const string Pages_Reports_Analysis_PayrollJVCustom016LoanFormatBmc = "Pages.Reports.Analysis.PayrollJVCustom016LoanFormatBmc";

    public const string Pages_Reports_Analysis_PayrollJVCustom016LoanFormatMyb = "Pages.Reports.Analysis.PayrollJVCustom016LoanFormatMyb";

    public const string Pages_Reports_Analysis_PayrollJVCustom016MainFormatBmc = "Pages.Reports.Analysis.PayrollJVCustom016MainFormatBmc";

    public const string Pages_Reports_Analysis_PayrollJVCustom016MainFormatMyb = "Pages.Reports.Analysis.PayrollJVCustom016MainFormatMyb";

    public const string Pages_Reports_Analysis_PayrollJVCustom016Detail = "Pages.Reports.Analysis.PayrollJVCustom016Detail";

    public const string Pages_Reports_Analysis_PayrollJVCustom016LeaveProvision = "Pages.Reports.Analysis.PayrollJVCustom016LeaveProvision";

    public const string Pages_Reports_Analysis_PayrollJVCustom016IndemnityProvision = "Pages.Reports.Analysis.PayrollJVCustom016IndemnityProvision";

    public const string Pages_Reports_Analysis_PayrollJVCustom017 = "Pages.Reports.Analysis.PayrollJVCustom017";

    public const string Pages_Reports_Analysis_PayrollJVCustom018 = "Pages.Reports.Analysis.PayrollJVCustom018";

    public const string Pages_Reports_Analysis_PayrollJVCustom019 = "Pages.Reports.Analysis.PayrollJVCustom019";

    public const string Pages_Reports_Analysis_PayrollJVCustom023 = "Pages.Reports.Analysis.PayrollJVCustom023";

    public const string Pages_Reports_Analysis_PayrollJVCustom030 = "Pages.Reports.Analysis.PayrollJVCustom030";

    public const string Pages_Reports_Analysis_PayrollJVCustom035 = "Pages.Reports.Analysis.PayrollJVCustom035";

    public const string Pages_Reports_Analysis_PayrollJVCustom045 = "Pages.Reports.Analysis.PayrollJVCustom045";

    public const string Pages_Reports_Analysis_PayrollJVCustom051 = "Pages.Reports.Analysis.PayrollJVCustom051";

    // Custom 042 (legacy uscRptPayrollJV_Custom042.ascx.cs): 10 live cmbType formats, each its own report.
    public const string Pages_Reports_Analysis_PayrollJVCustom042Salary = "Pages.Reports.Analysis.PayrollJVCustom042Salary";

    public const string Pages_Reports_Analysis_PayrollJVCustom042AdvanceSalary = "Pages.Reports.Analysis.PayrollJVCustom042AdvanceSalary";

    public const string Pages_Reports_Analysis_PayrollJVCustom042AnnualLeaveProvision = "Pages.Reports.Analysis.PayrollJVCustom042AnnualLeaveProvision";

    public const string Pages_Reports_Analysis_PayrollJVCustom042PublicHolidayLeaveProvision = "Pages.Reports.Analysis.PayrollJVCustom042PublicHolidayLeaveProvision";

    public const string Pages_Reports_Analysis_PayrollJVCustom042RamadanTimeBackLeaveProvision = "Pages.Reports.Analysis.PayrollJVCustom042RamadanTimeBackLeaveProvision";

    public const string Pages_Reports_Analysis_PayrollJVCustom042LieuDayLeaveProvision = "Pages.Reports.Analysis.PayrollJVCustom042LieuDayLeaveProvision";

    public const string Pages_Reports_Analysis_PayrollJVCustom042IndemnityProvision = "Pages.Reports.Analysis.PayrollJVCustom042IndemnityProvision";

    public const string Pages_Reports_Analysis_PayrollJVCustom042Overtime = "Pages.Reports.Analysis.PayrollJVCustom042Overtime";

    public const string Pages_Reports_Analysis_PayrollJVCustom042MissingAccounts = "Pages.Reports.Analysis.PayrollJVCustom042MissingAccounts";

    public const string Pages_Reports_Analysis_PayrollJVCustom042EmployeeWiseBreakDown = "Pages.Reports.Analysis.PayrollJVCustom042EmployeeWiseBreakDown";

    public const string Pages_Reports_Analysis_PayrollCTC = "Pages.Reports.Analysis.PayrollCTC";

    public const string Pages_Reports_Analysis_PayrollNetPayContractDiff = "Pages.Reports.Analysis.PayrollNetPayContractDiff";

    public const string Pages_Reports_Analysis_PrePayrollReport = "Pages.Reports.Analysis.PrePayrollReport";

    public const string Pages_Reports_Analysis_SalaryDeduction = "Pages.Reports.Analysis.SalaryDeduction";

    public const string Pages_Reports_Analysis_SalaryForecast = "Pages.Reports.Analysis.SalaryForecast";

    public const string Pages_Reports_Analysis_SalaryRetroResult = "Pages.Reports.Analysis.SalaryRetroResult";

    public const string Pages_Reports_Analysis_ServiceBonus = "Pages.Reports.Analysis.ServiceBonus";

    public const string Pages_Reports_Analysis_StaffNotinPayroll = "Pages.Reports.Analysis.StaffNotinPayroll";

    public const string Pages_Reports_Analysis_StaffOvertime = "Pages.Reports.Analysis.StaffOvertime";

    public const string Pages_Reports_Analysis_StaffTurnover = "Pages.Reports.Analysis.StaffTurnover";

    public const string Pages_Reports_Analysis_EmployeeAction = "Pages.Reports.Analysis.EmployeeAction";

    public const string Pages_Reports_Analysis_EmployeeActionChange = "Pages.Reports.Analysis.EmployeeActionChange";

    public const string Pages_Reports_Analysis_EmployeeAsset = "Pages.Reports.Analysis.EmployeeAsset";

    public const string Pages_Reports_Analysis_EmployeeAttendance = "Pages.Reports.Analysis.EmployeeAttendance";

    public const string Pages_Reports_Analysis_EmployeeBank = "Pages.Reports.Analysis.EmployeeBank";

    public const string Pages_Reports_Analysis_EmployeeBasicPay = "Pages.Reports.Analysis.EmployeeBasicPay";

    public const string Pages_Reports_Analysis_EmployeeBonus = "Pages.Reports.Analysis.EmployeeBonus";

    public const string Pages_Reports_Analysis_EmployeeCTC = "Pages.Reports.Analysis.EmployeeCTC";

    public const string Pages_Reports_Analysis_EmployeeDeduction = "Pages.Reports.Analysis.EmployeeDeduction";

    public const string Pages_Reports_Analysis_EmployeeHistory = "Pages.Reports.Analysis.EmployeeHistory";

    public const string Pages_Reports_Analysis_EmployeeIncentive = "Pages.Reports.Analysis.EmployeeIncentive";

    public const string Pages_Reports_Analysis_EmployeeInfo = "Pages.Reports.Analysis.EmployeeInfo";

    public const string Pages_Reports_Analysis_EmployeeManager = "Pages.Reports.Analysis.EmployeeManager";

    public const string Pages_Reports_Analysis_EmployeePostWise = "Pages.Reports.Analysis.EmployeePostWise";

    public const string Pages_Reports_Analysis_EmployeeRejoin = "Pages.Reports.Analysis.EmployeeRejoin";

    public const string Pages_Reports_Analysis_EmployeeSalaryChange = "Pages.Reports.Analysis.EmployeeSalaryChange";

    public const string Pages_Reports_Analysis_EmployeeSponsor = "Pages.Reports.Analysis.EmployeeSponsor";

    public const string Pages_Reports_Analysis_EmployeeStatus = "Pages.Reports.Analysis.EmployeeStatus";

    public const string Pages_Reports_Analysis_EmployeeTransfer = "Pages.Reports.Analysis.EmployeeTransfer";

    public const string Pages_Reports_Analysis_EmployeeWorkLocation = "Pages.Reports.Analysis.EmployeeWorkLocation";

    public const string Pages_Reports_Analysis_EmployeeWorkflowRole = "Pages.Reports.Analysis.EmployeeWorkflowRole";

    public const string Pages_Reports_Analysis_SalaryBenefits = "Pages.Reports.Analysis.SalaryBenefits";

    public const string Pages_Reports_Analysis_SalaryByNationality = "Pages.Reports.Analysis.SalaryByNationality";

    public const string Pages_Reports_Analysis_StaffHeadcount = "Pages.Reports.Analysis.StaffHeadcount";

    //TimeReport grouping (legacy HCM10.Web/UserControls/Reports/Analysis/TimeReport).
    public const string Pages_Reports_Analysis_MonthlyAttWithTolerance = "Pages.Reports.Analysis.MonthlyAttWithTolerance";

    public const string Pages_Reports_Analysis_MonthlyTimeAttendance = "Pages.Reports.Analysis.MonthlyTimeAttendance";

    public const string Pages_Reports_Analysis_TimeAnalysisByDept = "Pages.Reports.Analysis.TimeAnalysisByDept";

    public const string Pages_Reports_Analysis_TimeRegister = "Pages.Reports.Analysis.TimeRegister";

    public const string Pages_Reports_Analysis_TimeRegisterApproval = "Pages.Reports.Analysis.TimeRegisterApproval";

    public const string Pages_Reports_Analysis_EmployeeDutyPeriod = "Pages.Reports.Analysis.EmployeeDutyPeriod";

    public const string Pages_Reports_Analysis_LateEarlyLog = "Pages.Reports.Analysis.LateEarlyLog";

    public const string Pages_Reports_Analysis_AttendanceLog = "Pages.Reports.Analysis.AttendanceLog";

    public const string Pages_Reports_Analysis_EmpAttendanceLog = "Pages.Reports.Analysis.EmpAttendanceLog";

    public const string Pages_Reports_Analysis_DailyAttendanceSummary = "Pages.Reports.Analysis.DailyAttendanceSummary";

    public const string Pages_Reports_Analysis_DailyManHoursSummary = "Pages.Reports.Analysis.DailyManHoursSummary";

    public const string Pages_Reports_Analysis_LateEarlyLogWithPhoto = "Pages.Reports.Analysis.LateEarlyLogWithPhoto";

    public const string Pages_Reports_Analysis_TimeSchedule = "Pages.Reports.Analysis.TimeSchedule";

    //Per-customer TimeReport variants. Each legacy _Custom0NN control is a SEPARATE implementation with
    //its own column set and arithmetic, so each gets its own permission rather than sharing the base
    //report's -- a role granted the base Late/Early Log must not silently gain the variants.
    public const string Pages_Reports_Analysis_LateEarlyLogCustom040 = "Pages.Reports.Analysis.LateEarlyLogCustom040";

    public const string Pages_Reports_Analysis_LateEarlyLogCustom045 = "Pages.Reports.Analysis.LateEarlyLogCustom045";

    public const string Pages_Reports_Analysis_LateEarlyLogCustom053 = "Pages.Reports.Analysis.LateEarlyLogCustom053";

    public const string Pages_Reports_Analysis_MonthlyAttWithToleranceCustom019 = "Pages.Reports.Analysis.MonthlyAttWithToleranceCustom019";

    public const string Pages_Reports_Analysis_LateEarlyLogCustom019 = "Pages.Reports.Analysis.LateEarlyLogCustom019";

    public const string Pages_Reports_Analysis_LateEarlyLogCustom023 = "Pages.Reports.Analysis.LateEarlyLogCustom023";

    public const string Pages_Reports_Analysis_LateEarlyLogCustom033 = "Pages.Reports.Analysis.LateEarlyLogCustom033";

    public const string Pages_Reports_Analysis_LateEarlyLogCustom036 = "Pages.Reports.Analysis.LateEarlyLogCustom036";

    // Legacy uscRptAsset.ascx (MasterReport) -- Asset master analysis grid report.
    public const string Pages_Reports_Analysis_Asset = "Pages.Reports.Analysis.Asset";

    // Legacy uscRptEmployeeTravel.ascx (TravelReport) -- Employee Travel analysis grid report (Table1 only).
    public const string Pages_Reports_Analysis_EmployeeTravel = "Pages.Reports.Analysis.EmployeeTravel";

    // Status grouping (legacy HCM10.Web/UserControls/Reports/Analysis/Status/usc*.ascx).
    public const string Pages_Reports_Analysis_EmployeeDocMovement = "Pages.Reports.Analysis.EmployeeDocMovement";

    public const string Pages_Reports_Analysis_EmployeeFnFPayment = "Pages.Reports.Analysis.EmployeeFnFPayment";

    public const string Pages_Reports_Analysis_EmployeeNewJoiner = "Pages.Reports.Analysis.EmployeeNewJoiner";

    public const string Pages_Reports_Analysis_EmployeeOnHold = "Pages.Reports.Analysis.EmployeeOnHold";

    public const string Pages_Reports_Analysis_EmployeePassport = "Pages.Reports.Analysis.EmployeePassport";

    public const string Pages_Reports_Analysis_EmployeeProbation = "Pages.Reports.Analysis.EmployeeProbation";

    public const string Pages_Reports_Analysis_EmployeeResidency = "Pages.Reports.Analysis.EmployeeResidency";

    public const string Pages_Reports_Analysis_EmployeeSeparation = "Pages.Reports.Analysis.EmployeeSeparation";

    //Transaction analysis reports (legacy HCM10.Web/UserControls/Reports/Analysis/Transaction).
    public const string Pages_Reports_Analysis_News = "Pages.Reports.Analysis.News";

    public const string Pages_Reports_Analysis_EmployeeAssetInvoice = "Pages.Reports.Analysis.EmployeeAssetInvoice";

    public const string Pages_Reports_Analysis_EmployeeDisciplinary = "Pages.Reports.Analysis.EmployeeDisciplinary";

    public const string Pages_Reports_Analysis_EmployeeDocument = "Pages.Reports.Analysis.EmployeeDocument";

    public const string Pages_Reports_Analysis_EmployeeDocumentCustom033 = "Pages.Reports.Analysis.EmployeeDocumentCustom033";

    public const string Pages_Reports_Analysis_EmployeeHealthReimburse = "Pages.Reports.Analysis.EmployeeHealthReimburse";

    //Organization/Workflow analysis reports (legacy HCM10.Web/UserControls/Reports/Analysis/Organization, /Workflow).
    public const string Pages_Reports_Analysis_OrgUnitTree = "Pages.Reports.Analysis.OrgUnitTree";

    public const string Pages_Reports_Analysis_OrganizationDocument = "Pages.Reports.Analysis.OrganizationDocument";

    public const string Pages_Reports_Analysis_EmployeeRequests = "Pages.Reports.Analysis.EmployeeRequests";

    public const string Pages_Reports_Analysis_LeaveRequestAnalysis = "Pages.Reports.Analysis.LeaveRequestAnalysis";

    //Transaction analysis reports (legacy HCM10.Web/UserControls/Reports/Analysis/Transaction).
    public const string Pages_Reports_Analysis_EmployeeLeaveSalary = "Pages.Reports.Analysis.EmployeeLeaveSalary";

    public const string Pages_Reports_Analysis_EmployeeLoan = "Pages.Reports.Analysis.EmployeeLoan";

    public const string Pages_Reports_Analysis_EmployeeNonPayroll = "Pages.Reports.Analysis.EmployeeNonPayroll";

    public const string Pages_Reports_Analysis_EmployeePayment = "Pages.Reports.Analysis.EmployeePayment";

    public const string Pages_Reports_Analysis_EmployeeSocialSecurity = "Pages.Reports.Analysis.EmployeeSocialSecurity";

    public const string Pages_Reports_Analysis_EmpOvertime = "Pages.Reports.Analysis.EmpOvertime";

    public const string Pages_Reports_Analysis_EmpOvertimeByDay = "Pages.Reports.Analysis.EmpOvertimeByDay";

    public const string Pages_Reports_Analysis_MonthlyPayrollTransaction = "Pages.Reports.Analysis.MonthlyPayrollTransaction";

    // QuotaProvision grid reports (legacy HCM10.Web/UserControls/Reports/Analysis/QuotaProvision/usc*.ascx.cs).
    public const string Pages_Reports_Analysis_AirTicketQuotaBalance = "Pages.Reports.Analysis.AirTicketQuotaBalance";
    public const string Pages_Reports_Analysis_AnnualLeaveSheet = "Pages.Reports.Analysis.AnnualLeaveSheet";
    public const string Pages_Reports_Analysis_EndOfService = "Pages.Reports.Analysis.EndOfService";
    public const string Pages_Reports_Analysis_IndemnityProvision = "Pages.Reports.Analysis.IndemnityProvision";
    public const string Pages_Reports_Analysis_IndemnityProvisionAsOnDate = "Pages.Reports.Analysis.IndemnityProvisionAsOnDate";
    public const string Pages_Reports_Analysis_LeaveProvision = "Pages.Reports.Analysis.LeaveProvision";
    public const string Pages_Reports_Analysis_LeaveQuotaBalance = "Pages.Reports.Analysis.LeaveQuotaBalance";
    public const string Pages_Reports_Analysis_ProvisionChange = "Pages.Reports.Analysis.ProvisionChange";
    public const string Pages_Reports_Analysis_QuotaProvisionAsOnDate = "Pages.Reports.Analysis.QuotaProvisionAsOnDate";
    public const string Pages_Reports_Analysis_YearlyLeaveIndemnityDetailCustom016 = "Pages.Reports.Analysis.YearlyLeaveIndemnityDetailCustom016";
    public const string Pages_Reports_Analysis_YearlyLeaveIndemnityDetailCustom023 = "Pages.Reports.Analysis.YearlyLeaveIndemnityDetailCustom023";

    // PerformanceReport / TravelReport / MasterReport grid reports (legacy
    // HCM10.Web/UserControls/Reports/Analysis/{PerformanceReport,TravelReport,MasterReport}/usc*.ascx.cs).
    public const string Pages_Reports_Analysis_PAEmployee = "Pages.Reports.Analysis.PAEmployee";
    public const string Pages_Reports_Analysis_PAEmployeeScorecard = "Pages.Reports.Analysis.PAEmployeeScorecard";
    public const string Pages_Reports_Analysis_PASummary = "Pages.Reports.Analysis.PASummary";
    public const string Pages_Reports_Analysis_PerformanceAppraisal = "Pages.Reports.Analysis.PerformanceAppraisal";
    public const string Pages_Reports_Analysis_PerformanceAppraisalReviewerSummary = "Pages.Reports.Analysis.PerformanceAppraisalReviewerSummary";
    public const string Pages_Reports_Analysis_PerformanceAppraisalManagerEvaluation = "Pages.Reports.Analysis.PerformanceAppraisalManagerEvaluation";
    // Pages_Reports_Analysis_EmployeeTravel and Pages_Reports_Analysis_Asset already declared above.

    //ADMINISTRATION TOOLS (ported from legacy HCM10.Web/UserControls/Administration/*.ascx.cs)
    //
    //These are DESTRUCTIVE / BULK maintenance utilities, not settings screens. Legacy gated them by a
    //hardcoded username check ("SuperAdmin"/"Admin"); this port replaces that with real permissions.

    /// <summary>Root for the ported legacy Administration utility panels.</summary>
    public const string Pages_Administration_Tools = "Pages.Administration.Tools";

    /// <summary>
    /// Legacy uscPayrollConfigPanel "Generate Payroll Calendar" panel.
    /// </summary>
    public const string Pages_Administration_Tools_GeneratePayrollCalendar = "Pages.Administration.Tools.GeneratePayrollCalendar";

    /// <summary>
    /// Legacy uscPayrollConfigPanel "Delete Salary Register" panel. DESTRUCTIVE: permanently removes
    /// calculated payroll results.
    /// </summary>
    public const string Pages_Administration_Tools_DeleteSalaryRegister = "Pages.Administration.Tools.DeleteSalaryRegister";

    /// <summary>
    /// Legacy uscPayrollConfigPanel "Unlock Salary Register" panel. Clears the payroll lock that
    /// normally prevents recalculation of a closed period.
    /// </summary>
    public const string Pages_Administration_Tools_UnlockSalaryRegister = "Pages.Administration.Tools.UnlockSalaryRegister";

    /// <summary>
    /// Legacy uscPayrollConfigPanel "Fix Forfeit Quota" panel.
    /// </summary>
    public const string Pages_Administration_Tools_FixForfeitQuota = "Pages.Administration.Tools.FixForfeitQuota";

    /// <summary>
    /// Legacy uscPayrollConfigPanel "Fix Annual Leave Provision" / "Fix Indemnity Provision" /
    /// "Fix Total Leave Days" / "Fix Absent Unpaid Days" retro-recalculation panels.
    /// </summary>
    public const string Pages_Administration_Tools_FixPayrollProvisions = "Pages.Administration.Tools.FixPayrollProvisions";

    /// <summary>
    /// Legacy uscSystemConfigPanel "Recalculate ProbationDays" panel.
    /// </summary>
    public const string Pages_Administration_Tools_RecalculateProbation = "Pages.Administration.Tools.RecalculateProbation";

    /// <summary>
    /// Legacy uscSystemConfigPanel "Fetch Time Attendance Missing Data" / "Sync Time Attendance Missing
    /// API Data" panels.
    /// </summary>
    public const string Pages_Administration_Tools_TimeAttendanceResync = "Pages.Administration.Tools.TimeAttendanceResync";

    /// <summary>
    /// Legacy uscWorkFlowConfigPanel "Transfer Leaves" panel: bulk-reassigns every open, not-yet-approved
    /// workflow approval from one manager to another.
    /// </summary>
    public const string Pages_Administration_Tools_TransferApprovals = "Pages.Administration.Tools.TransferApprovals";

    /// <summary>
    /// Legacy uscRandomPassword grid + uscChangePassword "reset by role" panels, and the legacy
    /// uscSystemConfigPanel "Create Users with Default Roles" panel.
    ///
    /// SECURITY-SENSITIVE: every endpoint behind this permission mints new credentials for OTHER users and
    /// e-mails them in plaintext. It is deliberately a standalone permission (NOT implied by
    /// Pages_Administration_Users) so it can be granted to nobody by default.
    /// </summary>
    public const string Pages_Administration_Tools_BulkPasswordReset = "Pages.Administration.Tools.BulkPasswordReset";

    // Legacy uscCopyWorkflowSetup.ascx.cs -- clone a RequestMaster + its RequestDetail steps.
    public const string Pages_Administration_Tools_CopyWorkflowSetup = "Pages.Administration.Tools.CopyWorkflowSetup";

    // Legacy HCM10.Web/UserControls/ImportExport/. Legacy gated these behind the screens' own screen
    // permissions.
    //
    // SECURITY-SENSITIVE: the bulk import engine writes rows into ANY table in the system by reflection and
    // its Purge mode deletes every existing row of the selected entity first. Import and Export are split so
    // "let them pull a data extract" does not also mean "let them overwrite the system of record".
    public const string Pages_ImportExport = "Pages.ImportExport";
    public const string Pages_ImportExport_BulkImportExport = "Pages.ImportExport.BulkImportExport";
    public const string Pages_ImportExport_BulkImportExport_Import = "Pages.ImportExport.BulkImportExport.Import";
    public const string Pages_ImportExport_BulkImportExport_Export = "Pages.ImportExport.BulkImportExport.Export";
    public const string Pages_ImportExport_ProfilePhotoImport = "Pages.ImportExport.ProfilePhotoImport";
    public const string Pages_ImportExport_TimesheetImport = "Pages.ImportExport.TimesheetImport";

    // ProjectReport / Recruitment / AccommodationReport analysis grid reports (legacy
    // HCM10.Web/UserControls/Reports/Analysis/ProjectReport, /Recruitment, /AccommodationReport).
    public const string Pages_Reports_Analysis_ProjectCosting = "Pages.Reports.Analysis.ProjectCosting";
    public const string Pages_Reports_Analysis_ProjectCostingCustom052 = "Pages.Reports.Analysis.ProjectCostingCustom052";
    public const string Pages_Reports_Analysis_ProjectMonthwiseSummary = "Pages.Reports.Analysis.ProjectMonthwiseSummary";
    public const string Pages_Reports_Analysis_ProjectWiseAccrual = "Pages.Reports.Analysis.ProjectWiseAccrual";
    public const string Pages_Reports_Analysis_ApplicantSearch = "Pages.Reports.Analysis.ApplicantSearch";
    public const string Pages_Reports_Analysis_CandidateOnbInit = "Pages.Reports.Analysis.CandidateOnbInit";
    public const string Pages_Reports_Analysis_VacancyStatus = "Pages.Reports.Analysis.VacancyStatus";
    public const string Pages_Reports_Analysis_EmployeeAccommodation = "Pages.Reports.Analysis.EmployeeAccommodation";

    // TrainingReport / SurveyReport / Administration / GovtRelations analysis grid reports (legacy
    // HCM10.Web/UserControls/Reports/Analysis/TrainingReport, /SurveyReport, /Administration, /GovtRelations).
    public const string Pages_Reports_Analysis_EmployeeTraining = "Pages.Reports.Analysis.EmployeeTraining";
    public const string Pages_Reports_Analysis_TrainingCourse = "Pages.Reports.Analysis.TrainingCourse";
    public const string Pages_Reports_Analysis_TrainingSchedule = "Pages.Reports.Analysis.TrainingSchedule";
    public const string Pages_Reports_Analysis_EmployeeSurvey = "Pages.Reports.Analysis.EmployeeSurvey";
    public const string Pages_Reports_Analysis_SurveyStatus = "Pages.Reports.Analysis.SurveyStatus";
    public const string Pages_Reports_Analysis_AuditReport = "Pages.Reports.Analysis.AuditReport";
    public const string Pages_Reports_Analysis_ExecutionLogReport = "Pages.Reports.Analysis.ExecutionLogReport";
    public const string Pages_Reports_Analysis_Sponsor = "Pages.Reports.Analysis.Sponsor";
    public const string Pages_Reports_Analysis_SponsorJobTitleQuota = "Pages.Reports.Analysis.SponsorJobTitleQuota";
}
