using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace SC_LaborReporting.Users;

public interface IUserManagementAppService : IApplicationService
{
    Task<PagedResultDto<UserDetailDto>> GetListAsync(PagedAndSortedResultRequestDto input);
    Task<UserDetailDto> GetAsync(Guid id);
    Task<UserDetailDto> CreateAsync(CreateUserInput input);
    Task UpdateAsync(Guid id, UpdateUserInput input);
    Task DeleteAsync(Guid id);
    Task<List<string>> GetMyPermissionsAsync();
    Task ResetPasswordAsync(Guid id);
}