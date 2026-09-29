using FinancialAccount.API.DTOs;
using FinancialAccount.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinancialAccount.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TransactionsController : ControllerBase
{
    private readonly IAccountService _accountService;

    public TransactionsController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    /// <summary>
    /// Registra uma entrada de valores (crédito) na conta.
    /// </summary>
    /// <param name="request">Dados do crédito (valor e descrição).</param>
    /// <param name="accountId">ID opcional da conta (caso omitido, utiliza a conta padrão).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Dados da movimentação criada com novo saldo.</returns>
    [HttpPost("deposit")]
    [ProducesResponseType(typeof(TransactionResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TransactionResponseDto>> Deposit(
        [FromBody] CreateTransactionRequest request,
        [FromQuery] Guid? accountId,
        CancellationToken cancellationToken)
    {
        var transaction = await _accountService.DepositAsync(request, accountId, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, transaction);
    }

    /// <summary>
    /// Registra uma saída de valores (débito) na conta, garantindo que o saldo não fique negativo.
    /// </summary>
    /// <param name="request">Dados do débito (valor e descrição).</param>
    /// <param name="accountId">ID opcional da conta (caso omitido, utiliza a conta padrão).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Dados da movimentação criada com novo saldo.</returns>
    [HttpPost("withdraw")]
    [ProducesResponseType(typeof(TransactionResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TransactionResponseDto>> Withdraw(
        [FromBody] CreateTransactionRequest request,
        [FromQuery] Guid? accountId,
        CancellationToken cancellationToken)
    {
        var transaction = await _accountService.WithdrawAsync(request, accountId, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, transaction);
    }
}
