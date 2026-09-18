using System;
using System.Collections.Generic;

namespace SC_LaborReporting.Reports
{
    public class LaborReportSummaryQueryDto
    {
        public string StartMonth { get; set; } // 格式: "2025-01"
        public string EndMonth { get; set; }   // 格式: "2025-12"
        public string? Filter { get; set; } = string.Empty;     // 姓名或工号筛选
    }

    public class ReportProjectInfoDto
    {
        public string ProjectCode { get; set; }
        public string ProjectName { get; set; }
    }

    public class LaborReportSummaryDto
    {
        public string Month { get; set; }
        public string JobNumber { get; set; }
        public string DepartmentName { get; set; }
        public string Name { get; set; }

        // 研发活动按项目拆分：ProjectCode -> 耗时
        public Dictionary<string, double> ProjectHours { get; set; } = new Dictionary<string, double>();

        public double ProductionHours { get; set; }
        public double SalesHours { get; set; }
        public double ManagementHours { get; set; }
        public double TotalHours { get; set; }
    }

    public class LaborReportSummaryResultDto
    {
        public List<ReportProjectInfoDto> Projects { get; set; } = new List<ReportProjectInfoDto>();
        public List<LaborReportSummaryDto> Rows { get; set; } = new List<LaborReportSummaryDto>();
    }
}