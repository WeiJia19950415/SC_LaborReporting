using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace SC_LaborReporting.Projects
{
    // 继承 CrudAppService 后，ABP会自动帮你实现完整的增删改查逻辑
    public class ProjectAppService :
        CrudAppService<Project, ProjectDto, Guid, PagedAndSortedResultRequestDto, CreateUpdateProjectDto>,
        IProjectAppService
    {
        public ProjectAppService(IRepository<Project, Guid> repository)
            : base(repository)
        {
        }
    }
}