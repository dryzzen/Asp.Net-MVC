using LeaveTracker.Models;

namespace LeaveTracker.ViewModels
{
    public class UserLeaveViewModel
    {

        public ApplicationUser User { get; set; }

        public int SickDaysTaken { get; set; }

        public int AnnualLeaveDaysRemaining { get; set; }
        public int BonusLeaveDaysRemaining { get; set; }
    }
}
