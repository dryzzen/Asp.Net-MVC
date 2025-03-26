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
using LeaveManagement.Services;

namespace LeaveManagement.Controllers
{
    [Authorize]
    public class LeaveRequestController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILeaveService _leaveService;
        private readonly IFileUploadService _fileUploadService;

        public LeaveRequestController(UserManager<ApplicationUser> userManager, ApplicationDbContext context, IWebHostEnvironment environment,ILeaveService leaveService,IFileUploadService fileUploadService)
        {
            _userManager = userManager;
            _context = context;
            _environment = environment;
            _leaveService = leaveService;
            _fileUploadService = fileUploadService;
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

            if (model.StartDate < DateTime.Today)
            {
                ModelState.AddModelError("", "You cannot submit a leave request for a date before today.");
                return View(model);
            }

            var existingRequest = await _context.LeaveRequests
                .Where(lr => lr.UserId == user.Id && lr.Status == LeaveStatus.Pending) // Changed to enum
                .FirstOrDefaultAsync();

            if (existingRequest != null)
            {
                ModelState.AddModelError("", "You already have a pending leave request. Please wait for it to be approved or rejected before submitting a new one.");
                return View(model);
            }

            var overlappingRequests = await _context.LeaveRequests
            .Where(lr => lr.UserId == user.Id && lr.Status == LeaveStatus.Approved && // Changed to enum
                  ((model.StartDate >= lr.StartDate && model.StartDate <= lr.EndDate) ||
                   (model.EndDate >= lr.StartDate && model.EndDate <= lr.EndDate) ||
                   (model.StartDate <= lr.StartDate && model.EndDate >= lr.EndDate)))
             .ToListAsync();

            if (overlappingRequests.Any())
            {
                ModelState.AddModelError("", "You cannot submit a leave request that overlaps with an existing approved leave request.");
                return View(model);
            }

            if (!IsValidDateRange(model.StartDate, model.EndDate))
            {
                ModelState.AddModelError("", "The end date cannot be earlier than the start date.");
                return View(model);
            }

            if (model.LeaveType == LeaveType.Annual && requestedDays > await _leaveService.GetRemainingAnnualLeave(user.Id)) // Changed to enum
            {
                ModelState.AddModelError("", "You do not have enough annual leave days.");
            }
            else if (model.LeaveType == LeaveType.Bonus && requestedDays > await _leaveService.GetRemainingBonusLeave(user.Id)) // Changed to enum
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
                LeaveType = model.LeaveType, // Now using enum
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Comments = model.Comments,
                SubmittedBy = user,
                Status = LeaveStatus.Pending, // Changed to enum
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
                .Where(lr => lr.UserId == user.Id && lr.Status == LeaveStatus.Approved && // Changed to enum
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
                .Where(lr => lr.UserId == user.Id && lr.Status == LeaveStatus.Pending) // Changed to enum
                .FirstOrDefaultAsync();

            if (existingRequest != null)
            {
                ModelState.AddModelError("", "You already have a pending leave request. Please wait for it to be approved or rejected before submitting a new one.");
                return View(model);
            }

            if (!IsValidDateRange(model.StartDate, model.EndDate))
            {
                ModelState.AddModelError("", "The end date cannot be earlier than the start date.");
                return View(model);
            }

            var leaveRequest = new LeaveRequest
            {
                UserId = user.Id,
                LeaveType = LeaveType.Sick, // Changed to enum
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Comments = model.Comments,
                SubmittedBy = user,
                Status = LeaveStatus.Pending, // Changed to enum
            };

            if (model.MedicalReport != null && model.MedicalReport.Length > 0)
            {
                leaveRequest.MedicalReportPath = await _fileUploadService.UploadMedicalReport(model.MedicalReport);
            }
            else
            {
                leaveRequest.MedicalReportPath = null;
            }

            _context.LeaveRequests.Add(leaveRequest);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Search(LeaveSearchViewModel filters)
        {
            var query = _context.LeaveRequests.Include(lr => lr.SubmittedBy).AsQueryable();

            if (!string.IsNullOrEmpty(filters.Query))
                query = query.Where(lr => lr.SubmittedBy.UserName.Contains(filters.Query));

            if (filters.Status.HasValue)
                query = query.Where(lr => lr.Status == filters.Status.Value);

            if (filters.LeaveType.HasValue)
                query = query.Where(lr => lr.LeaveType == filters.LeaveType.Value);

            return View("SearchResults", await query.ToListAsync());
        }

        private bool IsValidDateRange(DateTime start, DateTime end)
        {
            return end >= start;
        }


    }
}