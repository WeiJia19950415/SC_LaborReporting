using System.ComponentModel.DataAnnotations;

namespace SC_LaborReporting.Users
{
    public class WeComLoginDto
    {
        [Required]
        public string Code { get; set; }
    }

    public class BindWeComDto
    {
        [Required]
        public string WeComUserId { get; set; }
    }
}