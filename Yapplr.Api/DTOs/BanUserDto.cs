namespace Yapplr.Api.DTOs;

public class BanUserDto
{
    public string Reason { get; set; } = string.Empty;
    public bool IsShadowBan { get; set; } = false;
}
