namespace Yapplr.Api.DTOs;

public class SystemConfigurationDto
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsVisible { get; set; }
    public bool IsEditable { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateSystemConfigurationDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public bool IsVisible { get; set; } = true;
    public bool IsEditable { get; set; } = true;
}

public class UpdateSystemConfigurationDto
{
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsVisible { get; set; }
    public bool IsEditable { get; set; }
}

public class SystemConfigurationBulkUpdateDto
{
    public Dictionary<string, string> Configurations { get; set; } = new();
}

public class ToggleSubscriptionSystemDto
{
    public bool Enabled { get; set; }
}
