namespace Chugim.API.Authorization;

public static class RateLimitPolicies
{
    // הרשמה והתחברות: מגביל ניסיונות חוזרים (ניחוש סיסמאות, בדיקת מיילים רשומים).
    public const string Auth = "auth";
}
