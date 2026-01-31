namespace PDV.Shared.DTOs;

public record OperatorDto(
    Guid Id,
    string Name,
    string Code,
    bool IsActive,
    bool IsAdmin,
    DateTime? LastLoginAt
);

public record OperatorLoginDto(
    Guid Id,
    string Name,
    string Code,
    bool IsAdmin
);
