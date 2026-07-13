using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SC_LaborReporting.enums;
using SC_LaborReporting.LaborCategories;
using SC_LaborReporting.ProductSeries;
using SC_LaborReporting.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Data; // 引入 GetProperty 扩展方法所在的命名空间
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity; // 引入 OrganizationUnit 所在的命名空间
using Volo.Abp.Timing;
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

        private readonly IRepository<OrganizationUnit, Guid> _organizationUnitRepository;
        private readonly IRepository<LaborReportApprovalStatus, Guid> _approvalStatusRepository;
        private readonly IRepository<LaborReportApprovalRecord, Guid> _approvalRecordRepository;
        private readonly IRepository<Project, Guid> _ProjectRepository;
        IRepository<ProductSeries.ProductSeries, Guid> _productSeriesRepository;

        public LaborReportAppService(
            IRepository<LaborReport, Guid> reportRepository,
            IRepository<LaborReportDetail, Guid> detailRepository,
            IRepository<LaborCategory, Guid> laborCategoryRepository,
            IdentityUserManager userManager,
            IRepository<OrganizationUnit, Guid> organizationUnitRepository,
            IRepository<LaborReportApprovalStatus, Guid> approvalStatusRepository,
            IRepository<LaborReportApprovalRecord, Guid> approvalRecordRepository,
            IRepository<Project, Guid> ProjectRepository,
            IRepository<ProductSeries.ProductSeries, Guid> productSeriesRepository)
        {
            _reportRepository = reportRepository;
            _detailRepository = detailRepository;
            _laborCategoryRepository = laborCategoryRepository;
            _userManager = userManager;

            _organizationUnitRepository = organizationUnitRepository;
            _approvalStatusRepository = approvalStatusRepository;
            _approvalRecordRepository = approvalRecordRepository;
            _ProjectRepository = ProjectRepository;
            _productSeriesRepository = productSeriesRepository;
        }

        // 查询接口
        public async Task<List<LaborReportItemDto>> GetListAsync(Guid? departmentId, Guid? reporterId, Guid? projectId, string categoryCode)
        {
            var query = (await _detailRepository.WithDetailsAsync(x => x.LaborReport))
                .WhereIf(departmentId.HasValue, x => x.LaborReport.DepartmentId == departmentId)
                .WhereIf(reporterId.HasValue, x => x.LaborReport.ReporterId == reporterId)
                .WhereIf(projectId.HasValue, x => x.ProjectId == projectId)
                .WhereIf(!string.IsNullOrWhiteSpace(categoryCode), x => x.LaborCategoryCode.StartsWith(categoryCode));

            var details = await query.ToListAsync();

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
                Status = d.Status,
                ProductSeriesId = d.ProductSeriesId
            }).ToList();
            return result;
        }

        // 修改接口
        public async Task UpdateDetailAsync(Guid reportId, Guid detailId, UpdateLaborReportDetailDto input)
        {
            var report = await _reportRepository.GetAsync(reportId, includeDetails: true);
            var detail = report.Details.FirstOrDefault(x => x.Id == detailId)
                ?? throw new EntityNotFoundException(typeof(LaborReportDetail), detailId);

            if (detail.Status != LaborReportStatus.Rejected && detail.Status != LaborReportStatus.Withdrawn)
            {
                throw new UserFriendlyException("只能修改状态为“退回”或“撤回”的申报记录！");
            }

            var category = await _laborCategoryRepository.GetAsync(input.LaborCategoryId);
            if (category.LaborClass == LaborClass.Project && !input.ProjectId.HasValue)
                throw new UserFriendlyException("工时类别为项目时，关联项目必填");

            detail.LaborCategoryId = input.LaborCategoryId;
            detail.LaborCategoryCode = input.LaborCategoryCode;
            detail.ProjectId = input.ProjectId;
            detail.Hours = input.Hours;
            detail.Status = LaborReportStatus.Pending;

            await _reportRepository.UpdateAsync(report);

            // 重新触发两级固定审批逻辑
            await GenerateApprovalFlowAsync(report.DepartmentId, detail.Id,detail.ProjectId);
        }

        // 删除接口
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

        // 审核接口
        public async Task ApproveAsync(ApproveInputDto input)
        {
            var query = await _reportRepository.WithDetailsAsync(x => x.Details);
            var report = await query.FirstOrDefaultAsync(x => x.Id == input.ReportId);
            var detail = report.Details.FirstOrDefault(x => x.Id == input.DetailId)
                ?? throw new EntityNotFoundException(typeof(LaborReportDetail), input.DetailId);

            var statusInfo = await _approvalStatusRepository.FirstOrDefaultAsync(x => x.LaborReportDetailId == input.DetailId);
            if (statusInfo == null || statusInfo.Status == ApprovalStatus.Completed)
            {
                throw new UserFriendlyException("当前申报明细不在审批状态中。");
            }

            var currentRecord = await _approvalRecordRepository.FirstOrDefaultAsync(x =>
                x.LaborReportDetailId == input.DetailId &&
                x.Level == statusInfo.CurrentLevel);

            if (currentRecord == null)
            {
                throw new UserFriendlyException("未找到对应的审批记录节点。");
            }

            if (currentRecord.ApproverId != CurrentUser.Id)
            {
                throw new UserFriendlyException("您没有当前节点的审批权限。");
            }

            // 更新审批记录
            currentRecord.Status = input.IsApproved ? ApprovalRecordStatus.Approved : ApprovalRecordStatus.Rejected;
            currentRecord.Comment = input.Comment;
            currentRecord.ApprovalTime = Clock.Now;
            await _approvalRecordRepository.UpdateAsync(currentRecord);

            // 更新审批状态
            if (!input.IsApproved)
            {
                statusInfo.Status = ApprovalStatus.Rejected;
                detail.Status = LaborReportStatus.Rejected;
            }
            else
            {
                if (statusInfo.CurrentLevel == 1)
                {
                    var nextRecord = await _approvalRecordRepository.FirstOrDefaultAsync(x =>
                        x.LaborReportDetailId == input.DetailId &&
                        x.Level == 2);

                    if (nextRecord != null && nextRecord.ApproverId == CurrentUser.Id)
                    {
                        nextRecord.Status = ApprovalRecordStatus.Approved;
                        nextRecord.Comment = string.IsNullOrWhiteSpace(input.Comment) ? "一、二级审批人为同一人，系统自动审批通过" : $"[自动审批] {input.Comment}";
                        nextRecord.ApprovalTime = Clock.Now;
                        await _approvalRecordRepository.UpdateAsync(nextRecord);
                        statusInfo.CurrentLevel = 2;
                        statusInfo.Status = ApprovalStatus.Completed;
                        detail.Status = LaborReportStatus.Approved;
                        report.RecalculateHours();
                    }
                    else
                    {
                        statusInfo.CurrentLevel = 2;
                        statusInfo.Status = ApprovalStatus.Approving;
                    }
                }

                await _approvalStatusRepository.UpdateAsync(statusInfo);
                await _reportRepository.UpdateAsync(report);
                CalculateAndAssignHoursFinance(report.Details.ToList());
            }
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

        // 退回接口
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

        // 日历查询
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

        // 提报工时
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
                        projectRoleName: dto.ProjectRoleName ?? "-",
                        productSeriesId: dto.ProductSeriesId
                    );

                    report.Details.Add(newDetail);
                }
            }
            await _reportRepository.UpdateAsync(report);

            // 触发自动创建审批记录流
            foreach (var detail in report.Details)
            {
                if (detail.Status == LaborReportStatus.Pending)
                {
                    var exists = await _approvalStatusRepository.AnyAsync(x => x.LaborReportDetailId == detail.Id);
                    if (!exists)
                    {
                        await GenerateApprovalFlowAsync(report.DepartmentId, detail.Id, detail.ProjectId);
                    }
                }
            }
            if (CurrentUnitOfWork != null)
            {
                await CurrentUnitOfWork.SaveChangesAsync();
            }
        }

        [HttpPost]
        public async Task<List<LaborReportItemDto>> GetDetailsByIdsAsync(List<Guid> detailIds)
        {
            if (detailIds == null || !detailIds.Any())
            {
                return new List<LaborReportItemDto>();
            }

            var query = await _detailRepository.WithDetailsAsync(x => x.LaborReport);
            var details = query.Where(x => detailIds.Contains(x.Id)).ToList();

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

        /// <summary>
        /// 生成审批流：部门负责人 -> 项目负责人
        /// </summary>
        private async Task GenerateApprovalFlowAsync(Guid departmentId, Guid detailId, Guid? ProjectId)
        {
            // 1. 获取部门负责人 ID (假设你的 Department 服务或仓储中可以获取到)
            // 这里的具体调用请根据你的实际服务方法替换
            var myDepartment = await _organizationUnitRepository.FirstOrDefaultAsync(x => x.Id == departmentId);
            if (myDepartment == null)
            {
                throw new UserFriendlyException("未找到提交人所在部门！");
            }
            var myManagerId = myDepartment.GetProperty<Guid?>("ManagerId");
            if (!myManagerId.HasValue)
            {
                throw new UserFriendlyException("提交人所在部门未设置负责人，请联系人事或管理员配置！");
            }
            Guid level1ApproverId = myManagerId.Value;

            // 寻找项目负责人
            Guid level2ApproverId;
            if (ProjectId == null)
            {
                 level2ApproverId = myManagerId.Value;
            }
            else
            {
                var project = await _ProjectRepository.FirstOrDefaultAsync(x => x.Id == ProjectId);
                level2ApproverId = project.ManagerId;
            }
            var oldStatus = await _approvalStatusRepository.FirstOrDefaultAsync(x => x.LaborReportDetailId == detailId);
            if (oldStatus != null)
            {
                await _approvalStatusRepository.DeleteAsync(oldStatus);
            }

            var oldRecords = await _approvalRecordRepository.GetListAsync(x => x.LaborReportDetailId == detailId);
            if (oldRecords.Any())
            {
                await _approvalRecordRepository.DeleteManyAsync(oldRecords);
            }

            var approvalStatus = new LaborReportApprovalStatus(GuidGenerator.Create(), detailId);
            await _approvalStatusRepository.InsertAsync(approvalStatus);

            var level1Record = new LaborReportApprovalRecord(GuidGenerator.Create(), detailId, 1, level1ApproverId);
            var level2Record = new LaborReportApprovalRecord(GuidGenerator.Create(), detailId, 2, level2ApproverId);

            await _approvalRecordRepository.InsertAsync(level1Record);
            await _approvalRecordRepository.InsertAsync(level2Record);
        }

        public async Task<List<LaborReportItemDto>> GetPendingApprovalsAsync(Guid? reporterId, Guid? departmentId, Guid? projectId)
        {
            var currentUserId = CurrentUser.Id;
            if (!currentUserId.HasValue) throw new UserFriendlyException("未检测到有效登录用户。");
            var query = from record in await _approvalRecordRepository.GetQueryableAsync()
                        join status in await _approvalStatusRepository.GetQueryableAsync()
                          on record.LaborReportDetailId equals status.LaborReportDetailId
                        where record.ApproverId == currentUserId.Value  
                           && (record.Status == ApprovalRecordStatus.Pending 
                           && record.Level == status.CurrentLevel
                           && (status.Status == ApprovalStatus.Approving || status.Status == ApprovalStatus.Submitted))
                        select record.LaborReportDetailId;

            var pendingDetailIds = await query.ToListAsync();

            if (!pendingDetailIds.Any()) return new List<LaborReportItemDto>();
            var detailQuery = (await _detailRepository.WithDetailsAsync(x => x.LaborReport))
                .Where(x => pendingDetailIds.Contains(x.Id))
                .WhereIf(departmentId.HasValue, x => x.LaborReport.DepartmentId == departmentId)
                .WhereIf(reporterId.HasValue, x => x.LaborReport.ReporterId == reporterId)
                .WhereIf(projectId.HasValue, x => x.ProjectId == projectId);

            var details = await detailQuery.ToListAsync();
            return details.Select(d => new LaborReportItemDto
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
                Status = d.Status,
                Jobresponsibilities = d.Jobresponsibilities,
                ProjectName = d.ProjectName
            }).ToList();
        }


        /// <summary>
        /// 统筹计算并分配当天的财务核算工时 (Hoursfinance)
        /// </summary>
        /// <param name="dailyDetails">当天该用户所有的报工明细记录</param>
        private void CalculateAndAssignHoursFinance(List<LaborReportDetail> dailyDetails)
        {
            if (dailyDetails == null || !dailyDetails.Any()) return;
            double totalRealHours = (double)dailyDetails.Sum(d => d.Hours);
            if (totalRealHours <= 8.0)
            {
                foreach (var detail in dailyDetails)
                {
                    detail.SetHoursfinance((double)detail.Hours);
                }
                return;
            }

            double targetTotal = 8.0;
            double unit = 0.5;
            var allocations = dailyDetails.Select(d => new
            {
                Detail = d,
                ExactAlloc = ((double)d.Hours / totalRealHours) * targetTotal 
            }).Select(x => new
            {
                x.Detail,
                x.ExactAlloc,
                BaseAlloc = Math.Floor(x.ExactAlloc / unit) * unit
            }).ToList();

            double currentSum = allocations.Sum(x => x.BaseAlloc);
            int remainingSteps = (int)Math.Round((targetTotal - currentSum) / unit);
            var orderedAllocations = allocations
                .Select(x => new
                {
                    x.Detail,
                    x.BaseAlloc,
                    Remainder = x.ExactAlloc - x.BaseAlloc
                })
                .OrderByDescending(x => x.Remainder)
                .ToList();

            var finalAllocations = orderedAllocations.ToDictionary(x => x.Detail, x => x.BaseAlloc);

            for (int i = 0; i < remainingSteps; i++)
            {
                var itemToCompensate = orderedAllocations[i % orderedAllocations.Count];
                finalAllocations[itemToCompensate.Detail] += unit;
            }
            foreach (var detail in dailyDetails)
            {
                detail.SetHoursfinance(finalAllocations[detail]);
            }
        }

    }
}