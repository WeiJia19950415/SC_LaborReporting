using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace SC_LaborReporting.LaborCategories
{
    public interface ILaborCategoryAppService : IApplicationService
    {
        Task<ListResultDto<LaborCategoryDto>> GetListAsync();
        Task<LaborCategoryDto> CreateAsync(CreateUpdateLaborCategoryInput input);
        Task<LaborCategoryDto> UpdateAsync(Guid id, CreateUpdateLaborCategoryInput input);
        Task DeleteAsync(Guid id);
    }
}
