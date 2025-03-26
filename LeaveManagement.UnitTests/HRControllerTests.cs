using Moq;
using Xunit;
using LeaveManagement.Controllers;
using LeaveManagement.Models;
using LeaveManagement.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using LeaveManagement.Data;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.AspNetCore.Http;
using LeaveManagement.Services;

namespace LeaveManagement.UnitTests
{
    public class HRControllerTests : IDisposable
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly Mock<ILeaveService> _mockLeaveService;
        private readonly HRController _controller;

        public HRControllerTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ApplicationDbContext(options);

            // Mock UserManager
            var store = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);

            // Mock LeaveService
            _mockLeaveService = new Mock<ILeaveService>();

            // Create controller with mock HR user
            _controller = new HRController(
                _dbContext,
                _mockUserManager.Object,
                _mockLeaveService.Object);

            // Mock HR user context
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

            // Act - Calling the GET version with string ID
            var result = await _controller.EditUser("user-123");

            // Assert
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
            // Arrange
            var userId = "user-123";
            var viewModel = new EditUserViewModel
            {
                Id = userId, // <-- Must match the `id` parameter
                Email = "updated@example.com",
                FirstName = "Updated",
                LastName = "User",
                DateOfBirth = new DateTime(1990, 1, 1),
                Position = "Senior Developer",
                AnnualLeaveDays = 25,
                BonusLeaveDays = 7,
                Role = "HR"
            };

            // Mock user retrieval
            _mockUserManager.Setup(x => x.FindByIdAsync(userId))
                .ReturnsAsync(new ApplicationUser { Id = userId });

            // Mock role updates
            _mockUserManager.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new List<string> { "User" });
            _mockUserManager.Setup(x => x.RemoveFromRolesAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "HR"))
                .ReturnsAsync(IdentityResult.Success);

            // Mock user update
            _mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            // Mock SaveChanges (since your controller calls _context.SaveChangesAsync())
            var mockDbContext = new Mock<ApplicationDbContext>();
            mockDbContext.Setup(x => x.SaveChangesAsync(default))
                .ReturnsAsync(1);
            // Ensure ModelState is valid (since the action checks ModelState.IsValid)
            _controller.ModelState.Clear(); // Clears any existing errors

            // Act - Call the POST version with BOTH parameters
            var result = await _controller.EditUser(userId, viewModel); // <-- Now passing both `id` and `model`

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("UserList", redirectResult.ActionName);
        }

        [Fact]
        public async Task EditUser_POST_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = "invalid-user";
            _mockUserManager.Setup(x => x.FindByIdAsync(invalidId))
                .ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _controller.EditUser(invalidId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task EditUser_GET_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            _mockUserManager.Setup(x => x.FindByIdAsync("invalid-id"))
                .ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _controller.EditUser("invalid-id");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}