using TrainingAPI.DTOs;

namespace TrainingAPI.Services.Messaging
{
    public interface IChatThreadService
    {
        Task<(bool Success, string? Error, long ThreadId)> CreateThreadAsync(
            int creatorId,
            CreateThreadRequestDTO request,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string? Error, List<ConversationDTO> Conversations)> GetConversationsAsync(
            int viewerId,
            int top = 30,
            DateTime? beforeTimeUTC = null,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string? Error, List<ParticipantDTO> Participants)> GetThreadParticipantsAsync(
            int viewerId,
            long threadId,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string? Error, ThreadDetailDTO? Data)> GetThreadDetailsAsync(
            int viewerId,
            long threadId,
            int limit = 20,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string? Error)> ManageParticipantAsync(
            int actionUserId,
            ManageParticipantRequestDTO request,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string? Error)> UpdateParticipantRoleAsync(
            int actionUserId,
            long threadId,
            UpdateRoleRequestDTO request,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string? Error)> UpdateThreadTitleAsync(
            int actionUserId,
            long threadId,
            UpdateTitleRequestDTO request,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string? Error)> ToggleHideThreadAsync(
            int userId,
            long threadId,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string? Error)> MuteThreadAsync(
            int userId,
            long threadId,
            MuteThreadRequestDTO request,
            CancellationToken cancellationToken = default);
    }
}