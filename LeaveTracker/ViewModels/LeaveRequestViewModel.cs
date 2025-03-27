using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LeaveTracker.Models;

namespace LeaveTracker.ViewModels
{
    public class LeaveRequestViewModel 
    {
        [Required]
        [Display(Name = "Leave Type")]
        public  LeaveType LeaveType{ get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        public string Comments { get; set; }

      
    }
}