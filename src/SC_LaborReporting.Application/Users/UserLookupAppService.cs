using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Identity;

namespace SC_LaborReporting.Users // 替换为你的实际命名空间
{
    // [Authorize] 标签表示只要用户处于登录状态就能访问，无需特定权限
    [Authorize]
    public class UserLookupAppService : ApplicationService
    {
        private readonly IIdentityUserRepository _userRepository;

        public UserLookupAppService(IIdentityUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        // ABP 会自动将这个方法生成为 API 路由：GET /api/app/user-lookup
        public async Task<List<object>> GetListAsync()
        {
            // 查询所有用户（如果有成千上万用户，这里可以加上 Take(1000) 等限制）
            var users = await _userRepository.GetListAsync();

            // 只返回下拉框需要的基础字段，避免泄露密码哈希、手机号等敏感信息
            return users.Select(u => new
            {
                id = u.Id,
                userName = u.UserName,
                name = u.Name
            }).ToList<object>();
        }
    }
}