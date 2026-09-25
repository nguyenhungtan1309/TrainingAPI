using System.Collections.Generic;

namespace TrainingAPI.DTOs
{
    public class ThreadDetailDTO
    {
        public long ThreadId { get; set; }
        public List<ParticipantDTO> Participants { get; set; } = new();
        public List<MessageResponseDTO> Messages { get; set; } = new();
    }
}