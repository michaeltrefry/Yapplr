namespace Yapplr.Api.Services.Payment;

/// <summary>
/// Event arguments for payment configuration changes
/// </summary>
public class PaymentConfigurationChangedEventArgs : EventArgs
{
    public string ProviderName { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty; // "Updated", "Enabled", "Disabled"
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}