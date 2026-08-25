using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CashFlow.IntegrationTests;

public static class TestTokens
{
    public const string Issuer = "https://identity.cashflow.test";
    public const string Audience = "cashflow-api";

    public static SymmetricSecurityKey TrustedKey { get; } = new(RandomNumberGenerator.GetBytes(32));

    private static SymmetricSecurityKey ForeignKey { get; } = new(RandomNumberGenerator.GetBytes(32));

    public static string Valid() => Mint(TrustedKey, DateTime.UtcNow.AddMinutes(10));

    public static string Expired() => Mint(TrustedKey, DateTime.UtcNow.AddMinutes(-10));

    public static string ForeignlySigned() => Mint(ForeignKey, DateTime.UtcNow.AddMinutes(10));

    private static string Mint(SymmetricSecurityKey key, DateTime expires) =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            NotBefore = expires.AddMinutes(-20),
            Expires = expires,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        });
}
