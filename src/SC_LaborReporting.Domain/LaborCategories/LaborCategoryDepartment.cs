using System;
using Volo.Abp.Domain.Entities;

namespace SC_LaborReporting.LaborCategories;

public class LaborCategoryDepartment : Entity<Guid>
{
    public Guid LaborCategoryId { get; set; }
    public Guid DepartmentId { get; set; }

    protected LaborCategoryDepartment() { }

    public LaborCategoryDepartment(Guid id, Guid laborCategoryId, Guid departmentId)
        : base(id) 
    {
        LaborCategoryId = laborCategoryId;
        DepartmentId = departmentId;
    }
}