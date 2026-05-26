using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkBridge.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptAndPaymentStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReceiptFileName",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptFilePath",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceiptFileName",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ReceiptFilePath",
                table: "Payments");
        }
    }
}
