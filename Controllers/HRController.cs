using LeaveManagement.Data;
using LeaveManagement.Models;
using LeaveManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Threading.Tasks;

namespace LeaveManagement.Controllers
{
    [Authorize(Roles = "HR")]
    public class HRController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly LeaveService _leaveService;

        public HRController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, LeaveService leaveService)
        {
            _context = context;
            _userManager = userManager;
            _leaveService = leaveService;
        }

        public async Task<IActionResult> Index()
        {
            var leaveRequests = await _context.LeaveRequests
                .Include(lr => lr.SubmittedBy) 
                .ToListAsync();
            return View(leaveRequests);
        }

        [Authorize(Roles = "HR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var leaveRequest = await _context.LeaveRequests.FindAsync(id);
            if (leaveRequest != null)
            {
                leaveRequest.Status = "Approved";

                var user = await _userManager.FindByIdAsync(leaveRequest.UserId);
                int approvedDays = (leaveRequest.EndDate - leaveRequest.StartDate).Days + 1;

                if (leaveRequest.LeaveType == "Annual")
                {
                    user.AnnualLeaveDays -= approvedDays;
                }
                else if (leaveRequest.LeaveType == "Bonus")
                {
                    user.BonusLeaveDays -= approvedDays;
                }
                else if (leaveRequest.LeaveType == "Sick")
                {
                    user.SickLeaveDays += approvedDays;
                }

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    await _context.SaveChangesAsync(); 
                }
            }
            return RedirectToAction("Index");
        }


        public async Task<IActionResult> Reject(int id)
        {
            var leaveRequest = await _context.LeaveRequests.FindAsync(id);
            if (leaveRequest == null)
            {
                return NotFound();
            }

            leaveRequest.Status = "Rejected";
            _context.LeaveRequests.Update(leaveRequest);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        public async Task<IActionResult> UserList()
        {
            var users = await _userManager.Users.ToListAsync();
            var leaveRequests = await _context.LeaveRequests.ToListAsync();

            var userLeaveData = users.Select(user => new
            {
                User = user,
                SickDaysTaken = leaveRequests
                    .Where(lr => lr.UserId == user.Id && lr.LeaveType == "Sick")
                    .Sum(lr => (lr.EndDate - lr.StartDate).Days + 1),
                AnnualLeaveDaysRemaining = user.AnnualLeaveDays,
                BonusLeaveDaysRemaining = user.BonusLeaveDays
            }).ToList();

            return View(userLeaveData);

        }

       
        [HttpGet]
        public IActionResult RegisterUser()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterUser(RegistrationViewModel model, string role)
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
                    Position = model.Position,
                    AnnualLeaveDays = 21,
                    BonusLeaveDays = 5
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // Assign the selected role (HR or User)
                    await _userManager.AddToRoleAsync(user, role);

                    return RedirectToAction("UserList"); // Redirect to the user list after successful registration
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model); // Return the view with the model if there are validation errors
        }



        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var model = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                DateOfBirth = user.DateOfBirth,
                Position = user.Position,
                AnnualLeaveDays = await _leaveService.GetRemainingAnnualLeave(user.Id),
                BonusLeaveDays = await _leaveService.GetRemainingBonusLeave(user.Id),
                Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() // Get the user's current role
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                user.Email = model.Email;
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.DateOfBirth = model.DateOfBirth;
                user.Position = model.Position;
                user.AnnualLeaveDays = model.AnnualLeaveDays;
                user.BonusLeaveDays = model.BonusLeaveDays;

                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, model.Role);

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction("UserList");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            //raboti ali poradi nekoja pricina "You cannot delete the HR user." ne izlegva. Vidi so e rabotata 
            if (user.Email == "hr@example.com")
            {
                ModelState.AddModelError(string.Empty, "You cannot delete the HR user.");
              
                var users = await _userManager.Users.ToListAsync();
                return View("UserList", users); 
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                return RedirectToAction("UserList");
            }
            //handle errors ?
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return RedirectToAction("UserList"); 
        }

        [Authorize(Roles = "HR")]
        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> DeleteLeaveRequest(int id)
        {

            var leaveRequest = await _context.LeaveRequests.FindAsync(id);
            if (leaveRequest != null)
            {
                var user = await _userManager.FindByIdAsync(leaveRequest.UserId);
                int approvedDays = (leaveRequest.EndDate - leaveRequest.StartDate).Days + 1;

                if (leaveRequest.LeaveType == "Annual")
                {
                    user.AnnualLeaveDays += approvedDays; 
                }
                else if (leaveRequest.LeaveType == "Bonus")
                {
                    user.BonusLeaveDays += approvedDays; 
                }
                else if (leaveRequest.LeaveType == "Sick")
                {
                    user.SickLeaveDays -= approvedDays; 
                }

                _context.LeaveRequests.Remove(leaveRequest);
                await _userManager.UpdateAsync(user); 
                await _context.SaveChangesAsync(); 
            }
            return RedirectToAction("Index");
        }


        public async Task<IActionResult> GrantBonusLeave(string userId, int days)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.BonusLeaveDays += days; 
                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction("Index");
        }

    }
}