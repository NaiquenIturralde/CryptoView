using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoView.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeUserFavorites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserFavorites",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CryptoCurrencyId = table.Column<int>(type: "int", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFavorites", x => new { x.UserId, x.CryptoCurrencyId });
                    table.ForeignKey(
                        name: "FK_UserFavorites_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserFavorites_CryptoCurrencies_CryptoCurrencyId",
                        column: x => x.CryptoCurrencyId,
                        principalTable: "CryptoCurrencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserFavorites_CryptoCurrencyId",
                table: "UserFavorites",
                column: "CryptoCurrencyId");

            // ── Migración de datos: copiar favoritos existentes desde el JSON
            //    UserProfiles.FavoriteCryptoIds hacia la tabla normalizada ──────
            //
            /// Seguridad garantizada:
            ///  - OPENJSON recibe siempre JSON válido gracias al CASE con ISJSON:
            ///    si el valor es NULL o corrupto, se usa '[]' (sin filas).
            ///  - TRY_CAST acepta IDs numéricos (números o strings numéricos) y
            ///    descarta cualquier elemento inválido sin romper la migración.
            ///  - EXISTS con CryptoCurrencies inserta solo criptomonedas reales.
            ///  - DISTINCT + NOT EXISTS + la PK compuesta evitan duplicados,
            ///    haciendo la migración idempotente.
            ///  - AddedAt toma UserProfiles.UpdatedAt, con fallback GETUTCDATE().
            ///  - FavoriteCryptoIds NO se modifica ni se elimina.
            migrationBuilder.Sql(
                """
                INSERT INTO [dbo].[UserFavorites] ([UserId], [CryptoCurrencyId], [AddedAt])
                SELECT DISTINCT
                       up.[UserId],
                       TRY_CAST(j.[value] AS int) AS [CryptoCurrencyId],
                       COALESCE(NULLIF(up.[UpdatedAt], '0001-01-01'), GETUTCDATE()) AS [AddedAt]
                FROM [dbo].[UserProfiles] up
                CROSS APPLY OPENJSON(
                         CASE
                             WHEN ISJSON(up.[FavoriteCryptoIds]) = 1
                                 THEN up.[FavoriteCryptoIds]
                             ELSE '[]'
                         END
                     ) AS j
                WHERE up.[FavoriteCryptoIds] IS NOT NULL
                  AND ISNUMERIC(j.[value]) = 1
                  AND TRY_CAST(j.[value] AS int) IS NOT NULL
                  AND EXISTS (
                      SELECT 1 FROM [dbo].[CryptoCurrencies] c
                      WHERE c.[Id] = TRY_CAST(j.[value] AS int)
                  )
                  AND NOT EXISTS (
                      SELECT 1 FROM [dbo].[UserFavorites] uf
                      WHERE uf.[UserId] = up.[UserId]
                        AND uf.[CryptoCurrencyId] = TRY_CAST(j.[value] AS int)
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserFavorites");
        }
    }
}
