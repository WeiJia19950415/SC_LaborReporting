using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace SC_LaborReporting.LaborCategories;

public class LaborCategoryDto : EntityDto<Guid>
{
    public LaborType LaborType { get; set; }
    public LaborClass LaborClass { get; set; }
    public string Name { get; set; }
    public Guid? ParentId { get; set; }
    public string Code { get; set; }
    public string Remark { get; set; }
    public Guid[] DepartmentIds { get; set; }
    public string[] DepartmentFullNames { get; set; } // 用于前端展示部门全称
    public Guid[] ProjectRoleIds { get; set; }
}

public class CreateUpdateLaborCategoryInput
{
    [Required] public LaborType LaborType { get; set; }
    [Required] public LaborClass LaborClass { get; set; }
    [Required] public string Name { get; set; }
    public Guid? ParentId { get; set; }
    public string Remark { get; set; }

    public Guid[] DepartmentIds { get; set; }
    public Guid[] ProjectRoleIds { get; set; }
}

