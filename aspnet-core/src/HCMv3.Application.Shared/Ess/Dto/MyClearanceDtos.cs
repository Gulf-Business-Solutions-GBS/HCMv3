using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;

namespace HCMv3.Ess.Dto;

/// <summary>An employee's own exit clearance, as their self-service list shows it.</summary>
public class MyClearanceDto : MyRequestDtoBase
{
    public int ClearanceRequestTypeId { get; set; }

    public string ClearanceRequestTypeNameEn { get; set; }

    public string ClearanceRequestTypeNameAr { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string Comments { get; set; }
}

/// <summary>
/// A clearance type the employee may actually choose. Only the types flagged
/// <c>IncludeInWorkflow</c> appear here -- the rest exist for reporting or for historical imports and would
/// route nowhere.
/// </summary>
public class ClearanceTypeLookupDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }
}

public class RequestClearanceInput
{
    /// <summary>Which checklist applies -- and the workflow sub-type, so different exits can route
    /// differently.</summary>
    [Required]
    public int ClearanceRequestTypeId { get; set; }

    public DateTime StartDate { get; set; }

    /// <summary>Last working day the clearance covers. The handler refuses an end date before the start.</summary>
    public DateTime EndDate { get; set; }

    [StringLength(1000)]
    public string Comments { get; set; }
}

/// <summary>
/// One checklist line as the signed-in approver sees it. The list they get back contains their department's
/// lines and nothing else -- see <c>IClearanceChecklistService</c> for why that is a rendering rule rather
/// than a routing one.
/// </summary>
public class ClearanceChecklistLineDto
{
    public int ClearanceRequestFieldId { get; set; }

    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    /// <summary>Null until this line has been answered.</summary>
    public string Value { get; set; }

    public int? AnsweredByEmployeeId { get; set; }
}

/// <summary>The approver's part of a clearance form.</summary>
public class ClearanceChecklistDto
{
    public int EmployeeClearanceId { get; set; }

    /// <summary>
    /// Whether answers can be recorded right now: true only while the clearance is open at a step configured
    /// <c>Parallel</c>. False makes the screen read-only rather than letting somebody type answers the save
    /// call would then refuse.
    /// </summary>
    public bool CanRecordAnswers { get; set; }

    public List<ClearanceChecklistLineDto> Lines { get; set; } = new List<ClearanceChecklistLineDto>();
}

public class ClearanceChecklistAnswerInput
{
    public int ClearanceRequestFieldId { get; set; }

    [Required]
    [StringLength(1000)]
    public string Value { get; set; }
}

public class SaveClearanceChecklistInput
{
    public int EmployeeClearanceId { get; set; }

    public List<ClearanceChecklistAnswerInput> Answers { get; set; } = new List<ClearanceChecklistAnswerInput>();
}
