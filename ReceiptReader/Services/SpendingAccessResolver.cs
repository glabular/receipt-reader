using Microsoft.Extensions.Configuration;

namespace ReceiptReader.Services;

internal sealed class SpendingAccessResolver
{
    private const string AliasesConfigurationKey = "ReceiptReader:SpendingAccess:Aliases";
    private readonly IReadOnlyDictionary<long, long> _aliases;

    public SpendingAccessResolver(IConfiguration configuration)
    {
        var aliases = configuration
            .GetSection(AliasesConfigurationKey)
            .GetChildren()
            .Select(c =>
            {
                var hasViewerId = long.TryParse(c.Key, out var viewerId);
                var hasOwnerId = long.TryParse(c.Value, out var ownerId);
                return new { hasViewerId, viewerId, hasOwnerId, ownerId };
            })
            .Where(x => x.hasViewerId && x.hasOwnerId)
            .ToDictionary(
                x => x.viewerId,
                x => x.ownerId);

        _aliases = aliases;
    }

    public long ResolveOwnerTelegramUserId(long requesterTelegramUserId)
    {
        return _aliases.TryGetValue(requesterTelegramUserId, out var ownerTelegramUserId)
            ? ownerTelegramUserId
            : requesterTelegramUserId;
    }
}
