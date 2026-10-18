using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquiposAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class PrestamosUnoAMuchos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Prestamos_EquipoId",
                table: "Prestamos");

            migrationBuilder.CreateIndex(
                name: "IX_Prestamos_EquipoId_FechaDevolucion",
                table: "Prestamos",
                columns: new[] { "EquipoId", "FechaDevolucion" },
                unique: true,
                filter: "[FechaDevolucion] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Prestamos_EquipoId_FechaDevolucion",
                table: "Prestamos");

            migrationBuilder.CreateIndex(
                name: "IX_Prestamos_EquipoId",
                table: "Prestamos",
                column: "EquipoId",
                unique: true,
                filter: "[FechaDevolucion] IS NULL");
        }
    }
}
