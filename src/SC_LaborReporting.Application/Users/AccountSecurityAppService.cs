using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.Identity;

namespace SC_LaborReporting.Users
{
    public class AccountSecurityAppService : SC_LaborReportingAppService
    {
        private readonly IdentityUserManager _userManager;

        public AccountSecurityAppService(IdentityUserManager userManager)
        {
            _userManager = userManager;
        }

        /// <summary>
        /// 检测当前用户是否需要强制修改密码
        /// </summary>
        [HttpGet("requires-password-change")]
        public async Task<bool> RequiresPasswordChangeAsync()
        {
            var userId = CurrentUser.Id;
            if (userId == null) return false;

            var user = await _userManager.GetByIdAsync(userId.Value);

            // 从额外属性读取标记。如果是 null（从来没设置过），我们默认它为 true（即初次登录必须改）
            var mustChange = user.GetProperty<bool?>("MustChangePassword");

            return mustChange ?? true;
        }

        /// <summary>
        /// 强制修改密码并清除强制标记
        /// </summary>
        [HttpPost("force-change-password")]
        public async Task ForceChangePasswordAsync(ForceChangePasswordDto input)
        {
            var userId = CurrentUser.Id;
            if (userId == null) throw new UserFriendlyException("登录状态已失效");
            var user = await _userManager.GetByIdAsync(userId.Value);
            // 调用 ABP 原生的改密方法，它会自动校验 OldPassword 是否正确以及新密码的复杂度
            var result = await _userManager.ChangePasswordAsync(user, input.CurrentPassword, input.NewPassword);

            if (!result.Succeeded)
            {
                // 可以把具体的密码规则错误抛给前端
                throw new UserFriendlyException("密码修改失败，请确认原密码是否正确，或新密码是否符合复杂度要求！");
            }
            // 修改成功后，清除强制修改标记
            user.SetProperty("MustChangePassword", false);
            await _userManager.UpdateAsync(user);
        }
    }

    public class ForceChangePasswordDto
    {
        [Required(ErrorMessage = "原密码不能为空")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "新密码不能为空")]
        [MinLength(6, ErrorMessage = "新密码长度至少为6位")]
        public string NewPassword { get; set; }
    }
}