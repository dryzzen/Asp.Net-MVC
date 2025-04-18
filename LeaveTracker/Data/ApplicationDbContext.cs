﻿using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using LeaveTracker.Models;

namespace LeaveTracker.Data
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