using UserService.Application.Interfaces;
using UserService.Application.Models;
using UserService.Domain.Entities;
using UserService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Auth;
using UserService.Application.Configurations;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Common.Paging;


namespace UserService.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenGenerator _tokenGenerator;
        private readonly IEmailService _emailService;
        private readonly GoogleAuthSettings _googleAuthSettings;

        public AuthService(UserDbContext context, IPasswordHasher hasher, ITokenGenerator tokenGenerator, IEmailService emailService, IOptions<GoogleAuthSettings> googleAuthOptions)
        {
            _context = context;
            _passwordHasher = hasher;
            _tokenGenerator = tokenGenerator;
            _emailService = emailService;
            _googleAuthSettings = googleAuthOptions.Value;
        }

        // Get all users with pagination
        public async Task<PagedResult<User>> GetAllAsync(int pageNo, int pageSize)
        {
            var users = await _context.Users.ToListAsync();
            return PagedResult<User>.Create(users, pageNo, pageSize);
        }

        // Get user by ID
        public async Task<User> GetByIdAsync(Guid userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        // This method handles user registration by validating input, checking for existing users, hashing the password, creating a new user, assigning a default role, and returning a JWT token.
        public async Task<AuthResponse> Register(RegisterRequest request)
        {
            if (request.Password != request.Repassword)
                throw new ArgumentException("Passwords do not match.");

            bool userExists = await _context.Users.AnyAsync(u => u.Email == request.Email || u.Username == request.Username);
            if (userExists)
                throw new InvalidOperationException("Email or Username already exists.");

            var passwordHash = _passwordHasher.HashPassword(request.Password);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = request.Username,
                Email = request.Email,
                Phone = request.Phone,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);

            var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "User");
            if (defaultRole == null)
                throw new InvalidOperationException("Default role 'User' not found.");

            _context.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = defaultRole.Id
            });

            await _context.SaveChangesAsync();

            var roles = await GetUserRoles(user.Id);
            return _tokenGenerator.GenerateToken(user, roles);
        }


        // Kích hoạt tài khoản qua email
        public async Task<(bool Success, string Message)> ActiveEmailAccount(string otp)
        {

            // Validate input
            if (string.IsNullOrWhiteSpace(otp) || otp.Length > 10)
                return (false, "Invalid OTP format.");

            // Find OTP record    
            var userOtp = await _context.UserOtps
                .Include(u => u.User)
                .FirstOrDefaultAsync(u => u.OtpCode == otp && u.Type == OtpType.Activation && !u.IsUsed);

            if (userOtp == null)
                return (false, "OTP does not exist.");
            if (userOtp.IsUsed)
                return (false, "OTP has already been used.");
            if (userOtp.Expiry < DateTime.UtcNow)
                return (false, "OTP has expired.");

            // Activate user account
            userOtp.IsUsed = true;
            userOtp.User.IsActive = true;

            await _context.SaveChangesAsync();
            return (true, "Account activated successfully.");
        }

        // This method handles user login by validating credentials and generating a JWT token.
        public async Task<AuthResponse> Login(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                throw new ArgumentException("Email and password are required.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
                throw new UnauthorizedAccessException("Invalid credentials.");

            bool isPasswordValid = _passwordHasher.VerifyPassword(user.PasswordHash, request.Password);
            if (!isPasswordValid)
                throw new UnauthorizedAccessException("Invalid credentials.");

            var roles = await GetUserRoles(user.Id);
            return _tokenGenerator.GenerateToken(user, roles);
        }

        // This method initiates the password reset process by generating a reset token and sending it via email.
        public async Task ForgetPassword(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return;

            var resetToken = Guid.NewGuid().ToString("N");

            var resetEntry = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = resetToken,
                ExpiryDate = DateTime.UtcNow.AddHours(2),
                IsUsed = false
            };

            _context.PasswordResetTokens.Add(resetEntry);
            await _context.SaveChangesAsync();

            await _emailService.SendPasswordResetEmail(user.Email, resetToken);
        }



        // This method resets the user's password using the provided token and new password.
        public async Task ResetPassword(string token, string newPassword)
        {
            var resetEntry = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.Token == token && t.ExpiryDate > DateTime.UtcNow && !t.IsUsed);

            if (resetEntry == null)
                throw new InvalidOperationException("Invalid or expired token.");

            var user = await _context.Users.FindAsync(resetEntry.UserId);
            if (user == null)
                throw new InvalidOperationException("User not found.");

            user.PasswordHash = _passwordHasher.HashPassword(newPassword);
            resetEntry.IsUsed = true;

            _context.Users.Update(user);
            _context.PasswordResetTokens.Update(resetEntry);

            await _context.SaveChangesAsync();
        }

        // Helper method to get roles of a user
        private async Task<List<string>> GetUserRoles(Guid userId)
        {
            return await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.Role.RoleName)
                .ToListAsync();
        }

        // This method handles Google login by validating the Google token, checking/creating the user in the database, and generating a JWT token.
        public async Task<AuthResponse> GoogleLogin(GoogleLoginRequest request)
        {
            // Lấy danh sách Accepted Audiences từ cấu hình
            var acceptedAudiences = _googleAuthSettings.AcceptedAudiences;

            // Xác thực token Google
            GoogleJsonWebSignature.Payload payload;
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = acceptedAudiences
                };

                payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
            }
            catch (Exception ex)
            {
                throw new UnauthorizedAccessException("Invalid Google token: " + ex.Message);
            }

            // Kiểm tra user trong DB
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == payload.Email);

            // Nếu chưa có, tạo user mới
            if (user == null)
            {
                user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = payload.Name ?? payload.Email.Split('@')[0],
                    Email = payload.Email,
                    Phone = null,
                    PasswordHash = null, // Không có mật khẩu
                    CreatedAt = DateTime.UtcNow,
                    IsGoogleUser = true, // Gắn cờ
                    IsActive = false
                };

                // Thêm user mới vào DB
                _context.Users.Add(user);

                // Gán role mặc định
                var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "User");
                if (defaultRole != null)
                {
                    _context.UserRoles.Add(new UserRole
                    {
                        UserId = user.Id,
                        RoleId = defaultRole.Id
                    });
                }

                // Lưu thay đổi vào DB
                await _context.SaveChangesAsync();
            }
            else
            {
                // 4️ Nếu user đã tồn tại
                if (!user.IsGoogleUser)
                {
                    // User trước đó đăng ký bằng mật khẩu
                    // Option 1: buộc liên kết account trước khi login bằng Google
                    // throw new UnauthorizedAccessException("Account exists. Please login with password or link accounts.");

                    // Option 2: cho phép login, đánh dấu account này hỗ trợ Google
                    user.IsGoogleUser = true;
                    await _context.SaveChangesAsync();
                }

                if (!user.IsActive)
                {
                    user.IsActive = true; // kích hoạt account nếu trước đó chưa active
                    await _context.SaveChangesAsync();
                }
            }
            // Sinh token của hệ thống
            var roles = await GetUserRoles(user.Id);
            return _tokenGenerator.GenerateToken(user, roles);
        }

        // Cập nhật username và phone number
        public async Task<string> UpdateUserNameAndPhoneNumberAsync(Guid userId, UpdateUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
                throw new ArgumentException("Username không được để trống.");

            if (request.Phone != null && request.Phone.Length > 20)
                throw new ArgumentException("Phone quá dài.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new Exception("User không tồn tại");

            user.Username = request.Username;
            user.Phone = request.Phone;

            await _context.SaveChangesAsync();

            return "User info updated successfully.";
        }


        public async Task<string> UpdateUserPasswordAsync(Guid userId, UpdateUserPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Password))
                throw new ArgumentException("Password không được để trống.");

            if (request.Password != request.Repassword)
                throw new ArgumentException("Password và Repassword không khớp.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new Exception("User không tồn tại");

            user.PasswordHash = _passwordHasher.HashPassword(request.Password);
            await _context.SaveChangesAsync();

            return "Password updated successfully.";
        }
    }
}
