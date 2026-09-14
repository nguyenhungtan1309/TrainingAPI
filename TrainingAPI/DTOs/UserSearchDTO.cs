namespace TrainingAPI.DTOs
{
    public record UserSearchDTO(
        int Id,
        string Username,
        string DisplayName,
        string? AvatarUrl
    );
}