using System.ComponentModel.DataAnnotations;

namespace TrainingAPI.DTOs
{
    public record RegisterDTO
    {
        [Required]
        public string Username { get; init; } = string.Empty;

        [Required]
        public string DisplayName { get; init; } = string.Empty;

        [Required]
        [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        public string Password { get; init; } = string.Empty;

        public int? EmployeeId { get; init; }
    }

    public record LoginDTO
    {
        [Required]
        public string Username { get; init; } = string.Empty;

        [Required]
        public string Password { get; init; } = string.Empty;
    }

    public record AuthResponseDTO(string Token, int UserId, string Username, string DisplayName);
}