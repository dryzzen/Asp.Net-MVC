using Moq;
using Xunit;
using LeaveTracker.Controllers;
using LeaveTracker.Models;
using LeaveTracker.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using LeaveTracker.Data;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.AspNetCore.Http;
using LeaveTracker.Services;

namespace LeaveTracker.UnitTests
{
    public class HRControllerTests : IDisposable
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly Mock<ILeaveService> _mockLeaveService;
        private readonly HRController _controller;

        public HRControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ApplicationDbContext(options);

            var store = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);

            _mockLeaveService = new Mock<ILeaveService>();

            _controller = new HRController(
                _dbContext,
                _mockUserManager.Object,
                _mockLeaveService.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "hr-user-1"),
                new Claim(ClaimTypes.Role, "HR")
            }));
            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }

        [Fact]
        public async Task EditUser_GET_WithValidId_ReturnsViewWithViewModel()
        {
            // Arrange
            var testUser = new ApplicationUser
            {
                Id = "user-123",
                Email = "test@example.com",
                FirstName = "John",
                LastName = "Doe",
                DateOfBirth = new DateTime(1990, 1, 1),
                Position = "Developer",
                AnnualLeaveDays = 20,
                BonusLeaveDays = 5
            };

            _mockUserManager.Setup(x => x.FindByIdAsync("user-123"))
                .ReturnsAsync(testUser);
            _mockUserManager.Setup(x => x.GetRolesAsync(testUser))
                .ReturnsAsync(new List<string> { "User" });

            var result = await _controller.EditUser("user-123");

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<EditUserViewModel>(viewResult.Model);

            Assert.Equal(testUser.Email, model.Email);
            Assert.Equal(testUser.FirstName, model.FirstName);
            Assert.Equal(testUser.LastName, model.LastName);
            Assert.Equal("User", model.Role);
        }

        [Fact]
        public async Task EditUser_POST_WithValidId_UpdatesUserAndRedirects()
        {
            var userId = "user-123";
            var viewModel = new EditUserViewModel
            {
                Id = userId,
                Email = "updated@example.com",
                FirstName = "Updated",
                LastName = "User",
                DateOfBirth = new DateTime(1990, 1, 1),
                Position = "Senior Developer",
                AnnualLeaveDays = 25,
                BonusLeaveDays = 7,
                Role = "HR"
            };

            _mockUserManager.Setup(x => x.FindByIdAsync(userId))
                .ReturnsAsync(new ApplicationUser { Id = userId });

            _mockUserManager.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new List<string> { "User" });
            _mockUserManager.Setup(x => x.RemoveFromRolesAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "HR"))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            var mockDbContext = new Mock<ApplicationDbContext>();
            mockDbContext.Setup(x => x.SaveChangesAsync(default))
                .ReturnsAsync(1);
            _controller.ModelState.Clear(); 

            var result = await _controller.EditUser(userId, viewModel); 

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("UserList", redirectResult.ActionName);
        }

        [Fact]
        public async Task EditUser_POST_WithInvalidId_ReturnsNotFound()
        {
            var invalidId = "invalid-user";
            _mockUserManager.Setup(x => x.FindByIdAsync(invalidId))
                .ReturnsAsync((ApplicationUser)null);

            var result = await _controller.EditUser(invalidId);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task EditUser_GET_WithInvalidId_ReturnsNotFound()
        {
            _mockUserManager.Setup(x => x.FindByIdAsync("invalid-id"))
                .ReturnsAsync((ApplicationUser)null);

            var result = await _controller.EditUser("invalid-id");

            Assert.IsType<NotFoundResult>(result);
        }
    }
}