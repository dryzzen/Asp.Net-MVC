using System.ComponentModel.DataAnnotations;

namespace LeaveManagement.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
