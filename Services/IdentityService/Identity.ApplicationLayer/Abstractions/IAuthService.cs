using Identity.ApplicationLayer.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.ApplicationLayer.Abstractions
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request, string? ip = null, CancellationToken ct = default);
        Task<AuthResponse> LoginAsync(LoginRequest request, string? ip = null, CancellationToken ct = default);
        Task<AuthResponse> RefreshAsync(RefreshRequest request, string? ip = null, CancellationToken ct = default);
        Task RevokeAsync(RefreshRevokeRequest request, string? ip = null, CancellationToken ct = default);
        Task<UserDto> GetMeAsync(Guid userId, CancellationToken ct = default);
    }
}
