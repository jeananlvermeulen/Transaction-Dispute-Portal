using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capitec.Dispute.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCancellationReasonTranslation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancellationReasonEnglish",
                table: "Disputes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReasonLanguage",
                table: "Disputes",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationReasonEnglish",
                table: "Disputes");

            migrationBuilder.DropColumn(
                name: "CancellationReasonLanguage",
                table: "Disputes");
        }
    }
}
