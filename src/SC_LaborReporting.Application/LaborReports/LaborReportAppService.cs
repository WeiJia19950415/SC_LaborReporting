using Microsoft.EntityFrameworkCore;
using SC_LaborReporting.LaborCategories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace SC_LaborReporting.LaborReports
{
    public class LaborReportAppService : SC_LaborReportingAppService, ILaborReportAppService
    {
        private readonly IRepository<LaborReport, Guid> _reportRepository;
        private readonly IRepository<LaborReportDetail, Guid> _detailRepository;
        private readonly IRepository<LaborCategory, Guid> _laborCategoryRepository;

        public LaborReportAppService(
            IRepository<LaborReport, Guid> reportRepository,
            IRepository<LaborReportDetail, Guid> detailRepository,
            IRepository<LaborCategory, Guid> laborCategoryRepository)
        {
            _reportRepository = reportRepository;
            _detailRepository = detailRepository;
            _laborCategoryRepository = laborCategoryRepository;
        }

        // 新增接口
        public async Task<Guid> CreateAsync(CreateLaborReportDto input)
        {
            var report = new LaborReport(GuidGenerator.Create(), input.ReporterId, input.DepartmentId, input.ReportDate);

            foreach (var detailDto in input.Details)
            {
                // 校验规则：如果工时分类的LaborClass = 1，则关联项目必填
                var category = await _laborCategoryRepository.GetAsync(detailDto.LaborCategoryId);
                if (category.LaborClass == LaborClass.Project && !detailDto.ProjectId.HasValue)
                {
                    throw new UserFriendlyException($"工时分类[{category.Name}]要求必填关联项目");
                }

                var detail = new LaborReportDetail(
                    GuidGenerator.Create(), report.Id, detailDto.LaborCategoryId,
                    detailDto.LaborCategoryCode, detailDto.ProjectId, detailDto.Hours, detailDto.Jobresponsibilities);

                report.Details.Add(detail);
            }
            await _reportRepository.InsertAsync(report);
            return report.Id;
        }

        //  查询接口
        public async Task<List<LaborReportItemDto>> GetListAsync(Guid? departmentId, Guid? reporterId, Guid? projectId, string categoryCode)
        {
            // 在EFCore中以从表为基准查询并Include主表，完美等效于以主表Right Join从表
            var query = (await _detailRepository.WithDetailsAsync(x => x.LaborReport))
                .WhereIf(departmentId.HasValue, x => x.LaborReport.DepartmentId == departmentId)
                .WhereIf(reporterId.HasValue, x => x.LaborReport.ReporterId == reporterId)
                .WhereIf(projectId.HasValue, x => x.ProjectId == projectId)
                // 右侧模糊查询 (Code%)
                .WhereIf(!string.IsNullOrWhiteSpace(categoryCode), x => x.LaborCategoryCode.StartsWith(categoryCode));

            var details = await query.ToListAsync();

            // 手动映射拼接返回结果
            var result = details.Select(d => new LaborReportItemDto
            {
                DetailId = d.Id,
                ReportId = d.LaborReport.Id,
                ReporterId = d.LaborReport.ReporterId,
                DepartmentId = d.LaborReport.DepartmentId,
                ReportDate = d.LaborReport.ReportDate,
                TotalEffectiveHours = d.LaborReport.TotalEffectiveHours,
                TotalOvertimeHours = d.LaborReport.TotalOvertimeHours,
                LaborCategoryId = d.LaborCategoryId,
                LaborCategoryCode = d.LaborCategoryCode,
                ProjectId = d.ProjectId,
                Hours = d.Hours,
                Status = d.Status
            }).ToList();
            return result;
        }

        //修改接口 (限制状态并重置为审核中)
        public async Task UpdateDetailAsync(Guid reportId, Guid detailId, UpdateLaborReportDetailDto input)
        {
            var report = await _reportRepository.GetAsync(reportId, includeDetails: true);
            var detail = report.Details.FirstOrDefault(x => x.Id == detailId)
                ?? throw new EntityNotFoundException(typeof(LaborReportDetail), detailId);

            if (detail.Status != LaborReportStatus.Rejected && detail.Status != LaborReportStatus.Withdrawn)
            {
                throw new UserFriendlyException("只能修改状态为“退回”或“撤回”的申报记录！");
            }
            // 规则校验
            var category = await _laborCategoryRepository.GetAsync(input.LaborCategoryId);
            if (category.LaborClass == LaborClass.Project && !input.ProjectId.HasValue)
                throw new UserFriendlyException("工时类别为项目时，关联项目必填");
            // 更新并重置状态为审核中
            detail.LaborCategoryId = input.LaborCategoryId;
            detail.LaborCategoryCode = input.LaborCategoryCode;
            detail.ProjectId = input.ProjectId;
            detail.Hours = input.Hours;
            detail.Status = LaborReportStatus.Pending;
            await _reportRepository.UpdateAsync(report);
        }

        // 删除从表数据
        public async Task DeleteDetailAsync(Guid reportId, Guid detailId)
        {
            var report = await _reportRepository.GetAsync(reportId, includeDetails: true);
            var detail = report.Details.FirstOrDefault(x => x.Id == detailId)
                ?? throw new EntityNotFoundException(typeof(LaborReportDetail), detailId);

            if (detail.Status != LaborReportStatus.Rejected && detail.Status != LaborReportStatus.Withdrawn)
            {
                throw new UserFriendlyException("只能删除状态为“退回”或“撤回”的申报记录！");
            }

            report.Details.Remove(detail);
            await _reportRepository.UpdateAsync(report);
        }

        // 审核接口 (修改状态并计算主表数据)
        public async Task ApproveAsync(Guid reportId, Guid detailId)
        {
            var report = await _reportRepository.GetAsync(reportId, includeDetails: true);
            var detail = report.Details.FirstOrDefault(x => x.Id == detailId)
                ?? throw new EntityNotFoundException(typeof(LaborReportDetail), detailId);

            detail.Status = LaborReportStatus.Approved;

            // 触发主表工时重新计算
            report.RecalculateHours();

            await _reportRepository.UpdateAsync(report);
        }

        // 撤回接口
        public async Task WithdrawAsync(Guid reportId, Guid detailId)
        {
            var report = await _reportRepository.GetAsync(reportId, includeDetails: true);
            var detail = report.Details.FirstOrDefault(x => x.Id == detailId)
                ?? throw new EntityNotFoundException(typeof(LaborReportDetail), detailId);

            if (detail.Status != LaborReportStatus.Pending)
                throw new UserFriendlyException("仅能撤回处于“审核中”的记录！");

            detail.Status = LaborReportStatus.Withdrawn;
            await _reportRepository.UpdateAsync(report);
        }

        // 退回接口 内部调用
        [RemoteService(IsEnabled = false)]
        public async Task RejectAsync(Guid reportId, Guid detailId)
        {
            var report = await _reportRepository.GetAsync(reportId, includeDetails: true);
            var detail = report.Details.FirstOrDefault(x => x.Id == detailId)
                ?? throw new EntityNotFoundException(typeof(LaborReportDetail), detailId);

            if (detail.Status != LaborReportStatus.Pending)
                throw new UserFriendlyException("仅能退回处于“审核中”的记录！");

            detail.Status = LaborReportStatus.Rejected;
            await _reportRepository.UpdateAsync(report);
        }

        //专门为前端日历提供的按日期范围查询接口
        public async Task<List<LaborReportDailyStatusDto>> GetCalendarStatusAsync(DateTime startDate, DateTime endDate)
        {
            var userId = CurrentUser.Id;
            if (!userId.HasValue) throw new UserFriendlyException("未检测到有效登录用户，请重新登录");
            var query = (await _detailRepository.WithDetailsAsync(x => x.LaborReport))
                .Where(x => x.LaborReport.ReporterId == userId.Value)
                .Where(x => x.LaborReport.ReportDate >= startDate && x.LaborReport.ReportDate <= endDate);
            var details = await query.ToListAsync();
            var result = details
                .GroupBy(x => x.LaborReport.ReportDate.ToString("yyyy-MM-dd"))
                .Select(g => new LaborReportDailyStatusDto
                {
                    Date = g.Key,
                    TotalEffectiveHours = g.First().LaborReport.TotalEffectiveHours,
                    ApprovedDetailIds = g.Where(x => x.Status == LaborReportStatus.Approved).Select(x => x.Id).ToList(),
                    PendingDetailIds = g.Where(x => x.Status == LaborReportStatus.Pending).Select(x => x.Id).ToList(),
                    RejectedOrWithdrawnDetailIds = g.Where(x => x.Status == LaborReportStatus.Rejected || x.Status == LaborReportStatus.Withdrawn).Select(x => x.Id).ToList()
                }).ToList();
            return result;
        }
    }
}