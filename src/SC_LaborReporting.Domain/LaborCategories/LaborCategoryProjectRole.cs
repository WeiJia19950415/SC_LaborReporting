using System;
using Volo.Abp.Domain.Entities;

namespace SC_LaborReporting.LaborCategories
{
    public class LaborCategoryProjectRole : Entity
    {
        public Guid LaborCategoryId { get; set; }
        public Guid ProjectRoleId { get; set; }

        public override object[] GetKeys()
        {
            return new object[] { LaborCategoryId, ProjectRoleId };
        }
    }
}