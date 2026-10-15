using Chugim.Application.Common;

namespace Chugim.Application.Interfaces;

public interface IUnitOfWork
{
    /// <summary>
    /// שומר את כל השינויים בטרנזקציה אחת.
    /// </summary>
    /// <exception cref="ConcurrencyConflictException">שורה ששונתה השתנתה בינתיים על ידי בקשה אחרת.</exception>
    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// מנקה את כל הישויות במעקב, כדי שניסיון חוזר יקרא נתונים טריים מהמסד.
    /// </summary>
    void ClearChanges();
}
