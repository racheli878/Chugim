# Chugim — הנחיות לעבודה על הפרויקט

## על הפרויקט
פרויקט סיום בקורס .NET (‏Web API). מערכת לניהול חוגים: מנהלת מפרסמת חוגים, משתתפות נרשמות.
המשאב המוגבל: מקומות פנויים בחוג (Capacity). התוכנית המלאה ומצב ההתקדמות: `PLAN.md`.

## שיטת עבודה (חובה)
- מבצעים **משימה אחת** מ-PLAN.md, מסבירים בקצרה מה נעשה ולמה, ו**עוצרים לאישור** לפני המשימה הבאה.
- אחרי אישור — מסמנים את המשימה `[x]` ב-PLAN.md.
- אין כפל קוד. קוד ברור, יעיל וקריא. הערות בעברית רק במקומות שבהם ה"למה" לא מובן מאליו.

## טכנולוגיות
‏.NET 8 · ASP.NET Core Web API (Controllers) · EF Core 8 + PostgreSQL (Npgsql) · AutoMapper 14 · JWT · NLog · xUnit + Moq + SQLite in-memory · לקוח React (Vite) ב-repo נפרד · פריסה ל-Render.

## ארכיטקטורה — Clean Architecture
```
src/Chugim.Domain          ישויות, enums, חוקי עסק בתוך הישויות      ← ללא שום תלות
src/Chugim.Application     ממשקים, DTOs, services, AutoMapper, Result  → Domain
src/Chugim.Infrastructure  EF, repositories, UnitOfWork, JWT, hashing  → Application
src/Chugim.API             controllers, middleware, Program.cs         → Application (+ Infrastructure לרישום DI בלבד)
tests/Chugim.Tests                                                     → Application, Infrastructure
```
כללים:
- Domain לא מפנה לשום פרויקט או חבילה (בפרט לא EF).
- ממשק מוגדר ב-Application, המימוש ב-Infrastructure.
- Controller לא מזריק DbContext — רק services.
- ישויות לא יוצאות ולא נכנסות דרך ה-API — רק DTOs.
- כל השרשרת async עם CancellationToken. אין `.Result` / `.Wait()`.
- כל התאריכים-שעות ב-UTC (`DateTime.UtcNow`).
- אין סודות בקוד או ב-repo — User Secrets בפיתוח, משתני סביבה ב-Render.

## החלטות שהתקבלו
- לכל ישות `Id` משלה (אין מחלקת בסיס).
- ישויות: User, Category, Course, Enrollment. ‏Enrollment היא ישות הביניים של הקשר רבים-לרבים User ↔ Course.
- Course מחזיק `EnrolledCount` (עם `private set`) — כדי שכל הרשמה תעדכן את שורת החוג ותתנגש עליה.
  ספירת Enrollments ואז INSERT לא ניתנת להגנה ב-optimistic concurrency.
- חוקי המקומות בתוך Course: `TryEnroll()`, `ReleaseSpot()`, `TryChangeCapacity()`. ‏`AvailableSpots` מחושב, לא נשמר.
- Concurrency token: ‏`Guid Version` על Course, מוגדר ב-Fluent API (`IsConcurrencyToken`), ומוחלף אוטומטית בדריסת `SaveChangesAsync`.
- UnitOfWork (Infrastructure) מתרגם `DbUpdateConcurrencyException` ל-`ConcurrencyConflictException` (Application).
- EnrollmentService: בהתנגשות — לוג Warning וניסיון חוזר עד 3 פעמים עם קריאה טרייה; חוג מלא → 409 "החוג מלא"; התנגשות מתמשכת → 409 "נסי שוב".
- Services מחזירים `Result<T>`; ההמרה לסטטוס HTTP נעשית במקום אחד (ApiControllerBase).
- Pagination דרך מתודת הרחבה אחת `ToPagedResultAsync` (Skip/Take בתוך השאילתה).
- Roles: ‏Participant, Admin.
- טיפוסים: `DayOfWeek`, `TimeOnly StartTime`, `DateOnly StartDate`, `decimal Price`, `DateTime EnrolledAtUtc`.
