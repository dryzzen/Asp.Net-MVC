using LeaveManagement.Models;

namespace LeaveManagement.ViewModels
{
    public class LeaveSearchViewModel
    {
        public string Query { get; set; }
        public LeaveStatus? Status { get; set; }
        public LeaveType? LeaveType { get; set; }
    }
}
