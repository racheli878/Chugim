# Chugim — מערכת לניהול חוגים והרשמה

> מצב התקדמות: משימה שהושלמה ואושרה מסומנת `[x]`.

## 1. הרעיון
מנהלת מפרסמת חוגים, כל חוג בנושא (קטגוריה) משלו ועם פרטים משלו: מדריכה, יום ושעה, מיקום, תאריך התחלה, מחיר, ו**מספר מקומות מקסימלי**. משתתפות נרשמות לחוגים ויכולות לבטל.

**המשאב המוגבל: המקומות הפנויים בחוג.**
מבחן המשפט: "בחוג מותרות 10 בנות וכבר רשומות 9. שתי בנות לחצו 'הירשמי' באותו רגע, שתיהן קיבלו אישור — ואסור היה ששתיהן יקבלו."

## 2. מודל הנתונים (4 ישויות)
| ישות | שדות עיקריים | הערות |
|---|---|---|
| User | Id, FullName, Email (ייחודי), PasswordHash, Role | Role: Participant / Admin |
| Category | Id, Name | נושא החוג (אומנות, מחשבים, ספורט...) |
| Course | Id, Title, Description, CategoryId, InstructorName, DayOfWeek, StartTime, Location, StartDateUtc, Price, **Capacity**, **EnrolledCount**, **Version** | המשאב המוגבל |
| Enrollment | Id, CourseId, UserId, EnrolledAtUtc, Status | Status: Active / Cancelled |

קשרים:
- **One-to-many:** Category → Courses, Course → Enrollments, User → Enrollments
- **Many-to-many:** User ↔ Course דרך ישות הביניים Enrollment (קשר רבים-לרבים עם נתונים על הקשר עצמו: תאריך וסטטוס)
- אינדקס ייחודי: משתתפת לא יכולה להחזיק שתי הרשמות פעילות לאותו חוג
- כל ההגדרות ב-Fluent API, כל ישות בקובץ IEntityTypeConfiguration משלה

### למה צריך את העמודה EnrolledCount (נקודה קריטית)
הדרך ה"טבעית" — לספור הרשמות (`Count()`) ואז להוסיף שורת Enrollment — **לא ניתנת להגנה ב-optimistic concurrency**: שתי הבקשות רק *מוסיפות* שורות חדשות, אף אחת לא *מעדכנת* שורה קיימת, ולכן אין על מה לבדוק Version.
הפתרון: מונה `EnrolledCount` על שורת החוג. כל הרשמה חייבת לעדכן אותו, ולכן שתי הרשמות במקביל מתנגשות על **אותה שורה** — ושם ה-Version תופס אותן.

## 3. ארכיטקטורה — Clean Architecture, ‏.NET 8
```
Chugim.sln
 src/
  Chugim.Domain          Entities, Enums, חוקי החוג (Course.Enroll / Course.ReleaseSpot)          ← אפס תלויות
  Chugim.Application     Interfaces, DTOs, Services, AutoMapper Profiles, Result<T>, PagedResult<T>,
                         ConcurrencyConflictException, AddApplication()                          → Domain
  Chugim.Infrastructure  AppDbContext, Configurations, Repositories, UnitOfWork, Migrations,
                         JwtTokenGenerator, PasswordHasher, AddInfrastructure()                  → Application
  Chugim.API             Controllers, Middleware, Program.cs, nlog.config                        → Application (+ Infrastructure לרישום בלבד)
 tests/
  Chugim.Tests           xUnit + Moq + SQLite in-memory                                          → Application, Infrastructure
```
מיפוי מהמבנה הישן: Core ← מתפצל ל-Domain (ישויות וחוקים) ול-Application (ממשקים ו-DTOs) · Service ← Application · Data ← Infrastructure (ומקבל גם JWT וגיבוב סיסמאות).
הכלל: ממשק מוגדר אצל מי שצריך אותו (Application), ומומש אצל מי שמספק את הטכנולוגיה (Infrastructure).

