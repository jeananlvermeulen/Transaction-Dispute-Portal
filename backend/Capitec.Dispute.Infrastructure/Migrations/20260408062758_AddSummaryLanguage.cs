using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capitec.Dispute.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSummaryLanguage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SummaryLanguage",
                table: "Disputes",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SummaryLanguage",
                table: "Disputes");
        }
    }
}
