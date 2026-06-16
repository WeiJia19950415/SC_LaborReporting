using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace SC_LaborReporting.ProjectRoles
{
    public interface IProjectRoleAppService :
        ICrudAppService<ProjectRoleDto, Guid, PagedAndSortedResultRequestDto, CreateUpdateProjectRoleDto>
    {
    }
}