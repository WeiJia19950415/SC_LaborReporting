using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SC_LaborReporting.LaborCategories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Uow;
using Volo.Abp.Users;
using static SC_LaborReporting.Permissions.SC_LaborReportingPermissions;

namespace SC_LaborReporting.LaborReports
{
    public class LaborReportAppService : SC_LaborReportingAppService, ILaborReportAppService
    {
        private readonly IRepository<LaborReport, Guid> _reportRepository;
        private readonly IRepository<LaborReportDetail, Guid> _detailRepository;
        private readonly IRepository<LaborCategory, Guid> _laborCategoryRepository;
        private readonly IdentityUserManager _userManager;

        public LaborReportAppService(
            IRepository<LaborReport, Guid> reportRepository,
            IRepository<LaborReportDetail, Guid> detailRepository,
            IRepository<LaborCategory, Guid> laborCategoryRepository,
            IdentityUserManager userManager)
        {
            _reportRepository = reportRepository;
            _detailRepository = detailRepository;
            _laborCategoryRepository = laborCategoryRepository;
            _userManager = userManager;
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
        public async Task SaveDailyReportAsync(SaveDailyLaborReportDto input)
        {
            var currentUserId = CurrentUser.GetId();
            var user = await _userManager.GetByIdAsync(currentUserId);
            var ous = await _userManager.GetOrganizationUnitsAsync(user);
             var departmentId = ous.FirstOrDefault()?.Id;
            if (!departmentId.HasValue)
            {
                throw new UserFriendlyException("当前用户未分配部门，无法提报工时，请联系管理员！");
            }
            input.DepartmentId = departmentId;
            var query = await _reportRepository.WithDetailsAsync(x => x.Details);
            var report = query.FirstOrDefault(x => x.ReporterId == currentUserId && x.ReportDate.Date == input.ReportDate.Date);
            if (report == null)
            {
                if (!input.DepartmentId.HasValue) throw new UserFriendlyException("缺少部门信息");
                report = new LaborReport(GuidGenerator.Create(), currentUserId, input.DepartmentId.Value, input.ReportDate);
                await _reportRepository.InsertAsync(report, autoSave: true);
            }
            foreach (var dto in input.Details)
            {
                if (dto.Id.HasValue && dto.Id.Value != Guid.Empty)
                {
                    var existingDetail = report.Details.FirstOrDefault(x => x.Id == dto.Id.Value);
                    if (existingDetail != null)
                    {
                        existingDetail.LaborClass = dto.LaborClass;
                        existingDetail.ProjectId = dto.ProjectId;
                        existingDetail.ProjectCode = dto.ProjectCode ?? "-";
                        existingDetail.ProjectName = dto.ProjectName ?? "-";
                        existingDetail.ProjectRoleId = dto.ProjectRoleId;
                        existingDetail.ProjectRoleName = dto.ProjectRoleName ?? "-";
                        existingDetail.LaborCategoryId = dto.LaborCategoryId;
                        existingDetail.LaborCategoryCode = dto.LaborCategoryCode;
                        existingDetail.Hours = dto.Hours;
                        existingDetail.Jobresponsibilities = dto.Jobresponsibilities;
                    }
                }
                else
                {
                    var newDetail = new LaborReportDetail(
                        id: GuidGenerator.Create(),
                        laborReportId: report.Id,
                        laborCategoryId: dto.LaborCategoryId,
                        laborCategoryCode: dto.LaborCategoryCode,
                        projectId: dto.ProjectId,
                        hours: dto.Hours,
                        jobresponsibilities: dto.Jobresponsibilities,
                        laborClass: dto.LaborClass,
                        projectCode: dto.ProjectCode ?? "-",
                        projectName: dto.ProjectName ?? "-",
                        projectRoleId: dto.ProjectRoleId,
                        projectRoleName: dto.ProjectRoleName ?? "-"
                    );

                    report.Details.Add(newDetail);
                }
            }
            await _reportRepository.UpdateAsync(report);
        }

        [HttpPost]
        public async Task<List<LaborReportItemDto>> GetDetailsByIdsAsync(List<Guid> detailIds)
        {
            if (detailIds == null || !detailIds.Any())
            {
                return new List<LaborReportItemDto>();
            }

            // 1. 查出这些明细，并且通过 Include (WithDetails) 顺带查出所属的主表信息
            var query = await _detailRepository.WithDetailsAsync(x => x.LaborReport);
            var details = query.Where(x => detailIds.Contains(x.Id)).ToList();

            // 2. 将实体数据手工组装映射为 Dto 吐给前端
            var result = details.Select(d => new LaborReportItemDto
            {
                DetailId = d.Id,
                ReportId = d.LaborReportId,
                ReporterId = d.LaborReport.ReporterId,
                DepartmentId = d.LaborReport.DepartmentId,
                ReportDate = d.LaborReport.ReportDate,
                LaborCategoryId = d.LaborCategoryId,
                LaborCategoryCode = d.LaborCategoryCode,
                Hours = d.Hours,
                Jobresponsibilities = d.Jobresponsibilities,
                Status = d.Status,
                ProjectId = d.ProjectId,
                LaborClass = d.LaborClass,
                ProjectCode = d.ProjectCode,
                ProjectName = d.ProjectName,
                ProjectRoleId = d.ProjectRoleId,
                ProjectRoleName = d.ProjectRoleName
            }).ToList();

            return result;
        }

    }
}