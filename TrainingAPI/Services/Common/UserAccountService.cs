using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using TrainingAPI.DTOs;
using TrainingAPI.Models;
using TrainingAPI.Models.Users;

namespace TrainingAPI.Services.Common
{
    public class UserAccountService : IUserAccountService
    {
        private readonly CompanyContext _context;
        private readonly ISqlDataAccess _db;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenService _jwtService;
        private readonly ILogger<UserAccountService> _logger;

        public UserAccountService(
            CompanyContext context,
            ISqlDataAccess db,
            IPasswordHasher passwordHasher,
            IJwtTokenService jwtService,
            ILogger<UserAccountService> logger)
        {
            _context = context;
            _db = db;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task<(bool Success, string? Error, int UserId)> RegisterAsync(
            RegisterRequestDTO request,
            CancellationToken cancellationToken = default)
        {
            var username = request.Username.Trim().ToLowerInvariant();
            var exists = await _context.AppUsers.AnyAsync(u => u.Username.ToLower() == username, cancellationToken);
            if (exists)
            {
                _logger.LogWarning("Đăng ký thất bại: Tên đăng nhập '{Username}' đã tồn tại", username);
                return (false, "Tên đăng nhập đã tồn tại trong hệ thống.", 0);
            }

            var salt = Guid.NewGuid().ToString("N");
            var hash = _passwordHasher.Hash(request.Password, salt);

            var newUser = new AppUser(
                username: username,
                displayName: request.DisplayName.Trim(),
                passwordHash: hash,
                passwordSalt: salt
            );

            _context.AppUsers.Add(newUser);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Người dùng mới {Username} đã đăng ký tài khoản thành công với Id {UserId}", newUser.Username, newUser.Id);
                return (true, null, newUser.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra khi đăng ký tài khoản {Username}", username);
                return (false, "Không thể đăng ký tài khoản. Vui lòng thử lại sau.", 0);
            }
        }

        public async Task<(bool Success, string? Error, string? Token, int UserId, string? Username, string? DisplayName, string? AvatarUrl)> LoginAsync(
            LoginRequestDTO request,
            CancellationToken cancellationToken = default)
        {
            var username = request.Username.Trim().ToLowerInvariant();
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Username.ToLower() == username, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("Đăng nhập thất bại: Tài khoản '{Username}' không tồn tại", username);
                return (false, "Tài khoản hoặc mật khẩu không chính xác.", null, 0, null, null, null);
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Đăng nhập bị từ chối: Tài khoản '{Username}' (Id: {UserId}) đang bị khóa", username, user.Id);
                return (false, "Tài khoản của bạn hiện đang bị khóa.", null, 0, null, null, null);
            }

            var inputHash = _passwordHasher.Hash(request.Password, user.PasswordSalt);
            if (inputHash != user.PasswordHash)
            {
                _logger.LogWarning("Đăng nhập thất bại: Sai mật khẩu cho tài khoản '{Username}'", username);
                return (false, "Tài khoản hoặc mật khẩu không chính xác.", null, 0, null, null, null);
            }

            var token = _jwtService.GenerateAccessToken(user);
            _logger.LogInformation("Người dùng {Username} (Id: {UserId}) đã đăng nhập thành công", user.Username, user.Id);

            return (true, null, token, user.Id, user.Username, user.DisplayName, user.AvatarUrl);
        }

        public async Task<(bool Success, string? Error)> UpdateProfileAsync(
            int currentUserId,
            UpdateProfileRequestDTO request,
            CancellationToken cancellationToken = default)
        {
            var user = await _context.AppUsers.FindAsync(new object[] { currentUserId }, cancellationToken);
            if (user == null || !user.IsActive)
            {
                return (false, "Người dùng không tồn tại hoặc đã bị khóa.");
            }

            user.UpdateProfile(request.DisplayName, request.AvatarUrl);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("User {UserId} đã cập nhật hồ sơ cá nhân thành công", currentUserId);
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật hồ sơ cho User {UserId}", currentUserId);
                return (false, "Không thể cập nhật hồ sơ.");
            }
        }

