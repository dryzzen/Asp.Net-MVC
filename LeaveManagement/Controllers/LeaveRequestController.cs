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

        //For all requests to pop up when the user goes here
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var leaveRequests = await _context.LeaveRequests
                .Include(lr => lr.SubmittedBy)
                .Where(lr => lr.UserId == user.Id)
                .ToListAsync();

            return View(leaveRequests);
        }

        [HttpGet]
        public IActionResult Create(LeaveType? leaveType = null)
        {
            var model = new LeaveRequestViewModel
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(1)
            };

            if (leaveType.HasValue && (leaveType.Value == LeaveType.Annual || leaveType.Value == LeaveType.Bonus))
            {
                model.LeaveType = leaveType.Value;
            }

            return View(model);
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

            // Validate dates
            if (model.StartDate < DateTime.Today)
            {
                ModelState.AddModelError("", "You cannot submit a leave request for a date before today.");
                return View(model);
            }

            if (!IsValidDateRange(model.StartDate, model.EndDate))
            {
                ModelState.AddModelError("", "The end date cannot be earlier than the start date.");
                return View(model);
            }

            // Cannot issue a req if the previous one hasnt been approved or rejected
            var existingRequest = await _context.LeaveRequests
                .Where(lr => lr.UserId == user.Id && lr.Status == LeaveStatus.Pending)
                .FirstOrDefaultAsync();

            if (existingRequest != null)
            {
                ModelState.AddModelError("", "You already have a pending leave request. Please wait for it to be approved or rejected before submitting a new one.");
                return View(model);
            }

            // Req cannot have a same day, for example 17-19, 19-28, 19 is not available
            var overlappingRequests = await _context.LeaveRequests
                .Where(lr => lr.UserId == user.Id &&
                            lr.Status == LeaveStatus.Approved &&
                            ((model.StartDate >= lr.StartDate && model.StartDate <= lr.EndDate) ||
                             (model.EndDate >= lr.StartDate && model.EndDate <= lr.EndDate) ||
                             (model.StartDate <= lr.StartDate && model.EndDate >= lr.EndDate)))
                .ToListAsync();

            if (overlappingRequests.Any())
            {
                ModelState.AddModelError("", "You cannot submit a leave request that overlaps with an existing approved leave request.");
                return View(model);
            }

            //Calculate if they have days 
            if (model.LeaveType == LeaveType.Annual && requestedDays > await _leaveService.GetRemainingAnnualLeave(user.Id))
            {
                ModelState.AddModelError("", "You do not have enough annual leave days.");
            }
            else if (model.LeaveType == LeaveType.Bonus && requestedDays > await _leaveService.GetRemainingBonusLeave(user.Id))
            {
                ModelState.AddModelError("", "You do not have enough bonus leave days.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Create and save the leave request
            var leaveRequest = new LeaveRequest
            {
                UserId = user.Id,
                LeaveType = model.LeaveType,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Comments = model.Comments,
                SubmittedBy = user,
                Status = LeaveStatus.Pending,
            };

            _context.LeaveRequests.Add(leaveRequest);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
        public IActionResult CreateSickLeave()
        {
            return View();
        }


        //Dont get me started , if someone asks MedicalReport Nullable did not work so seperate EVERYTHING was created :) ty
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
                .Where(lr => lr.UserId == user.Id && lr.Status == LeaveStatus.Approved && 
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
                .Where(lr => lr.UserId == user.Id && lr.Status == LeaveStatus.Pending) 
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
                LeaveType = LeaveType.Sick, 
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Comments = model.Comments,
                SubmittedBy = user,
                Status = LeaveStatus.Pending, 
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