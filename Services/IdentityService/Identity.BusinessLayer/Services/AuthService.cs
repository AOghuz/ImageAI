using Identity.ApplicationLayer.Abstractions;
using Identity.ApplicationLayer.DTOs;
using Identity.EntityLayer.Entities;
using Identity.PersistenceLayer.Db;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.BusinessLayer.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<AppUser> _users;
        private readonly SignInManager<AppUser> _signin;
        private readonly RoleManager<AppRole> _roles;
        private readonly IJwtTokenService _jwt;
        private readonly IGenericRepository<RefreshToken> _refreshRepo;
        private readonly AppDbContext _db;

        public AuthService(
            UserManager<AppUser> users,
            SignInManager<AppUser> signin,
            RoleManager<AppRole> roles,
            IJwtTokenService jwt,
            IGenericRepository<RefreshToken> refreshRepo,
            AppDbContext db)
        {
            _users = users; _signin = signin; _roles = roles; _jwt = jwt; _refreshRepo = refreshRepo; _db = db;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request, string? ip = null, CancellationToken ct = default)
        {
            var exists = await _users.FindByEmailAsync(request.Email);
            if (exists != null) throw new InvalidOperationException("Email zaten kayıtlı.");

            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                UserName = request.Email,
                DisplayName = request.DisplayName
            };

            var result = await _users.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

            if (!await _roles.RoleExistsAsync("user"))
                await _roles.CreateAsync(new AppRole { Name = "user" });
            await _users.AddToRoleAsync(user, "user");

            return await IssueTokensAsync(user, ip, ct);
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ip = null, CancellationToken ct = default)
        {
            var user = await _users.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct);
            if (user == null) throw new InvalidOperationException("Kullanıcı bulunamadı.");

            var pass = await _signin.CheckPasswordSignInAsync(user, request.Password, false);
            if (!pass.Succeeded) throw new InvalidOperationException("Geçersiz giriş.");

            return await IssueTokensAsync(user, ip, ct);
        }

        public async Task<AuthResponse> RefreshAsync(RefreshRequest request, string? ip = null, CancellationToken ct = default)
        {
            var token = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.Token == request.RefreshToken, ct);
            if (token == null || !token.IsActive) throw new InvalidOperationException("Geçersiz refresh token.");

            var user = await _users.FindByIdAsync(token.UserId.ToString());
            if (user == null) throw new InvalidOperationException("Kullanıcı bulunamadı.");

            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ip;
            var response = await IssueTokensAsync(user, ip, ct, replacedBy: token);

            await _refreshRepo.SaveChangesAsync(ct);
            return response;
        }

        public async Task RevokeAsync(RefreshRevokeRequest request, string? ip = null, CancellationToken ct = default)
        {
            var token = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.Token == request.RefreshToken, ct);
            if (token == null || !token.IsActive) return;

            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ip;
            token.ReasonRevoked = "Manual revoke";

            await _refreshRepo.SaveChangesAsync(ct);
        }

        public async Task<UserDto> GetMeAsync(Guid userId, CancellationToken ct = default)
        {
            var user = await _users.FindByIdAsync(userId.ToString());
            if (user == null) throw new InvalidOperationException("Kullanıcı bulunamadı.");
            return new UserDto(user.Id, user.Email!, user.DisplayName);
        }

        private async Task<AuthResponse> IssueTokensAsync(AppUser user, string? ip, CancellationToken ct, RefreshToken? replacedBy = null)
        {
            var roles = await _users.GetRolesAsync(user);
            var access = _jwt.CreateAccessToken(user, roles);

            var (refreshToken, expiresAt) = _jwt.CreateRefreshToken(ip);
            var rt = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = expiresAt,
                CreatedByIp = ip
            };

            if (replacedBy != null)
                replacedBy.ReplacedBy = rt.Token;

            await _refreshRepo.AddAsync(rt, ct);
            await _refreshRepo.SaveChangesAsync(ct);

            return new AuthResponse(
                access,
                DateTime.UtcNow.AddMinutes(60),
                refreshToken,
                expiresAt,
                new UserDto(user.Id, user.Email!, user.DisplayName)
            );
        }
    }
}
