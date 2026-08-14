using System;
using Volo.Abp.Application.Dtos;

// 查询参数 DTO (继承 ABP 标准分页 DTO)
public class GetApprovalHistoryInput : PagedAndSortedResultRequestDto
{
    public Guid? ReporterId { get; set; }
    public Guid? DepartmentId { get; set; }
}

// 历史记录返回 DTO
public class ApprovalHistoryDto
{
    public Guid DetailId { get; set; }
    public DateTime ReportDate { get; set; }
    public Guid ReporterId { get; set; }
    public string ReporterName { get; set; }
    public Guid? DepartmentId { get; set; }
    public string DepartmentName { get; set; }
    public string ProjectName { get; set; }
    public string LaborCategoryCode { get; set; }
    public decimal Hours { get; set; }
    public string Jobresponsibilities { get; set; }

    // 审批状态: 3=通过, 1/2=驳回/退回
    public int Status { get; set; }
    public DateTime ApprovalTime { get; set; }
    public string ApprovalComment { get; set; }
}