using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using HCMv3.PersonnelAdministration.Services;

namespace HCMv3.TimeManagement.Services;

/// <inheritdoc cref="IPunchDutyResolver"/>
public class PunchDutyResolver : HCMv3DomainServiceBase, IPunchDutyResolver
{
    private readonly IEmployeeAbsenceCalculationService _dutyCalculationService;
    private readonly IRepository<TransType> _transTypeRepository;

    public PunchDutyResolver(
        IEmployeeAbsenceCalculationService dutyCalculationService,
        IRepository<TransType> transTypeRepository)
    {
        _dutyCalculationService = dutyCalculationService;
        _transTypeRepository = transTypeRepository;
    }

    public async Task<Duty> TryResolveDutyAsync(int employeeId, DateTime date)
    {
        try
        {
            return await _dutyCalculationService.GetEmployeeDutyAsync(employeeId, date.Date);
        }
        catch (InvalidOperationException)
        {
            // GetEmployeeDutyAsync (and GetEmployeeDutyPeriodAsync beneath it) reports "no employee / no
            // duty period / no duty" by throwing InvalidOperationException. For leave calculation that is a
            // fault; here it is simply the answer, and the caller turns it into a refusal the employee can
            // read. Only this one exception type is swallowed on purpose: the roster path signals a broken
            // duty-period configuration with a plain Exception, and that genuinely is a fault somebody has
            // to fix rather than something to report as "not rostered".
            return null;
        }
    }

    public async Task<bool> IsClockInAsync(int transTypeId)
    {
        var code = await Task.FromResult(_transTypeRepository.GetAll()
            .Where(t => t.Id == transTypeId)
            .Select(t => t.Code)
            .FirstOrDefault());

        return string.Equals(code?.Trim(), TransTypeCodes.ClockIn, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<TimeSpan?> ResolveDefaultPunchTimeAsync(int employeeId, int transTypeId, DateTime date)
    {
        var duty = await TryResolveDutyAsync(employeeId, date);
        if (duty == null)
        {
            return null;
        }

        var isClockIn = await IsClockInAsync(transTypeId);

        return isClockIn ? duty.StartTime : duty.EndTime;
    }
}
