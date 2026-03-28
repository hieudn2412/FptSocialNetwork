using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddPostShareSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SharedFromPostId",
                table: "Posts",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_SharedFromPostId",
                table: "Posts",
                column: "SharedFromPostId");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Posts_SharedFromPostId",
                table: "Posts",
                column: "SharedFromPostId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Posts_SharedFromPostId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_SharedFromPostId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "SharedFromPostId",
                table: "Posts");
        }
    }
}
