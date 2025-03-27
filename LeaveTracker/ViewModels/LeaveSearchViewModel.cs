using LeaveTracker.Models;

namespace LeaveTracker.ViewModels
{
    public class LeaveSearchViewModel
    {
        public string Query { get; set; }
        public LeaveStatus? Status { get; set; }
        public LeaveType? LeaveType { get; set; }
    }
}
