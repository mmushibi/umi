using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UmiHealth.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchUpdatedAtDeletedAtToPaymentRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "payment_records",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "payment_records",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "payment_records",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_records_branch_id",
                table: "payment_records",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "ix_payment_records_updated_at",
                table: "payment_records",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "ix_payment_records_deleted_at",
                table: "payment_records",
                column: "DeletedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_payment_records_deleted_at",
                table: "payment_records");

            migrationBuilder.DropIndex(
                name: "ix_payment_records_updated_at",
                table: "payment_records");

            migrationBuilder.DropIndex(
                name: "ix_payment_records_branch_id",
                table: "payment_records");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "payment_records");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "payment_records");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "payment_records");
        }
    }
}
