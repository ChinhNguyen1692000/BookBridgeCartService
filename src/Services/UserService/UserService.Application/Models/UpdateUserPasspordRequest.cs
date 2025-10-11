using System;

namespace UserService.Application.Models
{
    public class UpdateUserPasswordRequest
    {
        public string Password { get; set; }
        public string Repassword { get; set; }
    }
}