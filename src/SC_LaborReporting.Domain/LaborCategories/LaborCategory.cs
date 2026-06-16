using SC_LaborReporting.ProjectRoles;
using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;

namespace SC_LaborReporting.LaborCategories;

public class LaborCategory : FullAuditedAggregateRoot<Guid>
{
    public LaborType LaborType { get; set; }
    public LaborClass LaborClass { get; set; }
    public string Name { get; set; }
    public Guid? ParentId { get; set; }
    public string Code { get; set; }
    public string Remark { get; set; }

    // ⭐ 新增：一对多关联关系集合
    public virtual ICollection<LaborCategoryDepartment> Departments { get; protected set; }
    public virtual ICollection<LaborCategoryProjectRole> ProjectRoles { get; protected set; }

    public LaborCategory()
    {
        Departments = new List<LaborCategoryDepartment>();
        ProjectRoles = new List<LaborCategoryProjectRole>();
    }
}