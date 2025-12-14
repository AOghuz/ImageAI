using Identity.ApplicationLayer.Abstractions;
using Identity.ApplicationLayer.DTOs;
using Identity.EntityLayer.Entities;
using Identity.PersistenceLayer.Db;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Identity.BusinessLayer.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<AppUser> _users;
        private readonly SignInManager<AppUser> _signin;
        private readonly IJwtTokenService _jwt;
        private readonly IGenericRepository<RefreshToken> _refreshRepo;
        private readonly AppDbContext _db;

        // RoleManager'a artık burada ihtiyacımız yok, sildik.
        public AuthService(
            UserManager<AppUser> users,
            SignInManager<AppUser> signin,
            IJwtTokenService jwt,
            IGenericRepository<RefreshToken> refreshRepo,
            AppDbContext db)
        {
            _users = users; _signin = signin; _jwt = jwt; _refreshRepo = refreshRepo; _db = db;
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

            // GÜNCELLEME: Rol kontrolünü kaldırdık, direkt atıyoruz. Program.cs'te oluşturduk.
            await _users.AddToRoleAsync(user, "user");

            return await IssueTokensAsync(user, ip, ct);
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ip = null, CancellationToken ct = default)
        {
            var user = await _users.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct);
            if (user == null) throw new InvalidOperationException("Kullanıcı bulunamadı.");

            // GÜVENLİK: lockoutOnFailure: true yaptık.
            // Üst üste yanlış girişte hesabı kilitler.
            var pass = await _signin.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

            if (pass.IsLockedOut) throw new InvalidOperationException("Hesabınız çok fazla başarısız deneme nedeniyle kilitlendi. Lütfen daha sonra tekrar deneyin.");
            if (!pass.Succeeded) throw new InvalidOperationException("Geçersiz e-posta veya şifre.");

            return await IssueTokensAsync(user, ip, ct);
        }

        // ... Diğer metodlar (RefreshAsync, RevokeAsync, IssueTokensAsync) aynı kalacak ...
        // (Buraya yer kaplamaması için diğer metodları tekrar kopyalamadım, mevcut kodunuzdaki halleriyle kalabilirler)
        public async Task<AuthResponse> RefreshAsync(RefreshRequest request, string? ip = null, CancellationToken ct = default)
        {
            // Mevcut kodunuzdaki RefreshAsync içeriği aynen kalacak
            // Sadece referans olması için başını ekliyorum:
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
            // Mevcut kodunuzdaki RevokeAsync içeriği aynen kalacak
            var token = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.Token == request.RefreshToken, ct);
            if (token == null || !token.IsActive) return;

            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ip;
            token.ReasonRevoked = "Manual revoke";

            await _refreshRepo.SaveChangesAsync(ct);
        }

        public async Task<UserDto> GetMeAsync(Guid userId, CancellationToken ct = default)
        {
            // Mevcut kodunuzdaki GetMeAsync içeriği aynen kalacak
            var user = await _users.FindByIdAsync(userId.ToString());
            if (user == null) throw new InvalidOperationException("Kullanıcı bulunamadı.");
            return new UserDto(user.Id, user.Email!, user.DisplayName);
        }

        private async Task<AuthResponse> IssueTokensAsync(AppUser user, string? ip, CancellationToken ct, RefreshToken? replacedBy = null)
        {
            // Mevcut kodunuzdaki IssueTokensAsync içeriği aynen kalacak
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