using SC_LaborReporting.LaborCategories;
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
        public Guid ProductSeriesId { get; set; }
        public string ProductSeriesCode { get; set; }
        public string ProductSeriesName { get; set; }
        public Guid DepartmentId { get; set; }
        public DateTime ReportDate { get; set; }
        public decimal TotalEffectiveHours { get; set; }
        public decimal TotalOvertimeHours { get; set; }
        public string ProjectCode { get; set; }
        public string ProjectName { get; set; }
        public Guid? ProjectRoleId { get; set; }
        public string ProjectRoleName { get; set; }
        public LaborClass LaborClass { get; set; }
        public Guid LaborCategoryId { get; set; }
        public string LaborCategoryCode { get; set; }
        public Guid? ProjectId { get; set; }
        public decimal Hours { get; set; }
        public decimal Hoursfinance { get; set; }
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

        public Guid ProductSeriesId {  get; set; }
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

    public class LaborReportDailyStatusDto
    {
        public string Date { get; set; }
        public decimal TotalEffectiveHours { get; set; }
        /// <summary>
        /// 审批通过的
        /// </summary>
        public List<Guid> ApprovedDetailIds { get; set; } = new List<Guid>();
        /// <summary>
        /// 待审批的
        /// </summary>
        public List<Guid> PendingDetailIds { get; set; } = new List<Guid>();
        /// <summary>
        /// 退回或撤销的
        /// </summary>
        public List<Guid> RejectedOrWithdrawnDetailIds { get; set; } = new List<Guid>();
    }

    public class SaveDailyLaborReportDto
    {
        public Guid? ReporterId { get; set; }
        public Guid? DepartmentId { get; set; }
        public DateTime ReportDate { get; set; }
        public List<LaborReportDetailItemDto> Details { get; set; } = new List<LaborReportDetailItemDto>();
    }
    public class LaborReportDetailItemDto
    {
        // 如果有 Id，说明是修改；如果没有 Id，说明是前端新添加的行
        public Guid? Id { get; set; }

        public LaborClass LaborClass { get; set; }
        public Guid? ProjectId { get; set; }
        public Guid ProductSeriesId { get; set; }

        public string ProjectCode { get; set; }
        public string ProjectName { get; set; }
        public Guid? ProjectRoleId { get; set; }
        public string ProjectRoleName { get; set; }

        public Guid LaborCategoryId { get; set; }
        public string LaborCategoryCode { get; set; }
        public decimal Hours { get; set; }
        public string Jobresponsibilities { get; set; }
    }

    // 审批入参
    public class ApproveInputDto
    {
        public Guid ReportId { get; set; }
        public Guid DetailId { get; set; }
        public bool IsApproved { get; set; }
        public string Comment { get; set; }
    }
}
