using FinancialAccount.API.DTOs;
using FinancialAccount.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinancialAccount.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    /// <summary>
    /// Consulta o saldo disponível e informações consolidadas da conta empresarial.
    /// </summary>
    /// <param name="accountId">ID opcional da conta (caso não informado, usa a conta padrão).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Dados do saldo atual e resumo financeiro.</returns>
    [HttpGet("balance")]
    [ProducesResponseType(typeof(BalanceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BalanceResponseDto>> GetBalance([FromQuery] Guid? accountId, CancellationToken cancellationToken)
    {
        var balance = await _accountService.GetBalanceAsync(accountId, cancellationToken);
        return Ok(balance);
    }

    /// <summary>
    /// Consulta o histórico de movimentações da conta empresarial com filtros e paginação.
    /// </summary>
    /// <param name="filter">Filtros de data, tipo de movimentação e paginação.</param>
    /// <param name="accountId">ID opcional da conta.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista paginada de movimentações registradas.</returns>
    [HttpGet("transactions")]
    [ProducesResponseType(typeof(PagedResultDto<TransactionResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<TransactionResponseDto>>> GetTransactions(
        [FromQuery] TransactionFilterDto filter,
        [FromQuery] Guid? accountId,
        CancellationToken cancellationToken)
    {
        var transactions = await _accountService.GetTransactionsAsync(filter, accountId, cancellationToken);
        return Ok(transactions);
    }
}
