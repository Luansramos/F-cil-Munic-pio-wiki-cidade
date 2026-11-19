using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FacilMunicipio.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class EventosSeguranca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventosSeguranca",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipioId = table.Column<string>(type: "character varying(60)", nullable: false),
                    OcorridoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Categoria = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Origem = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Destino = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Acao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Gravidade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Codigo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TraducaoEstado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TraducaoTentativas = table.Column<int>(type: "integer", nullable: false),
                    TraducaoTexto = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TraduzidaEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosSeguranca", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventosSeguranca_Municipios_MunicipioId",
                        column: x => x.MunicipioId,
                        principalTable: "Municipios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventosSeguranca_MunicipioId_OcorridoEm",
                table: "EventosSeguranca",
                columns: new[] { "MunicipioId", "OcorridoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_EventosSeguranca_TraducaoEstado_OcorridoEm",
                table: "EventosSeguranca",
                columns: new[] { "TraducaoEstado", "OcorridoEm" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventosSeguranca");
        }
    }
}
