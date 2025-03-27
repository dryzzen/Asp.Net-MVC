using LeaveManagement.Data;
using LeaveManagement.Models;
using LeaveManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Threading.Tasks;
using LeaveManagement.Services;

namespace LeaveManagement.Controllers
{
    [Authorize(Roles = "HR")]
    public class HRController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILeaveService _leaveService;

        public HRController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILeaveService leaveService)
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
                leaveRequest.Status = LeaveStatus.Approved;

                var user = await _userManager.FindByIdAsync(leaveRequest.UserId);
                int approvedDays = (leaveRequest.EndDate - leaveRequest.StartDate).Days + 1;

                if (leaveRequest.LeaveType == LeaveType.Annual)
                {
                    user.AnnualLeaveDays -= approvedDays;
                }
                if (leaveRequest.LeaveType == LeaveType.Bonus)
                {
                    user.BonusLeaveDays -= approvedDays;
                }
                if (leaveRequest.LeaveType == LeaveType.Sick)
                {
                    user.SickLeaveDays += approvedDays;
                }

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    TempData["Success"] = "Leave request approved successfully";

                    await _context.SaveChangesAsync(); 
                }
            }
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "HR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var leaveRequest = await _context.LeaveRequests.FindAsync(id);
            if (leaveRequest == null)
            {
                return NotFound();
            }
            TempData["Success"] = "Leave request rejected";
            leaveRequest.Status = LeaveStatus.Rejected;
            _context.LeaveRequests.Update(leaveRequest);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        public async Task<IActionResult> UserList()
        {
            var users = await _userManager.Users.ToListAsync();
            var leaveRequests = await _context.LeaveRequests.ToListAsync();

            var userLeaveData = users.Select(user => new UserListViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                DateOfBirth = user.DateOfBirth?.ToString("yyyy-MM-dd"),
                Position = user.Position,
                AnnualLeaveDaysRemaining = user.AnnualLeaveDays,
                BonusLeaveDaysRemaining = user.BonusLeaveDays,
                SickDaysTaken = leaveRequests
                    .Where(lr => lr.UserId == user.Id &&
                                 lr.LeaveType == LeaveType.Sick &&
                                 lr.Status == LeaveStatus.Approved)
                    .Sum(lr => (lr.EndDate - lr.StartDate).Days + 1)
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
                    await _userManager.AddToRoleAsync(user, role);

                    return RedirectToAction("UserList"); 
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model); 
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
                AnnualLeaveDays = user.AnnualLeaveDays,
                BonusLeaveDays = user.BonusLeaveDays,
                Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() 
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

                //dont change this 
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

           //so i left this because idk let it stay here , but the delete option was removed from hr@example.com
            if (user.Email == "hr@example.com")
            {
                TempData["Error"] = "Cannot delete HR user.";
                return RedirectToAction("UserList");
            }


            var userLeaveRequests = _context.LeaveRequests
          .Where(lr => lr.UserId == id)
         .ToList();

            _context.LeaveRequests.RemoveRange(userLeaveRequests);
            await _context.SaveChangesAsync(); 


            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                TempData["Error"] = "Failed to delete user.";
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

                if (leaveRequest.LeaveType == LeaveType.Annual)
                {
                    user.AnnualLeaveDays += approvedDays; 
                }
                if (leaveRequest.LeaveType == LeaveType.Bonus)
                {
                    user.BonusLeaveDays += approvedDays; 
                }
                if (leaveRequest.LeaveType == LeaveType.Sick)
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