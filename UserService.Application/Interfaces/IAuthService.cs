using UserService.Application.Interfaces;
using UserService.Application.Models;
using System.Threading.Tasks;
using Common.Paging;
using UserService.Domain.Entities;

namespace UserService.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> Register(RegisterRequest request);
        // Email login
        Task<AuthResponse> Login(LoginRequest request);
        Task ForgetPassword(string email);
        Task ResetPassword(string token, string newPassword);

        // Google Login
        Task<AuthResponse> GoogleLogin(GoogleLoginRequest request);

        // Kích hoạt tài khoản qua email
        Task<(bool Success, string Message)> ActiveEmailAccount(string otp);

        // Lấy thông tin người dùng bằng ID
        Task<User> GetByIdAsync(Guid userId);

        // Lấy tất cả người dùng với phân trang
        Task<PagedResult<User>> GetAllAsync(int pageNo, int pageSize);
        
        // Cập nhật username và phone number
        Task<string> UpdateUserNameAndPhoneNumberAsync(Guid userId, UpdateUserRequest request);

        // Cập nhật mật khẩu người dùng
        Task<string> UpdateUserPasswordAsync(Guid userId, UpdateUserPasswordRequest request);

    }
}