using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace SC_LaborReporting.ProjectRoles
{
    public class ProjectRole : FullAuditedAggregateRoot<Guid>
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }

        // 👇 🌟 【关键修改】：将 protected 改为 public
        public ProjectRole() { }

        public ProjectRole(Guid id, string name, string code, string description = null) : base(id)
        {
            Name = name;
            Code = code;
            Description = description;
        }
    }
}