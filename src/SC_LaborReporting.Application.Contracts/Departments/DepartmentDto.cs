using System;
using Volo.Abp.Application.Dtos;

namespace SC_LaborReporting.Departments
{
    public class DepartmentDto : FullAuditedEntityDto<Guid>
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Guid? ParentId { get; set; }
    }
}