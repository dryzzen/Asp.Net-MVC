using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using LeaveManagement.Models;

namespace LeaveManagement.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<LeaveRequest> LeaveRequests { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            //this is to configure the relationship between the User and LeaveRequest 1->many
            builder.Entity<ApplicationUser>()
                .HasMany(u => u.LeaveRequests)
                .WithOne(lr => lr.SubmittedBy)
                .HasForeignKey(lr => lr.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            builder.Entity<LeaveRequest>()
            .Property(lr => lr.LeaveType)
            .HasConversion<string>();

            builder.Entity<LeaveRequest>()
            .Property(lr => lr.Status)
            .HasConversion<string>();



        }

    }
}