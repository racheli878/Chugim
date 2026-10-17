using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Chugim.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoreDemoCourses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Courses",
                columns: new[] { "Id", "Capacity", "CategoryId", "DayOfWeek", "Description", "EnrolledCount", "InstructorName", "Location", "Price", "StartDate", "StartTime", "Title", "Version" },
                values: new object[,]
                {
                    { 101, 2, 4, "Monday", "סריגה במסרגות, רקמת צלב ופריטים קטנים למתנה.", 1, "שולמית הלפרין", "חדר 5", 190m, new DateOnly(2026, 11, 9), new TimeOnly(17, 30, 0), "סריגה ורקמה", new Guid("a3c5e7f9-1b2d-4f6a-8c0e-2d4f6a8c0e11") },
                    { 102, 2, 1, "Wednesday", "עבודה בידיים, צורות בסיס וקלייה של פריט אישי.", 1, "נעמי רוזן", "חדר 3", 240m, new DateOnly(2026, 11, 11), new TimeOnly(15, 30, 0), "פיסול בחימר", new Guid("b4d6f8a0-2c3e-4a7b-9d1f-3e5a7b9d1f12") },
                    { 103, 6, 3, "Tuesday", "קרם, בצק סוכר ועיצובים צבעוניים.", 0, "יהודית אורן", "המטבח", 210m, new DateOnly(2026, 11, 10), new TimeOnly(16, 0, 0), "קישוט עוגות", new Guid("c5e7a9b1-3d4f-4b8c-8e2a-4f6b8c0e2a13") },
                    { 104, 12, 2, "Thursday", "הקלדה מהירה בעברית ובאנגלית, בלי להסתכל על המקלדת.", 0, "אסתר כץ", "מעבדת מחשבים", 150m, new DateOnly(2026, 11, 12), new TimeOnly(15, 0, 0), "הקלדה עיוורת", new Guid("d6f8b0c2-4e5a-4c9d-9f3b-5a7c9d1f3b14") },
                    { 105, 40, 1, "Friday", "סדנה גדולה: הופכים קרטונים, בקבוקים ובדים ליצירות.", 2, "מלכה ויס", "האולם הגדול", 0m, new DateOnly(2026, 11, 13), new TimeOnly(10, 0, 0), "יצירה מחומרים ממוחזרים", new Guid("e7a9c1d3-5f6b-4dae-8a4c-6b8d0e2a4c15") },
                    { 106, 8, 4, "Tuesday", "כפתורים, מכפלות ותיקונים קטנים שכל אחת צריכה לדעת.", 0, "רבקה גולד", "חדר 5", 120m, new DateOnly(2026, 11, 17), new TimeOnly(18, 30, 0), "תיקוני תפירה", new Guid("f8b0d2e4-6a7c-4ebf-9b5d-7c9e1f3b5d16") }
                });

            migrationBuilder.InsertData(
                table: "Enrollments",
                columns: new[] { "Id", "CourseId", "EnrolledAtUtc", "Status", "UserId" },
                values: new object[,]
                {
                    { 101, 101, new DateTime(2026, 10, 1, 8, 0, 0, 0, DateTimeKind.Utc), "Active", 2 },
                    { 102, 102, new DateTime(2026, 10, 1, 8, 0, 0, 0, DateTimeKind.Utc), "Active", 2 },
                    { 103, 105, new DateTime(2026, 10, 1, 8, 0, 0, 0, DateTimeKind.Utc), "Active", 2 },
                    { 104, 105, new DateTime(2026, 10, 1, 8, 0, 0, 0, DateTimeKind.Utc), "Active", 3 }
                });

            // מזהים נקבעו ידנית ב-seed — מעדכנים את המונה כדי שהחוג הבא יקבל מזהה פנוי.
            foreach (var table in new[] { "Courses", "Enrollments" })
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
                keyValue: 103);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 104);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 106);

            migrationBuilder.DeleteData(
                table: "Enrollments",
                keyColumn: "Id",
                keyValue: 101);

            migrationBuilder.DeleteData(
                table: "Enrollments",
                keyColumn: "Id",
                keyValue: 102);

            migrationBuilder.DeleteData(
                table: "Enrollments",
                keyColumn: "Id",
                keyValue: 103);

            migrationBuilder.DeleteData(
                table: "Enrollments",
                keyColumn: "Id",
                keyValue: 104);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 101);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 102);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 105);
        }
    }
}