עקרונות נגד כפל קוד:
- **חוקי המקומות בישות עצמה:** `Course.Enroll()` / `Course.ReleaseSpot()` / `Course.ChangeCapacity()` — ההחלטה "יש מקום?" כתובה פעם אחת.
- **Result&lt;T&gt;** אחיד מכל service (Success / NotFound / Validation / Conflict / Forbidden), ו**מקום אחד** ב-API שממיר אותו לסטטוס HTTP.
- **PagedResult&lt;T&gt;** + מתודת הרחבה אחת `ToPagedResultAsync` על IQueryable — Skip/Take בתוך השאילתה, לכל הרשימות.
- **IUnitOfWork** אחד לשמירה; שם בלבד נתפסת DbUpdateConcurrencyException ומתורגמת לחריגה של Application.
- **החלפת Version אוטומטית** בדריסת SaveChangesAsync ב-DbContext.
- רישום שירותים במתודת הרחבה לכל שכבה — Program.cs נשאר קצר.

## 4. מנגנון התחרות (הלב)
- **Concurrency token:** `Guid Version` על Course, מוגדר ב-Fluent API כ-`IsConcurrencyToken()` (Domain נשאר נקי מ-EF). עובד זהה ב-PostgreSQL וב-SQLite של הבדיקות.
- **זרימת הרשמה (EnrollmentService.EnrollAsync):**
  1. טעינת החוג (tracked)
  2. בדיקות: החוג קיים (404), המשתתפת לא רשומה כבר (409)
  3. `course.Enroll()` — אם מלא → Conflict "החוג מלא"; אחרת EnrolledCount++
  4. יצירת Enrollment
  5. שמירה אחת → EF מריץ `UPDATE Courses ... WHERE Id=@id AND Version=@old` + INSERT, בטרנזקציה אחת
  6. אם מישהי הספיקה לפנינו → UnitOfWork זורק `ConcurrencyConflictException` → לוג Warning → **ניסיון חוזר** (עד 3 פעמים) עם קריאה טרייה של החוג
- **למה ניסיון חוזר:** התנגשות לא אומרת שהחוג מלא. ב-5/10 ששתיים נרשמות במקביל — לשתיהן מגיע מקום. בניסיון החוזר:
  - ב-5/10: השנייה רואה 6/10 ונרשמת בהצלחה
  - ב-9/10: השנייה רואה 10/10 ומקבלת **409 "החוג מלא"**
  - אם גם אחרי 3 ניסיונות יש התנגשות → **409 "יש עומס, נסי שוב"**
- ביטול הרשמה (`ReleaseSpot`) ושינוי Capacity ע"י מנהלת עוברים דרך אותו מנגנון. אסור להקטין Capacity מתחת ל-EnrolledCount.

## 5. Endpoints
| Method | Route | הרשאה | סטטוסים |
|---|---|---|---|
| POST | /api/auth/register | פתוח | 201, 400, 409 (מייל קיים) |
| POST | /api/auth/login | פתוח | 200, 401 |
| GET | /api/categories | פתוח | 200 |
| GET | /api/courses?categoryId=&search=&hasSpots=&sortBy=&desc=&page=&pageSize= | פתוח | 200 |
| GET | /api/courses/{id} | פתוח | 200, 404 |
| POST | /api/courses | Admin | 201 + Location, 400 |
| PUT | /api/courses/{id} | Admin | 204, 400, 404, 409 |
| DELETE | /api/courses/{id} | Admin | 204, 404, 409 (יש רשומות פעילות) |
| GET | /api/courses/{id}/enrollments | Admin | 200, 404 |
| POST | /api/enrollments | Participant | 201 + Location, 400, 404, **409** |
| GET | /api/enrollments?page=&pageSize= | מחוברת (משתתפת: שלה, מנהלת: הכול) | 200 |
| GET | /api/enrollments/{id} | בעלים / Admin | 200, 403, 404 |
| DELETE | /api/enrollments/{id} | בעלים / Admin | 204, 403, 404, 409 |

---

## 6. המשימות

### שלב 0 — הקמה
- [x] **0.1** יצירת solution, 5 פרויקטים בתיקיות src/tests, הפניות לפי כיוון התלויות, .gitignore, git init

### שלב 1 — Domain
- [x] **1.1** Entities + Enums + חוקי Course (Enroll / ReleaseSpot / ChangeCapacity)

### שלב 2 — Application (חלק א: חוזים)
- [x] **2.1** Common: Result&lt;T&gt;, ErrorType, PagedResult&lt;T&gt;, PaginationQuery, ConcurrencyConflictException
- [x] **2.2** DTOs ל-request (עם Data Annotations) ול-response
- [x] **2.3** Interfaces: Repositories, IUnitOfWork, Services, IJwtTokenGenerator, IPasswordHasher

