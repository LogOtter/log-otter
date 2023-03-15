using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace CustomerApi.Tests;

public class ConsumerStore
{
    public SecurityToken GivenAnExistingConsumer(string audience, params string[] roles)
    {
        var claims = roles.Select(r => new Claim(ClaimTypes.Role, r)).ToList();

        return new JwtSecurityToken(
            "TestGeneratedToken",
            audience,
            claims,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1));
    }
}
