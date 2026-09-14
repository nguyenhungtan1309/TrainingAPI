using System.ComponentModel.DataAnnotations;

namespace TrainingAPI.DTOs
{
    public record LoginRequestDTO(
        [Required] string Username,
        [Required] string Password
    );

    public record LoginResponseDTO(
        string Token,
        int UserId,
        string Username,
        string DisplayName,
        string? AvatarUrl
    );

    public record RegisterRequestDTO(
        [Required][MaxLength(50)] string Username,
        [Required] string Password,
        [Required][MaxLength(100)] string DisplayName,
        [MaxLength(1000)] string? AvatarUrl
    );
}