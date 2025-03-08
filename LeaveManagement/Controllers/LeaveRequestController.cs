using LeaveManagement.Data;
using LeaveManagement.Models;
using LeaveManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LeaveManagement.Controllers
{
    [Authorize]
    public class LeaveRequestController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public LeaveRequestController(UserManager<ApplicationUser> userManager, ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _context = context;
            _environment = environment;
        }

        // GET: LeaveRequest/Index
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var leaveRequests = await _context.LeaveRequests
                .Include(lr => lr.SubmittedBy)
                .Where(lr => lr.UserId == user.Id)
                .ToListAsync();

            return View(leaveRequests);
        }

        public IActionResult Create()
        {
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LeaveRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            int requestedDays = (model.EndDate - model.StartDate).Days + 1;

            var existingRequest = await _context.LeaveRequests
                .Where(lr => lr.UserId == user.Id && lr.Status == "Pending")
                .FirstOrDefaultAsync();

            if (existingRequest != null)
            {
                ModelState.AddModelError("", "You already have a pending leave request. Please wait for it to be approved or rejected before submitting a new one.");
                return View(model);
            }

        
            if (model.EndDate < model.StartDate)
            {
                ModelState.AddModelError("", "The end date cannot be earlier than the start date.");
                return View(model);
            }
  
            if (model.LeaveType == "Annual" && requestedDays > await GetRemainingAnnualLeave(user.Id))
            {
                ModelState.AddModelError("", "You do not have enough annual leave days.");
            }
            else if (model.LeaveType == "Bonus" && requestedDays > await GetRemainingBonusLeave(user.Id))
            {
                ModelState.AddModelError("", "You do not have enough bonus leave days.");
            }

           
            if (!ModelState.IsValid)
            {
                return View(model);
            }

           
            var leaveRequest = new LeaveRequest
            {
                UserId = user.Id,
                LeaveType = model.LeaveType,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Comments = model.Comments,
                SubmittedBy = user,
                Status = "Pending", 
            };

            _context.LeaveRequests.Add(leaveRequest);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        public IActionResult CreateSickLeave()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSickLeave(SickLeaveRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            int requestedDays = (model.EndDate - model.StartDate).Days + 1; 
            var existingRequest = await _context.LeaveRequests
            .Where(lr => lr.UserId == user.Id && lr.Status == "Pending")
            .FirstOrDefaultAsync();

            if (existingRequest != null)
            {
                ModelState.AddModelError("", "You already have a pending leave request. Please wait for it to be approved or rejected before submitting a new one.");
                return View(model);
            }

            if (model.EndDate < model.StartDate)
            {
                ModelState.AddModelError("", "The end date cannot be earlier than the start date.");
                return View(model);
            }

         
            
            var leaveRequest = new LeaveRequest
            {
                UserId = user.Id,
                LeaveType = model.LeaveType,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Comments = model.Comments,
                SubmittedBy = user,
                Status = "Pending", 
            };

            if (model.MedicalReport != null && model.MedicalReport.Length > 0)
            {
                var uploadsFolderPath = Path.Combine(_environment.ContentRootPath, "uploads");

                if (!Directory.Exists(uploadsFolderPath))
                {
                    Directory.CreateDirectory(uploadsFolderPath);
                }

                var fileName = model.MedicalReport.FileName; 
                var filePath = Path.Combine(uploadsFolderPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.MedicalReport.CopyToAsync(stream);
                }
                leaveRequest.MedicalReportPath = fileName; 
            }
            else
            {
                leaveRequest.MedicalReportPath = null;
            }

            _context.LeaveRequests.Add(leaveRequest);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Search(string query, string status, string leaveType)
        {
            var leaveRequests = await _context.LeaveRequests
                .Include(lr => lr.SubmittedBy) 
                .Where(lr =>
                    (string.IsNullOrEmpty(query) || lr.SubmittedBy.UserName.Contains(query) || lr.LeaveType.Contains(query)) &&
                    (string.IsNullOrEmpty(status) || lr.Status == status) &&
                    (string.IsNullOrEmpty(leaveType) || lr.LeaveType == leaveType)) 
                .ToListAsync();

            return View("SearchResults", leaveRequests); 
        }

        private async Task<int> GetRemainingAnnualLeave(string userId)
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
        private async Task<int> GetRemainingBonusLeave(string userId)
        {
            var totalBonusLeaveDays = 5; 

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

            return totalBonusLeaveDays - usedBonusLeaveDays; 
        }

        public async Task<IActionResult> ApproveRequests()
        {
            var pendingRequests = await _context.LeaveRequests
                .Where(lr => lr.Status == "Pending")
                .Include(lr => lr.SubmittedBy)
                .ToListAsync();

            return View(pendingRequests);
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

                await _userManager.UpdateAsync(user);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("User List");
        }
        


        [Authorize(Roles = "HR")] 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var leaveRequest = await _context.LeaveRequests.FindAsync(id);
            if (leaveRequest != null)
            {
                leaveRequest.Status = "Rejected";
                
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("ApproveRequests");
        }

        [Authorize(Roles = "HR")] 
        [HttpPost]
        [ValidateAntiForgeryToken]
        
        public async Task<IActionResult> Delete(int id)
        {
           
            Console.WriteLine($"Attempting to delete leave request with ID: {id}");

            var leaveRequest = await _context.LeaveRequests.FindAsync(id);
            if (leaveRequest != null)
            {
                _context.LeaveRequests.Remove(leaveRequest);
                await _context.SaveChangesAsync();
                Console.WriteLine("Leave request deleted successfully.");
            }
            else
            {
                Console.WriteLine("Leave request not found.");
            }
            return RedirectToAction("Index", "HR");
        }

    }

    
}