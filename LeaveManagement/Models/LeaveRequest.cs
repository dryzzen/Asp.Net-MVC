using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeaveManagement.Models
{
    public class LeaveRequest
    {
        public int Id { get; set; }

        [ForeignKey("SubmittedBy")]
        public string UserId { get; set; }

        [Required]
        [Display(Name = "Leave Type")]
        public string LeaveType { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        public string Status { get; set; } = "Pending";

        public string Comments { get; set; }

      
       
        public string? MedicalReportPath { get; set; } 

        public ApplicationUser SubmittedBy { get; set; }
    }
}