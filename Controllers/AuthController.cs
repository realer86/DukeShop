using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DukeShopBackend.Data;
using DukeShopBackend.Models;
using DukeShopBackend.Services;

namespace DukeShopBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly TokenService _tokenService;

        public AuthController(AppDbContext context, TokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Vui lòng nhập tên đăng nhập và mật khẩu từ 8 ký tự trở lên!" });
            }

            var cleanUsername = request.Username.Trim().ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == cleanUsername);

            if (user == null || !PasswordSecurity.VerifyPassword(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Sai tên đăng nhập hoặc mật khẩu!" });
            }

            string token = _tokenService.GenerateToken(user);

            return Ok(new
            {
                token,
                user = new
                {
                    id = user.Id,
                    fullName = user.FullName,
                    username = user.Username,
                    email = user.Email,
                    phone = user.Phone,
                    role = user.Role,
                    createdAt = user.CreatedAt
                }
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterStaffRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Thông tin đăng ký chưa hợp lệ (Mật khẩu phải từ 8 ký tự trở lên)!" });
            }

            var cleanUsername = request.Username.Trim().ToLower();
            if (cleanUsername == "quanly" || cleanUsername == "admin")
            {
                return Conflict(new { message = "Không thể đăng ký trùng tên tài khoản Quản lý!" });
            }

            var exists = await _context.Users.AnyAsync(u => u.Username.ToLower() == cleanUsername);
            if (exists)
            {
                return Conflict(new { message = "Tên đăng nhập này đã tồn tại trên hệ thống!" });
            }

            var newUser = new User
            {
                FullName = request.FullName.Trim(),
                Username = cleanUsername,
                Email = request.Email?.Trim() ?? string.Empty,
                Phone = request.Phone?.Trim() ?? string.Empty,
                PasswordHash = PasswordSecurity.HashPassword(request.Password),
                Role = "STAFF",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            string token = _tokenService.GenerateToken(newUser);

            return Ok(new
            {
                token,
                user = new
                {
                    id = newUser.Id,
                    fullName = newUser.FullName,
                    username = newUser.Username,
                    email = newUser.Email,
                    phone = newUser.Phone,
                    role = newUser.Role,
                    createdAt = newUser.CreatedAt
                }
            });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            _tokenService.RevokeRequestToken(Request);
            return Ok(new { message = "Đã đăng xuất và hủy phiên làm việc an toàn!" });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Vui lòng nhập đầy đủ thông tin và mật khẩu mới từ 8 ký tự trở lên!" });
            }

            var cleanUsername = request.Username.Trim().ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == cleanUsername);

            if (user == null)
            {
                return NotFound(new { message = "Không tìm thấy tài khoản với tên đăng nhập này!" });
            }

            if (user.Role == "ADMIN")
            {
                return StatusCode(403, new { message = "Tài khoản Quản lý được bảo mật cố định, không thể đặt lại mật khẩu công khai!" });
            }

            if (!string.Equals(user.FullName.Trim(), request.FullName.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Họ và tên xác minh không khớp với hồ sơ đăng ký!" });
            }

            user.PasswordHash = PasswordSecurity.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đặt lại mật khẩu thành công! Bạn có thể đăng nhập bằng mật khẩu mới." });
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var payload = _tokenService.ValidateRequest(Request);
            if (payload == null)
            {
                return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ hoặc đã hết hạn!" });
            }

            var user = await _context.Users.FindAsync(payload.UserId);
            if (user == null) return NotFound(new { message = "Không tìm thấy tài khoản!" });

            return Ok(new
            {
                id = user.Id,
                fullName = user.FullName,
                username = user.Username,
                email = user.Email,
                phone = user.Phone,
                role = user.Role,
                createdAt = user.CreatedAt
            });
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var payload = _tokenService.ValidateRequest(Request);
            if (payload == null)
            {
                return Unauthorized(new { message = "Vui lòng đăng nhập lại để cập nhật hồ sơ!" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Thông tin hồ sơ không hợp lệ!" });
            }

            var user = await _context.Users.FindAsync(payload.UserId);
            if (user == null) return NotFound(new { message = "Không tìm thấy tài khoản!" });

            user.FullName = request.FullName.Trim();
            user.Email = request.Email?.Trim() ?? string.Empty;
            user.Phone = request.Phone?.Trim() ?? string.Empty;

            await _context.SaveChangesAsync();

            string refreshedToken = _tokenService.GenerateToken(user);

            return Ok(new
            {
                message = "Cập nhật thông tin cá nhân thành công!",
                token = refreshedToken,
                user = new
                {
                    id = user.Id,
                    fullName = user.FullName,
                    username = user.Username,
                    email = user.Email,
                    phone = user.Phone,
                    role = user.Role,
                    createdAt = user.CreatedAt
                }
            });
        }

        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var payload = _tokenService.ValidateRequest(Request);
            if (payload == null)
            {
                return Unauthorized(new { message = "Vui lòng đăng nhập lại để đổi mật khẩu!" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Mật khẩu mới phải từ 8 ký tự trở lên!" });
            }

            var user = await _context.Users.FindAsync(payload.UserId);
            if (user == null) return NotFound(new { message = "Không tìm thấy tài khoản!" });

            if (!PasswordSecurity.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                return BadRequest(new { message = "Mật khẩu hiện tại không chính xác!" });
            }

            user.PasswordHash = PasswordSecurity.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đổi mật khẩu thành công!" });
        }
    }
}
