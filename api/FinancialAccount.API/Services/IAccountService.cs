using FinancialAccount.API.DTOs;
using FinancialAccount.API.Models;

namespace FinancialAccount.API.Services;

public interface IAccountService
{
    Task<BalanceResponseDto> GetBalanceAsync(Guid? accountId = null, CancellationToken cancellationToken = default);
    Task<TransactionResponseDto> DepositAsync(CreateTransactionRequest request, Guid? accountId = null, CancellationToken cancellationToken = default);
    Task<TransactionResponseDto> WithdrawAsync(CreateTransactionRequest request, Guid? accountId = null, CancellationToken cancellationToken = default);
    Task<PagedResultDto<TransactionResponseDto>> GetTransactionsAsync(TransactionFilterDto filter, Guid? accountId = null, CancellationToken cancellationToken = default);
    Task<Account> GetOrCreateAccountAsync(Guid? accountId = null, CancellationToken cancellationToken = default);
}
