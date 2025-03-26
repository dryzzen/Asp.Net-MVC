namespace LeaveManagement.ViewModels
{
    public class UserListViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DateOfBirth { get; set; }
        public string Position { get; set; }
        public int AnnualLeaveDaysRemaining { get; set; }
        public int BonusLeaveDaysRemaining { get; set; }
        public int SickDaysTaken { get; set; }
    }
}