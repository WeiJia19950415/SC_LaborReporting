using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SC_LaborReporting.enums;
using SC_LaborReporting.LaborCategories;
using SC_LaborReporting.LaborReports;
using SC_LaborReporting.Permissions;
using SC_LaborReporting.Projects;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Content;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using MiniExcelLibs;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.SS.Util;
using System.IO;

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

        private readonly IRepository<SC_LaborReporting.AttendanceDatas.AttendanceData, Guid> _attendanceRepository;

        public ReportAppService(
            IRepository<LaborReport, Guid> reportRepository,
            IRepository<LaborCategory, Guid> laborCategoryRepository,
            IdentityUserManager userManager,
            IRepository<IdentityUser, Guid> userRepository,
            IRepository<OrganizationUnit, Guid> ouRepository,
            IAuthorizationService authorizationService,
            IRepository<LaborReportDetail, Guid> detailRepository,
            IRepository<Project, Guid> ProjectRepository,
            IRepository<SC_LaborReporting.AttendanceDatas.AttendanceData, Guid> attendanceRepository)
        {
            _reportRepository = reportRepository;
            _laborCategoryRepository = laborCategoryRepository;
            _userManager = userManager;
            _userRepository = userRepository;
            _ouRepository = ouRepository;
            _authorizationService = authorizationService;
            _detailRepository = detailRepository;
            _ProjectRepository = ProjectRepository;
            _attendanceRepository = attendanceRepository;
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

        private async Task<List<Guid>> GetDepartmentAndAllChildrenIdsAsync(Guid rootDepartmentId)
        {
            var allDepartments = await _ouRepository.GetListAsync();
            var resultIds = new List<Guid> { rootDepartmentId };
            var queue = new Queue<Guid>();
            queue.Enqueue(rootDepartmentId);

            while (queue.Any())
            {
                var currentId = queue.Dequeue();
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

            var query = await _reportRepository.WithDetailsAsync(x => x.Details);
            var queryable = query.Where(x => x.ReportDate >= input.StartDate && x.ReportDate <= input.EndDate);
            if (input.departmentId.HasValue)
            {
                var targetDepartmentIds = await GetDepartmentAndAllChildrenIdsAsync(input.departmentId.Value);
                queryable = queryable.Where(x => targetDepartmentIds.Contains(x.DepartmentId));
            }

            var hasAllDataPermission = await _authorizationService.IsGrantedAsync(SC_LaborReportingPermissions.Reports.ReportManagement_BusinessDetailsALL);

            if (!hasAllDataPermission)
            {
                var user = await _userManager.GetByIdAsync(userId.Value);
                var userOus = await _userManager.GetOrganizationUnitsAsync(user);

                if (!userOus.Any())
                {
                    return new List<LaborReportDetail>();
                }

                var allOuQuery = await _ouRepository.GetQueryableAsync();
                var allowedOuIds = new List<Guid>();

                foreach (var userOu in userOus)
                {
                    var subOuIds = allOuQuery
                        .Where(ou => ou.Code.StartsWith(userOu.Code))
                        .Select(ou => ou.Id)
                        .ToList();

                    allowedOuIds.AddRange(subOuIds);
                }

                allowedOuIds = allowedOuIds.Distinct().ToList();
                queryable = queryable.Where(x => allowedOuIds.Contains(x.DepartmentId));
            }

            var reports = await queryable.ToListAsync();
            var details = reports.SelectMany(x => x.Details).ToList();

            if (!input.IncludeUnapproved)
            {
                details = details.Where(x => x.Status == LaborReportStatus.Approved).ToList();
            }
            else
            {
                details = details.Where(x => x.Status == LaborReportStatus.Approved || x.Status == LaborReportStatus.Pending).ToList();
            }

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
                while (currentOu != null)
                {
                    ouNamePieces.Insert(0, currentOu.DisplayName);
                    currentOu = currentOu.ParentId.HasValue
                        ? allOus.FirstOrDefault(x => x.Id == currentOu.ParentId.Value)
                        : null;
                }
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
                    DepartmentFullName = ouFullNames.TryGetValue(d.LaborReport.DepartmentId, out var ouname) ? ouname : "",
                    LaborCategoryFullName = string.Join(" - ", nameList),
                    Jobresponsibilities = d.Jobresponsibilities,
                    Hours = d.Hours,
                    SubTime = d.LaborReport.ReportDate.ToString("yyyy-MM-dd"),
                    Status = (int)d.Status,
                    MappingType = categoryMap.TryGetValue(d.LaborCategoryId.GetValueOrDefault(), out var cats)
                    ? cats.MappingType switch
                    {

                        (ActivityMappingType)1 => "管理活动",
                        (ActivityMappingType)2 => "研发活动",
                        (ActivityMappingType)3 => "按照人员部门归属",
                        (ActivityMappingType)4 => "生产活动",
                        (ActivityMappingType)5 => "销售活动",
                        _ => "" // 如果没有匹配的值，则输出空字符串
                    }
                    : ""
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
            sb.AppendLine("申报时间,关联项目名称,关联项目编号,项目角色,填报人,填报人所在部门,任务分类,工作描述,申报工时,申报状态,成本类型");

            foreach (var d in dtos)
            {
                var statusStr = d.Status == 3 ? "已审批" : (d.Status == 0 ? "审批中" : "其他");
                sb.AppendLine($"{EscapeCsv(d.SubTime)},{EscapeCsv(d.ProjectName)},{EscapeCsv(d.ProjectCode)},{EscapeCsv(d.ProjectRoleName)},{EscapeCsv(d.ReporterName)},{EscapeCsv(d.DepartmentFullName)},{EscapeCsv(d.LaborCategoryFullName)},{EscapeCsv(d.Jobresponsibilities)},{d.Hours},{statusStr},{EscapeCsv(d.MappingType)}");
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

            var allOuQuery = await _ouRepository.GetQueryableAsync();
            var userQuery = await _userRepository.GetQueryableAsync();

            var targetUsersQuery = userQuery.Where(u => u.OrganizationUnits.Any());

            if (input.DepartmentId.HasValue)
            {
                var selectedOu = await _ouRepository.GetAsync(input.DepartmentId.Value);
                var subOuIds = await allOuQuery
                    .Where(ou => ou.Code.StartsWith(selectedOu.Code))
                    .Select(ou => ou.Id)
                    .ToListAsync();

                targetUsersQuery = targetUsersQuery.Where(u => u.OrganizationUnits.Any(ou => subOuIds.Contains(ou.OrganizationUnitId)));
            }
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
                        var subOuIds = await allOuQuery
                            .Where(ou => ou.Code.StartsWith(userOu.Code))
                            .Select(ou => ou.Id)
                            .ToListAsync();
                        allowedOuIds.AddRange(subOuIds);
                    }
                    allowedOuIds = allowedOuIds.Distinct().ToList();

                    targetUsersQuery = targetUsersQuery.Where(u => u.OrganizationUnits.Any(ou => allowedOuIds.Contains(ou.OrganizationUnitId)));
                }
            }

            if (input.UserId.HasValue)
            {
                targetUsersQuery = targetUsersQuery.Where(u => u.Id == input.UserId.Value);
            }

            var targetUsers = await targetUsersQuery.Select(u => new { u.Id, u.Name }).ToListAsync();
            var targetUserIds = targetUsers.Select(u => u.Id).ToList();

            if (!targetUserIds.Any()) return new List<UserDailyProjectReportDto>();

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

            var rawData = await detailQuery.ToListAsync();

            var result = new List<UserDailyProjectReportDto>();

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

        [Authorize(SC_LaborReportingPermissions.Reports.UserFinanceReport)]
        public async Task<List<UnsubmittedUserDto>> GetUnsubmittedUsersAsync(UnsubmittedReportQueryDto input)
        {
            var userQuery = await _userRepository.GetQueryableAsync();

            if (input.DepartmentId.HasValue)
            {
                var targetOu = await _ouRepository.GetAsync(input.DepartmentId.Value);

                var ouQuery = await _ouRepository.GetQueryableAsync();
                var ouIds = await ouQuery
                    .Where(ou => ou.Code.StartsWith(targetOu.Code))
                    .Select(ou => ou.Id)
                    .ToListAsync();

                userQuery = userQuery.Where(u => u.OrganizationUnits.Any(x => ouIds.Contains(x.OrganizationUnitId)));
            }

            var targetUsers = await userQuery.ToListAsync();

            var submittedReports = await _reportRepository.GetListAsync(
                r => r.ReportDate.Date == input.QueryDate.Date);

            var submittedUserIds = submittedReports
                .Where(r => r.CreatorId.HasValue)
                .Select(r => r.CreatorId.Value)
                .ToHashSet();

            return targetUsers
                .Where(u => !submittedUserIds.Contains(u.Id))
                .Select(u => new UnsubmittedUserDto
                {
                    UserId = u.Id,
                    UserName = u.UserName,
                    DepartmentName = u.Name
                }).ToList();
        }

        /// <summary>
        /// 获取报表页面展示数据
        /// </summary>
        [HttpGet]
        public async Task<LaborReportSummaryResultDto> GetLaborSummaryReportAsync(LaborReportSummaryQueryDto input)
        {
            return await BuildSummaryDataAsync(input);
        }

        /// <summary>
        /// 导出与附件模板一模一样的 Excel
        /// </summary>
        [HttpGet]
        public async Task<IRemoteStreamContent> ExportLaborSummaryReportAsync(LaborReportSummaryQueryDto input)
        {
            var data = await BuildSummaryDataAsync(input);
            var excelList = new List<IDictionary<string, object>>();

            var nameRow = new ExpandoObject() as IDictionary<string, object>;
            nameRow["月份"] = "";
            nameRow["员工编号"] = "";
            nameRow["所在部门"] = "";
            nameRow["姓名"] = "";

            foreach (var p in data.Projects)
            {
                nameRow[p.ProjectCode] = p.ProjectName;
            }

            nameRow["生产活动"] = "";
            nameRow["销售活动"] = "";
            nameRow["管理活动"] = "";
            nameRow["合计"] = "";
            excelList.Add(nameRow);

            foreach (var row in data.Rows)
            {
                var dataRow = new ExpandoObject() as IDictionary<string, object>;
                dataRow["月份"] = row.Month;
                dataRow["员工编号"] = row.JobNumber;
                dataRow["所在部门"] = row.DepartmentName;
                dataRow["姓名"] = row.Name;

                foreach (var p in data.Projects)
                {
                    dataRow[p.ProjectCode] = row.ProjectHours.ContainsKey(p.ProjectCode) && row.ProjectHours[p.ProjectCode] > 0
                        ? row.ProjectHours[p.ProjectCode]
                        : null;
                }

                dataRow["生产活动"] = row.ProductionHours > 0 ? row.ProductionHours : null;
                dataRow["销售活动"] = row.SalesHours > 0 ? row.SalesHours : null;
                dataRow["管理活动"] = row.ManagementHours > 0 ? row.ManagementHours : null;
                dataRow["合计"] = row.TotalHours > 0 ? row.TotalHours : null;

                excelList.Add(dataRow);
            }

            var memoryStream = new MemoryStream();
            memoryStream.SaveAs(excelList); // 修改为同步方法以修复CS1061错误
            memoryStream.Seek(0, SeekOrigin.Begin);

            return new RemoteStreamContent(memoryStream, $"工时汇总表_{DateTime.Now:yyyyMMdd}.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }

        private async Task<LaborReportSummaryResultDto> BuildSummaryDataAsync(LaborReportSummaryQueryDto input)
        {
            var query = await _reportRepository.WithDetailsAsync(x => x.Details);

            if (!string.IsNullOrWhiteSpace(input.StartMonth) && DateTime.TryParse($"{input.StartMonth}-01", out var startMonthDate))
            {
                // startMonthDate 此时为当月 1 号 00:00:00
                // 减去 1 个月，再加上 25 天，即为上个月 26 号 00:00:00
                var startDate = startMonthDate.AddMonths(-1).AddDays(25);
                query = query.Where(x => x.ReportDate >= startDate);
            }

            if (!string.IsNullOrWhiteSpace(input.EndMonth) && DateTime.TryParse($"{input.EndMonth}-01", out var endMonthDate))
            {
                // endMonthDate 此时为当月 1 号 00:00:00
                // 加上 25 天为下个月（这里口误，是本月 26 号 00:00:00），再减去 1 秒，即为本月 25 号 23:59:59
                var endDate = endMonthDate.AddDays(25).AddSeconds(-1);

                // 或者用更直观的写法： 
                // var endDate = new DateTime(endMonthDate.Year, endMonthDate.Month, 25, 23, 59, 59);

                query = query.Where(x => x.ReportDate <= endDate);
            }

            var reports = await AsyncExecuter.ToListAsync(query);

            var categoryIds = reports.SelectMany(x => x.Details).Select(x => x.LaborCategoryId).Distinct().ToList();
            var categories = await _laborCategoryRepository.GetListAsync(x => categoryIds.Contains(x.Id));
            var categoryMapping = categories.ToDictionary(x => x.Id, x => x.MappingType);

            var validDetails = reports.SelectMany(r => r.Details
                .Where(d => d.Status == LaborReportStatus.Approved)
                .Select(d => new { Report = r, Detail = d })).ToList();

            var result = new LaborReportSummaryResultDto();
            var allProjects = new Dictionary<string, string>();
            var userRows = new Dictionary<string, LaborReportSummaryDto>();
            var userRowDepts = new Dictionary<string, Guid>(); // 用于记录每行对应的部门ID

            foreach (var item in validDetails)
            {
                var r = item.Report;
                var d = item.Detail;
                var monthStr = r.ReportDate.ToString("yyyy-MM");
                var key = $"{r.ReporterId}_{monthStr}";

                if (!userRows.TryGetValue(key, out var row))
                {
                    row = new LaborReportSummaryDto { Month = monthStr, JobNumber = r.ReporterId.ToString() };
                    userRows[key] = row;
                    userRowDepts[key] = r.DepartmentId; // 记录部门ID
                }

                categoryMapping.TryGetValue(d.LaborCategoryId.GetValueOrDefault(), out var mappingType);
                var hours = d.Hoursfinance;

                if (mappingType == ActivityMappingType.RnD)
                {
                    if (!string.IsNullOrWhiteSpace(d.ProjectCode))
                    {
                        var pCode = d.ProjectCode.Trim();
                        allProjects[pCode] = string.IsNullOrWhiteSpace(d.ProjectName) ? "未知项目" : d.ProjectName.Trim();

                        if (!row.ProjectHours.ContainsKey(pCode)) row.ProjectHours[pCode] = 0;
                        row.ProjectHours[pCode] += hours;
                    }
                }
                else if (mappingType == ActivityMappingType.Production)
                {
                    row.ProductionHours += hours;
                }
                else if (mappingType == ActivityMappingType.Sales)
                {
                    row.SalesHours += hours;
                }
                else if (mappingType == ActivityMappingType.Management || mappingType == ActivityMappingType.ByDepartment)
                {
                    row.ManagementHours += hours;
                }

                row.TotalHours += hours;
            }

            var userIds = userRows.Values.Select(x => Guid.Parse(x.JobNumber)).Distinct().ToList();
            var users = await _userRepository.GetListAsync();
            var userDict = users.Where(u => userIds.Contains(u.Id)).ToDictionary(x => x.Id, x => x);

            // 获取全量部门字典
            var allOus = await _ouRepository.GetListAsync();
            var ouDict = allOus.ToDictionary(x => x.Id, x => x);

            foreach (var kvp in userRows)
            {
                var key = kvp.Key;
                var row = kvp.Value;
                var uId = Guid.Parse(row.JobNumber);

                if (userDict.TryGetValue(uId, out var u))
                {
                    row.JobNumber = u.UserName;
                    row.Name = u.Name ?? u.UserName;
                }

                // 通过前面暂存的 userRowDepts 获取当前行的部门 ID，并向上递归查找全层级名称
                if (userRowDepts.TryGetValue(key, out var deptId) && ouDict.TryGetValue(deptId, out var ou))
                {
                    var pathNames = new List<string>();
                    var currentOu = ou;
                    while (currentOu != null)
                    {
                        pathNames.Insert(0, currentOu.DisplayName);
                        currentOu = currentOu.ParentId.HasValue && ouDict.ContainsKey(currentOu.ParentId.Value)
                            ? ouDict[currentOu.ParentId.Value]
                            : null;
                    }
                    row.DepartmentName = string.Join("-", pathNames);
                }
            }

            var rowsList = userRows.Values.ToList();
            if (!string.IsNullOrWhiteSpace(input.Filter))
            {
                var f = input.Filter.Trim().ToLower();
                rowsList = rowsList.Where(x =>
                    (x.Name != null && x.Name.ToLower().Contains(f)) ||
                    (x.JobNumber != null && x.JobNumber.ToLower().Contains(f))
                ).ToList();
            }

            result.Rows = rowsList.OrderBy(x => x.Month).ThenBy(x => x.JobNumber).ToList();
            result.Projects = allProjects.Select(x => new ReportProjectInfoDto { ProjectCode = x.Key, ProjectName = x.Value }).OrderBy(x => x.ProjectCode).ToList();

            return result;
        }

        [HttpGet]
        public async Task<IRemoteStreamContent> ExportUserCrossReportAsync([FromQuery] UserReportQueryDto input, [FromQuery] bool isFinance = false)
        {
            // 💡 防呆校验：如果前端参数没传过来（变成了默认的公元1年），直接抛出异常拦截，不让它查出全是 0 的脏数据
            if (input.StartDate == default || input.EndDate == default)
            {
                throw new UserFriendlyException("后台未能成功接收到时间参数，请检查前端请求格式！");
            }

            if ((input.EndDate - input.StartDate).TotalDays > 31)
                throw new UserFriendlyException("查询时间范围不能超过31天");

            // 1. 获取工时基础数据
            var rawData = await GetUserCrossReportAsync(input);

            // 2. 获取用户对应的部门名称映射（因为 UserDailyProjectReportDto 没有部门名称）
            var allOus = await _ouRepository.GetListAsync();
            var ouDict = allOus.ToDictionary(x => x.Id, x => x);

            var userIds = rawData.Select(x => x.UserId).Distinct().ToList();
            var userDeptMap = new Dictionary<Guid, string>();

            foreach (var uid in userIds)
            {
                var user = await _userManager.GetByIdAsync(uid);
                var userOus = await _userManager.GetOrganizationUnitsAsync(user);
                var firstOu = userOus.FirstOrDefault();

                if (firstOu != null && ouDict.TryGetValue(firstOu.Id, out var currentOu))
                {
                    // 💡 优化1：不断向上追溯，直到 ParentId 为空，此时的 currentOu 就是顶级部门
                    while (currentOu.ParentId.HasValue && ouDict.ContainsKey(currentOu.ParentId.Value))
                    {
                        currentOu = ouDict[currentOu.ParentId.Value];
                    }
                    userDeptMap[uid] = currentOu.DisplayName;
                }
                else
                {
                    userDeptMap[uid] = "-";
                }
            }

            // 3. 获取考勤数据（请根据你的实际仓储调整查询）
            // string startDateStr = input.StartDate.ToString("yyyy-MM-dd");
            // string endDateStr = input.EndDate.ToString("yyyy-MM-dd");
            // var attendanceList = await _attendanceRepository.GetListAsync(a => 
            //     string.Compare(a.AttendanceDate, startDateStr) >= 0 && 
            //     string.Compare(a.AttendanceDate, endDateStr) <= 0);
            string startDateStr = input.StartDate.ToString("yyyy-MM-dd");
            string endDateStr = input.EndDate.ToString("yyyy-MM-dd");
            var attendanceList = await _attendanceRepository.GetListAsync(a =>
                a.AttendanceDate.CompareTo(startDateStr) >= 0 &&
                a.AttendanceDate.CompareTo(endDateStr) <= 0
            );

            // 4. 构建列头结构 (模拟前端 DateColumns)
            var dateColumns = new List<DateColumnInfo>();
            for (var d = input.StartDate.Date; d <= input.EndDate.Date; d = d.AddDays(1))
            {
                var dateStr = d.ToString("yyyy-MM-dd");
                var projectsInDay = rawData
                    .Where(x => x.DateStr == dateStr && x.ProjectId != Guid.Empty)
                    .Select(x => new { x.ProjectId, x.ProjectName })
                    .Distinct()
                    .ToList();

                dateColumns.Add(new DateColumnInfo
                {
                    DateStr = dateStr,
                    Projects = projectsInDay.Select(p => (p.ProjectId, p.ProjectName)).ToList()
                });
            }

            // ==================== 使用 NPOI 构建复杂 Excel ====================
            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet(isFinance ? "财务工时矩阵" : "有效工时矩阵");

            // 创建样式：居中、垂直居中、加粗表头
            var headerStyle = workbook.CreateCellStyle();
            headerStyle.Alignment = HorizontalAlignment.Center;
            headerStyle.VerticalAlignment = VerticalAlignment.Center;
            var font = workbook.CreateFont();
            font.IsBold = true;
            headerStyle.SetFont(font);

            var row0 = sheet.CreateRow(0); // 第一行：固定的列头 + 日期
            var row1 = sheet.CreateRow(1); // 第二行：项目名称

            // 绘制固定列
            var fixedHeaders = new[] { "人员名称", "所在部门", "应报工时", "期间总计" };
            for (int i = 0; i < fixedHeaders.Length; i++)
            {
                var cell0 = row0.CreateCell(i);
                cell0.SetCellValue(fixedHeaders[i]);
                cell0.CellStyle = headerStyle;

                var cell1 = row1.CreateCell(i); // 预占位，用于合并
                cell1.CellStyle = headerStyle;

                // 纵向合并：第0行到第1行，第i列到第i列
                sheet.AddMergedRegion(new CellRangeAddress(0, 1, i, i));
            }

            // 绘制动态列（日期和项目名称）
            int colIndex = fixedHeaders.Length;
            foreach (var dc in dateColumns)
            {
                var dateTitle = dc.DateStr.Substring(5); // 仅显示 MM-dd
                int projectCount = dc.Projects.Count;
                int colsToSpan = projectCount > 0 ? projectCount : 1;

                var dateCell = row0.CreateCell(colIndex);
                dateCell.SetCellValue(dateTitle);
                dateCell.CellStyle = headerStyle;

                // 如果一天有多个项目，横向合并这几个项目对应的列
                if (colsToSpan > 1)
                {
                    sheet.AddMergedRegion(new CellRangeAddress(0, 0, colIndex, colIndex + colsToSpan - 1));
                }

                if (projectCount == 0)
                {
                    var pCell = row1.CreateCell(colIndex);
                    pCell.SetCellValue("(无)");
                    pCell.CellStyle = headerStyle;
                    colIndex++;
                }
                else
                {
                    foreach (var proj in dc.Projects)
                    {
                        var pCell = row1.CreateCell(colIndex);
                        pCell.SetCellValue(proj.ProjectName);
                        pCell.CellStyle = headerStyle;
                        colIndex++;
                    }
                }
            }

            // 5. 填充具体数据
            var userGroups = rawData.GroupBy(x => new { x.UserId, x.UserName });
            int rowIndex = 2; // 数据从第三行（索引2）开始

            foreach (var userGroup in userGroups)
            {
                var row = sheet.CreateRow(rowIndex++);
                var userName = userGroup.Key.UserName;

                // 计算应报工时
                double requiredHours = CalculateRequiredHours(attendanceList, userName, input.StartDate, input.EndDate);
                // 计算期间总工时
                double totalSum = isFinance ? userGroup.Sum(x => x.TotalFinanceHours) : userGroup.Sum(x => x.TotalHours);

                // 写入固定列
                row.CreateCell(0).SetCellValue(userName);
                row.CreateCell(1).SetCellValue(userDeptMap.ContainsKey(userGroup.Key.UserId) ? userDeptMap[userGroup.Key.UserId] : "-");
                row.CreateCell(2).SetCellValue(Math.Round(requiredHours, 1));
                row.CreateCell(3).SetCellValue(Math.Round(totalSum, 1));

                // 写入动态项目列
                int cIdx = fixedHeaders.Length;
                foreach (var dc in dateColumns)
                {
                    if (dc.Projects.Count == 0)
                    {
                        row.CreateCell(cIdx++).SetCellValue("-");
                    }
                    else
                    {
                        foreach (var proj in dc.Projects)
                        {
                            var cellData = userGroup.FirstOrDefault(x => x.DateStr == dc.DateStr && x.ProjectId == proj.ProjectId);
                            double val = cellData != null ? (isFinance ? cellData.TotalFinanceHours : cellData.TotalHours) : 0;

                            if (val > 0)
                                row.CreateCell(cIdx++).SetCellValue(Math.Round(val, 1));
                            else
                                row.CreateCell(cIdx++).SetCellValue("-");
                        }
                    }
                }
            }

            // 自动调整前面几列的列宽
            sheet.SetColumnWidth(0, 15 * 256); // 人员名称
            sheet.SetColumnWidth(1, 20 * 256); // 所在部门
            sheet.SetColumnWidth(2, 12 * 256); // 应报工时
            sheet.SetColumnWidth(3, 12 * 256); // 期间总计

            // 6. 导出文件流
            var memoryStream = new MemoryStream();
            workbook.Write(memoryStream); // 写入 Excel，即使 NPOI 在这步自动关闭了流也没关系

            // 从（可能已关闭的）内存流中安全提取完整的字节数组
            byte[] excelBytes = memoryStream.ToArray();

            // 拿着提取出的干净数据，重新创建一个处于打开状态的新流
            var finalStream = new MemoryStream(excelBytes);

            var titleName = isFinance ? "人员财务工时矩阵表" : "人员有效工时矩阵表";
            var fileName = $"{titleName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return new RemoteStreamContent(
                finalStream,
                fileName,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            );
        }

        /// <summary>
        /// 计算指定人员在指定时间段内的"应报工时"
        /// </summary>
        private double CalculateRequiredHours(List<SC_LaborReporting.AttendanceDatas.AttendanceData> attendanceList, string userName, DateTime startDate, DateTime endDate)
        {
            double totalRequired = 0;
            // 筛选该员工所有的考勤数据
            var userAtt = attendanceList.Where(a => a.Name == userName).ToList();

            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                var dateStr = date.ToString("yyyy-MM-dd"); // 根据你数据库中 AttendanceDate 的格式调整
                var todayAtt = userAtt.FirstOrDefault(a => a.AttendanceDate.StartsWith(dateStr));

                if (todayAtt == null)
                {
                    // 未查到数据：判断是否工作日 (默认排除周六周日)
                    if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                    {
                        totalRequired += 8.0;
                    }
                }
                else
                {
                    // 查到数据：判断是否缺卡
                    if (todayAtt.CheckInResults == "缺卡" || todayAtt.OffdutytimeResults == "缺卡" ||
                        string.IsNullOrWhiteSpace(todayAtt.ClockIntime) || string.IsNullOrWhiteSpace(todayAtt.Offdutytime))
                    {
                        totalRequired += 8.0; // 缺卡记为 8 小时
                    }
                    else
                    {
                        // 正常打卡：计算上班时间 - 下班时间
                        if (DateTime.TryParse(todayAtt.ClockIntime, out var clockIn) &&
                            DateTime.TryParse(todayAtt.Offdutytime, out var clockOut))
                        {
                            var diff = clockOut - clockIn;
                            double dayHours = diff.TotalHours;

                            // 午休扣减条件：上班时间包含中午12点至13点
                            // 如果早上12点前上班，且下午13点后下班，扣减1小时
                            if (clockIn.TimeOfDay <= new TimeSpan(12, 0, 0) && clockOut.TimeOfDay >= new TimeSpan(13, 0, 0))
                            {
                                dayHours -= 1.0;
                            }

                            // 晚休扣减条件：晚上19点之后下班，减去0.5小时
                            if (clockOut.TimeOfDay >= new TimeSpan(19, 0, 0))
                            {
                                dayHours -= 0.5;
                            }

                            if (dayHours < 0) dayHours = 0;
                            totalRequired += dayHours;
                        }
                    }
                }
            }
            return totalRequired;
        }
    }
}