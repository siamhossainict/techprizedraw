using TechPrizeDraw.Models.DTOs;

namespace TechPrizeDraw.Services.Interfaces;

public interface IPaymentVerificationService
{
    Task<PaymentApprovalResult> ApprovePaymentAsync(int orderId, string adminUsername, string? adminNote = null);
    Task<PaymentRejectionResult> RejectPaymentAsync(int orderId, string adminUsername, string rejectionReason, string? adminNote = null);
}
