using TrainingAPI.DTOs;

namespace TrainingAPI.Services.Messaging
{
    public interface IChatMessageService
    {
        Task<(bool Success, string? Error, long MessageId)> SendMessageAsync(
            int senderId,
            SendMessageRequestDTO request,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string? Error, List<MessageResponseDTO> Messages)> GetThreadMessagesAsync(
            int viewerId,
            long threadId,
            long? cursorMessageId = null,
            int limit = 20,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string? Error)> RevokeMessageAsync(
            int userId,
            long messageId,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string? Error)> EditMessageAsync(
            int userId,
            EditMessageRequestDTO request,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string? Error)> TogglePinMessageAsync(
            int userId,
            long threadId,
            long messageId,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string? Error)> DeleteMessageForMeAsync(
            int userId,
            long messageId,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string? Error)> ToggleReactionAsync(
            int userId,
            ToggleReactionRequestDTO request,
            CancellationToken cancellationToken = default);
    }
}