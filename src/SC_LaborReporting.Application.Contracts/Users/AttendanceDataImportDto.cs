using MiniExcelLibs.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SC_LaborReporting.Users
{
    public class AttendanceDataImportDto
    {
        [ExcelColumn(Name = "姓名")]
        public string Name { get; set; }

        /// <summary>
        /// 考勤组
        /// </summary>
        [ExcelColumn(Name = "考勤组")]
        public string AttendanceTeam { get; set; }

        /// <summary>
        /// 部门名称
        /// </summary>
        [ExcelColumn(Name = "部门")]        
        public string DepartmentName { get; set; }

        /// <summary>
        /// 工号
        /// </summary>
        [ExcelColumn(Name = "工号")]
        public string EmployeeID { get; set; }

        /// <summary>
        /// 考勤日期
        /// </summary>
        /// 
        [ExcelColumn(Name = "日期")]
        public string AttendanceDate { get; set; }

        /// <summary>
        /// 考勤时间
        /// </summary>
        [ExcelColumn(Name = "考勤时间")]
        public string AttendanceTime { get; set; }

        /// <summary>
        /// 打卡时间
        /// </summary>
        [ExcelColumn(Name = "上班1打卡时间")]
        public string ClockIntime { get; set; }

        /// <summary>
        /// 打卡结果
        /// </summary>
        [ExcelColumn(Name = "上班1打卡结果")]
        public string CheckInResults { get; set; }

        /// <summary>
        /// 打卡结果
        /// </summary>
        [ExcelColumn(Name = "下班1打卡时间")]
        public string Offdutytime { get; set; }
        /// <summary>
        /// 打卡结果
        /// </summary>
        [ExcelColumn(Name = "下班1打卡结果")]
        public string OffdutytimeResults { get; set; }
    }
}
