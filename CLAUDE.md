# Chugim — הנחיות לעבודה על הפרויקט

## על הפרויקט
פרויקט סיום בקורס .NET (‏Web API). מערכת לניהול חוגים: מנהלת מפרסמת חוגים, משתתפות נרשמות.
המשאב המוגבל: מקומות פנויים בחוג (Capacity). התוכנית המלאה ומצב ההתקדמות: `PLAN.md`.

## שיטת עבודה (חובה)
- מבצעים **משימה אחת** מ-PLAN.md, מסבירים בקצרה מה נעשה ולמה, ו**עוצרים לאישור** לפני המשימה הבאה.
- אחרי אישור — מסמנים את המשימה `[x]` ב-PLAN.md.
- הסברים תמציתיים אך ברורים.
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
- חוקי המקומות בתוך Course: `TryEnroll(userId, nowUtc, out enrollment)` (מגדיל מונה **ויוצר** Enrollment יחד), `ReleaseSpot()`, `TryChangeCapacity()` (משמש גם ביצירת חוג). ‏`Capacity`, `EnrolledCount`, `Version` עם `private set`. ‏`AvailableSpots`, `IsFull` מחושבים, לא נשמרים.
- `Enrollment.TryCancel()` מבטל **ומשחרר מקום** בחוג — דורש טעינת Course (Include).
- Concurrency token: ‏`Guid Version` על Course, מוגדר ב-Fluent API (`IsConcurrencyToken`), ומוחלף אוטומטית בדריסת `SaveChangesAsync`.
- UnitOfWork (Infrastructure) מתרגם `DbUpdateConcurrencyException` ל-`ConcurrencyConflictException` (Application).
- EnrollmentService: בהתנגשות — לוג Warning וניסיון חוזר עד 3 פעמים עם קריאה טרייה; חוג מלא → 409 "החוג מלא"; התנגשות מתמשכת → 409 "נסי שוב".
- Services מחזירים `Result` / `Result<T>` (ב-`Application/Common`, עם המרות מרומזות מ-T ומ-Error); ההמרה לסטטוס HTTP נעשית במקום אחד (ApiControllerBase).
- Pagination דרך מתודת הרחבה אחת `ToPagedResultAsync` (Skip/Take בתוך השאילתה).
- Roles: ‏Participant, Admin.
- טיפוסים: `DayOfWeek`, `TimeOnly StartTime`, `DateOnly StartDate`, `decimal Price`, `DateTime EnrolledAtUtc`.
- חוזים לתשתית (repositories, UnitOfWork, hasher, JWT) ב-`Application/Interfaces`; ממשקי services ליד המימושים ב-`Application/Services`.
- Repositories מחזירים ישויות: `GetByIdAsync` = קריאה בלבד (AsNoTracking), `GetForUpdateAsync` = ישות במעקב. ההמרה ל-DTO ב-service.
- אין `IEnrollmentRepository.Add` — הרשמה נוצרת דרך `Course.TryEnroll`.
- המשתמשת המחוברת מועברת ל-services כ-`CurrentUser(Id, Role)` (נבנה ב-API מה-claims), לא דרך HttpContext.
- מחיקת חוג עם `EnrolledCount > 0` → 409 (בלי שאילתה נוספת).
- AutoMapper: Profile יחיד `Application/Mappings/MappingProfile`. ‏`CourseRequest → Course` מתעלם מ-Capacity/EnrolledCount/Version (נקבעים רק דרך מתודות הישות). ‏User נוצר ידנית בהרשמה (נרמול מייל + hash).
- המרת רשימה מעומדת: `PagedResult<T>.Map(...)` שומר על נתוני העימוד.
- Auth: הודעות שגיאה כלליות (לא חושפות אם מייל רשום) + rate limiting על נקודות /api/auth (משימה 5.4).
- מחיקת חוג מותרת רק כש-EnrolledCount == 0; הרשמות מבוטלות שלו נמחקות ב-cascade (Fluent API, משימה 4.1).
- עדכון/מחיקת חוג: התנגשות concurrency → 409 בלי ניסיון חוזר (המנהלת תנסה שוב).
- הרשמה וביטול: ניסיון חוזר דרך `RetryOnConflictAsync` (עד 3) — כל ניסיון קורא מחדש, בודק ושומר.
- הודעות שגיאה קבועות מרוכזות ב-`Application/Common/AppErrors`.
- EF: ‏enums נשמרים כמחרוזות (`HasConversion<string>`). ‏Check constraints על Courses (Capacity ≥ 1, ‏0 ≤ EnrolledCount ≤ Capacity). אינדקס ייחודי חלקי על Enrollments (UserId, CourseId) WHERE Status = 'Active'.
- ‏Version מוחלף ב-`AppDbContext.SaveChanges/SaveChangesAsync` לכל Course במצב Modified.
- חבילות EF בגרסה 8.0.11 בכל הפרויקטים (Npgsql, Design, Sqlite) — חייבות להתאים זו לזו.
- JWT: ‏`JwtOptions` (סעיף "Jwt": Issuer, Audience, Key ≥ 32 תווים, ExpiryMinutes) ב-Infrastructure/Security. ‏Claims: NameIdentifier (Id), Name, Role, Jti. ‏System.IdentityModel.Tokens.Jwt **7.1.2** (תואם JwtBearer 8 — לא לשדרג ל-8.x).
- סיסמאות: מתאם סביב `Microsoft.AspNetCore.Identity.IPasswordHasher<User>` (מוזרק, לא new).
