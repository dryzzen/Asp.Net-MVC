using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using LeaveTracker.ViewModels; 
using System.Threading.Tasks;
using LeaveTracker.Models;
using Microsoft.EntityFrameworkCore;
using LeaveTracker.Data;
using LeaveTracker.Services;
using Microsoft.AspNetCore.Authorization;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _context; // DbContext
    private readonly ILeaveService _leaveService;


    public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext context,ILeaveService leaveService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _leaveService = leaveService;
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
                //on lgoin he is automaticly a user role
                await _userManager.AddToRoleAsync(user, "User"); // USer e Employee cuz i dumb

                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Profile", "Account");
            }
            AddIdentityErrors(result);
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
    
    //It takes the mail and sends us to ResetPassword
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

    //It takes the mail from the forgot password and here we cannot chnage it 
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
            //i will use this because what i had previously was RemovePassword and then AddPassword
            //because of the risk of leaving the user without a password we use a security token.
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation");
            }
            AddIdentityErrors(result); // Using helper because there were 1.8k lines of the exact code , and im not kidding :)
        }
        return View(model);
    }

    
    public IActionResult ResetPasswordConfirmation()
    {
        return View();
    }

    
    //User info , and leave info
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }



        var model = new ProfileViewModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            DateOfBirth = user.DateOfBirth,
            Position = user.Position,

            // Leave information
            RemainingAnnualLeave = user.AnnualLeaveDays, 
            RemainingBonusLeave = user.BonusLeaveDays,  
            TotalAnnualLeaveTaken = await _leaveService.GetTotalAnnualLeaveTaken(user.Id),
            TotalBonusLeaveTaken = await _leaveService.GetTotalBonusLeaveTaken(user.Id), 
            SickDaysTaken = await _leaveService.GetSickDaysTaken(user.Id) 
        };

        return View(model);
    }


    //for udpating the profile 
    [Authorize]
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
            AddIdentityErrors(result);
        }

        return View(model);
    }


    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}