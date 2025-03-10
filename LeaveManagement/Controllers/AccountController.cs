using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using LeaveManagement.ViewModels; 
using System.Threading.Tasks;
using LeaveManagement.Models;
using LeaveManagement.ViewModels;
using Microsoft.EntityFrameworkCore;
using LeaveManagement.Data;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _context; // DbContext 

    public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context; 
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegistrationViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                DateOfBirth = model.DateOfBirth,
                Position = model.Position
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Profile", "Account");
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        return View(model);
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                return RedirectToAction("Profile", "Account"); 
            }
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home"); 
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return RedirectToAction("ResetPasswordConfirmation");
            }

            return RedirectToAction("ResetPassword", new { email = model.Email });
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult ResetPassword(string email)
    {
        var model = new ResetPasswordViewModel { Email = email };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return RedirectToAction("ResetPasswordConfirmation");
            }

            var result = await _userManager.RemovePasswordAsync(user);
            if (result.Succeeded)
            {
                result = await _userManager.AddPasswordAsync(user, model.NewPassword);
                if (result.Succeeded)
                {
                    return RedirectToAction("ResetPasswordConfirmation");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
        }
        return View(model);
    }

    public IActionResult ResetPasswordConfirmation()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        // Fetch remaining leave days and sick days taken
        var remainingAnnualLeave = await GetRemainingAnnualLeave(user.Id);
        var remainingBonusLeave = await GetRemainingBonusLeave(user.Id);
        var sickDaysTaken = await GetSickDaysTaken(user.Id); 

        var model = new ProfileViewModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            DateOfBirth = user.DateOfBirth,
            Position = user.Position,
            RemainingAnnualLeave = remainingAnnualLeave,
            RemainingBonusLeave = remainingBonusLeave,
            SickDaysTaken = sickDaysTaken,
            TotalAnnualLeaveDays = 21 
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.DateOfBirth = model.DateOfBirth;
            user.Position = model.Position;

            

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return RedirectToAction("Profile"); 
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return View(model);
    }

    private async Task<int> GetRemainingAnnualLeave(string userId)
    {
        var totalAnnualLeaveDays = 21;

      
        var leaveRequests = await _context.LeaveRequests
            .Where(lr => lr.UserId == userId && lr.LeaveType == "Annual" && lr.Status == "Approved")
            .ToListAsync();

        var usedAnnualLeaveDays = leaveRequests.Sum(lr =>
        {
            //fix
            if (lr.EndDate >= lr.StartDate)
            {
                return (lr.EndDate - lr.StartDate).Days + 1;
            }
            return 0;
        });

        return totalAnnualLeaveDays - usedAnnualLeaveDays;
    }

    private async Task<int> GetRemainingBonusLeave(string userId)
    {
        var totalBonusLeaveDays = 5; 

        
        var leaveRequests = await _context.LeaveRequests
            .Where(lr => lr.UserId == userId && lr.LeaveType == "Bonus" && lr.Status == "Approved")
            .ToListAsync();

        var usedBonusLeaveDays = leaveRequests.Sum(lr =>
        {
            //fix....
            if (lr.EndDate >= lr.StartDate)
            {
                return (lr.EndDate - lr.StartDate).Days + 1;
            }
            return 0; 
        });

        return totalBonusLeaveDays - usedBonusLeaveDays;
    }

    private async Task<int> GetSickDaysTaken(string userId)
    {
       
        var sickLeaveRequests = await _context.LeaveRequests
            .Where(lr => lr.UserId == userId && lr.LeaveType == "Sick")
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
}