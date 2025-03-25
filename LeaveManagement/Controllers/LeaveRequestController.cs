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
        private readonly LeaveService _leaveService;

        public LeaveRequestController(UserManager<ApplicationUser> userManager, ApplicationDbContext context, IWebHostEnvironment environment,LeaveService leaveService)
        {
            _userManager = userManager;
            _context = context;
            _environment = environment;
            _leaveService = leaveService;
        }

        //za prikaz na site leave requestoj na usero
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

            //Da ne mozi da se submitni request pred denesniot den
            if (model.StartDate < DateTime.Today)
            {
                ModelState.AddModelError("", "You cannot submit a leave request for a date before today.");
                return View(model);
            }


            //samo eden request na vreme
            var existingRequest = await _context.LeaveRequests
                .Where(lr => lr.UserId == user.Id && lr.Status == "Pending")
                .FirstOrDefaultAsync();

            if (existingRequest != null)
            {
                ModelState.AddModelError("", "You already have a pending leave request. Please wait for it to be approved or rejected before submitting a new one.");
                return View(model);
            }

            //nemozi na odobren den pak da pustis request
            var overlappingRequests = await _context.LeaveRequests
            .Where(lr => lr.UserId == user.Id && lr.Status == "Approved" &&
                  ((model.StartDate >= lr.StartDate && model.StartDate <= lr.EndDate) ||
                   (model.EndDate >= lr.StartDate && model.EndDate <= lr.EndDate) ||
                   (model.StartDate <= lr.StartDate && model.EndDate >= lr.EndDate)))
             .ToListAsync();

            if (overlappingRequests.Any())
            {
                ModelState.AddModelError("", "You cannot submit a leave request that overlaps with an existing approved leave request.");
                return View(model);
            }


            //End date za request nemozi da e pomalo od Start date
            if (model.EndDate < model.StartDate)
            {
                ModelState.AddModelError("", "The end date cannot be earlier than the start date.");
                return View(model);
            }
            

            //ima pomalce denovi od baranite
            if (model.LeaveType == "Annual" && requestedDays > await _leaveService.GetRemainingAnnualLeave(user.Id))
            {
                ModelState.AddModelError("", "You do not have enough annual leave days.");
            }
            else if (model.LeaveType == "Bonus" && requestedDays > await _leaveService.GetRemainingBonusLeave(user.Id))
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


        //ti treba ova bidejki MedicalReportPath iako e nullable pak go bara za annual i za bonus days , vaka si ima seperate method.
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

            if (model.StartDate < DateTime.Today)
            {
                ModelState.AddModelError("", "You cannot submit a sick leave request for a date before today.");
                return View(model);
            }

            var overlappingRequests = await _context.LeaveRequests
                .Where(lr => lr.UserId == user.Id && lr.Status == "Approved" &&
                      ((model.StartDate >= lr.StartDate && model.StartDate <= lr.EndDate) ||
                       (model.EndDate >= lr.StartDate && model.EndDate <= lr.EndDate) ||
                       (model.StartDate <= lr.StartDate && model.EndDate >= lr.EndDate)))
                .ToListAsync();

            if (overlappingRequests.Any())
            {
                ModelState.AddModelError("", "You cannot submit a sick leave request that overlaps with an existing approved leave request.");
                return View(model);
            }

            if (requestedDays > 10)
            {
                ModelState.AddModelError("", "You cannot request more than 10 sick days at a time.");
                return View(model);
            }

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
                LeaveType = "Sick",
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Comments = model.Comments,
                SubmittedBy = user,
                Status = "Pending",
            };

            //handle za filoj
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
    }
}