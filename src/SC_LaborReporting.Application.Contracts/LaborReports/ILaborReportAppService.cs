using SC_LaborReporting.LaborCategories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Services;

namespace SC_LaborReporting.LaborReports
{
    public interface ILaborReportAppService : IApplicationService
    {
        [HttpPost]
        Task<List<LaborReportItemDto>> GetDetailsByIdsAsync(List<Guid> detailIds);
        //新增
        Task SaveDailyReportAsync(SaveDailyLaborReportDto input);

        //综合查询 
        Task<List<LaborReportItemDto>> GetListAsync(Guid? departmentId, Guid? reporterId, Guid? projectId, string categoryCode);

        //修改从表
        Task UpdateDetailAsync(Guid reportId, Guid detailId, UpdateLaborReportDetailDto input);

        //删除从表
        Task DeleteDetailAsync(Guid reportId, Guid detailId);

        //审核通过
        Task ApproveAsync(ApproveInputDto input);

        Task WithdrawAsync(Guid reportId, Guid detailId);

        Task<List<LaborReportDailyStatusDto>> GetCalendarStatusAsync(DateTime startDate, DateTime endDate);

        // 查询当前登录人待审批的工时明细
        Task<List<LaborReportItemDto>> GetPendingApprovalsAsync(Guid? reporterId, Guid? departmentId, Guid? projectId);
    }
}