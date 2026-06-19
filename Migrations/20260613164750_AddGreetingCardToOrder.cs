using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebBanHoa.Migrations
{
    /// <inheritdoc />
    public partial class AddGreetingCardToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CardContent",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardImage",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CardContent",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CardImage",
                table: "Orders");
        }
    }
}
