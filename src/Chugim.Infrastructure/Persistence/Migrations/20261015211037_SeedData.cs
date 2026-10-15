using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Chugim.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "אומנות" },
                    { 2, "מחשבים" },
                    { 3, "בישול" },
                    { 4, "תפירה" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Email", "FullName", "PasswordHash", "Role" },
                values: new object[,]
                {
                    { 1, "admin@chugim.dev", "מנהלת החוגים", "AQAAAAIAAYagAAAAEB2H1qcwWTIgMrWnBxGcjJdltEiL9giwNQ+0pbkMa1WcBNnXcpcRSlOWaRaqt3ZDHQ==", "Admin" },
                    { 2, "rachel@chugim.dev", "רחל כהן", "AQAAAAIAAYagAAAAEM+VyVetoyFclqPM4dO1/O7ggNB/Ry926RGQrmVSdTOYAHemBfI6JD7zefuXL8R2CQ==", "Participant" },
                    { 3, "leah@chugim.dev", "לאה לוי", "AQAAAAIAAYagAAAAEM+VyVetoyFclqPM4dO1/O7ggNB/Ry926RGQrmVSdTOYAHemBfI6JD7zefuXL8R2CQ==", "Participant" },
                    { 4, "sara@chugim.dev", "שרה מזרחי", "AQAAAAIAAYagAAAAEM+VyVetoyFclqPM4dO1/O7ggNB/Ry926RGQrmVSdTOYAHemBfI6JD7zefuXL8R2CQ==", "Participant" }
                });

            migrationBuilder.InsertData(
                table: "Courses",
                columns: new[] { "Id", "Capacity", "CategoryId", "DayOfWeek", "Description", "EnrolledCount", "InstructorName", "Location", "Price", "StartDate", "StartTime", "Title", "Version" },
                values: new object[,]
                {
                    { 1, 3, 1, "Sunday", "כתיבה אומנותית בעט ובמכחול.", 2, "חנה ברגר", "חדר 3", 180m, new DateOnly(2026, 11, 1), new TimeOnly(16, 30, 0), "קליגרפיה עברית", new Guid("6d1f0c52-3b8e-4f8a-9a52-1a7d3c9e4b01") },
                    { 2, 12, 1, "Tuesday", "טכניקות בסיס, נופים וטבע דומם.", 1, "מרים שטרן", "חדר 3", 220m, new DateOnly(2026, 11, 3), new TimeOnly(17, 0, 0), "ציור בצבעי מים", new Guid("2a7b9e14-5c3d-4e6f-8a91-0b2c4d6e8f02") },
                    { 3, 15, 2, "Monday", "יסודות התכנות בסביבה ידידותית.", 0, "אסתר כץ", "מעבדת מחשבים", 250m, new DateOnly(2026, 11, 2), new TimeOnly(15, 0, 0), "תכנות לנערות — צעדים ראשונים", new Guid("9c4e2f61-7a8b-4c3d-9e1f-2a3b4c5d6e03") },
                    { 4, 10, 2, "Wednesday", "עבודה עם כלי עיצוב והכנת הזמנות ופוסטרים.", 0, "דבורה פרידמן", "מעבדת מחשבים", 260m, new DateOnly(2026, 11, 4), new TimeOnly(16, 0, 0), "עיצוב גרפי", new Guid("4b8d1e73-9f2a-4b5c-8d6e-3c4d5e6f7a04") },
                    { 5, 8, 3, "Thursday", "לחמים, עוגות ועוגיות.", 0, "יהודית אורן", "המטבח", 200m, new DateOnly(2026, 11, 5), new TimeOnly(16, 30, 0), "אפייה ביתית", new Guid("7e2a5c84-1b3d-4e6f-9a8b-4d5e6f7a8b05") },
                    { 6, 10, 4, "Sunday", "הכרת מכונת התפירה ותפירת פריטים פשוטים.", 0, "רבקה גולד", "חדר 5", 230m, new DateOnly(2026, 11, 8), new TimeOnly(18, 0, 0), "תפירה למתחילות", new Guid("1f6c3d95-2e4a-4b7c-8d9e-5e6f7a8b9c06") }
                });

            migrationBuilder.InsertData(
                table: "Enrollments",
                columns: new[] { "Id", "CourseId", "EnrolledAtUtc", "Status", "UserId" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2026, 10, 1, 8, 0, 0, 0, DateTimeKind.Utc), "Active", 2 },
                    { 2, 1, new DateTime(2026, 10, 1, 8, 0, 0, 0, DateTimeKind.Utc), "Active", 3 },
                    { 3, 2, new DateTime(2026, 10, 1, 8, 0, 0, 0, DateTimeKind.Utc), "Active", 2 }
                });

            // ה-seed הכניס Id מפורשים, ולכן רצפי ה-identity של PostgreSQL לא התקדמו.
            // מקדמים אותם ל-MAX(Id), כדי שהשורה הבאה שתיווצר לא תתנגש עם נתוני הדמו.
            foreach (var table in new[] { "Users", "Categories", "Courses", "Enrollments" })
            {
                migrationBuilder.Sql(
                    $"SELECT setval(pg_get_serial_sequence('\"{table}\"', 'Id'), (SELECT MAX(\"Id\") FROM \"{table}\"));");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Enrollments",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Enrollments",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Enrollments",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
