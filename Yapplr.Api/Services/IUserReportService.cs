using Yapplr.Api.DTOs;

namespace Yapplr.Api.Services;

public interface IUserReportService
{
    Task<UserReportDto?> CreateReportAsync(int reportedByUserId, CreateUserReportDto dto);
    Task<IEnumerable<UserReportDto>> GetUserReportsAsync(int userId, int page = 1, int pageSize = 25);
    Task<IEnumerable<UserReportDto>> GetAllReportsAsync(int page = 1, int pageSize = 50);
    Task<UserReportDto?> ReviewReportAsync(int reportId, int reviewedByUserId, ReviewUserReportDto dto);
    Task<UserReportDto?> GetReportByIdAsync(int reportId);
    Task<bool> HideContentFromReportAsync(int reportId, int moderatorUserId, string reason);
}
