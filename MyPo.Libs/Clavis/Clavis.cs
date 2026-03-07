using System.Security.Cryptography;
using System.Text;

namespace MyPo.Libs.Clavis;

/// <summary>
/// Simple library to quickly encrypt and decrypt strings using AES encryption.
/// </summary>
public class Clavis
{
	private readonly byte[] Key;

	public Clavis(string key)
	{
		if (string.IsNullOrEmpty(key))
		{
			throw new ArgumentException("Key cannot be null or empty.", nameof(key));
		}
		Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
	}

	public Clavis(byte[] key)
	{
		if (key == null || key.Length < 32)
		{
			throw new ArgumentException("Key must be at least 32 bytes long.", nameof(key));
		}
		Key = [.. key.Take(32)];
	}

	private const int IV_LENGTH = 12;
	private const int TAG_LENGTH = 16;

	/// <summary>
	/// Encrypts the given plaintext byte array and returns the ciphertext as a byte array.
	/// </summary>
	/// <param name="plainTextBytes"></param>
	/// <returns></returns>
	public byte[] Encrypt(byte[] plainTextBytes)
	{
		var nonce = new byte[IV_LENGTH]; // IV
		var tag = new byte[TAG_LENGTH];
		var ciphertext = new byte[plainTextBytes.Length];

		// Generate random nonce (IV)
		using (var rng = RandomNumberGenerator.Create())
		{
			rng.GetBytes(nonce);
		}

		using (AesGcm aesGcm = new AesGcm(Key, tag.Length))
		{
			aesGcm.Encrypt(nonce, plainTextBytes, ciphertext, tag);
		}

		// Combine: [nonce/IV | tag | ciphertext]
		var combined = new byte[nonce.Length + tag.Length + ciphertext.Length];
		Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
		Buffer.BlockCopy(tag, 0, combined, nonce.Length, tag.Length);
		Buffer.BlockCopy(ciphertext, 0, combined, nonce.Length + tag.Length, ciphertext.Length);

		return combined;
	}

	/// <summary>
	/// Encrypts the given plaintext byte array and returns the ciphertext as a Base64-encoded string.
	/// </summary>
	/// <param name="plainTextBytes"></param>
	/// <returns></returns>
	public string EncryptAsBase64(byte[] plainTextBytes)
	{
		var encryptedBytes = Encrypt(plainTextBytes);
		return Convert.ToBase64String(encryptedBytes);
	}

	/// <summary>
	/// Encrypts the given plaintext string and returns the ciphertext as a byte array.
	/// </summary>
	/// <param name="plainText"></param>
	/// <returns></returns>
	public byte[] Encrypt(string plainText)
	{
		var plaintextBytes = Encoding.UTF8.GetBytes(plainText);
		return Encrypt(plaintextBytes);
	}

	/// <summary>
	/// Encrypts the given plaintext string and returns the ciphertext as a Base64-encoded string.
	/// </summary>
	/// <param name="plainText"></param>
	/// <returns></returns>
	public string EncryptAsBase64(string plainText)
	{
		var encryptedBytes = Encrypt(plainText);
		return Convert.ToBase64String(encryptedBytes);
	}

	/// <summary>
	/// Decrypts the given ciphertext byte array and returns the original bytes.
	/// </summary>
	/// <param name="cipherTextBytes"></param>
	/// <returns></returns>
	public byte[] Decrypt(byte[] cipherTextBytes)
	{
		if (cipherTextBytes == null || cipherTextBytes.Length < IV_LENGTH+TAG_LENGTH)
		{
			throw new ArgumentException("Ciphertext is too short to be valid.", nameof(cipherTextBytes));
		}

		var nonce = cipherTextBytes.Take(IV_LENGTH).ToArray();
		var tag = cipherTextBytes.Skip(IV_LENGTH).Take(TAG_LENGTH).ToArray();
		var encryptedBytes = cipherTextBytes.Skip(IV_LENGTH+TAG_LENGTH).ToArray();

		var decryptedBytes = new byte[encryptedBytes.Length];

		using (AesGcm aesGcm = new AesGcm(Key, tag.Length))
		{
			aesGcm.Decrypt(nonce, encryptedBytes, tag, decryptedBytes);
		}

		return decryptedBytes;
	}

	/// <summary>
	/// Decrypts the given ciphertext byte array and returns the original plaintext string.
	/// </summary>
	/// <param name="cipherTextBytes"></param>
	/// <returns></returns>
	public string DecryptToString(byte[] cipherTextBytes)
	{
		var decryptedBytes = Decrypt(cipherTextBytes);
		return Encoding.UTF8.GetString(decryptedBytes);
	}

	/// <summary>
	/// Decrypts the given Base64-encoded ciphertext string and returns the original bytes.
	/// </summary>
	/// <param name="base64CipherText"></param>
	/// <returns></returns>
	public byte[] DecryptFromBase64(string base64CipherText)
	{
		var ciphertextBytes = Convert.FromBase64String(base64CipherText);
		return Decrypt(ciphertextBytes);
	}

	/// <summary>
	/// Decrypts the given Base64-encoded ciphertext string and returns the original plaintext string.
	/// </summary>
	/// <param name="base64CipherText"></param>
	/// <returns></returns>
	public string DecryptToStringFromBase64(string base64CipherText)
	{
		var decryptedBytes = DecryptFromBase64(base64CipherText);
		return Encoding.UTF8.GetString(decryptedBytes);
	}
}
