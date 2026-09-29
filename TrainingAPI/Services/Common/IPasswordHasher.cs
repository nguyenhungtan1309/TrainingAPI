namespace TrainingAPI.Services.Common
{

    public interface IPasswordHasher
    {
        string GenerateSalt();

        string Hash(string rawPassword, string salt);

        bool Verify(string rawPassword, string salt, string expectedHash);
    }
}
