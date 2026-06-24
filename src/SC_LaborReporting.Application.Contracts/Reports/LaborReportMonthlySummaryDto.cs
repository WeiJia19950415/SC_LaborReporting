using System;
using System.Collections.Generic;

namespace SC_LaborReporting.Reports
{
    public class LaborReportMonthlySummaryDto
    {
        // 已报工时
        public decimal TotalReportedHours { get; set; }
        // 过审工时
        public decimal TotalApprovedHours { get; set; }
        // 待审工时
        public decimal TotalPendingHours { get; set; }
        // 加班工时
        public decimal TotalOvertimeHours { get; set; }

        public List<ReportItemDetailDto> Details { get; set; } = new List<ReportItemDetailDto>();
    }

    public class ReportItemDetailDto
    {
        public Guid DetailId { get; set; }
        public Guid ReportId { get; set; }
        public Guid ReporterId { get; set; }
        public Guid DepartmentId { get; set; }
        public DateTime ReportDate { get; set; }
        public Guid LaborCategoryId { get; set; }
        public string LaborCategoryCode { get; set; }
        // 新增属性：任务分类全称名称
        public string LaborCategoryFullName { get; set; }
        public Guid? ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string ProjectCode { get; set; }
        public Guid? ProjectRoleId { get; set; }
        public string ProjectRoleName { get; set; }
        public decimal Hours { get; set; }
        public int Status { get; set; }
        public string Jobresponsibilities { get; set; }
    }
}