using FluentAssertions;
using FinancialAccount.API.Data;
using FinancialAccount.API.DTOs;
using FinancialAccount.API.Exceptions;
using FinancialAccount.API.Models;
using FinancialAccount.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FinancialAccount.Tests;

public class AccountServiceTests
{
    private AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            // O provider InMemory não suporta transações reais. Isso configura
            // o EF Core para ignorar o aviso e tratar BeginTransaction como no-op,
            // permitindo testar a lógica de negócio sem precisar de um banco real.
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options);
    }

    private ILogger<AccountService> CreateMockLogger()
        => new Mock<ILogger<AccountService>>().Object;

    // =========================================================================
    // DEPÓSITO
    // =========================================================================

    [Fact]
    public async Task DepositAsync_WithValidAmount_ShouldIncreaseBalanceAndRecordTransaction()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new AccountService(context, CreateMockLogger());

        var account = new Account
        {
            Id = DbInitializer.DefaultAccountId,
            AccountName = "Conta Teste",
            Balance = 100.00m,
            CreatedAt = DateTime.UtcNow
        };
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var request = new CreateTransactionRequest { Amount = 50.00m, Description = "Depósito de Vendas" };

        // Act
        var result = await service.DepositAsync(request, account.Id);

        // Assert
        result.Should().NotBeNull();
        result.Amount.Should().Be(50.00m);
        result.Type.Should().Be("Entrada");
        result.TypeValue.Should().Be((int)TransactionType.Credit);
        result.BalanceAfter.Should().Be(150.00m);
        result.Description.Should().Be("Depósito de Vendas");

        var updatedAccount = await context.Accounts.FindAsync(account.Id);
        updatedAccount!.Balance.Should().Be(150.00m);

        var transactionInDb = await context.Transactions.FirstOrDefaultAsync(t => t.Id == result.Id);
        transactionInDb.Should().NotBeNull();
        transactionInDb!.Amount.Should().Be(50.00m);
        transactionInDb.Type.Should().Be(TransactionType.Credit);
        transactionInDb.BalanceAfter.Should().Be(150.00m);
    }

    [Fact]
    public async Task DepositAsync_WithoutDescription_ShouldUseDefaultDescription()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new AccountService(context, CreateMockLogger());

        var account = new Account
        {
            Id = DbInitializer.DefaultAccountId,
            AccountName = "Conta Teste",
            Balance = 0.00m,
            CreatedAt = DateTime.UtcNow
        };
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        // Nenhuma descrição fornecida
        var request = new CreateTransactionRequest { Amount = 200.00m, Description = null };

        // Act
        var result = await service.DepositAsync(request, account.Id);

        // Assert
        result.Description.Should().Be("Entrada de valores");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(-0.01)]
    public async Task DepositAsync_WithZeroOrNegativeAmount_ShouldThrowBusinessException(decimal invalidAmount)
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new AccountService(context, CreateMockLogger());

        var request = new CreateTransactionRequest { Amount = invalidAmount, Description = "Depósito Inválido" };

        // Act
        Func<Task> act = async () => await service.DepositAsync(request);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*maior que zero*");
    }

    // =========================================================================
    // SAQUE
    // =========================================================================

    [Fact]
    public async Task WithdrawAsync_WithSufficientBalance_ShouldDecreaseBalanceAndRecordTransaction()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new AccountService(context, CreateMockLogger());

        var account = new Account
        {
            Id = DbInitializer.DefaultAccountId,
            AccountName = "Conta Teste",
            Balance = 300.00m,
            CreatedAt = DateTime.UtcNow
        };
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var request = new CreateTransactionRequest { Amount = 120.00m, Description = "Pagamento de Fornecedor" };

        // Act
        var result = await service.WithdrawAsync(request, account.Id);

        // Assert
        result.Should().NotBeNull();
        result.Amount.Should().Be(120.00m);
        result.Type.Should().Be("Saída");
        result.TypeValue.Should().Be((int)TransactionType.Debit);
        result.BalanceAfter.Should().Be(180.00m);
        result.Description.Should().Be("Pagamento de Fornecedor");

        var updatedAccount = await context.Accounts.FindAsync(account.Id);
        updatedAccount!.Balance.Should().Be(180.00m);

        var transactionInDb = await context.Transactions.FirstOrDefaultAsync(t => t.Id == result.Id);
        transactionInDb.Should().NotBeNull();
        transactionInDb!.Amount.Should().Be(120.00m);
        transactionInDb.Type.Should().Be(TransactionType.Debit);
        transactionInDb.BalanceAfter.Should().Be(180.00m);
    }

    [Fact]
    public async Task WithdrawAsync_WithExactBalance_ShouldLeaveZeroBalance()
    {
        // Arrange — regra de negócio: saque exato do saldo deve resultar em R$ 0,00 (não negativo)
        using var context = CreateInMemoryDbContext();
        var service = new AccountService(context, CreateMockLogger());

        var account = new Account
        {
            Id = DbInitializer.DefaultAccountId,
            AccountName = "Conta Teste",
            Balance = 75.50m,
            CreatedAt = DateTime.UtcNow
        };
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var request = new CreateTransactionRequest { Amount = 75.50m, Description = "Saque Total" };

        // Act
        var result = await service.WithdrawAsync(request, account.Id);

        // Assert
        result.BalanceAfter.Should().Be(0.00m);
        var updatedAccount = await context.Accounts.FindAsync(account.Id);
        updatedAccount!.Balance.Should().Be(0.00m);
    }

    [Fact]
    public async Task WithdrawAsync_WhenAmountExceedsBalance_ShouldThrowInsufficientBalanceException_AndNotModifyBalance()
    {
        // Arrange — garante consistência: saldo negativo jamais deve ser permitido
        using var context = CreateInMemoryDbContext();
        var service = new AccountService(context, CreateMockLogger());

        var account = new Account
        {
            Id = DbInitializer.DefaultAccountId,
            AccountName = "Conta Teste",
            Balance = 50.00m,
            CreatedAt = DateTime.UtcNow
        };
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var request = new CreateTransactionRequest { Amount = 100.00m, Description = "Tentativa de Saque Maior que Saldo" };

        // Act
        Func<Task> act = async () => await service.WithdrawAsync(request, account.Id);

        // Assert
        var exception = await act.Should().ThrowAsync<InsufficientBalanceException>();
        exception.Which.RequestedAmount.Should().Be(100.00m);
        exception.Which.CurrentBalance.Should().Be(50.00m);

        // Saldo deve permanecer intacto e nenhuma transação registrada
        var updatedAccount = await context.Accounts.FindAsync(account.Id);
        updatedAccount!.Balance.Should().Be(50.00m);

        var transactionsCount = await context.Transactions.CountAsync();
        transactionsCount.Should().Be(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public async Task WithdrawAsync_WithZeroOrNegativeAmount_ShouldThrowBusinessException(decimal invalidAmount)
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new AccountService(context, CreateMockLogger());

        var request = new CreateTransactionRequest { Amount = invalidAmount, Description = "Saque Inválido" };

        // Act
        Func<Task> act = async () => await service.WithdrawAsync(request);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*maior que zero*");
    }

    // =========================================================================
    // SALDO
    // =========================================================================

    [Fact]
    public async Task GetBalanceAsync_ShouldCalculateTotalsAndReturnCorrectConsolidatedInfo()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new AccountService(context, CreateMockLogger());

        var account = new Account
        {
            Id = DbInitializer.DefaultAccountId,
            AccountName = "Conta Corporativa",
            Balance = 250.00m,
            CreatedAt = DateTime.UtcNow
        };
        context.Accounts.Add(account);
        context.Transactions.AddRange(
            new Transaction { Id = Guid.NewGuid(), AccountId = account.Id, Type = TransactionType.Credit, Amount = 200m, BalanceAfter = 200m, CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
            new Transaction { Id = Guid.NewGuid(), AccountId = account.Id, Type = TransactionType.Credit, Amount = 100m, BalanceAfter = 300m, CreatedAt = DateTime.UtcNow.AddMinutes(-5) },
            new Transaction { Id = Guid.NewGuid(), AccountId = account.Id, Type = TransactionType.Debit,  Amount = 50m,  BalanceAfter = 250m, CreatedAt = DateTime.UtcNow.AddMinutes(-1) }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetBalanceAsync(account.Id);

        // Assert
        result.Should().NotBeNull();
        result.Balance.Should().Be(250.00m);
        result.TotalCredits.Should().Be(300.00m);
        result.TotalDebits.Should().Be(50.00m);
        result.TotalTransactions.Should().Be(3);
    }

    [Fact]
    public async Task GetBalanceAsync_WithNoTransactions_ShouldReturnZeroTotals()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new AccountService(context, CreateMockLogger());

        var account = new Account
        {
            Id = DbInitializer.DefaultAccountId,
            AccountName = "Conta Vazia",
            Balance = 0.00m,
            CreatedAt = DateTime.UtcNow
        };
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetBalanceAsync(account.Id);

        // Assert
        result.Balance.Should().Be(0.00m);
        result.TotalCredits.Should().Be(0.00m);
        result.TotalDebits.Should().Be(0.00m);
        result.TotalTransactions.Should().Be(0);
    }

    // =========================================================================
    // HISTÓRICO / FILTROS
    // =========================================================================

    [Fact]
    public async Task GetTransactionsAsync_WithFilters_ShouldReturnFilteredAndPagedResults()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new AccountService(context, CreateMockLogger());

        var account = new Account
        {
            Id = DbInitializer.DefaultAccountId,
            AccountName = "Conta Corporativa",
            Balance = 500.00m,
            CreatedAt = DateTime.UtcNow
        };
        context.Accounts.Add(account);

        for (int i = 1; i <= 15; i++)
        {
            context.Transactions.Add(new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                Type = i % 2 == 0 ? TransactionType.Credit : TransactionType.Debit,
                Amount = 10m * i,
                Description = $"Transação #{i}",
                BalanceAfter = 100m + i,
                CreatedAt = DateTime.UtcNow.AddHours(-i)
            });
        }
        await context.SaveChangesAsync();

        var filter = new TransactionFilterDto { Page = 1, PageSize = 5 };

        // Act
        var result = await service.GetTransactionsAsync(filter, account.Id);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(5);
        result.TotalItems.Should().Be(15);
        result.TotalPages.Should().Be(3);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetTransactionsAsync_WithTypeFilter_ShouldReturnOnlyCreditTransactions()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new AccountService(context, CreateMockLogger());

        var account = new Account
        {
            Id = DbInitializer.DefaultAccountId,
            AccountName = "Conta Corporativa",
            Balance = 100m,
            CreatedAt = DateTime.UtcNow
        };
        context.Accounts.Add(account);
        context.Transactions.AddRange(
            new Transaction { Id = Guid.NewGuid(), AccountId = account.Id, Type = TransactionType.Credit, Amount = 50m, BalanceAfter = 50m,  CreatedAt = DateTime.UtcNow.AddHours(-3) },
            new Transaction { Id = Guid.NewGuid(), AccountId = account.Id, Type = TransactionType.Credit, Amount = 80m, BalanceAfter = 130m, CreatedAt = DateTime.UtcNow.AddHours(-2) },
            new Transaction { Id = Guid.NewGuid(), AccountId = account.Id, Type = TransactionType.Debit,  Amount = 30m, BalanceAfter = 100m, CreatedAt = DateTime.UtcNow.AddHours(-1) }
        );
        await context.SaveChangesAsync();

        var filter = new TransactionFilterDto { Type = "Credit", Page = 1, PageSize = 10 };

        // Act
        var result = await service.GetTransactionsAsync(filter, account.Id);

        // Assert
        result.TotalItems.Should().Be(2);
        result.Items.Should().AllSatisfy(t => t.Type.Should().Be("Entrada"));
    }

    [Fact]
    public async Task GetTransactionsAsync_WithDateFilter_ShouldReturnOnlyTransactionsInRange()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new AccountService(context, CreateMockLogger());

        var account = new Account
        {
            Id = DbInitializer.DefaultAccountId,
            AccountName = "Conta Corporativa",
            Balance = 100m,
            CreatedAt = DateTime.UtcNow
        };
        context.Accounts.Add(account);

        var baseDate = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        context.Transactions.AddRange(
            new Transaction { Id = Guid.NewGuid(), AccountId = account.Id, Type = TransactionType.Credit, Amount = 10m, BalanceAfter = 10m,  CreatedAt = baseDate.AddDays(-10) }, // fora do range
            new Transaction { Id = Guid.NewGuid(), AccountId = account.Id, Type = TransactionType.Credit, Amount = 20m, BalanceAfter = 30m,  CreatedAt = baseDate },               // dentro
            new Transaction { Id = Guid.NewGuid(), AccountId = account.Id, Type = TransactionType.Debit,  Amount = 5m,  BalanceAfter = 25m,  CreatedAt = baseDate.AddDays(2) },    // dentro
            new Transaction { Id = Guid.NewGuid(), AccountId = account.Id, Type = TransactionType.Credit, Amount = 15m, BalanceAfter = 40m,  CreatedAt = baseDate.AddDays(10) }   // fora do range
        );
        await context.SaveChangesAsync();

        var filter = new TransactionFilterDto
        {
            StartDate = baseDate.AddDays(-1),
            EndDate = baseDate.AddDays(3),
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await service.GetTransactionsAsync(filter, account.Id);

        // Assert
        result.TotalItems.Should().Be(2);
    }

    // =========================================================================
    // CONTA — GetOrCreateAccount
    // =========================================================================

    [Fact]
    public async Task GetOrCreateAccountAsync_WhenNonExistentNonDefaultId_ShouldThrowNotFoundException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var service = new AccountService(context, CreateMockLogger());

        var randomId = Guid.NewGuid(); // ID que não existe e não é o padrão

        // Act
        Func<Task> act = async () => await service.GetOrCreateAccountAsync(randomId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{randomId}*");
    }

    [Fact]
    public async Task GetOrCreateAccountAsync_WhenDefaultAccountNotExists_ShouldCreateIt()
    {
        // Arrange — banco vazio, sem nenhuma conta
        using var context = CreateInMemoryDbContext();
        var service = new AccountService(context, CreateMockLogger());

        // Act
        var account = await service.GetOrCreateAccountAsync(DbInitializer.DefaultAccountId);

        // Assert
        account.Should().NotBeNull();
        account.Id.Should().Be(DbInitializer.DefaultAccountId);
        account.Balance.Should().Be(0.00m);

        var accountInDb = await context.Accounts.FindAsync(DbInitializer.DefaultAccountId);
        accountInDb.Should().NotBeNull();
    }
}
