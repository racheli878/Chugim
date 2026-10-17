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
- סודות (User Secrets, ‏UserSecretsId ב-Chugim.API.csproj): `ConnectionStrings:Default`, `Jwt:Key`. ‏appsettings.json מכיל רק Jwt:Issuer/Audience/ExpiryMinutes.
- פיתוח: PostgreSQL 17 ב-Docker (container `postgres-dev`, DB `appdb`, port 5432).
- Migrations ב-`Infrastructure/Persistence/Migrations`; מוחלים אוטומטית בעלייה (`InitializeDatabaseAsync`).
  פקודה: `dotnet ef migrations add <Name> -p src/Chugim.Infrastructure -s src/Chugim.API -o Persistence/Migrations`
- Seed (`Persistence/Seed/SeedData.cs`, migration SeedData): admin@chugim.dev / Admin123! (Admin); rachel@, leah@, sara@chugim.dev / Demo1234! (Participant). חוג 1 "קליגרפיה עברית" — 3 מקומות, 2 רשומות (להדגמת התחרות).
- Seed עם Id מפורש לא מקדם את רצפי ה-identity ב-PostgreSQL — ה-migration כולל `setval` לכל טבלה.
- Configuration: ‏appsettings.json (בסיס, נכנס ל-repo, בלי סודות) ← appsettings.Development.json (דורס בפיתוח) ← User Secrets (פיתוח) / משתני סביבה (Render: `ConnectionStrings__Default`, `Jwt__Key`, `Cors__AllowedOrigins__0`).
- בפיתוח: לוג Debug ל-namespace ‏Chugim, ו-SQL של EF ברמת Information (לבדיקת pagination ו-N+1).
- NLog: ‏`nlog.config` = יעדים ופורמט בלבד (מסוף + קובץ יומי `logs/chugim-yyyy-MM-dd.log`); רמות ב-appsettings (`RemoveLoggerFactoryFilter = false`). שורה: זמן | רמה | CorrelationId | logger | הודעה.
- רמות: Debug לפיתוח, Information לזרימה רגילה, Warning לחריג שטופל (התנגשות, התחברות שנכשלה), Error לכשל. אף פעם לא סיסמאות, tokens או תוכן הרשמה.
- Middleware (API/Middleware): ‏ExceptionHandling ראשון (חריגה לא צפויה → Error ללוג + 500 ProblemDetails), אחריו CorrelationId (header ‏`X-Correlation-ID`, ‏`HttpContext.TraceIdentifier`, log scope). שגיאות צפויות חוזרות כ-Result ולא כחריגות.
- כל תשובת שגיאה בפורמט ProblemDetails עם `correlationId` (AddProblemDetails + CustomizeProblemDetails).
- Auth ב-API: ‏`AddJwtAuthentication` (מאמת לפי אותו JwtOptions), ‏`Roles.Admin` / `Roles.Participant` ל-[Authorize], ‏`User.ToCurrentUser()` בונה CurrentUser מה-claims. ‏Rate limit ‏`RateLimitPolicies.Auth`: 5 לדקה ל-IP (ב-Render צריך ForwardedHeaders כדי לקבל IP אמיתי).
- Program.cs משתמש במתודות הרחבה (API/Extensions) — נשאר קצר וקריא.
- Controllers יורשים `ApiControllerBase`: ‏`FromResult` (200/204 או ProblemDetails), ‏`CreatedFromResult` (201 + Location). ‏ModelState נבדק אוטומטית ע"י [ApiController] (400 עם פירוט שדות).
- Endpoints: ‏auth/register (201, בלי Location — אין GET למשתמשת), auth/login (200); courses ו-categories — [Authorize] ברמת ה-controller (האתר כולו למחוברות בלבד, החלטה מ-17.10); POST/PUT/DELETE + `{id}/enrollments` למנהלת; enrollments — [Authorize], POST למשתתפת בלבד.
- JSON: enums כמחרוזות (`JsonStringEnumConverter`).
- Pipeline: ExceptionHandling → ForwardedHeaders → CorrelationId → StatusCodePages → Swagger (גם בייצור) → HttpsRedirection → Cors → Authentication → Authorization → RateLimiter → Controllers.
- Program.cs עטוף ב-try/catch עם לוגר NLog לכשלי עלייה; `HostAbortedException` (dotnet ef) לא נחשב כשל.
- בדיקות: `tests/Chugim.Tests/{Domain,Application,Infrastructure}`. שמות: `Method_Scenario_Expected`. ‏Domain בלי Moq ובלי מסד.
- `Course.TryEnroll` קובע גם `enrollment.Course = this` (נמצא בבדיקות — בלעדיו TryCancel נכשל על הרשמה חדשה בזיכרון).
- בדיקות Infrastructure: ‏`SqliteTestDatabase` — SQLite בזיכרון עם אותו AppDbContext (EnsureCreated כולל seed). ‏ConcurrencyTests = בדיקת ההוכחה הנדרשת (שני DbContext).
- קטגוריות: מנהלת יכולה להוסיף (POST /api/categories, 409 על שם קיים). צבע הקטגוריה לא נשמר במסד — הלקוח גוזר אותו מה-Id מתוך פלטה קבועה של 8 גוונים.
- לקוח: React 18 + Vite + React Router, JavaScript, CSS ידני עם משתני עיצוב (בלי ספריית UI). תיקייה ו-repo נפרדים: `chugim-client`. כל האתר מאחורי התחברות (חוץ מ-/login ו-/register); יציאה → /login.
  עיצוב מאושר (עליז לילדות): דיו #2A2433, רקע #F8F6FB, ראשי פטל #CC2E5D, מקום אחרון מנדרינה #FF8B3D, הצלחה #2F8A4C; 8 צבעי קטגוריה לפי (id-1)%8; גופנים Fredoka (כותרות ומספרים) + Rubik. כרטיס חוג בצורת "כרטיס כניסה" עם שורת מושבים ו"ספח" עם מספר המקומות הפנויים. ניסוחים חבריים: פנייה למשתמשת בלשון נקבה, והמערכת מדברת על עצמה ברבים ("מתחברים…", "שומרים…", "לא הצלחנו").
