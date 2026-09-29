using Inventory.Application.Common.Exceptions;
using Microsoft.Data.SqlClient;
namespace Inventory.Infrastructure.Persistence;
// The handlers check uniqueness before writing, but two concurrent requests can both pass
// that check. The unique index is the real guard; this turns its violation into the same
// ConflictException the check would have thrown, so the API answers 409 instead of 500.
internal static class UniqueConstraintGuard
{
    private const int DuplicateKeyInUniqueIndex = 2601;
    private const int DuplicateKeyInUniqueConstraint = 2627;
    public static async Task<TResult> RunAsync<TResult>(Func<Task<TResult>> write, string conflictMessage)
    {
        try
        {
            return await write();
        }
        catch (SqlException exception) when (IsUniqueViolation(exception))
        {
            throw new ConflictException(conflictMessage, exception);
        }
    }
    public static async Task RunAsync(Func<Task> write, string conflictMessage)
    {
        try
        {
            await write();
        }
        catch (SqlException exception) when (IsUniqueViolation(exception))
        {
            throw new ConflictException(conflictMessage, exception);
        }
    }
    private static bool IsUniqueViolation(SqlException exception)
    {
        return exception.Number is DuplicateKeyInUniqueIndex or DuplicateKeyInUniqueConstraint;
    }
}
