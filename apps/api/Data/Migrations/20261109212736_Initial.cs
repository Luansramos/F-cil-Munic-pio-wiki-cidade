using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FacilMunicipio.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Municipios",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Uf = table.Column<string>(type: "text", nullable: false),
                    CanalOficialUrl = table.Column<string>(type: "text", nullable: true),
                    VersaoAtivaId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Municipios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Fontes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipioId = table.Column<string>(type: "character varying(60)", nullable: false),
                    Titulo = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    Texto = table.Column<string>(type: "text", nullable: false),
                    DataConsulta = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fontes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Fontes_Municipios_MunicipioId",
                        column: x => x.MunicipioId,
                        principalTable: "Municipios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    SenhaHash = table.Column<string>(type: "text", nullable: false),
                    Perfil = table.Column<string>(type: "text", nullable: false),
                    MunicipioId = table.Column<string>(type: "character varying(60)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Usuarios_Municipios_MunicipioId",
                        column: x => x.MunicipioId,
                        principalTable: "Municipios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Rascunhos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FonteId = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipioId = table.Column<string>(type: "character varying(60)", nullable: false),
                    ConteudoJson = table.Column<string>(type: "text", nullable: false),
                    RevisaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Aprovado = table.Column<bool>(type: "boolean", nullable: false),
                    RevisadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    RevisadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rascunhos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rascunhos_Fontes_FonteId",
                        column: x => x.FonteId,
                        principalTable: "Fontes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Rascunhos_Municipios_MunicipioId",
                        column: x => x.MunicipioId,
                        principalTable: "Municipios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Versoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipioId = table.Column<string>(type: "character varying(60)", nullable: false),
                    RascunhoId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriadaEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Versoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Versoes_Municipios_MunicipioId",
                        column: x => x.MunicipioId,
                        principalTable: "Municipios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Versoes_Rascunhos_RascunhoId",
                        column: x => x.RascunhoId,
                        principalTable: "Rascunhos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Fontes_MunicipioId",
                table: "Fontes",
                column: "MunicipioId");

            migrationBuilder.CreateIndex(
                name: "IX_Rascunhos_FonteId",
                table: "Rascunhos",
                column: "FonteId");

            migrationBuilder.CreateIndex(
                name: "IX_Rascunhos_MunicipioId",
                table: "Rascunhos",
                column: "MunicipioId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_MunicipioId",
                table: "Usuarios",
                column: "MunicipioId");

            migrationBuilder.CreateIndex(
                name: "IX_Versoes_MunicipioId_CriadaEm",
                table: "Versoes",
                columns: new[] { "MunicipioId", "CriadaEm" });

            migrationBuilder.CreateIndex(
                name: "IX_Versoes_RascunhoId",
                table: "Versoes",
                column: "RascunhoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Versoes");

            migrationBuilder.DropTable(
                name: "Rascunhos");

            migrationBuilder.DropTable(
                name: "Fontes");

            migrationBuilder.DropTable(
                name: "Municipios");
        }
    }
}
