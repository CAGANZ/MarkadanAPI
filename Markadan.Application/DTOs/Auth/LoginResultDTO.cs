using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Markadan.Application.DTOs.Auth
{
    public record LoginResultDTO(
        string AccessToken,      // JWT
        DateTime ExpiresAtUtc,   // erişim token bitiş
        string RefreshToken,     // opsiyonel ama önerilir
        int UserId,
        string Name,
        string Surname,
        string Email,
        string[] Roles,          // ["Admin","User",...]
        bool IsAdmin             // convenience flag (Roles.Contains("Admin"))
        );
}
