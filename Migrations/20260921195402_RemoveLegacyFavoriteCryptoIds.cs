using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoView.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyFavoriteCryptoIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FavoriteCryptoIds",
                table: "UserProfiles");

            migrationBuilder.UpdateData(
                table: "CryptoCurrencies",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateAdded", "LastUpdated" },
                values: new object[] { new DateTime(2026, 9, 21, 19, 54, 1, 808, DateTimeKind.Utc).AddTicks(6629), new DateTime(2026, 9, 21, 19, 54, 1, 808, DateTimeKind.Utc).AddTicks(6632) });

            migrationBuilder.UpdateData(
                table: "CryptoCurrencies",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "DateAdded", "LastUpdated" },
                values: new object[] { new DateTime(2026, 9, 21, 19, 54, 1, 808, DateTimeKind.Utc).AddTicks(6636), new DateTime(2026, 9, 21, 19, 54, 1, 808, DateTimeKind.Utc).AddTicks(6637) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FavoriteCryptoIds",
                table: "UserProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "CryptoCurrencies",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateAdded", "LastUpdated" },
                values: new object[] { new DateTime(2026, 9, 21, 17, 36, 20, 996, DateTimeKind.Utc).AddTicks(5983), new DateTime(2026, 9, 21, 17, 36, 20, 996, DateTimeKind.Utc).AddTicks(5984) });

            migrationBuilder.UpdateData(
                table: "CryptoCurrencies",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "DateAdded", "LastUpdated" },
                values: new object[] { new DateTime(2026, 9, 21, 17, 36, 20, 996, DateTimeKind.Utc).AddTicks(5988), new DateTime(2026, 9, 21, 17, 36, 20, 996, DateTimeKind.Utc).AddTicks(5988) });
        }
    }
}
