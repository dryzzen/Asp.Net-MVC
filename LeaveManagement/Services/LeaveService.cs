using LeaveManagement.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using LeaveManagement.Models;

public class LeaveService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;


    public LeaveService(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<int> GetSickDaysTaken(string userId)
    {
        var sickLeaveRequests = await _context.LeaveRequests
            .Where(lr => lr.UserId == userId && lr.LeaveType == "Sick" && lr.Status == "Approved")
            .ToListAsync();

        var totalSickDaysTaken = sickLeaveRequests.Sum(lr =>
        {
            if (lr.EndDate >= lr.StartDate)
            {
                return (lr.EndDate - lr.StartDate).Days + 1;
            }
            return 0;
        });

        return totalSickDaysTaken;
    }

   
   
    //SICKDAYSONLYABOVE----------------------------------------------------------------------------------------------
    //SICKDAYSONLYABOVE----------------------------------------------------------------------------------------------
    //SICKDAYSONLYABOVE----------------------------------------------------------------------------------------------    //SICKDAYSONLYABOVE----------------------------------------------------------------------------------------------
    //SICKDAYSONLYABOVE----------------------------------------------------------------------------------------------
    //SICKDAYSONLYABOVE----------------------------------------------------------------------------------------------

    public async Task<int> GetRemainingAnnualLeave(string userId)
    {
        var totalAnnualLeaveDays = 21;
        var firstPartDays = 10;
        var secondPartDays = 11;

        var leaveRequests = await _context.LeaveRequests
            .Where(lr => lr.UserId == userId && lr.LeaveType == "Annual" && lr.Status == "Approved")
            .ToListAsync();

        var usedAnnualLeaveDays = leaveRequests.Sum(lr =>
        {
            //neka sedi vo slucaj da ne go napram :)
            if (lr.EndDate >= lr.StartDate)
            {
                return (lr.EndDate - lr.StartDate).Days + 1;
            }
            return 0;
        });


        int remainingLeaveDays = totalAnnualLeaveDays - usedAnnualLeaveDays;
        //odat vo minus idk zs
        remainingLeaveDays = Math.Max(remainingLeaveDays, 0);


        // proveri dali raboti , nekogas ako imas vreme 
        var expiredFirstPart = leaveRequests
            .Where(lr => lr.StartDate <= new DateTime(DateTime.Now.Year, 12, 31) && lr.EndDate >= new DateTime(DateTime.Now.Year, 12, 31))
            .Sum(lr => (lr.EndDate - lr.StartDate).Days + 1);


        if (expiredFirstPart > 0)
        {
            remainingLeaveDays -= Math.Min(firstPartDays, expiredFirstPart);
        }


        var expiredSecondPart = leaveRequests
            .Where(lr => lr.StartDate.Year == DateTime.Now.Year + 1 && lr.StartDate <= new DateTime(DateTime.Now.Year + 1, 6, 30) && lr.EndDate >= new DateTime(DateTime.Now.Year + 1, 6, 30))
            .Sum(lr => (lr.EndDate - lr.StartDate).Days + 1);


        if (expiredSecondPart > 0)
        {
            remainingLeaveDays -= Math.Min(secondPartDays, expiredSecondPart);
        }

        return remainingLeaveDays;
    }
    public async Task<int> GetTotalAnnualLeaveTaken(string userId)
    {
        var leaveRequests = await _context.LeaveRequests
            .Where(lr => lr.UserId == userId && lr.LeaveType == "Annual" && lr.Status == "Approved")
            .ToListAsync();

        return leaveRequests.Sum(lr => (lr.EndDate - lr.StartDate).Days + 1);
    }


    //ANNUALDAYSONLYABOVE----------------------------------------------------------------------------------------------
    //ANNUALDAYSONLYABOVE----------------------------------------------------------------------------------------------
    //ANNUALDAYSONLYABOVE----------------------------------------------------------------------------------------------
    //ANNUALDAYSONLYABOVE----------------------------------------------------------------------------------------------
    //ANNUALDAYSONLYABOVE----------------------------------------------------------------------------------------------

    public async Task<int> GetRemainingBonusLeave(string userId)
    {

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return 0; // User not found
        }

        // Calculate remaining bonus leave days
        var leaveRequests = await _context.LeaveRequests
            .Where(lr => lr.UserId == userId && lr.LeaveType == "Bonus" && lr.Status == "Approved")
            .ToListAsync();

        var usedBonusLeaveDays = leaveRequests.Sum(lr =>
        {
            if (lr.EndDate >= lr.StartDate)
            {
                return (lr.EndDate - lr.StartDate).Days + 1;
            }
            return 0;
        });

        return user.BonusLeaveDays - usedBonusLeaveDays;
    }


    public async Task<int> GetTotalBonusLeaveTaken(string userId)
    {
        var bonusLeaveRequests = await _context.LeaveRequests
            .Where(lr => lr.UserId == userId && lr.LeaveType == "Bonus" && lr.Status == "Approved")
            .ToListAsync();

        return bonusLeaveRequests.Sum(lr => (lr.EndDate - lr.StartDate).Days + 1); // Total days taken
    }

    //BONUSDAYSONLYABOVE----------------------------------------------------------------------------------------------
    //BONUSDAYSONLYABOVE----------------------------------------------------------------------------------------------
    //BONUSDAYSONLYABOVE----------------------------------------------------------------------------------------------
    //BONUSDAYSONLYABOVE----------------------------------------------------------------------------------------------
    //BONUSDAYSONLYABOVE----------------------------------------------------------------------------------------------

}

