using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquiposAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class FechaRegistroEquipo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaRegistro",
                table: "Equipos",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "CONVERT(datetime2, (SYSUTCDATETIME() AT TIME ZONE 'UTC') AT TIME ZONE 'Central America Standard Time')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaRegistro",
                table: "Equipos");
        }
    }
}
