using HealthyApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace HealthyApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly DataContext _context;

        public UserController(DataContext context)
        {
            _context = context;
        }

        // ====================== 登录接口 ======================
        // POST: api/user/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 根据用户名查找用户
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == loginRequest.Username);
            if (user == null)
                return Unauthorized("用户名或密码不正确");

            // 验证密码哈希
            bool isPasswordValid = VerifyPasswordHash(loginRequest.Password, user.PasswordHash);
            if (!isPasswordValid)
                return Unauthorized("用户名或密码不正确");

            // ✅ 登录成功：返回用户数据（格式完全按前端文档）
            return Ok(new
            {
                user_id = user.UserId,
                username = user.Username,
                age = user.Age,
                gender = user.Gender,
                height = user.Height,
                initial_weight = user.InitialWeight,
                target_weight = user.TargetWeight
            });
        }

        // 验证密码哈希
        private bool VerifyPasswordHash(string password, string storedHash)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                byte[] passwordHash = sha256.ComputeHash(passwordBytes);
                string computedHash = BitConverter.ToString(passwordHash).Replace("-", "").ToLower();
                return computedHash == storedHash;
            }
        }

        // ====================== 个人信息修改接口 ======================
        // POST: api/user/update
        [HttpPost("update")]
        public async Task<IActionResult> UpdateUser([FromBody] User user)
        {
            // 查找要修改的用户
            var existingUser = await _context.Users.FindAsync(user.UserId);
            if (existingUser == null)
                return NotFound("用户不存在");

            // 更新字段（前端每次发送完整信息）
            existingUser.Username = user.Username;
            existingUser.Age = user.Age;
            existingUser.Gender = user.Gender;
            existingUser.Height = user.Height;
            existingUser.InitialWeight = user.InitialWeight;
            existingUser.TargetWeight = user.TargetWeight;

            await _context.SaveChangesAsync();

            // ✅ 按前端文档：无需返回内容，只需 HTTP200
            return Ok();
        }

        // ====================== 注册接口（保留原逻辑） ======================
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest registerRequest)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (await _context.Users.AnyAsync(u => u.Username == registerRequest.Username))
                return Conflict("该用户名已存在，请选择其他用户名");

            string passwordHash = HashPassword(registerRequest.Password);

            var user = new User
            {
                Username = registerRequest.Username,
                PasswordHash = passwordHash,
                Age = registerRequest.Age,
                Gender = registerRequest.Gender,
                Height = registerRequest.Height,
                InitialWeight = registerRequest.InitialWeight,
                TargetWeight = registerRequest.TargetWeight,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "注册成功",
                user_id = user.UserId,
                username = user.Username
            });
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                byte[] hashBytes = sha256.ComputeHash(passwordBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
    }

    // ====================== 请求模型 ====================== //
    public class LoginRequest
    {
        [Required(ErrorMessage = "用户名不能为空")]
        public string Username { get; set; }

        [Required(ErrorMessage = "密码不能为空")]
        public string Password { get; set; }
    }

    public class RegisterRequest
    {
        [Required(ErrorMessage = "用户名不能为空")]
        public string Username { get; set; }

        [Required(ErrorMessage = "密码不能为空")]
        [MinLength(6, ErrorMessage = "密码至少6位")]
        public string Password { get; set; }

        public int? Age { get; set; }
        public string Gender { get; set; }
        public double? Height { get; set; }
        public double? InitialWeight { get; set; }
        public double? TargetWeight { get; set; }
    }
}
