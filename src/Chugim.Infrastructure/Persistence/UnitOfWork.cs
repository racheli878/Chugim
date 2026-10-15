using Chugim.Application.Common;
using Chugim.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Chugim.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;

    public UnitOfWork(AppDbContext db) => _db = db;

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // התרגום היחיד במערכת מחריגת EF לחריגה ש-Application מכיר.
            throw new ConcurrencyConflictException("The data was modified by another request.", ex);
        }
    }

    public void ClearChanges() => _db.ChangeTracker.Clear();
}
