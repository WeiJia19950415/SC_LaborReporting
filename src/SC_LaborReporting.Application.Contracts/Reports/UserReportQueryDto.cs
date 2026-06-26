using System;

// 查询参数
public class UserReportQueryDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? UserId { get; set; }
}

// 扁平化的明细返回对象
public class UserDailyProjectReportDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; }
    public string DateStr { get; set; } // 格式: yyyy-MM-dd
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; }
    public double TotalHours { get; set; }
    public double TotalFinanceHours { get; set; }
}