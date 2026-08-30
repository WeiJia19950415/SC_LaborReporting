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
        /// 打卡时间
        /// </summary>
        public string ClockIntime {  get; set; }

        /// <summary>
        /// 打卡结果
        /// </summary>
        public string CheckInResults { get; set; }

        /// <summary>
        /// 打卡地址
        /// </summary>
        public string CheckInAddress { get; set; }
    }
}
