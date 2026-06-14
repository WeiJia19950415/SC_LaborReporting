using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace SC_LaborReporting.Departments
{
    public interface IDepartmentAppService :
        ICrudAppService<
            DepartmentDto,
            Guid,
            PagedAndSortedResultRequestDto,
            CreateUpdateDepartmentDto>
    {
    }
}