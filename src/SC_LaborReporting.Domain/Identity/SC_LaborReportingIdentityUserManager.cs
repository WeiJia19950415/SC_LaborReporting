using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;
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
using Volo.Abp.Domain.Repositories;

namespace SC_LaborReporting.Identity;

[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(IdentityUserManager))]
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

    public override async Task<IdentityUser> FindByNameAsync(string userName)
    {
        // 1. 先尝试使用 ABP 原生的用户名去查找用户
        var user = await base.FindByNameAsync(userName);
        if (user != null)
        {
            return user;
        }
        var genericRepository = UserRepository as IRepository<IdentityUser, Guid>;
        if (genericRepository != null)
        {
            var queryable = await genericRepository.GetQueryableAsync();
            user = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                queryable,
                u => EF.Property<string>(u, "JobNumber") == userName
            );
        }
        
        return user;
    }
}