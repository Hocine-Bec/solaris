using System.Security.Cryptography;
using System.Text;

namespace Domain.Entities;

/// <summary>
/// Reusable address entity for customers and installations
/// </summary>
public class Address
{
    public int Id { get; set; }
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    
    /* --------- natural key to check ---------- */
    public string UniqueAddressHash { get; set; } = string.Empty;


    /// <summary>
    /// Generates a unique SHA-256 hash for the specified address components.
    /// </summary>
    /// <param name="street">The street address.</param>
    /// <param name="city">The city name.</param>
    /// <param name="state">The state or region.</param>
    /// <param name="zipCode">The postal or ZIP code.</param>
    /// <param name="country">The country name.</param>
    /// <returns>A hexadecimal string representing the hash of the normalized address.</returns>
    public static string BuildHash(string street, string city, string state, string zipCode, string country)
    {
        var normalized = $"{street}|{city}|{state}|{zipCode}|{country}".ToUpperInvariant();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }
}