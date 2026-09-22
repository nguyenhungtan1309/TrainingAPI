using System.ComponentModel.DataAnnotations;

namespace TrainingAPI.DTOs
{
    public class ToggleBlockRequestDTO
    {
        public int BlockedUserId { get; set; }
    }

    public class RegisterDeviceRequestDTO
    {
        public string DeviceToken { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
    }

    public class UpdateProfileRequestDTO
    {
        [Required(ErrorMessage = "Tên hiển thị không được để trống")]
        [MaxLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }
    }

    public class ChangePasswordRequestDTO
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu cũ")]
        public string OldPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự")]
        public string NewPassword { get; set; } = string.Empty;
    }
}