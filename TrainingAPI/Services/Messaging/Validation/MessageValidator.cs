using TrainingAPI.DTOs;

namespace TrainingAPI.Services.Messaging.Validation
{
    public abstract class MessageValidator
    {
        public abstract ValidationResult Validate(SendMessageRequestDTO request);
    }

    public class ValidationResult
    {
        public bool IsValid { get; }
        public string ErrorMessage { get; }

        private ValidationResult(bool isValid, string errorMessage)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }

        public static ValidationResult Success() => new(true, string.Empty);
        public static ValidationResult Fail(string message) => new(false, message);
    }
}