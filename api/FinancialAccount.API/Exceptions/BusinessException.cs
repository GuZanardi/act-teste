namespace FinancialAccount.API.Exceptions;

public class BusinessException : Exception
{
    public BusinessException(string message) : base(message)
    {
    }
}

public class InsufficientBalanceException : BusinessException
{
    public decimal RequestedAmount { get; }
    public decimal CurrentBalance { get; }

    public InsufficientBalanceException(decimal requestedAmount, decimal currentBalance)
        : base($"Saldo insuficiente para realizar a transação de R$ {requestedAmount:N2}. Saldo atual disponível: R$ {currentBalance:N2}.")
    {
        RequestedAmount = requestedAmount;
        CurrentBalance = currentBalance;
    }
}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}
