using TrainingAPI.DTOs;

namespace TrainingAPI.Services.Messaging
{

    public interface IConversationCacheService
    {
        Task<List<ConversationDTO>?> GetFirstPageAsync(int viewerId, int top, CancellationToken cancellationToken = default);

        Task SetFirstPageAsync(int viewerId, int top, List<ConversationDTO> data, CancellationToken cancellationToken = default);

        Task InvalidateAsync(int viewerId, CancellationToken cancellationToken = default);
    }
}
