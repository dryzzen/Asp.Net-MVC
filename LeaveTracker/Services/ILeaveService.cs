namespace LeaveTracker.Services
{
    public interface ILeaveService
    {
        Task<int> GetSickDaysTaken(string userId);
        Task<int> GetRemainingAnnualLeave(string userId);
        Task<int> GetTotalAnnualLeaveTaken(string userId);
        Task<int> GetRemainingBonusLeave(string userId);
        Task<int> GetTotalBonusLeaveTaken(string userId);
    }
}
