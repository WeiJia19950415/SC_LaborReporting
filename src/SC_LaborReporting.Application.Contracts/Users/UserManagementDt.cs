using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace SC_LaborReporting.Users;

// 列表与详情返回实体
public class UserDetailDto : EntityDto<Guid>
{
    public string UserName { get; set; }
    public string Name { get; set; }
    public string PhoneNumber { get; set; }
    public string JobNumber { get; set; }

    public bool IsActive { get; set; }
    public Guid? DepartmentId { get; set; }
    public string DepartmentName { get; set; }
    public string[] RoleNames { get; set; }
}

// 创建用户输入实体
public class CreateUserInput
{
    [Required] public string UserName { get; set; }
    [Required] public string Name { get; set; }
    [Required] public string PhoneNumber { get; set; }
    [Required] public string Password { get; set; } // 新增用户必须有密码
    [Required] public string JobNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? DepartmentId { get; set; }
    public string[] RoleNames { get; set; }
}

// 修改用户输入实体
public class UpdateUserInput
{
    public bool IsActive { get; set; }
    public Guid? DepartmentId { get; set; }
    public string[] RoleNames { get; set; }
}