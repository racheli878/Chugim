namespace Chugim.Application.Common;

// כל סוג תואם לסטטוס HTTP אחד. ההמרה עצמה נעשית רק בשכבת ה-API.
public enum ErrorType
{
    Validation = 1,   // 400
    Unauthorized = 2, // 401
    Forbidden = 3,    // 403
    NotFound = 4,     // 404
    Conflict = 5      // 409
}
