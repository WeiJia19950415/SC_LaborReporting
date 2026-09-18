using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Content;

namespace SC_LaborReporting.AttendanceData
{
    public interface IAttendanceDataAppService : IApplicationService
    {
        Task<List<DailyAttendanceSummaryDto>> GetMyMonthlyAttendanceAsync(string monthStr);
    }
}