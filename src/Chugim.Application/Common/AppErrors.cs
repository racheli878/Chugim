namespace Chugim.Application.Common;

// כל הודעות השגיאה הקבועות במקום אחד — בלי שכפול בין services.
public static class AppErrors
{
    public static readonly Error CourseNotFound = Error.NotFound("החוג לא נמצא.");
    public static readonly Error CategoryNotFound = Error.Validation("הקטגוריה שנבחרה אינה קיימת.");
    public static readonly Error CategoryDoesNotExist = Error.NotFound("הקטגוריה לא נמצאה.");
    public static readonly Error CategoryNameTaken = Error.Conflict("כבר קיימת קטגוריה בשם הזה.");
    public static readonly Error StartDateInPast = Error.Validation("תאריך הפתיחה כבר עבר. צריך לבחור את היום או תאריך עתידי.");
    public static readonly Error InvalidCapacity = Error.Validation("מספר המקומות חייב להיות לפחות 1.");
    public static readonly Error CourseHasParticipants = Error.Conflict("לא ניתן למחוק חוג שיש בו משתתפות רשומות.");
    public static readonly Error CourseModifiedConcurrently =
        Error.Conflict("החוג השתנה בינתיים (למשל נרשמה אליו משתתפת). רענני ונסי שוב.");

    public static readonly Error CourseFull = Error.Conflict("החוג מלא.");
    public static readonly Error AlreadyEnrolled = Error.Conflict("את כבר רשומה לחוג הזה.");
    public static readonly Error EnrollmentBusy = Error.Conflict("יש כרגע עומס הרשמות לחוג. נסי שוב בעוד רגע.");
    public static readonly Error EnrollmentNotFound = Error.NotFound("ההרשמה לא נמצאה.");
    public static readonly Error EnrollmentForbidden = Error.Forbidden("אין לך הרשאה להרשמה זו.");
    public static readonly Error EnrollmentAlreadyCancelled = Error.Conflict("ההרשמה כבר בוטלה.");
}
