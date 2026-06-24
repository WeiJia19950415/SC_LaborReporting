using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace SC_LaborReporting.Reports
{
    public interface IReportAppService : IApplicationService
    {
        // 获取本月汇总数据
        Task<LaborReportMonthlySummaryDto> GetMonthlySummaryAsync(DateTime startDate, DateTime endDate);
        Task<List<ChartDataDto>> GetDepartmentChartAsync(DepartmentReportQueryDto input);
        Task<PagedResultDto<DepartmentReportDetailDto>> GetDepartmentTableAsync(DepartmentReportQueryDto input);

        [HttpGet]
        Task<ExportFileDto> ExportDepartmentTableAsync(DepartmentReportQueryDto input);
    }
}