using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Infrastructure.Repositories;

public sealed partial class FreeSqlAiRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<AiSecret>> ListSecretsAsync(CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiSecret>().OrderBy(s => s.SecretRef).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AiSecret?> GetSecretByRefAsync(string secretRef, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AiSecret>().Where(s => s.SecretRef == secretRef).FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AiSecret> SaveSecretAsync(AiSecret secret, CancellationToken cancellationToken = default)
    {
        if (secret.Id == 0)
        {
            var inserted = await FreeSql.Insert(secret).ExecuteInsertedAsync(cancellationToken);
            secret.Id = inserted.First().Id;
            return secret;
        }

        await FreeSql.Update<AiSecret>().SetSource(secret).ExecuteAffrowsAsync(cancellationToken);
        return secret;
    }
}
