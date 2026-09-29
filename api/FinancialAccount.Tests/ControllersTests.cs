using FluentAssertions;
using FinancialAccount.API.Controllers;
using FinancialAccount.API.DTOs;
using FinancialAccount.API.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace FinancialAccount.Tests;

public class ControllersTests
{
    [Fact]
    public async Task AccountController_GetBalance_ReturnsOkWithBalanceResponse()
    {
        // Arrange
        var mockService = new Mock<IAccountService>();
        var expectedBalance = new BalanceResponseDto
        {
            AccountId = Guid.NewGuid(),
            AccountName = "Conta Teste",
            Balance = 1500.00m,
            TotalCredits = 2000.00m,
            TotalDebits = 500.00m,
            TotalTransactions = 4
        };

        mockService.Setup(s => s.GetBalanceAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedBalance);

        var controller = new AccountController(mockService.Object);

        // Act
        var actionResult = await controller.GetBalance(null, CancellationToken.None);

        // Assert
        actionResult.Result.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedBalance);
    }

    [Fact]
    public async Task AccountController_GetTransactions_ReturnsOkWithPagedResult()
    {
        // Arrange
        var mockService = new Mock<IAccountService>();
        var expectedPagedResult = new PagedResultDto<TransactionResponseDto>
        {
            Items = new List<TransactionResponseDto>
            {
                new() { Id = Guid.NewGuid(), Amount = 100m, Type = "Entrada", Description = "Pix recebido" }
            },
            Page = 1,
            PageSize = 10,
            TotalItems = 1
        };

        mockService.Setup(s => s.GetTransactionsAsync(It.IsAny<TransactionFilterDto>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPagedResult);

        var controller = new AccountController(mockService.Object);
        var filter = new TransactionFilterDto();

        // Act
        var actionResult = await controller.GetTransactions(filter, null, CancellationToken.None);

        // Assert
        actionResult.Result.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedPagedResult);
    }

    [Fact]
    public async Task TransactionsController_Deposit_ReturnsCreated()
    {
        // Arrange
        var mockService = new Mock<IAccountService>();
        var request = new CreateTransactionRequest
        {
            Amount = 350.00m,
            Description = "Aporte Inicial"
        };
        var expectedResponse = new TransactionResponseDto
        {
            Id = Guid.NewGuid(),
            Amount = 350.00m,
            Type = "Entrada",
            BalanceAfter = 350.00m,
            Description = "Aporte Inicial",
            CreatedAt = DateTime.UtcNow
        };

        mockService.Setup(s => s.DepositAsync(request, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var controller = new TransactionsController(mockService.Object);

        // Act
        var actionResult = await controller.Deposit(request, null, CancellationToken.None);

        // Assert
        actionResult.Result.Should().BeOfType<ObjectResult>();
        var objectResult = actionResult.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(201);
        objectResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task TransactionsController_Withdraw_ReturnsCreated()
    {
        // Arrange
        var mockService = new Mock<IAccountService>();
        var request = new CreateTransactionRequest
        {
            Amount = 50.00m,
            Description = "Taxa bancária"
        };
        var expectedResponse = new TransactionResponseDto
        {
            Id = Guid.NewGuid(),
            Amount = 50.00m,
            Type = "Saída",
            BalanceAfter = 300.00m,
            Description = "Taxa bancária",
            CreatedAt = DateTime.UtcNow
        };

        mockService.Setup(s => s.WithdrawAsync(request, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var controller = new TransactionsController(mockService.Object);

        // Act
        var actionResult = await controller.Withdraw(request, null, CancellationToken.None);

        // Assert
        actionResult.Result.Should().BeOfType<ObjectResult>();
        var objectResult = actionResult.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(201);
        objectResult.Value.Should().BeEquivalentTo(expectedResponse);
    }
}
