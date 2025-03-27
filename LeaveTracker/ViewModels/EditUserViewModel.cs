using System.ComponentModel.DataAnnotations;

namespace LeaveTracker.ViewModels
{
    public class EditUserViewModel
    {
        public string Id { get; set; } 

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Display(Name = "First Name")]
        public string? FirstName { get; set; } 

        [Display(Name = "Last Name")]
        public string? LastName { get; set; } 

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; } 

        [Display(Name = "Position")]
        public string? Position { get; set; }

        public int AnnualLeaveDays { get; set; }
        public int BonusLeaveDays { get; set; }
        public string Role { get; set; }
    }

}
