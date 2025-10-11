using Microsoft.AspNetCore.Mvc;
using UserService.Application.Interfaces;
using UserService.Application.Models;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace UserService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }


        // GET: api/Auth
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAllUser([FromQuery] int pageNo, [FromQuery] int pageSize)
        {
            var list = await _authService.GetAllAsync(pageNo, pageSize);
            return Ok(list);
        }


        // GET: api/Auth/{id}
        [Authorize(Roles = "Admin")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            var user = await _authService.GetByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        // POST: api/Auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var response = await _authService.Register(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                // Nên sử dụng Custom Exception và HttpStatus code phù hợp hơn
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/Auth/active-email
        [HttpPost("active-email")]
        public async Task<IActionResult> ActiveEmailAccount([FromBody] string otp)
        {
            var (success, message) = await _authService.ActiveEmailAccount(otp);
            if (!success)
                return BadRequest(new { message });
            return Ok(new { message });
        }


        // POST: api/Auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var response = await _authService.Login(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = "Email or password is incorrect." });
            }
        }

        // PUT: api/Auth/update-user-info
        [Authorize]
        [HttpPut("update-user-info")]
        public async Task<IActionResult> UpdateUserNameAndPhoneNumber([FromBody] UpdateUserRequest request)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var message = await _authService.UpdateUserNameAndPhoneNumberAsync(userId, request);
                return Ok(new { message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // PUT: api/Auth/update-user-password
        [Authorize]
        [HttpPut("update-user-password")]
        public async Task<IActionResult> UpdateUserPassword([FromBody] UpdateUserPasswordRequest request)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var message = await _authService.UpdateUserPasswordAsync(userId, request);
                return Ok(new { message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }




        // POST: api/Auth/forget-password
        [HttpPost("forget-password")]
        public async Task<IActionResult> ForgetPassword([FromQuery] string email) // Dùng FromQuery hoặc dùng 1 Model đơn giản
        {
            try
            {
                await _authService.ForgetPassword(email);
                // Luôn trả về 200/202 để tránh bị brute-force check email
                return Accepted(new { message = "If the email exists, a password reset link has been sent." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // POST: api/Auth/reset-password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetRequest request) // Tận dụng LoginRequest để truyền Token và NewPassword
        {
            // **LƯU Ý:** Đảm bảo LoginRequest có 2 field Token và Password/Repassword
            if (string.IsNullOrEmpty(request.Token) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { message = "Token and New Password are required." });
            }

            try
            {
                // Giả định bạn đã chỉnh sửa LoginRequest để chấp nhận Token và NewPassword
                await _authService.ResetPassword(request.Token, request.Password);
                return Ok(new { message = "Password has been reset successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/Auth/google-login
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            try
            {
                var response = await _authService.GoogleLogin(request);
                return Ok(response);
            }
            catch (NotImplementedException)
            {
                return StatusCode(501, new { message = "Google Login is not fully implemented yet." });
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = "Google Login failed: " + ex.Message });
            }
        }
    }
}