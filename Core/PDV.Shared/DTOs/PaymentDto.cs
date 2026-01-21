using PDV.Shared.Enums;

namespace PDV.Shared.DTOs;

public record PaymentDto(
    Guid Id,
    PaymentMethod Method,
    decimal Amount,
    string? AuthorizationCode,
    string? CardBrand,
    DateTime PaymentDate
);
