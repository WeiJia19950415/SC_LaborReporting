using System;
using Volo.Abp.Domain.Entities;

namespace SC_LaborReporting.AttendanceDatas
{
    public class AttendanceData: Entity<Guid>
    {
        public string Name { get; set; }

        /// <summary>
        /// 考勤组
        /// </summary>
        public string AttendanceTeam {  get; set; }

        /// <summary>
        /// 部门名称
        /// </summary>
        public string DepartmentName { get; set; }

        /// <summary>
        /// 工号
        /// </summary>
        public string EmployeeID { get; set; }

        /// <summary>
        /// 考勤日期
        /// </summary>
        public  string AttendanceDate { get; set; }

        /// <summary>
        /// 考勤时间
        /// </summary>
        public string AttendanceTime { get; set; }

        /// <summary>
        /// 上班打卡时间
        /// </summary>
        public string ClockIntime {  get; set; }

        /// <summary>
        /// 上班打卡结果
        /// </summary>
        public string CheckInResults { get; set; }

        /// <summary>
        /// 下班打卡时间
        /// </summary>
        public string Offdutytime { get; set; }

        /// <summary>
        /// 下班打卡结果
        /// </summary>
        public string OffdutytimeResults { get; set; }

        public AttendanceData(Guid id, string name, string attendanceTeam, string departmentName, string employeeID, string attendanceDate, string attendanceTime, string clockIntime, string checkInResults, string offdutytime, string offdutytimeResults) : base(id)
        {
            // 使用 ?? string.Empty 判断，如果是 null 则赋值为空字符串 ""
            Name = name ?? string.Empty;
            AttendanceTeam = attendanceTeam ?? string.Empty;
            DepartmentName = departmentName ?? string.Empty;
            EmployeeID = employeeID ?? string.Empty;
            AttendanceDate = attendanceDate ?? string.Empty;
            AttendanceTime = attendanceTime ?? string.Empty;
            ClockIntime = clockIntime ?? string.Empty;
            CheckInResults = checkInResults ?? string.Empty;
            Offdutytime = offdutytime ?? string.Empty;
            OffdutytimeResults = offdutytimeResults ?? string.Empty;
        }
    }
}
