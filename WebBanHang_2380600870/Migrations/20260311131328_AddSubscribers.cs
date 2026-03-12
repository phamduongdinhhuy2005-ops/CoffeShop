using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebBanHang_2380600870.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscribers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Subscribers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubscribedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscribers", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                column: "ImageUrl",
                value: "https://lh3.googleusercontent.com/aida-public/AB6AXuBquJBlkgANzMfyJGh1XkspjUt3umtwxQzexDd8I0mt0FgPpx6FXkExK_M7Xm-wM6-LP7BG6Uy4DlU4Zs_SLn4nniVk8-ZjKvOweTbUCwoZ4qlVJ3Ng2_hYfQ3E6HoYycQvSScsSUO1HRJnZF3sizIF_rZ_Dy-byN31UV3fduUnRR-A1zsaH7m7UztM6dHj1VAVWReoWicfHForfNH45bxgCGWHUAofQXMqeY9jauQltovivmnmKywndNVfLUxF3nPKs7Lg3ozOwc5Re");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Subscribers");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                column: "ImageUrl",
                value: "https://lh3.googleusercontent.com/aida-public/AB6AXuBquJBlkgANzMfyJGh1XkspjUt3umtwxQzexDd8I0mt0FgPpx6FXkExK_M7Xm-wM6-LP7BG6Uy4DlU4Zs_SLn4nniVk8-ZjKvOweTbUCwoZ4qlVJ3Ng2_hYfQ3E6HoYycQvSScsSUO1HRJnZF3sizIF_rZ_Dy-byN31UV3fduUnRR-A1zsaH7m7UztM6dHj1VAVWReoWicfHForfNH45bxgCWHUAofQXMqeY9jauQltovivmnmKywndNVfLUxF3nPKs7Lg3ozOwc5Re");
        }
    }
}
