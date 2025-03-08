using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaveManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicalReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MedicalReportPath",
                table: "LeaveRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SickLeaveReason",
                table: "LeaveRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MedicalReportPath",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "SickLeaveReason",
                table: "LeaveRequests");
        }
    }
}
