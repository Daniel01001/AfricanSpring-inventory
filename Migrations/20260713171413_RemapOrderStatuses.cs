using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AfricanSpringInventory.Migrations
{
    /// <inheritdoc />
    public partial class RemapOrderStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Order statuses are stored as strings; map the old set to the new one.
            migrationBuilder.Sql("UPDATE \"Orders\" SET \"Status\" = 'Confirmed' WHERE \"Status\" = 'Contacted';");
            migrationBuilder.Sql("UPDATE \"Orders\" SET \"Status\" = 'Delivered' WHERE \"Status\" = 'Fulfilled';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
