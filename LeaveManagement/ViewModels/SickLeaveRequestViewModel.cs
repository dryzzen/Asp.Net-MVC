using System;
using System.ComponentModel.DataAnnotations;
using LeaveManagement.Models;
using Microsoft.AspNetCore.Http;

namespace LeaveManagement.ViewModels
{
    public class SickLeaveRequestViewModel
    {
        [Required]
        [Display(Name = "Leave Type")]
        public LeaveType LeaveType { get; set; } = LeaveType.Sick;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        public string Comments { get; set; }

        [Required(ErrorMessage = "Medical report is required for sick leave.")]
        [Display(Name = "Medical Report")]
        public IFormFile MedicalReport { get; set; } // For file upload
    }
}