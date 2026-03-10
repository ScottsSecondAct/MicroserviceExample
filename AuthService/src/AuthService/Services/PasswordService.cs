using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace AuthService.Services;

public class PasswordService : IPasswordService
{
  public string HashPassword(string password)
  {
    byte[] salt = new byte[16];
    using (var rng = RandomNumberGenerator.Create())
    {
      rng.GetBytes(salt);
    }

    byte[] hash = KeyDerivation.Pbkdf2(
        password: password,
        salt: salt,
        prf: KeyDerivationPrf.HMACSHA256,
        iterationCount: 10000,
        numBytesRequested: 32);

    // Combine salt and hash
    byte[] hashBytes = new byte[48];
    System.Buffer.BlockCopy(salt, 0, hashBytes, 0, 16);
    System.Buffer.BlockCopy(hash, 0, hashBytes, 16, 32);

    // Convert to Base64
    return System.Convert.ToBase64String(hashBytes);
  }

  public bool VerifyPassword(string password, string storedHash)
  {
    // Extract salt
    byte[] hashBytes = System.Convert.FromBase64String(storedHash);
    byte[] salt = new byte[16];
    System.Buffer.BlockCopy(hashBytes, 0, salt, 0, 16);

    // Hash the input password
    byte[] hash = KeyDerivation.Pbkdf2(
        password: password,
        salt: salt,
        prf: KeyDerivationPrf.HMACSHA256,
        iterationCount: 10000,
        numBytesRequested: 32);

    // Compare the results
    for (int i = 0; i < 32; i++)
    {
      if (hashBytes[i + 16] != hash[i])
        return false;
    }
    return true;
  }
}
