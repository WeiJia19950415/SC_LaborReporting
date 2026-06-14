using System;
using System.ComponentModel.DataAnnotations;

namespace SC_LaborReporting.Departments
{
    public class CreateUpdateDepartmentDto
    {
        [Required]
        [MaxLength(128)]
        public string Name { get; set; }
        public string Description { get; set; }
        public Guid? ParentId { get; set; }
    }
}