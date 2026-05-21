using Xunit;
using BudgetApp.Api.Modules.Transactions;
using BudgetApp.Api.Modules.Transactions.Models;
using BudgetApp.Api.Modules.Transactions.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BudgetApp.Tests.Transactions;

public class TransactionsControllerTests
{
    private readonly Mock<ITransactionRepository> _repoMock = new();

    private TransactionsController CreateController(string userId = "user1")
    {
        var controller = new TransactionsController(_repoMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.HttpContext.Items["UserId"] = userId;
        return controller;
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtAction_WhenTransactionIsValid()
    {
        string validCategory = Categories.Expense.FirstOrDefault() ?? "Food";
        var request = new TransactionRequest(-15.50m, DateTime.UtcNow, validCategory, "Grocery shopping");

        var result = await CreateController("user1").Create(request);

        Assert.IsType<CreatedAtActionResult>(result);
        
        _repoMock.Verify(r => r.InsertAsync(It.Is<Transaction>(t => 
            t.Amount == -15.50m && 
            t.Category == validCategory && 
            t.UserId == "user1"
        )), Times.Once);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenCategoryIsInvalid()
    {
        var request = new TransactionRequest(-50.00m, DateTime.UtcNow, "Cosmos", "Unreal category");

        var result = await CreateController("user1").Create(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        
        _repoMock.Verify(r => r.InsertAsync(It.IsAny<Transaction>()), Times.Never);
    }

    [Fact]
    public async Task Update_ReturnsNoContent_WhenSuccessful()
    {
        string transactionId = "tx-123";
        string validCategory = Categories.Expense.FirstOrDefault() ?? "Food";
        var request = new TransactionRequest(-20.00m, DateTime.UtcNow, validCategory, "Updated description");

        _repoMock.Setup(r => r.UpdateAsync(transactionId, "user1", request.Amount, request.Date, request.Category, request.Description))
            .ReturnsAsync(true);

        var result = await CreateController("user1").Update(transactionId, request);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenTransactionDoesNotExist()
    {
        string transactionId = "missing-tx";
        string validCategory = Categories.Expense.FirstOrDefault() ?? "Food";
        var request = new TransactionRequest(-20.00m, DateTime.UtcNow, validCategory, "Updated description");

        _repoMock.Setup(r => r.UpdateAsync(transactionId, "user1", request.Amount, request.Date, request.Category, request.Description))
            .ReturnsAsync(false);

        var result = await CreateController("user1").Update(transactionId, request);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent_WhenSuccessful()
    {
        string transactionId = "tx-999";
        _repoMock.Setup(r => r.DeleteAsync(transactionId, "user1")).ReturnsAsync(true);

        var result = await CreateController("user1").Delete(transactionId);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenTransactionDoesNotExist()
    {
        string transactionId = "tx-999";
        _repoMock.Setup(r => r.DeleteAsync(transactionId, "user1")).ReturnsAsync(false);

        var result = await CreateController("user1").Delete(transactionId);

        Assert.IsType<NotFoundResult>(result);
    }
}