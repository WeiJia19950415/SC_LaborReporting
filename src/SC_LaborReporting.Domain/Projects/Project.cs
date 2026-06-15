using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace SC_LaborReporting.Projects
{
    // 继承 FullAuditedAggregateRoot 包含创建、修改、删除的审计字段
    public class Project : FullAuditedAggregateRoot<Guid>
    {
        /// <summary>
        /// 项目名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 项目编号 (用户手动填写)
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// 项目负责人 ID (关联 AbpUsers 表的 Id)
        /// </summary>
        public Guid ManagerId { get; set; }

        public Project() { }

        public Project(Guid id, string name, string code, Guid managerId)
            : base(id)
        {
            Name = name;
            Code = code;
            ManagerId = managerId;
        }
    }
}