using System.ComponentModel.DataAnnotations;

namespace LeaveTracker.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
