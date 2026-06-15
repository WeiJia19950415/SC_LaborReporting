using System;
using Volo.Abp.Domain.Entities;

namespace SC_LaborReporting.LaborCategories;

public class LaborCategoryRole : Entity<Guid>
{
    public Guid LaborCategoryId { get; set; }
    public string RoleName { get; set; }

    protected LaborCategoryRole() { }
    public LaborCategoryRole(Guid id, Guid laborCategoryId, string roleName)
        : base(id)
    {
        LaborCategoryId = laborCategoryId;
        RoleName = roleName;
    }
}