namespace Yapplr.Api.DTOs;

public class UserPreferencesDto
{
    public bool DarkMode { get; set; }
}

public class UpdateUserPreferencesDto
{
    public bool? DarkMode { get; set; }
}
