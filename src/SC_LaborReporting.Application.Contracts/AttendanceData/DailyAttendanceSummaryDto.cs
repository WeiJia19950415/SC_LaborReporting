using System;

namespace SC_LaborReporting.AttendanceData
{
    public class DailyAttendanceSummaryDto
    {
        public string Date { get; set; } // 日期，例如 2025-01-01
        public string FirstPunchTime { get; set; } // 最早打卡时间
        public string LastPunchTime { get; set; } // 最晚打卡时间
        public double DurationHours { get; set; } // 打卡时长(小时)

        public string CheckInResults { get; set; } // 上班打卡结果

        public string OffdutytimeResults { get; set; }  // 下班打卡结果

    }
}