using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaveManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmittedByToLeaveRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SubmittedById",
                table: "LeaveRequests",
                type: "nvarchar(450)",
                nullable: true); // Change to nullable

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_SubmittedById",
                table: "LeaveRequests",
                column: "SubmittedById");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequests_AspNetUsers_SubmittedById",
                table: "LeaveRequests",
                column: "SubmittedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict); // Change to Restrict or SetNull
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequests_AspNetUsers_SubmittedById",
                table: "LeaveRequests");

            migrationBuilder.DropIndex(
                name: "IX_LeaveRequests_SubmittedById",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "SubmittedById",
                table: "LeaveRequests");
        }
    }
}