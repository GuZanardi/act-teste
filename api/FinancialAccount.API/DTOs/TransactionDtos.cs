using System.ComponentModel.DataAnnotations;

namespace FinancialAccount.API.DTOs;

public class CreateTransactionRequest
{
    [Required(ErrorMessage = "O valor da transação é obrigatório.")]
    [Range(0.01, 100_000_000, ErrorMessage = "O valor da transação deve ser maior que zero (mínimo R$ 0,01).")]
    public decimal Amount { get; set; }

    [MaxLength(255, ErrorMessage = "A descrição não pode exceder 255 caracteres.")]
    public string? Description { get; set; }
}

public class TransactionResponseDto
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string Type { get; set; } = string.Empty;
    public int TypeValue { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal BalanceAfter { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BalanceResponseDto
{
    public Guid AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public decimal TotalCredits { get; set; }
    public decimal TotalDebits { get; set; }
    public int TotalTransactions { get; set; }
    public DateTime? LastUpdated { get; set; }
}

public class TransactionFilterDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Type { get; set; } // "Credit", "Debit" or null for all
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class PagedResultDto<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / (PageSize > 0 ? PageSize : 10));
}

public class ErrorResponseDto
{
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
