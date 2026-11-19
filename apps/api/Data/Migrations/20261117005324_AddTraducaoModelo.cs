using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FacilMunicipio.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTraducaoModelo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TraducaoModelo",
                table: "EventosSeguranca",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TraducaoModelo",
                table: "EventosSeguranca");
        }
    }
}
