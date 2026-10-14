using TechPrizeDraw.Models.DTOs;
using TechPrizeDraw.Models.Entities;
using TechPrizeDraw.Models.ViewModels;

namespace TechPrizeDraw.Services.Interfaces;

public interface IOrderProcessingService
{
    Task<OrderSubmissionResult> SubmitOrderAsync(CheckoutViewModel model, string? ipAddress);
    Task<Order?> GetOrderByNumberAsync(string orderNumber);
    Task<bool> IsTransactionIdUsedAsync(string transactionId);
    Task<int> ReleaseExpiredReservationsAsync();
}
