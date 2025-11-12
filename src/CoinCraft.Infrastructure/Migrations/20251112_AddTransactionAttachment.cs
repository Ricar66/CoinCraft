using Microsoft.EntityFrameworkCore.Migrations;

namespace CoinCraft.Infrastructure.Migrations
{
    public partial class AddTransactionAttachment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachmentPath",
                table: "Transactions",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachmentPath",
                table: "Transactions");
        }
    }
}