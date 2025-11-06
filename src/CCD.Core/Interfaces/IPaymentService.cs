using CCD.Core.Dtos;

namespace CCD.Core.Interfaces;

public interface IPaymentService
{
    Task<CreatePreferenceResponseDto?> CreatePreferenceAsync(int planId, Guid userId);
    
    Task ProcessPaymentNotificationAsync(long paymentId);
}