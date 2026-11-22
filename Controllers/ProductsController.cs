using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DukeShopBackend.Data;
using DukeShopBackend.Models;
using DukeShopBackend.Services;

namespace DukeShopBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly TokenService _tokenService;

        public ProductsController(AppDbContext context, TokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            var auth = _tokenService.ValidateRequest(Request);
            if (auth == null)
            {
                return Unauthorized(new { message = "Vui lòng đăng nhập để xem danh sách sản phẩm!" });
            }

            var list = await _context.Products
                .Include(p => p.Category)
                .OrderBy(p => p.Id)
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    categoryId = p.CategoryId,
                    category = p.Category != null ? p.Category.Name : "Khác",
                    price = p.Price,
                    quantity = p.Quantity,
                    updatedAt = p.UpdatedAt
                })
                .ToListAsync();

            return Ok(list);
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] ProductUpsertRequest request)
        {
            var auth = _tokenService.ValidateRequest(Request);
            if (auth == null) return Unauthorized(new { message = "Vui lòng đăng nhập!" });
            if (auth.Role != "ADMIN")
                return StatusCode(403, new { message = "Truy cập bị từ chối: Nhân viên không có quyền thêm sản phẩm!" });

            if (!ModelState.IsValid) return BadRequest(ModelState);

            var category = await _context.Categories.FindAsync(request.CategoryId);
            if (category == null)
                return BadRequest(new { message = "Danh mục sản phẩm không tồn tại!" });

            var allIds = await _context.Products.Select(p => p.Id).ToListAsync();
            int maxNumber = 0;
            foreach (var id in allIds)
            {
                if (id.StartsWith("SP") && int.TryParse(id.Substring(2), out int num))
                {
                    if (num > maxNumber) maxNumber = num;
                }
            }
            int nextNumber = maxNumber + 1;
            string newId = nextNumber < 10 ? $"SP0{nextNumber}" : $"SP{nextNumber}";

            var product = new Product
            {
                Id = newId,
                Name = request.Name.Trim(),
                CategoryId = request.CategoryId,
                Price = request.Price,
                Quantity = request.Quantity,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Products.Add(product);
            _context.InventoryLogs.Add(new InventoryLog
            {
                ProductId = product.Id,
                ProductNameSnapshot = product.Name,
                UserId = auth.UserId,
                ActionType = "CREATE",
                QuantityChange = product.Quantity,
                Note = $"Thêm mới sản phẩm ({category.Name})",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = product.Id,
                name = product.Name,
                categoryId = product.CategoryId,
                category = category.Name,
                price = product.Price,
                quantity = product.Quantity,
                updatedAt = product.UpdatedAt
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(string id, [FromBody] ProductUpsertRequest request)
        {
            var auth = _tokenService.ValidateRequest(Request);
            if (auth == null) return Unauthorized(new { message = "Vui lòng đăng nhập!" });
            if (auth.Role != "ADMIN")
                return StatusCode(403, new { message = "Truy cập bị từ chối: Nhân viên không có quyền sửa sản phẩm!" });

            if (!ModelState.IsValid) return BadRequest(ModelState);

            var existingProduct = await _context.Products.FindAsync(id);
            if (existingProduct == null)
                return NotFound(new { message = "Không tìm thấy sản phẩm!" });

            var category = await _context.Categories.FindAsync(request.CategoryId);
            if (category == null)
                return BadRequest(new { message = "Danh mục sản phẩm không tồn tại!" });

            int diff = request.Quantity - existingProduct.Quantity;

            existingProduct.Name = request.Name.Trim();
            existingProduct.CategoryId = request.CategoryId;
            existingProduct.Price = request.Price;
            existingProduct.Quantity = request.Quantity;
            existingProduct.UpdatedAt = DateTime.UtcNow;

            _context.InventoryLogs.Add(new InventoryLog
            {
                ProductId = existingProduct.Id,
                ProductNameSnapshot = existingProduct.Name,
                UserId = auth.UserId,
                ActionType = "UPDATE",
                QuantityChange = diff,
                Note = $"Cập nhật thông tin / tồn kho ({category.Name})",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = existingProduct.Id,
                name = existingProduct.Name,
                categoryId = existingProduct.CategoryId,
                category = category.Name,
                price = existingProduct.Price,
                quantity = existingProduct.Quantity,
                updatedAt = existingProduct.UpdatedAt
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(string id)
        {
            var auth = _tokenService.ValidateRequest(Request);
            if (auth == null) return Unauthorized(new { message = "Vui lòng đăng nhập!" });
            if (auth.Role != "ADMIN")
                return StatusCode(403, new { message = "Truy cập bị từ chối: Nhân viên không có quyền xóa sản phẩm!" });

            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound(new { message = "Không tìm thấy sản phẩm!" });

            _context.InventoryLogs.Add(new InventoryLog
            {
                ProductId = null,
                ProductNameSnapshot = $"{product.Id} - {product.Name}",
                UserId = auth.UserId,
                ActionType = "DELETE",
                QuantityChange = -product.Quantity,
                Note = "Xóa sản phẩm khỏi danh mục kinh doanh",
                CreatedAt = DateTime.UtcNow
            });

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa sản phẩm thành công!" });
        }

        [HttpPost("reset")]
        public async Task<IActionResult> ResetSampleData()
        {
            var auth = _tokenService.ValidateRequest(Request);
            if (auth == null) return Unauthorized(new { message = "Vui lòng đăng nhập!" });
            if (auth.Role != "ADMIN")
                return StatusCode(403, new { message = "Truy cập bị từ chối: Nhân viên không có quyền khôi phục dữ liệu!" });

            if (!await _context.Categories.AnyAsync())
            {
                _context.Categories.AddRange(
                    new Category { Id = 1, Name = "Áo", Description = "Áo thun, sơ mi, áo khoác thời trang" },
                    new Category { Id = 2, Name = "Quần", Description = "Quần Jeans, kaki, quần short nam nữ" },
                    new Category { Id = 3, Name = "Giày dép", Description = "Giày sneaker, giày thể thao & dép" },
                    new Category { Id = 4, Name = "Phụ kiện", Description = "Balo laptop, mũ lưỡi trai, túi xách" }
                );
                await _context.SaveChangesAsync();
            }

            var currentProducts = await _context.Products.ToListAsync();
            _context.Products.RemoveRange(currentProducts);
            await _context.SaveChangesAsync();

            var now = DateTime.UtcNow;
            var sampleProducts = new List<Product>
            {
                new Product { Id = "SP01", Name = "Áo Thun Polo Duke Classic", CategoryId = 1, Price = 250000, Quantity = 30, UpdatedAt = now },
                new Product { Id = "SP02", Name = "Áo Khoác Bomber Nam Đen", CategoryId = 1, Price = 480000, Quantity = 4, UpdatedAt = now },
                new Product { Id = "SP03", Name = "Quần Jeans Slimfit Xanh Đậm", CategoryId = 2, Price = 390000, Quantity = 15, UpdatedAt = now },
                new Product { Id = "SP04", Name = "Giày Sneaker Duke Trắng", CategoryId = 3, Price = 650000, Quantity = 0, UpdatedAt = now },
                new Product { Id = "SP05", Name = "Balo Laptop Chống Nước", CategoryId = 4, Price = 320000, Quantity = 12, UpdatedAt = now },
                new Product { Id = "SP06", Name = "Áo Sơ Mi Oxford Trắng", CategoryId = 1, Price = 290000, Quantity = 22, UpdatedAt = now },
                new Product { Id = "SP07", Name = "Quần Short Kaki Basic", CategoryId = 2, Price = 210000, Quantity = 3, UpdatedAt = now },
                new Product { Id = "SP08", Name = "Mũ Lưỡi Trai Thêu Logo Duke", CategoryId = 4, Price = 150000, Quantity = 18, UpdatedAt = now }
            };

            await _context.Products.AddRangeAsync(sampleProducts);
            _context.InventoryLogs.Add(new InventoryLog
            {
                ProductId = "SP01",
                ProductNameSnapshot = "Khôi phục dữ liệu mẫu (8 SP)",
                UserId = auth.UserId,
                ActionType = "IMPORT",
                QuantityChange = 104,
                Note = "Quản lý khôi phục 8 sản phẩm mẫu ban đầu",
                CreatedAt = now
            });
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã khôi phục 8 sản phẩm mẫu thành công!" });
        }
    }
}
