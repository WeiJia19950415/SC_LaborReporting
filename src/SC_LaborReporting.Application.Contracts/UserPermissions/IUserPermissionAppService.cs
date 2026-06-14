using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace SC_LaborReporting.UserPermissions
{
    // 继承 IApplicationService，ABP 框架会自动将它识别为一个 API 接口
    public interface IUserPermissionAppService : IApplicationService
    {
        // 定义一个异步方法，返回当前用户拥有的所有权限名称的列表
        Task<List<string>> GetMyPermissionsAsync();
    }
}