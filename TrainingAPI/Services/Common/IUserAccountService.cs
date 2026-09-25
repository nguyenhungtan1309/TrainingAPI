using TrainingAPI.DTOs;

namespace TrainingAPI.Services.Common
{
    public interface IUserAccountService
    {
        Task<(bool Success, string? Error, int UserId)> RegisterAsync(
            RegisterRequestDTO request,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string? Error, string? Token, int UserId, string? Username, string? DisplayName, string? AvatarUrl)> LoginAsync(
             LoginRequestDTO request,
             CancellationToken cancellationToken = default);

        Task<(bool Success, string? Error)> UpdateProfileAsync(
            int currentUserId,
            UpdateProfileRequestDTO request,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string? Error)> ChangePasswordAsync(
            int currentUserId,
            ChangePasswordRequestDTO request,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string? Error)> ToggleBlockUserAsync(
            int currentUserId,
            int blockedUserId,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string? Error)> RegisterDeviceAsync(
            int currentUserId,
            RegisterDeviceRequestDTO request,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string? Error, List<UserSearchDTO> Users)> SearchUsersAsync(
            int currentUserId,
            string keyword,
            CancellationToken cancellationToken = default);
    }
}