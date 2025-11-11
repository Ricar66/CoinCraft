using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CoinCraft.Infrastructure.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(maxLength: 80, nullable: false),
                    Tipo = table.Column<int>(nullable: false),
                    SaldoInicial = table.Column<decimal>(type: "TEXT", nullable: false),
                    Ativa = table.Column<bool>(nullable: false),
                    CorHex = table.Column<string>(nullable: true),
                    Icone = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(maxLength: 80, nullable: false),
                    CorHex = table.Column<string>(nullable: true),
                    Icone = table.Column<string>(nullable: true),
                    ParentCategoryId = table.Column<int>(nullable: true),
                    LimiteMensal = table.Column<decimal>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_Categories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Goals",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CategoryId = table.Column<int>(nullable: false),
                    LimiteMensal = table.Column<decimal>(type: "TEXT", nullable: false),
                    Ano = table.Column<int>(nullable: false),
                    Mes = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Goals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Goals_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Chave = table.Column<string>(nullable: false),
                    Valor = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Data = table.Column<DateTime>(nullable: false),
                    Tipo = table.Column<int>(nullable: false),
                    Valor = table.Column<decimal>(type: "TEXT", nullable: false),
                    AccountId = table.Column<int>(nullable: false),
                    CategoryId = table.Column<int>(nullable: true),
                    Descricao = table.Column<string>(nullable: true),
                    OpostoAccountId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Transactions_Accounts_OpostoAccountId",
                        column: x => x.OpostoAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Transactions_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentCategoryId",
                table: "Categories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Goals_CategoryId",
                table: "Goals",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AccountId",
                table: "Transactions",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_CategoryId",
                table: "Transactions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Data",
                table: "Transactions",
                column: "Data");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_OpostoAccountId",
                table: "Transactions",
                column: "OpostoAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Tipo_AccountId",
                table: "Transactions",
                columns: new[] { "Tipo", "AccountId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Transactions");
            migrationBuilder.DropTable(name: "Goals");
            migrationBuilder.DropTable(name: "UserSettings");
            migrationBuilder.DropTable(name: "Accounts");
            migrationBuilder.DropTable(name: "Categories");
        }
    }
}