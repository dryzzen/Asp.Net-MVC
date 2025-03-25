using Microsoft.AspNetCore.Identity;

namespace LeaveManagement.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Position { get; set; }
        public int AnnualLeaveDays { get; set; } = 21; 
        public int BonusLeaveDays { get; set; } = 0;
        public int SickLeaveDays { get; set; }



    }
}
