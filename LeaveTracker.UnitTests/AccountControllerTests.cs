using Moq;
using Xunit;
using LeaveTracker.Controllers;
using LeaveTracker.ViewModels;
using LeaveTracker.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using LeaveTracker.Data;
using LeaveTracker.Services;
using System;

namespace LeaveTracker.UnitTests
{
    public class AccountControllerTests : IDisposable
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly Mock<ILeaveService> _mockLeaveService;
        private readonly AccountController _controller;

        public AccountControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ApplicationDbContext(options);

            var store = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);

            _mockSignInManager = new Mock<SignInManager<ApplicationUser>>(
                _mockUserManager.Object,
                Mock.Of<IHttpContextAccessor>(),
                Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
                null, null, null, null);

           
            _mockLeaveService = new Mock<ILeaveService>();

            _controller = new AccountController(
                _mockUserManager.Object,
                _mockSignInManager.Object,
                _dbContext,
                _mockLeaveService.Object);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }

        [Fact]
        public async Task Register_ValidModel_RedirectsToProfile()
        {
            var model = new RegistrationViewModel
            {
                Email = "test@example.com",
                Password = "Test@123",
                ConfirmPassword = "Test@123",
                FirstName = "Test",
                LastName = "User",
                DateOfBirth = new DateTime(1990, 1, 1),
                Position = "Developer"
            };

            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), model.Password))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"))
                .ReturnsAsync(IdentityResult.Success);

            var result = await _controller.Register(model);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Profile", redirectResult.ActionName);
        }

        [Fact]
        public async Task Register_InvalidModel_ReturnsViewWithModel()
        {
            var model = new RegistrationViewModel();
            _controller.ModelState.AddModelError("Email", "Required");

            var result = await _controller.Register(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
        }

        [Fact]
        public async Task Login_ValidCredentials_RedirectsToProfile()
        {
            var model = new LoginViewModel
            {
                Email = "test@example.com",
                Password = "Test@123",
                RememberMe = false
            };

            _mockSignInManager.Setup(x => x.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            var result = await _controller.Login(model);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Profile", redirectResult.ActionName);
        }

        [Fact]
        public async Task Profile_AuthenticatedUser_ReturnsViewWithModel()
        {
            var user = new ApplicationUser
            {
                Id = "1",
                FirstName = "Test",
                LastName = "User",
                DateOfBirth = new DateTime(1990, 1, 1),
                Position = "Developer",
                AnnualLeaveDays = 20,
                BonusLeaveDays = 5
            };

            _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);

            _mockLeaveService.Setup(x => x.GetTotalAnnualLeaveTaken(user.Id))
                .ReturnsAsync(5);
            _mockLeaveService.Setup(x => x.GetTotalBonusLeaveTaken(user.Id))
                .ReturnsAsync(2);
            _mockLeaveService.Setup(x => x.GetSickDaysTaken(user.Id))
                .ReturnsAsync(3);

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, user.Id) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            var result = await _controller.Profile();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ProfileViewModel>(viewResult.Model);

            Assert.Equal(user.FirstName, model.FirstName);
            Assert.Equal(user.LastName, model.LastName);
            Assert.Equal(5, model.TotalAnnualLeaveTaken);
            Assert.Equal(2, model.TotalBonusLeaveTaken);
            Assert.Equal(3, model.SickDaysTaken);
        }

        [Fact]
        public async Task ResetPassword_ValidModel_RedirectsToConfirmation()
        {
            var model = new ResetPasswordViewModel
            {
                Email = "test@example.com",
                NewPassword = "NewPass@123",
                ConfirmNewPassword = "NewPass@123"
            };

            var user = new ApplicationUser { Email = model.Email };

            _mockUserManager.Setup(x => x.FindByEmailAsync(model.Email))
                .ReturnsAsync(user);

            _mockUserManager.Setup(x => x.GeneratePasswordResetTokenAsync(user))
                .ReturnsAsync("generated-token");

            _mockUserManager.Setup(x => x.ResetPasswordAsync(user, It.IsAny<string>(), model.NewPassword))
                .ReturnsAsync(IdentityResult.Success);

            
            var result = await _controller.ResetPassword(model);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ResetPasswordConfirmation", redirectResult.ActionName);
        }

        [Fact]
        public async Task Logout_Always_RedirectsToHomeIndex()
        {
            _mockSignInManager.Setup(x => x.SignOutAsync())
                .Returns(Task.CompletedTask);

            var result = await _controller.Logout();

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
        }
    }
}