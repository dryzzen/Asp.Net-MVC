using LeaveManagement.Data;
using LeaveManagement.Models;
using LeaveManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace LeaveManagement.Controllers
{
    [Authorize(Roles = "HR")]
    public class HRController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HRController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var leaveRequests = await _context.LeaveRequests
                .Include(lr => lr.SubmittedBy) 
                .ToListAsync();
            return View(leaveRequests);
        }

        public async Task<IActionResult> Approve(int id)
        {
            var leaveRequest = await _context.LeaveRequests.FindAsync(id);
            if (leaveRequest == null)
            {
                return NotFound();
            }

            leaveRequest.Status = "Approved";
            _context.LeaveRequests.Update(leaveRequest);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
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
        public async Task<IActionResult> RegisterUser(RegistrationViewModel model)
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
                    await _userManager.AddToRoleAsync(user, model.Role); 

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
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                DateOfBirth = user.DateOfBirth,
                Position = user.Position
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

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
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
    }
}