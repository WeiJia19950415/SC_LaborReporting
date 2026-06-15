using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace SC_LaborReporting.Projects
{
    // ICrudAppService 会自动为你生成标准的增删改查 API 接口规范
    public interface IProjectAppService :
        ICrudAppService<ProjectDto, Guid, PagedAndSortedResultRequestDto, CreateUpdateProjectDto>
    {

    }
}