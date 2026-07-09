using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Caching;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Security.Claims;
using Volo.Abp.Settings;
using Volo.Abp.Threading;

namespace SC_LaborReporting.Identity;

// 👇 [Dependency(ReplaceServices = true)] 极为重要，它会告诉 ABP 用这个子类全面替代原有的内置 UserManager
[Dependency(ReplaceServices = true)]
public class SC_LaborReportingIdentityUserManager : IdentityUserManager
{
    public SC_LaborReportingIdentityUserManager(IdentityUserStore store, 
        IIdentityRoleRepository roleRepository, 
        IIdentityUserRepository userRepository, 
        IOptions<IdentityOptions> optionsAccessor, 
        IPasswordHasher<IdentityUser> passwordHasher, 
        IEnumerable<IUserValidator<IdentityUser>> userValidators, 
        IEnumerable<IPasswordValidator<IdentityUser>> passwordValidators, 
        ILookupNormalizer keyNormalizer, IdentityErrorDescriber errors, 
        IServiceProvider services, ILogger<IdentityUserManager> logger, 
        ICancellationTokenProvider cancellationTokenProvider, 
        IOrganizationUnitRepository organizationUnitRepository, 
        ISettingProvider settingProvider, 
        IDistributedEventBus distributedEventBus, 
        IIdentityLinkUserRepository identityLinkUserRepository, 
        IDistributedCache<AbpDynamicClaimCacheItem> dynamicClaimCache, 
        IOptions<AbpMultiTenancyOptions> multiTenancyOptions,
        ICurrentTenant currentTenant, IDataFilter dataFilter) :
        base(store, 
            roleRepository, 
            userRepository, 
            optionsAccessor, 
            passwordHasher, 
            userValidators, 
            passwordValidators, 
            keyNormalizer, 
            errors,
            services, 
            logger, 
            cancellationTokenProvider, 
            organizationUnitRepository, 
            settingProvider, 
            distributedEventBus, 
            identityLinkUserRepository, 
            dynamicClaimCache, 
            multiTenancyOptions, 
            currentTenant, 
            dataFilter)
    {
    }


    /// <summary>
    /// 重写根据名称查找用户的方法
    /// </summary>
    public override async Task<IdentityUser> FindByNameAsync(string userName)
    {
        // 1. 首先，尝试按照系统原生的“用户名 (UserName)”去查找
        var user = await base.FindByNameAsync(userName);
        if (user != null)
        {
            return user;
        }
        user = await Users.FirstOrDefaultAsync(u => EF.Property<string>(u, "JobNumber") == userName);
        return user;
    }
}