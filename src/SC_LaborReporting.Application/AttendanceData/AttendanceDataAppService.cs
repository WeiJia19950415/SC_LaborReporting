using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace SC_LaborReporting.AttendanceData
{
    [Authorize] // 只要登录就能查自己的
    public class AttendanceDataAppService : ApplicationService, IAttendanceDataAppService
    {
        private readonly IRepository<SC_LaborReporting.AttendanceDatas.AttendanceData, Guid> _attendanceRepository;

        public AttendanceDataAppService(IRepository<SC_LaborReporting.AttendanceDatas.AttendanceData, Guid> attendanceRepository)
        {
            _attendanceRepository = attendanceRepository;
        }

        /// <summary>
        /// [需求7,8] 获取当前登录用户某个月的考勤汇总
        /// </summary>
        /// <param name="monthStr">格式：2025-01</param>
        public async Task<List<DailyAttendanceSummaryDto>> GetMyMonthlyAttendanceAsync(string monthStr)
        {
            // 在系统中，UserName 与工号(JobNumber)一致
            var name = CurrentUser.Name;

            // 查出该用户当月的所有打卡记录
            var query = await _attendanceRepository.GetQueryableAsync();

            // 过滤条件使用 AttendanceDate
            var monthData = query.Where(x => x.Name == name && x.AttendanceDate.StartsWith(monthStr)).ToList();

            var result = new List<DailyAttendanceSummaryDto>();

            foreach (var data in monthData)
            {
                var dto = new DailyAttendanceSummaryDto
                {
                    Date = data.AttendanceDate,
                    FirstPunchTime = data.ClockIntime,
                    LastPunchTime = data.Offdutytime,
                    CheckInResults = data.CheckInResults,
                    OffdutytimeResults = data.OffdutytimeResults,
                    DurationHours = 0
                };

                // 分别判断上班和下班是否打卡（能否成功解析为时间）
                bool hasFirstPunch = DateTime.TryParse(dto.FirstPunchTime, out var firstTime);
                bool hasLastPunch = DateTime.TryParse(dto.LastPunchTime, out var lastTime);

                if (hasFirstPunch && hasLastPunch)
                {
                    // 两次都打卡了，正常计算时长
                    if (firstTime <= lastTime)
                    {
                        var duration = lastTime - firstTime;

                        // 1、处理 12:00 - 13:00 的扣除逻辑
                        var lunchStart = firstTime.Date.AddHours(12);
                        var lunchEnd = firstTime.Date.AddHours(13);

                        var overlapStart = firstTime > lunchStart ? firstTime : lunchStart;
                        var overlapEnd = lastTime < lunchEnd ? lastTime : lunchEnd;

                        if (overlapStart < overlapEnd)
                        {
                            duration -= (overlapEnd - overlapStart);
                        }

                        // 2、处理 19:00 之后的扣除逻辑
                        var eveningThreshold = lastTime.Date.AddHours(19);
                        if (lastTime > eveningThreshold)
                        {
                            duration -= TimeSpan.FromMinutes(30);
                        }

                        if (duration < TimeSpan.Zero)
                        {
                            duration = TimeSpan.Zero;
                        }

                        // 3、处理小数部分（向下取整到0.5的倍数）
                        dto.DurationHours = Math.Floor(duration.TotalHours * 2) / 2.0;
                    }
                }
                else if (hasFirstPunch || hasLastPunch)
                {
                    // 只有一次打卡记录，另一次缺卡，默认考勤时长为8小时
                    dto.DurationHours = 8.0;
                }
                // 否则（hasFirstPunch 和 hasLastPunch 都为 false），即全天缺卡，保持初始化的 0 即可

                result.Add(dto);
            }

            return result;
        }
    }
}