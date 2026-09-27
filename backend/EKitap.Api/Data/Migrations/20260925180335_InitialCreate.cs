using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EKitap.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Kitaplar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ad = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Durum = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PdfYolu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HataMesaji = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Asama = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kitaplar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bildiriler",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KitapId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sira = table.Column<int>(type: "int", nullable: false),
                    DosyaAdi = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    Baslik = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    OrijinalDosyaYolu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SayfaBaslangic = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bildiriler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bildiriler_Kitaplar_KitapId",
                        column: x => x.KitapId,
                        principalTable: "Kitaplar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bildiriler_KitapId_Sira",
                table: "Bildiriler",
                columns: new[] { "KitapId", "Sira" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bildiriler");

            migrationBuilder.DropTable(
                name: "Kitaplar");
        }
    }
}