        public async Task<(bool Success, string? Error)> ChangePasswordAsync(
            int currentUserId,
            ChangePasswordRequestDTO request,
            CancellationToken cancellationToken = default)
        {
            var user = await _context.AppUsers.FindAsync(new object[] { currentUserId }, cancellationToken);
            if (user == null || !user.IsActive)
            {
                return (false, "Người dùng không tồn tại hoặc đã bị khóa.");
            }

            var inputOldHash = _passwordHasher.Hash(request.OldPassword, user.PasswordSalt);
            if (inputOldHash != user.PasswordHash)
            {
                _logger.LogWarning("User {UserId} đổi mật khẩu thất bại: Mật khẩu cũ không khớp", currentUserId);
                return (false, "Mật khẩu hiện tại không đúng.");
            }

            var newSalt = Guid.NewGuid().ToString("N");
            var newHash = _passwordHasher.Hash(request.NewPassword, newSalt);

            user.ChangePassword(newHash, newSalt);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("User {UserId} đã đổi mật khẩu thành công", currentUserId);
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đổi mật khẩu cho User {UserId}", currentUserId);
                return (false, "Không thể đổi mật khẩu.");
            }
        }

        public async Task<(bool Success, string? Error)> ToggleBlockUserAsync(
            int currentUserId,
            int blockedUserId,
            CancellationToken cancellationToken = default)
        {
            if (currentUserId == blockedUserId)
            {
                return (false, "Bạn không thể tự chặn chính mình.");
            }

            try
            {
                await _db.ExecuteNonQueryAsync(
                    "dbo.sp_ToggleBlockUser",
                    parameters =>
                    {
                        parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = currentUserId });
                        parameters.Add(new SqlParameter("@BlockedUserId", SqlDbType.Int) { Value = blockedUserId });
                    },
                    cancellationToken);

                _logger.LogInformation("User {UserId} đã chuyển trạng thái chặn với User {BlockedUserId}", currentUserId, blockedUserId);
                return (true, null);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi khi User {UserId} thao tác chặn User {BlockedUserId}", currentUserId, blockedUserId);
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string? Error)> RegisterDeviceAsync(
            int currentUserId,
            RegisterDeviceRequestDTO request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _db.ExecuteNonQueryAsync(
                    "dbo.sp_RegisterDevice",
                    parameters =>
                    {
                        parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = currentUserId });
                        parameters.Add(new SqlParameter("@DeviceToken", SqlDbType.VarChar, 255) { Value = request.DeviceToken });
                        parameters.Add(new SqlParameter("@Platform", SqlDbType.VarChar, 20) { Value = request.Platform });
                    },
                    cancellationToken);

                return (true, null);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi khi đăng ký thiết bị cho User {UserId}", currentUserId);
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string? Error, List<UserSearchDTO> Users)> SearchUsersAsync(
            int currentUserId,
            string keyword,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await using var result = await _db.ExecuteReaderAsync(
                    "dbo.sp_SearchUsersToChat",
                    parameters =>
                    {
                        parameters.Add(new SqlParameter("@CurrentUserId", SqlDbType.Int) { Value = currentUserId });
                        parameters.Add(new SqlParameter("@Keyword", SqlDbType.NVarChar, 50) { Value = keyword.Trim() });
                    },
                    cancellationToken);

                var list = new List<UserSearchDTO>();
                while (await result.Reader.ReadAsync(cancellationToken))
                {
                    var id = Convert.ToInt32(result.Reader["Id"]);
                    var username = result.Reader["Username"].ToString() ?? string.Empty;
                    var displayName = result.Reader["DisplayName"].ToString() ?? string.Empty;
                    var avatarUrl = result.Reader["AvatarUrl"] as string;

                    list.Add(new UserSearchDTO(id, username, displayName, avatarUrl));
                }

                return (true, null, list);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi khi tìm kiếm người dùng cho User {UserId} với từ khóa {Keyword}", currentUserId, keyword);
                return (false, ex.Message, new List<UserSearchDTO>());
            }
        }
    }
}