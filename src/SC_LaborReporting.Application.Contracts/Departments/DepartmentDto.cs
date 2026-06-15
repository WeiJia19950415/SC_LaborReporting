using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace SC_LaborReporting.Departments;

public class DepartmentDto : EntityDto<Guid>
{
    public Guid? ParentId { get; set; }
    public string Code { get; set; }
    public string DisplayName { get; set; }

    // ======== 新增字段 ========
    public string FullName { get; set; } // 部门全称
    public DepartmentType DepartmentType { get; set; } // 部门类型
    public Guid? ManagerId { get; set; } // 负责人ID
    public string ManagerName { get; set; } // 负责人姓名
}

public class CreateDepartmentInput
{
    public Guid? ParentId { get; set; }

    [Required]
    [RegularExpression(@"^[a-zA-Z0-9\u4e00-\u9fa5]+$", ErrorMessage = "部门名称只能包含中文、英文、数字，不允许出现符号。")]
    public string DisplayName { get; set; }

    public DepartmentType DepartmentType { get; set; }
    public Guid? ManagerId { get; set; }
}

public class UpdateDepartmentInput
{
    [Required]
    [RegularExpression(@"^[a-zA-Z0-9\u4e00-\u9fa5]+$", ErrorMessage = "部门名称只能包含中文、英文、数字，不允许出现符号。")]
    public string DisplayName { get; set; }
    public DepartmentType DepartmentType { get; set; }
    public Guid? ManagerId { get; set; }
}