using PDV.Core.Entities.Base;

namespace PDV.Core.Entities;

public class Operator : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string PinHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public bool IsAdmin { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    private Operator() { }

    public Operator(string name, string code, string pin, bool isAdmin = false)
    {
        SetName(name);
        SetCode(code);
        SetPin(pin);
        IsAdmin = isAdmin;
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        Name = name.Trim();
        SetUpdatedAt();
    }

    public void SetCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code cannot be empty", nameof(code));

        Code = code.Trim().ToUpperInvariant();
        SetUpdatedAt();
    }

    public void SetPin(string pin)
    {
        if (string.IsNullOrWhiteSpace(pin) || pin.Length < 4)
            throw new ArgumentException("PIN must be at least 4 characters", nameof(pin));

        //TODO: Simple hash for PIN (in production, use proper hashing like BCrypt)
        PinHash = HashPin(pin);
        SetUpdatedAt();
    }

    public bool ValidatePin(string pin)
    {
        if (string.IsNullOrWhiteSpace(pin))
            return false;

        return PinHash == HashPin(pin);
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        SetUpdatedAt();
    }

    public void Activate()
    {
        IsActive = true;
        SetUpdatedAt();
    }

    public void Deactivate()
    {
        IsActive = false;
        SetUpdatedAt();
    }

    public void SetAdmin(bool isAdmin)
    {
        IsAdmin = isAdmin;
        SetUpdatedAt();
    }

    private static string HashPin(string pin)
    {
        // Simple hash using SHA256 (in production, use BCrypt or Argon2)
        var bytes = System.Text.Encoding.UTF8.GetBytes(pin);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
