using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HCMv3.MultiTenancy.HostDashboard.Dto;

namespace HCMv3.MultiTenancy.HostDashboard;

public interface IIncomeStatisticsService
{
    Task<List<IncomeStastistic>> GetIncomeStatisticsData(DateTime startDate, DateTime endDate,
        ChartDateInterval dateInterval);
}
