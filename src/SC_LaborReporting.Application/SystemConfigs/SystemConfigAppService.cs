using SC_LaborReporting.Settings;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.SettingManagement;
using Volo.Abp.Settings;

namespace SC_LaborReporting.SystemConfigs
{
    public class SystemConfigAppService : SC_LaborReportingAppService
    {
        private readonly ISettingManager _settingManager;

        public SystemConfigAppService(ISettingManager settingManager)
        {
            _settingManager = settingManager;
        }

        /// <summary>
        /// 获取系统当前配置
        /// </summary>
        public async Task<SystemConfigDto> GetConfigAsync()
        {
            var ret = new SystemConfigDto
            {
                AttendanceStartDate = await SettingProvider.GetAsync<int>(SC_LaborReportingSettings.AttendanceStartDate),
                AttendanceEndDate = await SettingProvider.GetAsync<int>(SC_LaborReportingSettings.AttendanceEndDate),
                AuditStatus = await SettingProvider.GetAsync<bool>(SC_LaborReportingSettings.AuditStatus),
                WeComAgentId = await SettingProvider.GetOrNullAsync(SC_LaborReportingSettings.WeComAgentId),
                WeComCorpID = await SettingProvider.GetOrNullAsync(SC_LaborReportingSettings.WeComCorpID),
                WeComSecret = await SettingProvider.GetOrNullAsync(SC_LaborReportingSettings.WeComSecret)
            };
            return ret;
        }

        /// <summary>
        /// 更新系统配置（带校验逻辑）
        /// </summary>
        public async Task UpdateConfigAsync(SystemConfigDto input)
        {
            // 核心业务校验：确保日期只能在 1-31 之间
            if (input.AttendanceStartDate < 1 || input.AttendanceStartDate > 31)
            {
                throw new UserFriendlyException("考勤起始日期必须在 1 到 31 之间！");
            }
            if (input.AttendanceEndDate < 1 || input.AttendanceEndDate > 31)
            {
                throw new UserFriendlyException("考勤截至日期必须在 1 到 31 之间！");
            }

            // 更新到全局设置中
            await _settingManager.SetGlobalAsync(SC_LaborReportingSettings.AttendanceStartDate, input.AttendanceStartDate.ToString());
            await _settingManager.SetGlobalAsync(SC_LaborReportingSettings.AttendanceEndDate, input.AttendanceEndDate.ToString());
            await _settingManager.SetGlobalAsync(SC_LaborReportingSettings.AuditStatus, input.AuditStatus.ToString().ToLower());
            // 更新到全局设置中
            await _settingManager.SetGlobalAsync(SC_LaborReportingSettings.WeComAgentId, input.WeComAgentId);
            await _settingManager.SetGlobalAsync(SC_LaborReportingSettings.WeComCorpID, input.WeComCorpID);
            await _settingManager.SetGlobalAsync(SC_LaborReportingSettings.WeComSecret, input.WeComSecret);
        }
    }
}