### שלב 3 — Application (חלק ב: לוגיקה)
- [x] **3.1** AutoMapper Profiles
- [ ] **3.2** AuthService
- [ ] **3.3** CourseService + CategoryService — CRUD, סינון, מיון, pagination
- [ ] **3.4** EnrollmentService — הרשמה עם טיפול בתחרות וניסיון חוזר, ביטול, שליפה לפי הרשאה
- [ ] **3.5** AddApplication()

### שלב 4 — Infrastructure
- [ ] **4.1** AppDbContext + Configurations ב-Fluent API (many-to-many, אינדקס ייחודי, concurrency token) + דריסת SaveChangesAsync
- [ ] **4.2** Repositories + UnitOfWork + ToPagedResultAsync (AsNoTracking בקריאות, Include/ThenInclude)
- [ ] **4.3** PasswordHasher + JwtTokenGenerator (JwtOptions דרך IOptions)
- [ ] **4.4** Migration ראשון: Init
- [ ] **4.5** Migration שני: Seed data (מנהלת + 2 משתתפות לדמו, קטגוריות, 5–6 חוגים — אחד מהם כמעט מלא להדגמה)
- [ ] **4.6** AddInfrastructure() + החלת migrations אוטומטית בעלייה

### שלב 5 — API
- [ ] **5.1** Configuration: appsettings.json, appsettings.Development.json, User Secrets (connection string, מפתח JWT)
- [ ] **5.2** NLog: חבילה, nlog.config, חיבור ל-ILogger
- [ ] **5.3** Middleware: CorrelationId (+ לוג לכל בקשה) ו-ExceptionHandling (JSON אחיד + לוג Error)
- [ ] **5.4** JWT Authentication + Authorization עם roles
- [ ] **5.5** ApiControllerBase (Result → HTTP) + Controllers: Auth, Categories, Courses, Enrollments
- [ ] **5.6** Program.cs — רישום, Swagger עם כפתור Authorize, סדר pipeline נכון

### שלב 6 — בדיקות
- [ ] **6.1** Course (Domain): Enroll כשיש מקום / כשמלא
- [ ] **6.2** EnrollmentService עם Moq: הצלחה, חוג מלא → Conflict, כבר רשומה → Conflict, התנגשות ואז הצלחה בניסיון חוזר, חוג לא קיים → NotFound
- [ ] **6.3** בדיקת ההוכחה: שני DbContext נפרדים על אותו חוג — הראשון מצליח, השני מקבל DbUpdateConcurrencyException
- [ ] **6.4** בדיקות נוספות: AuthService, CourseService (בסיסי)

### שלב 7 — לקוח React (Vite) — מינימלי (סמינר: הלקוח לא נבדק)
- [ ] **7.1** התחברות / הרשמה, שמירת JWT, axios עם header
- [ ] **7.2** רשימת חוגים עם pagination מהשרת ("נותרו X מקומות")
- [ ] **7.3** כפתור "הירשמי" + "ההרשמות שלי" עם ביטול
- [ ] **7.4** טיפול ברור ב-409 (הודעה + רענון מספר המקומות)

### שלב 8 — הגשה
- [ ] **8.1** README: תיאור, המשאב המוגבל והטיפול בתחרות, הוראות הרצה, משתמשי דמו
- [ ] **8.2** שני repositories ב-GitHub
- [ ] **8.3** פריסה ל-Render: PostgreSQL, שרת (Dockerfile + משתני סביבה), לקוח (Static Site)

## 7. חבילות עיקריות (גרסאות 8.x התואמות ל-.NET 8)
Application: AutoMapper 14 (אחרונה ברישיון חופשי) · Infrastructure: Npgsql.EntityFrameworkCore.PostgreSQL, System.IdentityModel.Tokens.Jwt, Microsoft.Extensions.Identity.Core (PasswordHasher) · API: Microsoft.EntityFrameworkCore.Design, Microsoft.AspNetCore.Authentication.JwtBearer, NLog.Web.AspNetCore, Swashbuckle.AspNetCore · Tests: xunit, Moq, Microsoft.EntityFrameworkCore.Sqlite
