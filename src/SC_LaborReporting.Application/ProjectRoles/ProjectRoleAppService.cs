using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace SC_LaborReporting.ProjectRoles
{
    public class ProjectRoleAppService :
        CrudAppService<ProjectRole, ProjectRoleDto, Guid, PagedAndSortedResultRequestDto, CreateUpdateProjectRoleDto>,
        IProjectRoleAppService
    {
        public ProjectRoleAppService(IRepository<ProjectRole, Guid> repository) : base(repository)
        {
        }
    }
}