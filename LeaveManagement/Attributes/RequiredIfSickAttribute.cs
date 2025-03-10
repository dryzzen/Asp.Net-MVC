using System;
using System.ComponentModel.DataAnnotations;

namespace LeaveManagement.Attributes
{
    public class RequiredIfSickAttribute : ValidationAttribute
    {
        private readonly string _comparisonProperty;

        public RequiredIfSickAttribute(string comparisonProperty)
        {
            _comparisonProperty = comparisonProperty;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var leaveTypeProperty = validationContext.ObjectType.GetProperty(_comparisonProperty);
            if (leaveTypeProperty == null)
            {
                return new ValidationResult($"Unknown property: {_comparisonProperty}");
            }

            var leaveTypeValue = leaveTypeProperty.GetValue(validationContext.ObjectInstance)?.ToString();
            if (leaveTypeValue == "Sick" && value == null)
            {
                return new ValidationResult("Medical report is required for sick leave.");
            }

            return ValidationResult.Success;
        }
    }
}