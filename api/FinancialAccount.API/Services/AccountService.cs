using System.Data;
using FinancialAccount.API.Data;
using FinancialAccount.API.DTOs;
using FinancialAccount.API.Exceptions;
using FinancialAccount.API.Models;
using Microsoft.EntityFrameworkCore;

namespace FinancialAccount.API.Services;

public class AccountService : IAccountService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AccountService> _logger;

    public AccountService(AppDbContext context, ILogger<AccountService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Account> GetOrCreateAccountAsync(Guid? accountId = null, CancellationToken cancellationToken = default)
    {
        var targetId = accountId ?? DbInitializer.DefaultAccountId;
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == targetId, cancellationToken);

        if (account == null)
        {
            if (targetId == DbInitializer.DefaultAccountId)
            {
                account = new Account
                {
                    Id = DbInitializer.DefaultAccountId,
                    AccountName = "Conta Empresarial Principal",
                    Balance = 0.00m,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.Accounts.AddAsync(account, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new NotFoundException($"Conta com ID '{targetId}' não foi encontrada.");
            }
        }

        return account;
    }

    public async Task<BalanceResponseDto> GetBalanceAsync(Guid? accountId = null, CancellationToken cancellationToken = default)
    {
        var account = await GetOrCreateAccountAsync(accountId, cancellationToken);

        var transactionsQuery = _context.Transactions.Where(t => t.AccountId == account.Id);

        var totalCredits = await transactionsQuery
            .Where(t => t.Type == TransactionType.Credit)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0.00m;

        var totalDebits = await transactionsQuery
            .Where(t => t.Type == TransactionType.Debit)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0.00m;

        var totalTransactions = await transactionsQuery.CountAsync(cancellationToken);

        return new BalanceResponseDto
        {
            AccountId = account.Id,
            AccountName = account.AccountName,
            Balance = account.Balance,
            TotalCredits = totalCredits,
            TotalDebits = totalDebits,
            TotalTransactions = totalTransactions,
            LastUpdated = account.UpdatedAt ?? account.CreatedAt
        };
    }

    public async Task<TransactionResponseDto> DepositAsync(CreateTransactionRequest request, Guid? accountId = null, CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
        {
            throw new BusinessException("O valor do depósito deve ser maior que zero.");
        }

        var executionStrategy = _context.Database.CreateExecutionStrategy();

        return await executionStrategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

            var targetId = accountId ?? DbInitializer.DefaultAccountId;
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == targetId, cancellationToken);

            if (account == null)
            {
                account = await GetOrCreateAccountAsync(targetId, cancellationToken);
            }

            account.Balance += request.Amount;
            account.UpdatedAt = DateTime.UtcNow;

            var transactionEntity = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                Type = TransactionType.Credit,
                Amount = request.Amount,
                Description = string.IsNullOrWhiteSpace(request.Description) ? "Entrada de valores" : request.Description.Trim(),
                BalanceAfter = account.Balance,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Transactions.AddAsync(transactionEntity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Depósito de R$ {Amount} realizado com sucesso na conta {AccountId}. Novo saldo: R$ {Balance}",
                request.Amount, account.Id, account.Balance);

            return MapToDto(transactionEntity);
        });
    }

    public async Task<TransactionResponseDto> WithdrawAsync(CreateTransactionRequest request, Guid? accountId = null, CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
        {
            throw new BusinessException("O valor do saque/débito deve ser maior que zero.");
        }

        var executionStrategy = _context.Database.CreateExecutionStrategy();

        return await executionStrategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

            var targetId = accountId ?? DbInitializer.DefaultAccountId;
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == targetId, cancellationToken);

            if (account == null)
            {
                account = await GetOrCreateAccountAsync(targetId, cancellationToken);
            }

            // Regra crucial de negócio: Não permitir saldo negativo
            if (account.Balance < request.Amount)
            {
                _logger.LogWarning("Tentativa de saque rejeitada por saldo insuficiente. Conta: {AccountId}, Saldo: {Balance}, Solicitado: {Amount}",
                    account.Id, account.Balance, request.Amount);
                throw new InsufficientBalanceException(request.Amount, account.Balance);
            }

            account.Balance -= request.Amount;
            account.UpdatedAt = DateTime.UtcNow;

            var transactionEntity = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                Type = TransactionType.Debit,
                Amount = request.Amount,
                Description = string.IsNullOrWhiteSpace(request.Description) ? "Saída de valores" : request.Description.Trim(),
                BalanceAfter = account.Balance,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Transactions.AddAsync(transactionEntity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Saque de R$ {Amount} realizado com sucesso na conta {AccountId}. Novo saldo: R$ {Balance}",
                request.Amount, account.Id, account.Balance);

            return MapToDto(transactionEntity);
        });
    }

    public async Task<PagedResultDto<TransactionResponseDto>> GetTransactionsAsync(TransactionFilterDto filter, Guid? accountId = null, CancellationToken cancellationToken = default)
    {
        var targetId = accountId ?? DbInitializer.DefaultAccountId;
        var query = _context.Transactions.AsNoTracking().Where(t => t.AccountId == targetId);

        if (filter.StartDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= filter.EndDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Type))
        {
            if (Enum.TryParse<TransactionType>(filter.Type, true, out var typeEnum))
            {
                query = query.Where(t => t.Type == typeEnum);
            }
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var page = filter.Page > 0 ? filter.Page : 1;
        var pageSize = filter.PageSize > 0 ? Math.Min(filter.PageSize, 100) : 10;

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TransactionResponseDto
            {
                Id = t.Id,
                AccountId = t.AccountId,
                Type = t.Type == TransactionType.Credit ? "Entrada" : "Saída",
                TypeValue = (int)t.Type,
                Amount = t.Amount,
                Description = t.Description,
                BalanceAfter = t.BalanceAfter,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResultDto<TransactionResponseDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    private static TransactionResponseDto MapToDto(Transaction transaction)
    {
        return new TransactionResponseDto
        {
            Id = transaction.Id,
            AccountId = transaction.AccountId,
            Type = transaction.Type == TransactionType.Credit ? "Entrada" : "Saída",
            TypeValue = (int)transaction.Type,
            Amount = transaction.Amount,
            Description = transaction.Description,
            BalanceAfter = transaction.BalanceAfter,
            CreatedAt = transaction.CreatedAt
        };
    }
}
