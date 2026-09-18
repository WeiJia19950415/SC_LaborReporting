using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using SC_LaborReporting.LaborReports; // 确保引入 LaborReportDetail 所在的命名空间
// ... 其他你原有的 using

namespace SC_LaborReporting.Projects
{
    // 【注意】保留你原有的类名和继承关系，通常长这样：
    public class ProjectAppService : CrudAppService<
        Project,
        ProjectDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateProjectDto>, IProjectAppService
    {
        private readonly IRepository<LaborReportDetail, Guid> _laborReportDetailRepository;

        // 构造函数：注入基类需要的 Repository 以及我们需要用到的 LaborReportDetailRepository
        public ProjectAppService(
            IRepository<Project, Guid> repository,
            IRepository<LaborReportDetail, Guid> laborReportDetailRepository)
            : base(repository)
        {
            _laborReportDetailRepository = laborReportDetailRepository;
        }

        // 【重点】使用 override 重写基类的 UpdateAsync 方法
        public override async Task<ProjectDto> UpdateAsync(Guid id, CreateUpdateProjectDto input)
        {
            // 1. 在更新前，先获取数据库中的旧数据
            var oldProject = await Repository.GetAsync(id);

            // 2. 判断名称或编号是否即将发生改变
            bool isInfoChanged = oldProject.Name != input.Name || oldProject.Code != input.Code;

            // 3. 调用基类的 UpdateAsync 方法，完成 Project 自身的更新
            var updatedProjectDto = await base.UpdateAsync(id, input);

            // 4. 如果信息发生了改变，执行同步更新逻辑 (这里使用的是方案一的直接更新，如果用事件总线请换成 publish 代码)
            if (isInfoChanged)
            {
                var details = await _laborReportDetailRepository.GetListAsync(x => x.ProjectId == id);
                if (details.Count > 0)
                {
                    foreach (var detail in details)
                    {
                        // 同步最新的名称和编号
                        detail.ProjectName = input.Name;
                        detail.ProjectCode = input.Code;
                    }
                    // 批量更新冗余字段
                    await _laborReportDetailRepository.UpdateManyAsync(details);
                }
            }

            return updatedProjectDto;
        }
    }
}