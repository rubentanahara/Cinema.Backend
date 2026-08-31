using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cinema.Payments.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "payments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // S1186: intentionally empty. This schema holds the module's __EFMigrationsHistory,
            // so dropping it here would erase the record of the rollback itself.
        }
    }
}
