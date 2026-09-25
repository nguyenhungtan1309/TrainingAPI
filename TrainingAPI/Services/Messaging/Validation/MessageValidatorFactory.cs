namespace TrainingAPI.Services.Messaging.Validation
{
    public static class MessageValidatorFactory
    {
        private static readonly TextMessageValidator _textValidator = new();
        private static readonly ImageMessageValidator _imageValidator = new();
        private static readonly FileMessageValidator _fileValidator = new();
        private static readonly SystemMessageValidator _systemValidator = new();

        public static MessageValidator GetValidator(string messageType)
        {
            return (messageType?.Trim().ToLowerInvariant()) switch
            {
                "image" => _imageValidator,
                "file" => _fileValidator,
                "system" => _systemValidator,
                _ => _textValidator
            };
        }
    }
}