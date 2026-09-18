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
using Volo.Abp.Application.Dtos;
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
                LaborCategoryId = d.LaborCategoryId.Value,
                LaborCategoryCode = d.LaborCategoryCode,
                Hours = d.Hours,
                Status = d.Status,
                ProductSeriesId = d.ProductSeriesId.Value
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
            await GenerateApprovalFlowAsync(report.DepartmentId, detail.Id, detail.ProjectId);
        }
        // 删除接口
        public async Task DeleteDetailAsync(Guid detailId)
        {
            // 1. 直接通过明细仓储获取数据
            var detail = await _detailRepository.GetAsync(detailId);
            if (detail == null)
            {
                throw new EntityNotFoundException(typeof(LaborReportDetail), detailId);
            }

            // 2. 状态校验：只能删除退回或撤回状态的记录
            //if (detail.Status != LaborReportStatus.Rejected && detail.Status != LaborReportStatus.Withdrawn)
            //{
            //    throw new UserFriendlyException("只能删除状态为“退回”或“撤回”的申报记录！");
            //}
            // 3. 直接从明细仓储中物理/逻辑删除
            await _detailRepository.DeleteAsync(detail);
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
                else if (statusInfo.CurrentLevel == 2)
                {
                    statusInfo.Status = ApprovalStatus.Completed;
                    detail.Status = LaborReportStatus.Approved;
                    report.RecalculateHours();
                }
                await _approvalStatusRepository.UpdateAsync(statusInfo);
                await _reportRepository.UpdateAsync(report);
                CalculateAndAssignHoursFinance(report.Details.ToList());
            }
        }
        // 撤回接口
        public async Task WithdrawAsync(Guid detailId)
        {
            // 1. 直接通过明细仓储获取数据
            var detail = await _detailRepository.GetAsync(detailId);
            if (detail == null)
            {
                throw new EntityNotFoundException(typeof(LaborReportDetail), detailId);
            }            // 2. 基础状态校验
            if (detail.Status != LaborReportStatus.Pending)
            {
                throw new UserFriendlyException("仅能撤回处于“审核中”的记录！");
            }
            // 3. 核心安全校验：检查是否已经有任何审批人处理过
            var records = await _approvalRecordRepository.GetListAsync(x => x.LaborReportDetailId == detailId);
            if (records.Any(x => x.Status != ApprovalRecordStatus.Pending))
            {
                if (!records.Any(x => x.Status == ApprovalRecordStatus.Rejected))
                {
                    throw new UserFriendlyException("该明细已被审批（或部分审批），无法撤回！");
                }
            }
            // 4. 状态变更为撤回并保存
            detail.Status = LaborReportStatus.Withdrawn;
            await _detailRepository.UpdateAsync(detail);
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
            if (input.IsHistory)
            {
                var minDate = new DateTime(2025, 1, 1);
                var maxDate = new DateTime(2026, 7, 31);
                if (input.ReportDate.Date < minDate.Date || input.ReportDate.Date > maxDate.Date)
                {
                    throw new UserFriendlyException("历史工时提交失败：提交日期仅限于 2025年1月1日 至 2026年7月31日之间！");
                }
            }

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
            // (如果你的“已通过”枚举不叫 Approved，请修改这里)
            var targetStatus = input.IsHistory ? LaborReportStatus.Approved : LaborReportStatus.Pending;

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
                        existingDetail.ProjectName = dto.ProjectName ?? "其它工时";
                        existingDetail.ProjectRoleId = dto.ProjectRoleId;
                        existingDetail.ProjectRoleName = dto.ProjectRoleName ?? "-";
                        existingDetail.Status = targetStatus;
                        existingDetail.LaborCategoryId = dto.LaborCategoryId.Value;
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
                        laborCategoryId: dto.LaborCategoryId.Value,
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
                    newDetail.Status = targetStatus;
                    report.Details.Add(newDetail);
                }
            }

            await _reportRepository.UpdateAsync(report);

            // 触发自动创建审批记录流
            foreach (var detail in report.Details)
            {
                // 这里的 if 条件天然地拦截了历史记录 (因为历史记录的 Status 是 Approved，不是 Pending)
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
                LaborCategoryId = d.LaborCategoryId.Value,
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

            // 1. 判断是否为管理员 (请根据你系统的实际逻辑调整，如 Role 检查或 Permission 检查)
            bool isAdmin = CurrentUser.IsInRole("admin");
            // bool isAdmin = await _permissionChecker.IsGrantedAsync("YourAdminPermissionName"); // 如果基于权限策略

            var approvalRecordQuery = await _approvalRecordRepository.GetQueryableAsync();
            var approvalStatusQuery = await _approvalStatusRepository.GetQueryableAsync();

            // 2. 修改此处查询：如果是管理员，则忽略 ApproverId == currentUserId.Value 的限制
            var query = from record in approvalRecordQuery
                        join status in approvalStatusQuery
                          on record.LaborReportDetailId equals status.LaborReportDetailId
                        where (isAdmin || record.ApproverId == currentUserId.Value) // <--- 核心修改点
                           && record.Status == ApprovalRecordStatus.Pending
                           && record.Level == status.CurrentLevel
                           && (status.Status == ApprovalStatus.Approving || status.Status == ApprovalStatus.Submitted)
                        select record.LaborReportDetailId;

            // 添加 .Distinct() 防止多个人同时审批同一条记录时，管理员查出重复的 DetailId
            var pendingDetailIds = await query.Distinct().ToListAsync();

            if (!pendingDetailIds.Any()) return new List<LaborReportItemDto>();

            var detailQuery = (await _detailRepository.WithDetailsAsync(x => x.LaborReport))
                .Where(x => pendingDetailIds.Contains(x.Id))
                .WhereIf(departmentId.HasValue, x => x.LaborReport.DepartmentId == departmentId)
                .WhereIf(reporterId.HasValue, x => x.LaborReport.ReporterId == reporterId)
                .WhereIf(projectId.HasValue, x => x.ProjectId == projectId);

            var details = await detailQuery.ToListAsync();

            // 3. 提取所有不重复的 LaborCategoryId
            var categoryIds = details
                .Where(d => d.LaborCategoryId.HasValue)
                .Select(d => d.LaborCategoryId.Value)
                .Distinct()
                .ToList();

            var productSeriesIds = details
                .Where(d => d.ProductSeriesId.HasValue)
                .Select(d => d.ProductSeriesId.Value)
                .Distinct()
                .ToList();

            // 4. 批量查询 Category 并转为字典 (Id -> Name)
            var categoryQuery = await _laborCategoryRepository.GetQueryableAsync();
            var categoryDict = await categoryQuery
                .Where(c => categoryIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name);

            var productSeriesQuery = await _productSeriesRepository.GetQueryableAsync();
            var productSeriesDic = await productSeriesQuery
                .Where(c => productSeriesIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name);
            // 5. 在内存中进行同步映射
            return details.Select(d => new LaborReportItemDto
            {
                DetailId = d.Id,
                ReportId = d.LaborReport.Id,
                ReporterId = d.LaborReport.ReporterId,
                DepartmentId = d.LaborReport.DepartmentId,
                ReportDate = d.LaborReport.ReportDate,
                TotalEffectiveHours = d.LaborReport.TotalEffectiveHours,
                TotalOvertimeHours = d.LaborReport.TotalOvertimeHours,
                LaborCategoryId = d.LaborCategoryId.Value,
                LaborCategoryCode = d.LaborCategoryCode,
                ProjectId = d.ProjectId,
                ProductSeriesId = d.ProductSeriesId.GetValueOrDefault(),
                ProductSeriesName = d.ProductSeriesId.HasValue && productSeriesDic.TryGetValue(d.ProductSeriesId.Value, out var name)
                                    ? name
                                    : null,
                LaborCategoryName = d.LaborCategoryId.HasValue && categoryDict.TryGetValue(d.LaborCategoryId.Value, out var name2)
                                    ? name2
                                    : null,

                Hours = d.Hours,
                Status = d.Status,
                Jobresponsibilities = d.Jobresponsibilities,
                ProjectName = d.ProjectName
            }).ToList();
        }
        public async Task<List<LaborReportApprovalRecordDto>> GetApprovalRecordsAsync(Guid id)
        {
            var records = await _approvalRecordRepository.GetListAsync(x => x.LaborReportDetailId == id);
            var detail = await _detailRepository.GetAsync(id);
            if (!records.Any())
            {
                return new List<LaborReportApprovalRecordDto>();
            }
            var approverIds = records.Select(x => x.ApproverId).Distinct().ToList();
            var userDictionary = new Dictionary<Guid, string>();
            foreach (var approverId in approverIds)
            {
                var user = await _userManager.FindByIdAsync(approverId.ToString());
                if (user != null)
                {
                    userDictionary[approverId] = !string.IsNullOrWhiteSpace(user.Name) ? user.Name : user.UserName;
                }
            }
            var dtos = records
                .OrderBy(x => x.CreationTime)
                .Select(x => new LaborReportApprovalRecordDto
                {
                    Id = x.Id,
                    LaborReportId = x.LaborReportDetailId,
                    ApproverName = userDictionary.ContainsKey(x.ApproverId)
                                    ? userDictionary[x.ApproverId]
                                    : "未知用户",
                    ApprovalNode = x.Level == 1 ? "部门负责人审批" : (x.Level == 2 && detail.LaborClass == LaborClass.Other ? "上级部门负责人审批" : $"项目经理审批"),
                    ApprovalStatus = GetStatusText(x.Status),
                    ApprovalContent = x.Comment ?? "无",
                    CreationTime = x.ApprovalTime ?? x.CreationTime
                }).ToList();

            return dtos;
        }
        private string GetStatusText(ApprovalRecordStatus status)
        {
            return status switch
            {
                ApprovalRecordStatus.Pending => "待审批",
                ApprovalRecordStatus.Approved => "审批通过",
                ApprovalRecordStatus.Rejected => "审批不通过", // 或者写 "审批驳回"
                _ => status.ToString() // 兜底选项，防止未来增加枚举但忘了改这里
            };
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
        public async Task<PagedResultDto<ApprovalHistoryDto>> GetApprovalHistoryAsync(GetApprovalHistoryInput input)
        {
            var currentUserId = CurrentUser.Id;
            if (!currentUserId.HasValue)
                throw new UserFriendlyException("未检测到有效登录用户。");
            // 1. 获取基础 IQueryable (注意 ABP 中仓储需要 await GetQueryableAsync)
            var recordQuery = await _approvalRecordRepository.GetQueryableAsync();
            var detailQuery = await _detailRepository.GetQueryableAsync();
            var reportQuery = await _reportRepository.GetQueryableAsync();
            // 2. 联表查询：关联审批记录、明细和主表
            var query = from record in recordQuery
                        join detail in detailQuery on record.LaborReportDetailId equals detail.Id
                        join report in reportQuery on detail.LaborReportId equals report.Id
                        where record.ApproverId == currentUserId.Value
                           // 核心过滤：剔除“待审批(Pending)”，只查我已经处理过(通过/驳回)的记录
                           && record.Status != ApprovalRecordStatus.Pending
                        select new { record, detail, report };
            // 3. 动态条件过滤 (填报发起人)
            if (input.ReporterId.HasValue)
            {
                query = query.Where(q => q.report.ReporterId == input.ReporterId.Value);
            }
            // 4. 动态条件过滤 (发起部门)
            if (input.DepartmentId.HasValue)
            {
                query = query.Where(q => q.report.DepartmentId == input.DepartmentId.Value);
            }
            // 5. 统计总数 (用于前端分页展示 Total)
            var totalCount = await query.CountAsync();
            // 6. 分页与排序 (按审批时间倒序)
            var pagedQuery = query.OrderByDescending(q => q.record.ApprovalTime ?? q.record.CreationTime)
                                  .Skip(input.SkipCount)
                                  .Take(input.MaxResultCount);
            var items = await pagedQuery.ToListAsync();
            // 7. 映射组装给前端的 DTO
            var dtos = items.Select(q => new ApprovalHistoryDto
            {
                DetailId = q.detail.Id,
                ReportDate = q.report.ReportDate,
                ReporterId = q.report.ReporterId,
                DepartmentId = q.report.DepartmentId,
                ProjectName = q.detail.ProjectName ?? "-",
                LaborCategoryCode = q.detail.LaborCategoryCode,
                Hours = (decimal)q.detail.Hours,
                Jobresponsibilities = q.detail.Jobresponsibilities,
                // 这里映射的是“我”在这个节点的审批状态 (如果是枚举类型，转为 int 给前端匹配 tag 颜色)
                Status = (int)q.record.Status,
                ApprovalTime = q.record.ApprovalTime ?? q.record.CreationTime,
                ApprovalComment = q.record.Comment ?? "无"
            }).ToList();
            return new PagedResultDto<ApprovalHistoryDto>(totalCount, dtos);
        }
    }
}