using Identity.ApplicationLayer.Abstractions;
using Identity.BusinessLayer.Options;
using Identity.EntityLayer.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Identity.BusinessLayer.Services
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly JwtOptions _opt;
        public JwtTokenService(IOptions<JwtOptions> opt) => _opt = opt.Value;

        public string CreateAccessToken(AppUser user, IEnumerable<string> roles, IDictionary<string, string>? customClaims = null)
        {
            var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new("display_name", user.DisplayName ?? string.Empty)
        };

            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
            if (customClaims != null)
                claims.AddRange(customClaims.Select(kv => new Claim(kv.Key, kv.Value)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var now = DateTime.UtcNow;

            var token = new JwtSecurityToken(
                issuer: _opt.Issuer,
                audience: _opt.Audience,
                claims: claims,
                notBefore: now,
                expires: now.AddMinutes(_opt.AccessTokenMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public (string token, DateTime expiresAt) CreateRefreshToken(string? ipAddress = null)
        {
            var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            var expiresAt = DateTime.UtcNow.AddDays(_opt.RefreshTokenDays);
            return (token, expiresAt);
        }
    }
}
