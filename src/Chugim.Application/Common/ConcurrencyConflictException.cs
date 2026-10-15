namespace Chugim.Application.Common;

/// <summary>
/// נזרקת מ-Infrastructure כשהשמירה נכשלה כי מישהו אחר שינה את אותה שורה בינתיים.
/// מאפשרת ל-Application לטפל בהתנגשות בלי להכיר את EF.
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
