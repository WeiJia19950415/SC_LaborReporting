using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace SC_LaborReporting.Departments
{
    public class DepartmentAppService :
        CrudAppService<
            Department,
            DepartmentDto,
            Guid,
            PagedAndSortedResultRequestDto,
            CreateUpdateDepartmentDto>,
        IDepartmentAppService
    {
        public DepartmentAppService(IRepository<Department, Guid> repository)
            : base(repository)
        {
        }

        public override async Task DeleteAsync(Guid id)
        {
            // 检查当前部门是否被其他部门作为 ParentId 引用
            var hasChildren = await Repository.AnyAsync(x => x.ParentId == id);

            if (hasChildren)
            {
                // 如果有子部门，抛出友好的业务异常，拦截删除操作
                throw new UserFriendlyException("该部门下存在子部门，无法直接删除！请先删除或转移子部门。");
            }
            await base.DeleteAsync(id);
        }
    }
}