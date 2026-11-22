using System.ComponentModel.DataAnnotations;

namespace DukeShopBackend.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ và tên!")]
        [MaxLength(120)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập!")]
        [MinLength(3, ErrorMessage = "Tên đăng nhập phải từ 3 ký tự trở lên!")]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [MaxLength(120)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = "STAFF";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<InventoryLog> InventoryLogs { get; set; } = new List<InventoryLog>();
    }

    public class LoginRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập!")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu!")]
        [MinLength(8, ErrorMessage = "Mật khẩu phải có từ 8 ký tự trở lên!")]
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterStaffRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên!")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập!")]
        [MinLength(3, ErrorMessage = "Tên đăng nhập phải từ 3 ký tự trở lên!")]
        public string Username { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Địa chỉ Email không hợp lệ!")]
        public string? Email { get; set; }

        public string? Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu!")]
        [MinLength(8, ErrorMessage = "Mật khẩu phải từ 8 ký tự trở lên!")]
        public string Password { get; set; } = string.Empty;
    }

    public class ForgotPasswordRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập!")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập họ và tên đã đăng ký để xác minh!")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới!")]
        [MinLength(8, ErrorMessage = "Mật khẩu mới phải từ 8 ký tự trở lên!")]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class UpdateProfileRequest
    {
        [Required(ErrorMessage = "Họ và tên không được để trống!")]
        [MaxLength(120)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(120)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;
    }

    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại!")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới!")]
        [MinLength(8, ErrorMessage = "Mật khẩu mới phải từ 8 ký tự trở lên!")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
