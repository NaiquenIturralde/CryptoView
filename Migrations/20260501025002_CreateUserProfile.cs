using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CryptoView.Migrations
{
    /// <inheritdoc />
    public partial class CreateUserProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Role = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CryptoCurrencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoinId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CurrentPrice = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    MarketCap = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Volume24h = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Change24h = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PriceThreshold = table.Column<decimal>(type: "decimal(18,8)", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CryptoCurrencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Bio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FavoriteCryptoIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserProfiles_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CryptoCurrencyId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNotes_CryptoCurrencies_CryptoCurrencyId",
                        column: x => x.CryptoCurrencyId,
                        principalTable: "CryptoCurrencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "CryptoCurrencies",
                columns: new[] { "Id", "Change24h", "CoinId", "CurrentPrice", "DateAdded", "IsActive", "LastUpdated", "MarketCap", "Name", "PriceThreshold", "Symbol", "Volume24h" },
                values: new object[,]
                {
                    { 1, 0m, "bitcoin", 0m, new DateTime(2026, 5, 1, 2, 50, 1, 475, DateTimeKind.Utc).AddTicks(9390), true, new DateTime(2026, 5, 1, 2, 50, 1, 475, DateTimeKind.Utc).AddTicks(9391), 0m, "Bitcoin", null, "BTC", 0m },
                    { 2, 0m, "ethereum", 0m, new DateTime(2026, 5, 1, 2, 50, 1, 475, DateTimeKind.Utc).AddTicks(9397), true, new DateTime(2026, 5, 1, 2, 50, 1, 475, DateTimeKind.Utc).AddTicks(9397), 0m, "Ethereum", null, "ETH", 0m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_Role",
                table: "AppUsers",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_Username",
                table: "AppUsers",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CryptoCurrencies_CoinId",
                table: "CryptoCurrencies",
                column: "CoinId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CryptoCurrencies_IsActive",
                table: "CryptoCurrencies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CryptoCurrencies_Symbol",
                table: "CryptoCurrencies",
                column: "Symbol");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotes_CreatedAt",
                table: "UserNotes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotes_CryptoCurrencyId",
                table: "UserNotes",
                column: "CryptoCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_Email",
                table: "UserProfiles",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_FullName",
                table: "UserProfiles",
                column: "FullName");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_UserId",
                table: "UserProfiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserNotes");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropTable(
                name: "CryptoCurrencies");

            migrationBuilder.DropTable(
                name: "AppUsers");
        }
    }
}
