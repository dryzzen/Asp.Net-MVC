using System;
using System.ComponentModel.DataAnnotations;

namespace LeaveTracker.ViewModels
{
    public class ProfileViewModel
    {
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Position")]
        public string Position { get; set; }

        // Leave information
        public int RemainingAnnualLeave { get; set; }
        public int RemainingBonusLeave { get; set; }
        public int TotalAnnualLeaveTaken { get; set; }
      
        public int TotalBonusLeaveTaken { get; set; }
        public int SickDaysTaken { get; set; } 
     



      
    }
}