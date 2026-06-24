using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SC_LaborReporting.LaborCategories; // 引入工时分类命名空间
using SC_LaborReporting.LaborReports;
using SC_LaborReporting.Permissions;
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


        public ReportAppService(
            IRepository<LaborReport, Guid> reportRepository,
            IRepository<LaborCategory, Guid> laborCategoryRepository,
            IdentityUserManager userManager,
            IRepository<IdentityUser, Guid> userRepository,
            IRepository<OrganizationUnit, Guid> ouRepository,
            IAuthorizationService authorizationService)
        {
            _reportRepository = reportRepository;
            _laborCategoryRepository = laborCategoryRepository;
            _userManager = userManager;
            _userRepository = userRepository;
            _ouRepository = ouRepository;
            _authorizationService = authorizationService;
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
                        LaborCategoryId = d.LaborCategoryId,
                        LaborCategoryCode = d.LaborCategoryCode,
                        LaborCategoryFullName = fullNameText, 
                        ProjectId = d.ProjectId,
                        ProjectName = d.ProjectName,
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
        private async Task<List<LaborReportDetail>> GetFilteredDetailsAsync(DepartmentReportQueryDto input)
        {
            var userId = CurrentUser.Id;
            if (!userId.HasValue) throw new UserFriendlyException("未检测到有效登录用户");

            var hasAllDataPermission = await _authorizationService.IsGrantedAsync(SC_LaborReportingPermissions.Reports.AllData);

            var query = await _reportRepository.WithDetailsAsync(x => x.Details);
            var queryable = query.Where(x => x.ReportDate >= input.StartDate && x.ReportDate <= input.EndDate);

            if (!hasAllDataPermission)
            {
                var user = await _userManager.GetByIdAsync(userId.Value);
                var ous = await _userManager.GetOrganizationUnitsAsync(user);
                var myOuIds = ous.Select(o => o.Id).ToList();

                queryable = queryable.Where(x => myOuIds.Contains(x.DepartmentId));
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
                                         Name = string.IsNullOrEmpty(g.Key.ProjectName) ? "其它工时" : g.Key.ProjectName,
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

            var ouIds = details.Select(x => x.LaborReport.DepartmentId).Distinct().ToList();
            var ous = await _ouRepository.GetListAsync(x => ouIds.Contains(x.Id));
            var ouMap = ous.ToDictionary(x => x.Id, x => x.DisplayName);

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
                    DepartmentFullName = ouMap.TryGetValue(d.LaborReport.DepartmentId, out var ouname) ? ouname : "",
                    LaborCategoryFullName = string.Join(" - ", nameList),
                    Jobresponsibilities = d.Jobresponsibilities,
                    Hours = d.Hours,
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
  
    }
}