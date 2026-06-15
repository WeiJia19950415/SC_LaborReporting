using System;
using System.Collections.Generic;
using System.Text;

namespace SC_LaborReporting.LaborReports
{
    // 查询返回的扁平化Dto（从表主表合并视角）
    public class LaborReportItemDto
    {
        public Guid DetailId { get; set; }
        public Guid ReportId { get; set; }
        public Guid ReporterId { get; set; }
        public Guid DepartmentId { get; set; }
        public DateTime ReportDate { get; set; }
        public decimal TotalEffectiveHours { get; set; }
        public decimal TotalOvertimeHours { get; set; }

        public Guid LaborCategoryId { get; set; }
        public string LaborCategoryCode { get; set; }
        public Guid? ProjectId { get; set; }
        public decimal Hours { get; set; }
        public string Jobresponsibilities { get; set; }
        public LaborReportStatus Status { get; set; }
    }

    public class CreateLaborReportDto
    {
        public Guid ReporterId { get; set; }
        public Guid DepartmentId { get; set; }
        public DateTime ReportDate { get; set; }
        public List<CreateLaborReportDetailDto> Details { get; set; }
    }

    public class CreateLaborReportDetailDto
    {
        public Guid LaborCategoryId { get; set; }
        public string LaborCategoryCode { get; set; }
        public Guid? ProjectId { get; set; }
        public decimal Hours { get; set; }
        public string Jobresponsibilities { get; set; }
    }

    public class UpdateLaborReportDetailDto
    {
        /// <summary>
        /// 工时类型Id
        /// </summary>
        public Guid LaborCategoryId { get; set; }
        /// <summary>
        /// 工时类型代码
        /// </summary>
        public string LaborCategoryCode { get; set; }
        /// <summary>
        /// 项目Id
        /// </summary>
        public Guid? ProjectId { get; set; }
        /// <summary>
        /// 工时
        /// </summary>
        public decimal Hours { get; set; }
        /// <summary>
        /// 工作内容
        /// </summary>
        public string Jobresponsibilities { get; set; }
    }
}
