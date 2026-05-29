using Microsoft.AspNetCore.Mvc;
using SchoolMS.Core.DTOs;
using SchoolMS.Core.Interfaces;

namespace SchoolMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDto dto)
        {
            var result = await _authService.LoginAsync(dto);
            if (result == null)
                return Unauthorized(new { success = false, message = "Invalid email or password" });

            return Ok(new { success = true, data = result });
        }

        [HttpGet("hash/{password}")]
        public IActionResult GetHash(string password)
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(password);
            return Ok(new { hash });
        }
    }
}