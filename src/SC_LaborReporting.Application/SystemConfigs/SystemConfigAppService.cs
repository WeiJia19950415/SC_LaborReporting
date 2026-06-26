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
            return new SystemConfigDto
            {
                AttendanceStartDate = await SettingProvider.GetAsync<int>(SC_LaborReportingSettings.AttendanceStartDate),
                AttendanceEndDate = await SettingProvider.GetAsync<int>(SC_LaborReportingSettings.AttendanceEndDate),
                AuditStatus = await SettingProvider.GetAsync<bool>(SC_LaborReportingSettings.AuditStatus)
            };
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
        }
    }
}