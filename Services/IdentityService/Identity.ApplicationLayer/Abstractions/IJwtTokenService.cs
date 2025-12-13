using Identity.EntityLayer.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.ApplicationLayer.Abstractions
{
    public interface IJwtTokenService
    {
        string CreateAccessToken(AppUser user, IEnumerable<string> roles, IDictionary<string, string>? customClaims = null);
        (string token, DateTime expiresAt) CreateRefreshToken(string? ipAddress = null);
    }
}
