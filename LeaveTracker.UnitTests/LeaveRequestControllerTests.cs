using Moq;
using Xunit;
using LeaveTracker.Controllers;
using LeaveTracker.Data;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using LeaveTracker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using LeaveTracker.Services;
using LeaveTracker.ViewModels;
using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System;

namespace LeaveTracker.UnitTests
{
    public class LeaveRequestControllerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly Mock<IWebHostEnvironment> _mockEnv;
        private readonly Mock<ILeaveService> _mockLeaveService;
        private readonly LeaveRequestController _controller;
        private readonly Mock<IFileUploadService> _fileUploadService;

        public LeaveRequestControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ApplicationDbContext(options);

            var store = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);

            _mockEnv = new Mock<IWebHostEnvironment>();
            _mockEnv.Setup(e => e.WebRootPath).Returns("wwwroot");
            _mockLeaveService = new Mock<ILeaveService>();

            _fileUploadService = new Mock<IFileUploadService>();

            _controller = new LeaveRequestController(
                _mockUserManager.Object,
                _dbContext,
                _mockEnv.Object,
                _mockLeaveService.Object,
                _fileUploadService.Object); // ← FIXED

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
        new Claim(ClaimTypes.NameIdentifier, "1"),
            }));
            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
        }
        [Fact]
        public async Task Create_ValidAnnualRequest_RedirectsToIndex()
        {
            var model = new LeaveRequestViewModel
            {
                LeaveType = LeaveType.Annual,
                StartDate = DateTime.Now.AddDays(1),
                EndDate = DateTime.Now.AddDays(5),
                Comments="Test Comments"
            };

            _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(new ApplicationUser { Id = "1" });

            _mockLeaveService.Setup(x => x.GetRemainingAnnualLeave(It.IsAny<string>()))
                .ReturnsAsync(30);
            _mockLeaveService.Setup(x => x.GetRemainingBonusLeave(It.IsAny<string>()))
                .ReturnsAsync(5);



            var result = await _controller.Create(model);



            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Single(await _dbContext.LeaveRequests.ToListAsync()); 
        }

        [Fact]
        public async Task Create_InsufficientAnnualLeave_ReturnsError()
        {
            var model = new LeaveRequestViewModel
            {
                LeaveType = LeaveType.Annual,
                StartDate = DateTime.Now.AddDays(1),
                EndDate = DateTime.Now.AddDays(30)
            };

            _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(new ApplicationUser { Id = "1" });

            _mockLeaveService.Setup(x => x.GetRemainingAnnualLeave(It.IsAny<string>()))
                .ReturnsAsync(10);
            _mockLeaveService.Setup(x => x.GetRemainingBonusLeave(It.IsAny<string>()))
                .ReturnsAsync(5);

           



            var result = await _controller.Create(model);





            Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
            Assert.Contains("You do not have enough annual leave days",
                _controller.ModelState[""].Errors[0].ErrorMessage);
            Assert.Empty(await _dbContext.LeaveRequests.ToListAsync()); 
        }
        [Fact]
        public async Task Create_ValidSickLeaveRequest_RedirectsToIndex()
        {
            var model = new SickLeaveRequestViewModel
            {
                LeaveType = LeaveType.Sick,
                StartDate = DateTime.Now.AddDays(1),
                EndDate = DateTime.Now.AddDays(3),
                Comments = "Sick leave request",
                MedicalReport = null
            };

            _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(new ApplicationUser { Id = "1" });

            _fileUploadService.Setup(x => x.UploadMedicalReport(It.IsAny<IFormFile>()))
                .ReturnsAsync((string)null);

            var result = await _controller.CreateSickLeave(model);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }
    }
}