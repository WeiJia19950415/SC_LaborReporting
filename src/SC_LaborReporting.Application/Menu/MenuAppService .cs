using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization.Permissions;

namespace SC_LaborReporting.Menu
{
    public class MenuAppService : ApplicationService
    {
        private readonly IPermissionChecker _permissionChecker;

        public MenuAppService(IPermissionChecker permissionChecker)
        {
            _permissionChecker = permissionChecker;
        }

        public async Task<List<MenuDto>> GetMenusAsync()
        {
            var menus = new List<MenuDto>();

            if (await _permissionChecker.IsGrantedAsync("SC_LaborReporting.System"))
            {
                menus.Add(new MenuDto
                {
                    Name = "系统管理",
                    Path = "/system"
                });
            }

            return menus;
        }
    }
}
