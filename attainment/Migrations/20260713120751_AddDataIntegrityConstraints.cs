using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace attainment.Migrations
{
    /// <inheritdoc />
    public partial class AddDataIntegrityConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE Subjects
                SET Name = substr(Name, 1, 35) || ' (' || Id || ')'
                WHERE Id IN (
                    SELECT Id FROM (
                        SELECT Id, ROW_NUMBER() OVER (PARTITION BY Name COLLATE NOCASE ORDER BY Id) AS RowNumber
                        FROM Subjects
                    ) WHERE RowNumber > 1
                );

                UPDATE Resources
                SET Title = substr(Title, 1, 80) || ' (' || Id || ')'
                WHERE Id IN (
                    SELECT Id FROM (
                        SELECT Id, ROW_NUMBER() OVER (PARTITION BY Title COLLATE NOCASE ORDER BY Id) AS RowNumber
                        FROM Resources
                    ) WHERE RowNumber > 1
                );

                UPDATE Products
                SET Name = substr(Name, 1, 80) || ' (' || Id || ')'
                WHERE Id IN (
                    SELECT Id FROM (
                        SELECT Id, ROW_NUMBER() OVER (PARTITION BY Name COLLATE NOCASE ORDER BY Id) AS RowNumber
                        FROM Products
                    ) WHERE RowNumber > 1
                );
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Subjects",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                collation: "NOCASE",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Resources",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                collation: "NOCASE",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Products",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                collation: "NOCASE",
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_Name",
                table: "Subjects",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Resources_Title",
                table: "Resources",
                column: "Title",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_Name",
                table: "Products",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subjects_Name",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Resources_Title",
                table: "Resources");

            migrationBuilder.DropIndex(
                name: "IX_Products_Name",
                table: "Products");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Subjects",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 50,
                oldCollation: "NOCASE");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Resources",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 100,
                oldCollation: "NOCASE");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Products",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 100,
                oldCollation: "NOCASE");
        }
    }
}
