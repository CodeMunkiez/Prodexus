using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prodexus.Functions.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConstrainParsedRecordStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_ParsedRecords_Status",
                table: "ParsedRecords",
                sql: "[Status] IN ('Pending', 'Accepted', 'Rejected')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ParsedRecords_Status",
                table: "ParsedRecords");
        }
    }
}

