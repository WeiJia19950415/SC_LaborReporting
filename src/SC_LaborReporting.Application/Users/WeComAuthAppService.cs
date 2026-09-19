using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.Identity;

namespace SC_LaborReporting.Users
{
    public class WeComAuthAppService : SC_LaborReportingAppService
    {
        private readonly IdentityUserManager _userManager;
        private readonly IHttpClientFactory _httpClientFactory;

        // 这里应放在 appsettings.json 中读取，此处仅作演示
        private const string CorpId = "ww36932a56e46020af";
        private const string Secret = "DR9uRbgJt28ufDOBVCTtfRtGyEcVDMUlE-3rhPMUdfQ";

        public WeComAuthAppService(
            IdentityUserManager userManager,
            IHttpClientFactory httpClientFactory)
        {
            _userManager = userManager;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// 企微静默登录 (前端通过企微返回的code调用)
        /// </summary>
        public async Task<string> LoginByWeComCodeAsync(WeComLoginDto input)
        {
            // 1. 通过 Code 获取企微的 WeComUserId
            string weComUserId = await GetWeComUserIdAsync(input.Code);

            // 2. 在系统用户表中查找是否已有绑定该 WeComUserId 的用户
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => EF.Property<string>(u, "WeComUserId") == weComUserId);

            if (user == null)
            {
                // 3. 如果没找到，抛出带有特定错误码的异常，并把 weComUserId 传给前端，让前端跳去绑定页
                throw new UserFriendlyException("未绑定系统账号", "WeCom_Not_Bound")
                    .WithData("WeComUserId", weComUserId);
            }

            // 4. 如果找到了，签发 JWT Token 给前端登录
            // (注意：此处需根据你项目使用的是 OpenIddict 还是 JwtBearer，调用对应的 Token 生成方法)
            return await GenerateJwtTokenAsync(user);
        }

        /// <summary>
        /// 绑定企微ID (前端使用账号密码登录获取Token后，携Token和WeComUserId调用此接口)
        /// </summary>
        [Authorize]
        public async Task BindWeComUserIdAsync(BindWeComDto input)
        {
            var userId = CurrentUser.Id.Value;
            var user = await _userManager.GetByIdAsync(userId);

            // 使用 ABP 扩展属性保存企业微信ID
            user.SetProperty("WeComUserId", input.WeComUserId);

            await _userManager.UpdateAsync(user);
        }

        // ================== 辅助私有方法 ==================

        private async Task<string> GetWeComUserIdAsync(string code)
        {
            var client = _httpClientFactory.CreateClient();

            // a. 获取 AccessToken
            var tokenResponse = await client.GetAsync($"https://qyapi.weixin.qq.com/cgi-bin/gettoken?corpid={CorpId}&corpsecret={Secret}");
            var tokenJson = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync());
            var accessToken = tokenJson.RootElement.GetProperty("access_token").GetString();

            // b. 换取 UserId
            var userResponse = await client.GetAsync($"https://qyapi.weixin.qq.com/cgi-bin/auth/getuserinfo?access_token={accessToken}&code={code}");
            var userJson = JsonDocument.Parse(await userResponse.Content.ReadAsStringAsync());

            if (userJson.RootElement.TryGetProperty("userid", out var userIdElement))
            {
                return userIdElement.GetString();
            }

            throw new UserFriendlyException("获取企微用户信息失败");
        }

        private async Task<string> GenerateJwtTokenAsync(IdentityUser user)
        {
            // TODO: 在这里实现你系统的 Token 签发逻辑。
            // 如果系统使用了 ABP 的 OpenIddict 默认实现，建议在这里利用 HttpContext 模拟密码登录流程，
            // 或者使用 JwtSecurityTokenHandler 手动签发与系统配置一致的 Token。
            return "your_generated_jwt_token";
        }
    }
}