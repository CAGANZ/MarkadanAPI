using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Markadan.Application.DTOs.Auth
{
    public record LoginResultDTO(
        string AccessToken,
        DateTime ExpiresAtUtc,
        string RefreshToken,
        int UserId,
        string Name,
        string Surname,
        string Email,
        string[] Roles,
        bool IsAdmin
        );
}
