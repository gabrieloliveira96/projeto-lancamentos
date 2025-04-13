using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cashflow.Consolidado.API.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateConsolidado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SaldosConsolidados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Saldo = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaldosConsolidados", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SaldosConsolidados");
        }
    }
}
