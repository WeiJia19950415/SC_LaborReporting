using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace SC_LaborReporting.LaborReports
{
    public interface ILaborReportAppService : IApplicationService
    {
        //新增
        Task<Guid> CreateAsync(CreateLaborReportDto input);

        //综合查询 
        Task<List<LaborReportItemDto>> GetListAsync(Guid? departmentId, Guid? reporterId, Guid? projectId, string categoryCode);

        //修改从表
        Task UpdateDetailAsync(Guid reportId, Guid detailId, UpdateLaborReportDetailDto input);

        //删除从表
        Task DeleteDetailAsync(Guid reportId, Guid detailId);

        //审核通过
        Task ApproveAsync(Guid reportId, Guid detailId);

        Task WithdrawAsync(Guid reportId, Guid detailId);
        
    }
}