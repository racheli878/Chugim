# Chugim — מערכת הרשמה לחוגים

Web API ב-ASP.NET Core 8 להרשמה לחוגים לבנות. המנהלת מפרסמת חוגים, לכל חוג **מספר מקומות מוגבל**, והמשתתפות נרשמות ומבטלות.
צד הלקוח (React) נמצא ב-repo נפרד: **[chugim-client](https://github.com/racheli878/chugim-client)**.

> **המשאב המוגבל: המקומות בחוג.**
> בחוג מותרות 10 בנות וכבר רשומות 9. שתי בנות לוחצות "הירשמי" באותו רגע — רק אחת מקבלת את המקום, והשנייה מקבלת **409 Conflict**.

---

## איך מונעים הרשמה כפולה למקום האחרון

### הבעיה
הדרך ה"טבעית" היא לספור את ההרשמות ואז להוסיף שורת `Enrollment`. כששתי בקשות רצות במקביל, שתיהן סופרות 9, שתיהן מוסיפות שורה, ובחוג נרשמות 11. גם optimistic concurrency לא עוזר כאן: כל בקשה רק *מוסיפה* שורה חדשה, אף אחת לא *מעדכנת* שורה קיימת, ולכן אין על מה לבדוק גרסה.

### הפתרון: מונה על החוג ו-concurrency token
1. לחוג יש מונה **`EnrolledCount`**. כל הרשמה וכל ביטול חייבים לעדכן אותו, ולכן שתי הרשמות במקביל מתנגשות על **אותה שורה**.
2. לחוג יש **`Guid Version`**, שמוגדר ב-Fluent API כ-`IsConcurrencyToken()` ומתחדש בכל שמירה של חוג (בדריסת `SaveChanges` ב-`AppDbContext`).
3. EF Core שומר ב-`UPDATE "Courses" … WHERE "Id" = @id AND "Version" = @old`. אם מישהי הספיקה לשנות את החוג קודם, אף שורה לא מתעדכנת ונזרקת `DbUpdateConcurrencyException`. ה-`UnitOfWork` מתרגם אותה ל-`ConcurrencyConflictException` של שכבת Application.
4. **ההחלטה "יש מקום?" כתובה פעם אחת**, בישות עצמה: `Course.TryEnroll` / `ReleaseSpot` / `TryChangeCapacity`.

### למה גם ניסיון חוזר
התנגשות לא אומרת שהחוג מלא. ב-5/10, כששתיים נרשמות במקביל, לשתיהן מגיע מקום. לכן `EnrollmentService` מנסה שוב, עד 3 פעמים, עם קריאה טרייה של החוג:

| מצב | תוצאה לשנייה |
|---|---|
| 5/10 | בניסיון החוזר היא רואה 6/10 ונרשמת → **201** |
| 9/10 | בניסיון החוזר היא רואה 10/10 → **409 "החוג מלא."** |
| התנגשות גם אחרי 3 ניסיונות | **409 "יש כרגע עומס הרשמות…"** |

כל התנגשות נכתבת ללוג ברמת Warning.

### ההוכחה
`tests/Chugim.Tests/Infrastructure/ConcurrencyTests.cs` פותח **שני `DbContext` נפרדים** על אותו חוג עם מקום אחד. הראשון שומר בהצלחה, והשני מקבל `DbUpdateConcurrencyException`, בדיוק כמו שתי בקשות HTTP במקביל.

**בדיקה ידנית:** בלקוח, פתחי חלון רגיל עם לאה וחלון גלישה פרטית עם שרה, היכנסי בשניהם ל"סריגה ורקמה" (נשאר בו מקום אחד) ולחצי "הירשמי" בשני החלונות בזה אחר זה.

---

## ארכיטקטורה — Clean Architecture

```
src/
  Chugim.Domain          ישויות, Enums, חוקי החוג (TryEnroll / ReleaseSpot / TryChangeCapacity)   ← בלי תלויות
  Chugim.Application     DTOs, ממשקים, Services, AutoMapper, Result<T>, PagedResult<T>             → Domain
  Chugim.Infrastructure  EF Core (DbContext, Configurations, Repositories, UnitOfWork, Migrations),
                         JWT, גיבוב סיסמאות                                                         → Application
  Chugim.API             Controllers, Middleware, Program.cs, nlog.config                           → Application + Infrastructure
tests/
  Chugim.Tests           xUnit + Moq + SQLite in-memory
```

- **הכלל:** ממשק מוגדר אצל מי שצריך אותו (Application), ומומש אצל מי שמספק את הטכנולוגיה (Infrastructure). Application לא מכירה את EF Core.
- **`Result<T>`:** כל service מחזיר תוצאה אחידה (Success / NotFound / Validation / Conflict / Forbidden / Unauthorized). **מקום אחד**, `ApiControllerBase`, ממיר אותה לסטטוס HTTP ול-ProblemDetails.
- **בלי כפל קוד:**
  - כל הודעות השגיאה נמצאות ב-`AppErrors`.
  - יש מתודת עימוד אחת, `ToPagedResultAsync`, לכל הרשימות.
  - כל שכבה רושמת את השירותים שלה במתודת הרחבה משלה (`AddApplication`, `AddInfrastructure`).

## מודל הנתונים

| ישות | שדות עיקריים |
|---|---|
| User | Id, FullName, Email (ייחודי), PasswordHash, Role (Participant / Admin) |
| Category | Id, Name (ייחודי) |
| Course | Id, Title, Description, CategoryId, InstructorName, DayOfWeek, StartTime, StartDate, Location, Price, **Capacity**, **EnrolledCount**, **Version** |
| Enrollment | Id, CourseId, UserId, EnrolledAtUtc, Status (Active / Cancelled) |

- **One-to-many:** Category → Courses, Course → Enrollments, User → Enrollments.
- **Many-to-many:** User ↔ Course, דרך ישות הביניים **Enrollment**. זו ישות מלאה ולא טבלת קישור פשוטה, כי לקשר עצמו יש נתונים: מתי נרשמה, ומה הסטטוס.
- **אילוצים במסד:**
  - אינדקס ייחודי **מסונן**: הרשמה *פעילה* אחת לכל משתתפת בכל חוג. הרשמה מבוטלת לא חוסמת הרשמה מחדש.
  - Check constraints, למשל `0 <= EnrolledCount <= Capacity`.
- **תאריך פתיחה:** חוג חדש חייב להיפתח היום או בעתיד. חוג שכבר התחיל נשאר ברשימה (עם הכיתוב "התחיל ב-…"), ואפשר עדיין להצטרף אליו. הלקוח מבקש קודם אישור, כי השיעורים שעברו לא יחזרו. אפשר גם לערוך אותו בלי לשנות את התאריך.
- **הגדרות:** כל ההגדרות ב-Fluent API, וכל ישות בקובץ `IEntityTypeConfiguration` משלה. ה-Enums נשמרים כמחרוזות.
- **Migrations** (Code-First):
  1. `Init`: הסכמה.
  2. `SeedData`: משתמשות, קטגוריות, חוגים והרשמות.
  3. `MoreDemoCourses`: חוגים נוספים לעימוד ולהדגמת 409.

## Endpoints

כל ה-endpoints, חוץ מ-auth, דורשים התחברות (JWT). שגיאות מוחזרות כ-ProblemDetails עם `correlationId`.

| Method | Route | הרשאה | סטטוסים |
|---|---|---|---|
| POST | `/api/auth/register` | פתוח (rate limit) | 201, 400, 409 |
| POST | `/api/auth/login` | פתוח (rate limit) | 200, 400, 401 |
| GET | `/api/categories` | מחוברת | 200 |
| GET | `/api/categories/{id}` | מחוברת | 200, 404 |
| POST | `/api/categories` | Admin | 201, 400, 409 |
| GET | `/api/courses?categoryId&search&hasAvailableSpots&sortBy&descending&page&pageSize` | מחוברת | 200, 400 |
| GET | `/api/courses/{id}` | מחוברת | 200, 404 |
| POST | `/api/courses` | Admin | 201 + Location, 400 |
| PUT | `/api/courses/{id}` | Admin | 204, 400, 404, 409 |
| DELETE | `/api/courses/{id}` | Admin | 204, 404, 409 (יש רשומות) |
| GET | `/api/courses/{id}/enrollments` | Admin | 200, 404 |
| POST | `/api/enrollments` | Participant | 201 + Location, 400, 404, **409** |
| GET | `/api/enrollments?page&pageSize` | מחוברת (משתתפת: שלה, מנהלת: הכול) | 200 |
| GET | `/api/enrollments/{id}` | בעלים / Admin | 200, 403, 404 |
| DELETE | `/api/enrollments/{id}` | בעלים / Admin | 204, 403, 404, 409 |

**עימוד:**
- `pageSize` יכול להיות לכל היותר 50.
- `sortBy` מקבל `StartDate`, `Title`, `Price` או `AvailableSpots`.
- הרשימה נחתכת בתוך שאילתת ה-SQL עצמה (Skip/Take), עם `AsNoTracking`.

## איך כל דרישה מומשה

| דרישה | איפה |
|---|---|
| REST, סטטוסים נכונים, Swagger | Controllers, `ApiControllerBase`, Swagger עם כפתור Authorize |
| Model binding ו-Validation | Data Annotations על ה-DTOs, ו-ValidationProblemDetails אוטומטי |
| שכבות ו-DI | 4 פרויקטים, רישום שירותים במתודות הרחבה |
| EF Core Code-First, migrations, seed | `Persistence/Configurations`, 3 migrations, `SeedData` עם `HasData` |
| LINQ, עימוד אמיתי, AsNoTracking | `CourseRepository`, `EnrollmentRepository`, `QueryableExtensions` |
| One-to-many ו-many-to-many עם Fluent API | ראו "מודל הנתונים" |
| DTOs ו-AutoMapper | `Application/DTOs`, `MappingProfile` (נבדק ב-`AssertConfigurationIsValid`) |
| async ו-CancellationToken | בכל שרשרת הקריאות, מה-Controller ועד EF |
| Middleware | `ExceptionHandlingMiddleware` (שגיאה אחידה ולוג Error), `CorrelationIdMiddleware` (כותרת `X-Correlation-ID` ו-scope בלוג) |
| קונפיגורציה וסודות | `appsettings.*.json` בלי סודות; User Secrets בפיתוח, משתני סביבה בייצור |
| JWT עם 2 תפקידים | `JwtTokenGenerator`, `[Authorize(Roles = …)]`, `Participant` ו-`Admin` |
| בדיקות xUnit ו-Moq | 39 בדיקות, כולל בדיקת שני DbContext |
| Optimistic concurrency ו-409 | `Course.Version` ו-`UnitOfWork`, עם ניסיון חוזר ב-`EnrollmentService` |
| NLog | `nlog.config`: קונסול וקובץ יומי, עם CorrelationId בכל שורה |

**אבטחה נוספת:**
- סיסמאות מגובבות עם `PasswordHasher` של ASP.NET Core Identity.
- rate limit על auth: 5 בקשות לדקה לכל IP.
- הודעת הרשמה כללית, שלא מגלה אילו כתובות מייל רשומות.
- CORS לכתובות מוגדרות בלבד.

## הרצה מקומית

**דרישות:** .NET 8 SDK ושרת PostgreSQL (גרסה 14 ומעלה). אפשר להתקין אותו בשתי דרכים, והתוצאה זהה:

1. **PostgreSQL** — אחת מהשתיים:
   - **התקנה רגילה (בלי Docker):** מורידים את ההתקנה מ-[postgresql.org/download](https://www.postgresql.org/download/), ובוחרים סיסמה למשתמש `postgres` בזמן ההתקנה. מסד הנתונים `appdb` נוצר לבד בהרצה הראשונה.
   - **Docker:**
     ```bash
     docker run --name postgres-dev -e POSTGRES_PASSWORD=<סיסמה> -p 5432:5432 -d postgres:17
     ```
     בפעמים הבאות מספיק `docker start postgres-dev`.

   > ההתקנה הזו נדרשת **רק להרצה מקומית**. באתר שבענן, Render מספק את מסד הנתונים, והמשתמשות רק פותחות כתובת בדפדפן.

2. **סודות (User Secrets).** הם לא נשמרים ב-repo:
   ```bash
   dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=appdb;Username=postgres;Password=<סיסמה>" --project src/Chugim.API
   dotnet user-secrets set "Jwt:Key" "<מפתח אקראי של 32 תווים לפחות>" --project src/Chugim.API
   ```

3. **הרצה:**
   ```bash
   dotnet run --project src/Chugim.API --launch-profile https
   ```
   ה-migrations מוחלים אוטומטית כשהשרת עולה. Swagger זמין בכתובת https://localhost:7180/swagger.

4. **בדיקות:**
   ```bash
   dotnet test
   ```
   הבדיקות רצות על SQLite בזיכרון, בלי Docker.

### משתמשות לדמו
| תפקיד | מייל | סיסמה |
|---|---|---|
| מנהלת | admin@chugim.dev | `Admin123!` |
| משתתפות | rachel@chugim.dev, leah@chugim.dev, sara@chugim.dev | `Demo1234!` |

**ב-Swagger:** קודם `POST /api/auth/login`, ואחר כך מדביקים ב-**Authorize** רק את ה-token (מתחיל ב-`eyJ`), בלי מירכאות ובלי `Bearer`.

**נתוני הדמו:**
- "קליגרפיה עברית": 2 מתוך 3 מקומות תפוסים.
- "סריגה ורקמה" ו"פיסול בחימר": נשאר בכל אחד מקום אחד, מתאים להדגמת התחרות.
- "יצירה מחומרים ממוחזרים": חוג גדול של 40 מקומות.

## לוגים

- הלוג נכתב לקונסול ולקובץ `logs/chugim-<תאריך>.log` בתיקיית ההרצה. נשמרים 14 ימים אחרונים.
- כל שורה כוללת את ה-**CorrelationId** של הבקשה. אותו מזהה חוזר ללקוח בכותרת `X-Correlation-ID` ובגוף השגיאה, כך שאפשר לאתר בדיוק את הבקשה שנכשלה.
- רמות הלוג נקבעות ב-`appsettings` (סעיף `Logging`), ו-NLog קובע רק לאן ובאיזה פורמט כותבים.
- **לא נכתבים ללוג:** סיסמאות, tokens ותוכן בקשות ההרשמה.

## הערה על AutoMapper

הפרויקט משתמש ב-**AutoMapper 14.0.0**, הגרסה האחרונה ברישיון חופשי (MIT). מגרסה 15 נדרש רישיון מסחרי.
לגרסה הזו יש אזהרת אבטחה ידועה (GHSA-rvv3-g6hj-g44x). היא נוגעת למיפוי של גרפים עמוקים מאוד מקלט לא מהימן, וזה לא מצב שקורה כאן: המיפוי הוא מישויות לתשובות שטוחות. לכן האזהרה מושתקת במכוון ב-`Chugim.Application.csproj` (`NuGetAuditSuppress`), עם הסבר בקובץ.

## פריסה ל-Render

שלושה רכיבים, כולם באותו אזור (Frankfurt, הקרוב לישראל):

| רכיב ב-Render | מקור | הגדרות |
|---|---|---|
| **PostgreSQL** | — | נוצר ריק. ה-migrations והנתונים נוצרים בעלייה הראשונה של השרת |
| **Web Service** (השרת) | repo `Chugim`, Runtime: **Docker** | נבנה מה-`Dockerfile`. Health Check Path: `/health` |
| **Static Site** (הלקוח) | repo `chugim-client` | Build: `npm ci && npm run build` · Publish: `dist` · Rewrite: `/*` → `/index.html` |

**משתני סביבה של השרת.** הסודות נמצאים רק כאן ולא ב-repo:

| משתנה | ערך |
|---|---|
| `ConnectionStrings__Default` | ה-**Internal Database URL** של מסד הנתונים. השרת מקבל גם את פורמט ה-URL (`postgres://…`) וממיר אותו בעצמו |
| `Jwt__Key` | מחרוזת אקראית של 32 תווים לפחות. Render יכול לייצר אותה ("Generate") |
| `Cors__AllowedOrigins__0` | כתובת הלקוח, למשל `https://chugim-client.onrender.com`, בלי `/` בסוף |

**משתנה הסביבה של הלקוח:** `VITE_API_URL` = כתובת השרת, למשל `https://chugim-api.onrender.com`.

- **`__` במקום `:`:** משתני סביבה לא יכולים להכיל נקודתיים, ו-ASP.NET Core קורא את `ConnectionStrings__Default` כאילו נכתב `ConnectionStrings:Default`. כך אותו קוד קורא User Secrets בפיתוח ומשתני סביבה בענן.
- **במסלול החינמי** השרת "נרדם" אחרי זמן בלי שימוש, והבקשה הראשונה אחרי זה לוקחת עד כדקה. לפני הדגמה כדאי לפתוח את `/health` כדי להעיר אותו.
