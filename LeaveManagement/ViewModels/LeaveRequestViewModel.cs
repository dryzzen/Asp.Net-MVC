using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LeaveManagement.ViewModels
{
    public class LeaveRequestViewModel : IValidatableObject
    {
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

        public string Comments { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if ((LeaveType == "Annual" || LeaveType == "Bonus") && string.IsNullOrWhiteSpace(Comments))
            {
                yield return new ValidationResult("Comments are required for Annual and Bonus leave.", new[] { nameof(Comments) });
            }
        }
    }
}