using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SC_LaborReporting.Users;
using System.IO;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc;

namespace SC_LaborReporting.Controllers
{
    [Route("api/app/user-management/import")]
    public class UserImportController : AbpController
    {
        private readonly IUserManagementAppService _userManagementAppService;

        public UserImportController(IUserManagementAppService userManagementAppService)
        {
            _userManagementAppService = userManagementAppService;
        }

        [HttpPost]
        public async Task<IActionResult> ImportUsers(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("没有获取到文件");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);

            // 调用服务层
            var errorExcelBytes = await _userManagementAppService.ImportUsersAsync(ms.ToArray());

            if (errorExcelBytes != null && errorExcelBytes.Length > 0)
            {
                return File(errorExcelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "导入失败人员名单.xlsx");
            }

            return Ok(new { success = true });
        }
    }
}