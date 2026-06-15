using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace SC_LaborReporting.Projects
{
    public class ProjectDto : AuditedEntityDto<Guid>
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public Guid ManagerId { get; set; }
    }

    public class CreateUpdateProjectDto
    {
        [Required]
        [MaxLength(128)]
        public string Name { get; set; }

        [Required]
        [MaxLength(64)]
        public string Code { get; set; }

        [Required]
        public Guid ManagerId { get; set; }
    }
}