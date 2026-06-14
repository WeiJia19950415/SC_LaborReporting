using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities.Auditing;

namespace SC_LaborReporting.Departments
{
    // 继承 FullAuditedAggregateRoot，ABP 会自动为你处理逻辑删除 (IsDeleted 字段)
    public class Department : FullAuditedAggregateRoot<Guid>
    {
        [Required]
        public string Name { get; set; } // 部门名称
        public string Description { get; set; } // 部门描述
        public Guid? ParentId { get; set; } // 父级ID，为空代表是顶级部门
        public virtual Department Parent { get; set; }
        public virtual ICollection<Department> Children { get; set; }

        // 构造函数初始化集合
        public Department()
        {
            Children = new List<Department>();
        }

        public Department(Guid id, string name, string code, Guid? parentId = null) : base(id)
        {
            Name = name;
            ParentId = parentId;
            Children = new List<Department>();
        }
    }
}