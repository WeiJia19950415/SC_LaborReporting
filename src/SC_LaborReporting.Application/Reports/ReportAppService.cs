using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SC_LaborReporting.LaborCategories; 
using SC_LaborReporting.LaborReports;
using SC_LaborReporting.Permissions;
using SC_LaborReporting.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace SC_LaborReporting.Reports
{
    public class ReportAppService : SC_LaborReportingAppService, IReportAppService
    {
        private readonly IRepository<LaborReport, Guid> _reportRepository;
        private readonly IRepository<LaborCategory, Guid> _laborCategoryRepository;
        private readonly IdentityUserManager _userManager;
        private readonly IRepository<IdentityUser, Guid> _userRepository;
        private readonly IRepository<OrganizationUnit, Guid> _ouRepository;
        private readonly IAuthorizationService _authorizationService;
        private readonly IRepository<LaborReportDetail, Guid> _detailRepository;
        private readonly IRepository<Project, Guid> _ProjectRepository;

        public ReportAppService(
            IRepository<LaborReport, Guid> reportRepository,
            IRepository<LaborCategory, Guid> laborCategoryRepository,
            IdentityUserManager userManager,
            IRepository<IdentityUser, Guid> userRepository,
            IRepository<OrganizationUnit, Guid> ouRepository,
            IAuthorizationService authorizationService,
            IRepository<LaborReportDetail, Guid> detailRepository,
            IRepository<Project, Guid> ProjectRepository)
        {
            _reportRepository = reportRepository;
            _laborCategoryRepository = laborCategoryRepository;
            _userManager = userManager;
            _userRepository = userRepository;
            _ouRepository = ouRepository;
            _authorizationService = authorizationService;
            _detailRepository = detailRepository;
            _ProjectRepository = ProjectRepository;
        }
        public async Task<LaborReportMonthlySummaryDto> GetMonthlySummaryAsync(DateTime startDate, DateTime endDate)
        {
            var userId = CurrentUser.Id;
            if (!userId.HasValue) throw new UserFriendlyException("未检测到有效登录用户");

            if ((endDate - startDate).TotalDays > 31)
            {
                throw new UserFriendlyException("查询时间范围不能超过31天");
            }
            var categories = await _laborCategoryRepository.GetListAsync();
            var categoryMap = categories.ToDictionary(x => x.Id);
            var query = (await _reportRepository.WithDetailsAsync(x => x.Details))
                .Where(x => x.ReporterId == userId.Value)
                .Where(x => x.ReportDate >= startDate && x.ReportDate <= endDate);

            var reports = await query.ToListAsync();

            var dto = new LaborReportMonthlySummaryDto();
            dto.TotalOvertimeHours = reports.Sum(x => x.TotalOvertimeHours);

            foreach (var report in reports)
            {
                foreach (var d in report.Details)
                {
                    dto.TotalReportedHours += d.Hours;

                    if (d.Status == LaborReportStatus.Approved)
                    {
                        dto.TotalApprovedHours += d.Hours;
                    }
                    else if (d.Status == LaborReportStatus.Pending)
                    {
                        dto.TotalPendingHours += d.Hours;
                    }
                    var nameList = new List<string>();
                    var currentId = (Guid?)d.LaborCategoryId;
                    while (currentId.HasValue && categoryMap.TryGetValue(currentId.Value, out var cat))
                    {
                        nameList.Insert(0, cat.Name);
                        currentId = cat.ParentId;
                    }
                    string fullNameText = string.Join(" - ", nameList);
                    dto.Details.Add(new ReportItemDetailDto
                    {
                        DetailId = d.Id,
                        ReportId = report.Id,
                        ReporterId = report.ReporterId,
                        DepartmentId = report.DepartmentId,
                        ReportDate = report.ReportDate,
                        LaborCategoryId = d.LaborCategoryId.Value,
                        LaborCategoryCode = d.LaborCategoryCode,
                        LaborCategoryFullName = fullNameText,
                        ProjectId = d.ProjectId,
                        ProjectName = d.ProjectName == "-" ? "其他工时" : d.ProjectName,
                        ProjectCode = d.ProjectCode,
                        ProjectRoleId = d.ProjectRoleId,
                        ProjectRoleName = d.ProjectRoleName,
                        Hours = d.Hours,
                        Status = (int)d.Status,
                        Jobresponsibilities = d.Jobresponsibilities
                    });
                }
            }
            dto.Details = dto.Details.OrderByDescending(x => x.ReportDate).ToList();

            return dto;
        }

        // 请确保您的 Service 中注入了部门的仓储，例如：
        // private readonly IRepository<Department, Guid> _departmentRepository;

        private async Task<List<Guid>> GetDepartmentAndAllChildrenIdsAsync(Guid rootDepartmentId)
        {
            var allDepartments = await _ouRepository.GetListAsync();
            var resultIds = new List<Guid> { rootDepartmentId };
            var queue = new Queue<Guid>();
            queue.Enqueue(rootDepartmentId);

            while (queue.Any())
            {
                var currentId = queue.Dequeue();
                // 假设您的部门实体中表示父级ID的字段叫 ParentId
                var childrenIds = allDepartments
                    .Where(d => d.ParentId == currentId)
                    .Select(d => d.Id)
                    .ToList();

                foreach (var childId in childrenIds)
                {
                    if (!resultIds.Contains(childId))
                    {
                        resultIds.Add(childId);
                        queue.Enqueue(childId);
                    }
                }
            }

            return resultIds;
        }
        private async Task<List<LaborReportDetail>> GetFilteredDetailsAsync(DepartmentReportQueryDto input)
        {
            var userId = CurrentUser.Id;
            if (!userId.HasValue) throw new UserFriendlyException("未检测到有效登录用户");

            // 1. 获取主表查询对象并应用基础的时间条件
            var query = await _reportRepository.WithDetailsAsync(x => x.Details);
            var queryable = query.Where(x => x.ReportDate >= input.StartDate && x.ReportDate <= input.EndDate);
            if (input.departmentId.HasValue)
            {
                var targetDepartmentIds = await GetDepartmentAndAllChildrenIdsAsync(input.departmentId.Value);
                queryable = queryable.Where(x => targetDepartmentIds.Contains(x.DepartmentId));
            }

            // 3. 数据权限隔离控制（划定最大可见边界）
            var hasAllDataPermission = await _authorizationService.IsGrantedAsync(SC_LaborReportingPermissions.Reports.ReportManagement_BusinessDetailsALL);

            if (!hasAllDataPermission)
            {
                var user = await _userManager.GetByIdAsync(userId.Value);
                var userOus = await _userManager.GetOrganizationUnitsAsync(user);

                if (!userOus.Any())
                {
                    return new List<LaborReportDetail>(); // 无权限且无部门，直接返回空
                }

                var allOuQuery = await _ouRepository.GetQueryableAsync();
                var allowedOuIds = new List<Guid>();

                foreach (var userOu in userOus)
                {
                    // 抓取当前部门及其所有下级部门
                    var subOuIds = allOuQuery
                        .Where(ou => ou.Code.StartsWith(userOu.Code))
                        .Select(ou => ou.Id)
                        .ToList();

                    allowedOuIds.AddRange(subOuIds);
                }

                allowedOuIds = allowedOuIds.Distinct().ToList();

                // 核心：强制增加数据库层面的 IN 条件，绝不可能被后续的布尔值突破
                queryable = queryable.Where(x => allowedOuIds.Contains(x.DepartmentId));
            }

            // 4. 发送 SQL 到数据库，拉取被部门和日期过滤后的【安全数据】
            var reports = await queryable.ToListAsync();
            var details = reports.SelectMany(x => x.Details).ToList();

            // 5. 在内存中，对安全数据进行状态过滤
            if (!input.IncludeUnapproved)
            {
                details = details.Where(x => x.Status == LaborReportStatus.Approved).ToList();
            }
            else
            {
                details = details.Where(x => x.Status == LaborReportStatus.Approved || x.Status == LaborReportStatus.Pending).ToList();
            }

            // 6. 其他条件过滤
            if (input.FilterByProject)
            {
                details = details.Where(x => x.ProjectId == input.ProjectId).ToList();
            }

            if (input.LaborCategoryId.HasValue)
            {
                details = details.Where(x => x.LaborCategoryId == input.LaborCategoryId).ToList();
            }

            return details;
        }
        public async Task<List<ChartDataDto>> GetDepartmentChartAsync(DepartmentReportQueryDto input)
        {
            var details = await GetFilteredDetailsAsync(input);

            if (!input.FilterByProject)
            {
                var grouped = details.GroupBy(x => new { x.ProjectId, x.ProjectName })
                                     .Select(g => new ChartDataDto
                                     {
                                         ReferenceId = g.Key.ProjectId,
                                         Name = string.IsNullOrEmpty(g.Key.ProjectName) || g.Key.ProjectName == "-" ? "其它工时" : g.Key.ProjectName,
                                         Value = g.Sum(x => x.Hours)
                                     }).ToList();
                return grouped;
            }
            else
            {
                var categories = await _laborCategoryRepository.GetListAsync();
                var categoryMap = categories.ToDictionary(x => x.Id);

                var result = new List<ChartDataDto>();
                var grouped = details.GroupBy(x => x.LaborCategoryId);

                foreach (var g in grouped)
                {
                    var nameList = new List<string>();
                    var currentId = (Guid?)g.Key;
                    while (currentId.HasValue && categoryMap.TryGetValue(currentId.Value, out var cat))
                    {
                        nameList.Insert(0, cat.Name);
                        currentId = cat.ParentId;
                    }
                    var fullNameText = nameList.Any() ? string.Join(" - ", nameList) : "未知分类";

                    result.Add(new ChartDataDto
                    {
                        ReferenceId = g.Key,
                        Name = fullNameText,
                        Value = g.Sum(x => x.Hours)
                    });
                }
                return result;
            }
        }
        private async Task<List<DepartmentReportDetailDto>> BuildDetailDtosAsync(List<LaborReportDetail> details)
        {
            var categories = await _laborCategoryRepository.GetListAsync();
            var categoryMap = categories.ToDictionary(x => x.Id);

            var reporterIds = details.Select(x => x.LaborReport.ReporterId).Distinct().ToList();
            var users = await _userRepository.GetListAsync(x => reporterIds.Contains(x.Id));
            var userMap = users.ToDictionary(x => x.Id, x => x.Name ?? x.UserName);

            var allOus = await _ouRepository.GetListAsync();
            var ouFullNames = new Dictionary<Guid, string>();

            var targetOuIds = details.Select(x => x.LaborReport.DepartmentId).Distinct().ToList();

            foreach (var targetOuId in targetOuIds)
            {
                var ouNamePieces = new List<string>();
                var currentOu = allOus.FirstOrDefault(x => x.Id == targetOuId);
                // 循环向上追溯上级部门，直到顶级部门
                while (currentOu != null)
                {
                    ouNamePieces.Insert(0, currentOu.DisplayName); // 每次插入到列表头部
                    currentOu = currentOu.ParentId.HasValue
                        ? allOus.FirstOrDefault(x => x.Id == currentOu.ParentId.Value)
                        : null;
                }
                // 将层级名称使用 " - " 拼接，例如："总公司 - 研发中心 - 前端开发组"
                ouFullNames[targetOuId] = string.Join(" - ", ouNamePieces);
            }

            var dtos = new List<DepartmentReportDetailDto>();

            foreach (var d in details)
            {
                var nameList = new List<string>();
                var currentId = (Guid?)d.LaborCategoryId;
                while (currentId.HasValue && categoryMap.TryGetValue(currentId.Value, out var cat))
                {
                    nameList.Insert(0, cat.Name);
                    currentId = cat.ParentId;
                }

                dtos.Add(new DepartmentReportDetailDto
                {
                    LaborClass = d.LaborClass.ToString(),
                    ProjectName = d.ProjectName,
                    ProjectCode = d.ProjectCode,
                    ProjectRoleName = d.ProjectRoleName,
                    ReporterName = userMap.TryGetValue(d.LaborReport.ReporterId, out var uname) ? uname : "",

                    // 赋值刚刚拼接好的完整部门层级名称
                    DepartmentFullName = ouFullNames.TryGetValue(d.LaborReport.DepartmentId, out var ouname) ? ouname : "",

                    LaborCategoryFullName = string.Join(" - ", nameList),
                    Jobresponsibilities = d.Jobresponsibilities,
                    Hours = d.Hours,
                    SubTime = d.LaborReport.ReportDate.ToString("yyyy-MM-dd"),
                    Status = (int)d.Status
                });
            }

            return dtos.OrderByDescending(x => x.Status).ToList();
        }
        public async Task<PagedResultDto<DepartmentReportDetailDto>> GetDepartmentTableAsync(DepartmentReportQueryDto input)
        {
            var details = await GetFilteredDetailsAsync(input);
            var totalCount = details.Count;

            var pagedDetails = details.Skip(input.SkipCount).Take(input.MaxResultCount).ToList();
            var dtos = await BuildDetailDtosAsync(pagedDetails);
            foreach (var item in dtos)
            {
                if (item.LaborClass == "Other")
                {
                    item.LaborClass = "其他工时";
                }
                else
                {
                    item.LaborClass = "项目工时";
                }
            }

            return new PagedResultDto<DepartmentReportDetailDto>(totalCount, dtos);
        }
        [HttpGet]
        public async Task<ExportFileDto> ExportDepartmentTableAsync(DepartmentReportQueryDto input)
        {
            var details = await GetFilteredDetailsAsync(input);
            var dtos = await BuildDetailDtosAsync(details);

            var sb = new StringBuilder();
            sb.AppendLine("工时类别,关联项目名称,关联项目编号,项目角色,填报人,填报人所在部门,任务分类,工作描述,申报工时,申报状态");

            foreach (var d in dtos)
            {
                var statusStr = d.Status == 3 ? "已审批" : (d.Status == 0 ? "审批中" : "其他");
                sb.AppendLine($"{EscapeCsv(d.LaborClass)},{EscapeCsv(d.ProjectName)},{EscapeCsv(d.ProjectCode)},{EscapeCsv(d.ProjectRoleName)},{EscapeCsv(d.ReporterName)},{EscapeCsv(d.DepartmentFullName)},{EscapeCsv(d.LaborCategoryFullName)},{EscapeCsv(d.Jobresponsibilities)},{d.Hours},{statusStr}");
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();

            return new ExportFileDto
            {
                FileName = $"部门报表导出_{DateTime.Now:yyyyMMddHHmmss}.csv",
                Content = bytes
            };
        }
        private string EscapeCsv(string field)
        {
            if (string.IsNullOrEmpty(field)) return "";
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            return field;
        }
        public async Task<List<UserDailyProjectReportDto>> GetUserCrossReportAsync(UserReportQueryDto input)
        {
            var currentUserId = CurrentUser.Id;
            if (!currentUserId.HasValue) throw new UserFriendlyException("未检测到有效登录用户");

            // ================= 1. 获取目标用户集合 (无需额外注入仓储) =================
            var allOuQuery = await _ouRepository.GetQueryableAsync();
            var userQuery = await _userRepository.GetQueryableAsync();

            // 初始过滤：只查出分配了至少一个部门的用户 (底层自动翻译为 EXISTS SQL)
            var targetUsersQuery = userQuery.Where(u => u.OrganizationUnits.Any());

            // 过滤1：前端传入了指定的查询部门
            if (input.DepartmentId.HasValue)
            {
                var selectedOu = await _ouRepository.GetAsync(input.DepartmentId.Value);
                // 【修复】必须使用 ToListAsync() 避免 500 报错
                var subOuIds = await allOuQuery
                    .Where(ou => ou.Code.StartsWith(selectedOu.Code))
                    .Select(ou => ou.Id)
                    .ToListAsync();

                targetUsersQuery = targetUsersQuery.Where(u => u.OrganizationUnits.Any(ou => subOuIds.Contains(ou.OrganizationUnitId)));
            }
            // 过滤2：没有传部门时，走数据权限隔离规则
            else
            {
                var hasAllDataPermission = await _authorizationService.IsGrantedAsync(SC_LaborReportingPermissions.Reports.ReportManagement_BusinessDetailsALL);
                if (!hasAllDataPermission)
                {
                    var user = await _userManager.GetByIdAsync(currentUserId.Value);
                    var userOus = await _userManager.GetOrganizationUnitsAsync(user);

                    if (!userOus.Any()) return new List<UserDailyProjectReportDto>();

                    var allowedOuIds = new List<Guid>();
                    foreach (var userOu in userOus)
                    {
                        // 【修复】必须使用 ToListAsync()
                        var subOuIds = await allOuQuery
                            .Where(ou => ou.Code.StartsWith(userOu.Code))
                            .Select(ou => ou.Id)
                            .ToListAsync();
                        allowedOuIds.AddRange(subOuIds);
                    }
                    allowedOuIds = allowedOuIds.Distinct().ToList();

                    // 限制为只能看到权限范围内部门的用户
                    targetUsersQuery = targetUsersQuery.Where(u => u.OrganizationUnits.Any(ou => allowedOuIds.Contains(ou.OrganizationUnitId)));
                }
            }

            // 过滤3：前端单独筛选了某个人
            if (input.UserId.HasValue)
            {
                targetUsersQuery = targetUsersQuery.Where(u => u.Id == input.UserId.Value);
            }

            // 【修复】将目标用户的 Id 和 Name 异步拉取到内存中，避免查全表造成浪费
            var targetUsers = await targetUsersQuery.Select(u => new { u.Id, u.Name }).ToListAsync();
            var targetUserIds = targetUsers.Select(u => u.Id).ToList();

            // 如果连满足条件的用户都没有，直接返回空
            if (!targetUserIds.Any()) return new List<UserDailyProjectReportDto>();


            // ================= 2. 批量拉取这些目标用户的工时数据 =================
            var query = await _reportRepository.GetQueryableAsync();
            var nextDay = input.EndDate.Date.AddDays(1);

            var reportQuery = query.Where(r =>
                targetUserIds.Contains(r.ReporterId) &&
                r.ReportDate >= input.StartDate.Date &&
                r.ReportDate < nextDay);

            var detailQuery = reportQuery.SelectMany(r => r.Details
                .Where(d => input.Status == 3 ? d.Status == LaborReportStatus.Approved : true)
                .Select(d => new
                {
                    Date = r.ReportDate.Date,
                    UserId = r.ReporterId,
                    ProjectId = d.ProjectId,
                    ProjectName = d.ProjectName,
                    Hours = d.Hours,
                    HoursFinance = d.Hoursfinance
                }));

            // 这里是真正的数据库执行
            var rawData = await detailQuery.ToListAsync();


            // ================= 3. 内存 Left Join：拼装最终结果 =================
            var result = new List<UserDailyProjectReportDto>();

            // 以人为主体进行循环，确保所有人必定生成至少一条记录
            foreach (var user in targetUsers)
            {
                var userReports = rawData.Where(x => x.UserId == user.Id).ToList();

                if (userReports.Any())
                {
                    var userGrouped = userReports
                        .GroupBy(x => new
                        {
                            x.Date,
                            ProjectId = x.ProjectId ?? Guid.Empty,
                            ProjectName = string.IsNullOrWhiteSpace(x.ProjectName) ? "非项目工时" : x.ProjectName
                        })
                        .Select(g => new UserDailyProjectReportDto
                        {
                            DateStr = g.Key.Date.ToString("yyyy-MM-dd"),
                            UserId = user.Id,
                            UserName = user.Name ?? "未知人员",
                            ProjectId = g.Key.ProjectId,
                            ProjectName = g.Key.ProjectName,
                            TotalHours = (double)g.Sum(x => x.Hours),
                            TotalFinanceHours = g.Sum(x => x.HoursFinance)
                        });
                    result.AddRange(userGrouped);
                }
                else
                {
                    // 该用户没有任何数据，塞入一条兜底的空数据
                    result.Add(new UserDailyProjectReportDto
                    {
                        DateStr = string.Empty,
                        UserId = user.Id,
                        UserName = user.Name ?? "未知人员",
                        ProjectId = Guid.Empty,
                        ProjectName = "-",
                        TotalHours = 0,
                        TotalFinanceHours = 0
                    });
                }
            }

            return result;
        }
    }
}