using SchoolMS.Core.DTOs;
using SchoolMS.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolMS.Core.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto dto);
        string GenerateToken(User user);
    }
}
