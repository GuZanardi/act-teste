using FinancialAccount.API.Models;
using Microsoft.EntityFrameworkCore;

namespace FinancialAccount.API.Data;

public static class DbInitializer
{
    public static readonly Guid DefaultAccountId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static async Task InitializeAsync(AppDbContext context, ILogger logger)
    {
        try
        {
            logger.LogInformation("Verificando e aplicando migrações/criação do banco de dados...");
            await context.Database.EnsureCreatedAsync();

            var defaultAccount = await context.Accounts.FirstOrDefaultAsync(a => a.Id == DefaultAccountId);
            if (defaultAccount == null)
            {
                logger.LogInformation("Criando conta empresarial padrão...");
                defaultAccount = new Account
                {
                    Id = DefaultAccountId,
                    AccountName = "Conta Empresarial Principal",
                    Balance = 0.00m,
                    CreatedAt = DateTime.UtcNow
                };

                await context.Accounts.AddAsync(defaultAccount);
                await context.SaveChangesAsync();
                logger.LogInformation("Conta padrão criada com sucesso com ID: {AccountId}", DefaultAccountId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ocorreu um erro ao inicializar o banco de dados.");
            throw;
        }
    }
}
