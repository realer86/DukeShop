using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DukeShopBackend.Data;
using DukeShopBackend.Models;
using DukeShopBackend.Services;

namespace DukeShopBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly TokenService _tokenService;

        public CategoriesController(AppDbContext context, TokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var auth = _tokenService.ValidateRequest(Request);
            if (auth == null)
                return Unauthorized(new { message = "Vui lòng đăng nhập để truy cập danh mục!" });

            var categories = await _context.Categories
                .Include(c => c.Products)
                .OrderBy(c => c.Id)
                .Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    description = c.Description,
                    productCount = c.Products.Count,
                    totalStock = c.Products.Sum(p => p.Quantity),
                    totalValue = c.Products.Sum(p => p.Price * p.Quantity)
                })
                .ToListAsync();

            return Ok(categories);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryRequest request)
        {
            var auth = _tokenService.ValidateRequest(Request);
            if (auth == null) return Unauthorized(new { message = "Vui lòng đăng nhập!" });
            if (auth.Role != "ADMIN")
                return StatusCode(403, new { message = "Truy cập bị từ chối: Chỉ Quản lý mới có quyền thêm danh mục!" });

            if (!ModelState.IsValid) return BadRequest(ModelState);

            var cleanName = request.Name.Trim();
            if (await _context.Categories.AnyAsync(c => c.Name.ToLower() == cleanName.ToLower()))
            {
                return Conflict(new { message = "Tên danh mục này đã tồn tại!" });
            }

            var category = new Category
            {
                Name = cleanName,
                Description = request.Description?.Trim() ?? string.Empty
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = category.Id,
                name = category.Name,
                description = category.Description,
                productCount = 0,
                totalStock = 0,
                totalValue = 0m
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var auth = _tokenService.ValidateRequest(Request);
            if (auth == null) return Unauthorized(new { message = "Vui lòng đăng nhập!" });
            if (auth.Role != "ADMIN")
                return StatusCode(403, new { message = "Truy cập bị từ chối: Chỉ Quản lý mới có quyền xóa danh mục!" });

            var category = await _context.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
            if (category == null) return NotFound(new { message = "Không tìm thấy danh mục!" });

            if (category.Products.Any())
            {
                return BadRequest(new { message = $"Không thể xóa danh mục '{category.Name}' vì vẫn còn {category.Products.Count} sản phẩm đang thuộc danh mục này!" });
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa danh mục thành công!" });
        }
    }
}
