using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DukeShopBackend.Data;
using DukeShopBackend.Models;
using DukeShopBackend.Services;

namespace DukeShopBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryLogsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly TokenService _tokenService;

        public InventoryLogsController(AppDbContext context, TokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        [HttpGet]
        public async Task<IActionResult> GetLogs()
        {
            var auth = _tokenService.ValidateRequest(Request);
            if (auth == null)
                return Unauthorized(new { message = "Vui lòng đăng nhập để xem nhật ký kho!" });

            var logs = await _context.InventoryLogs
                .Include(l => l.User)
                .OrderByDescending(l => l.CreatedAt)
                .Take(50)
                .Select(l => new
                {
                    id = l.Id,
                    productId = l.ProductId ?? "-",
                    productName = l.ProductNameSnapshot,
                    userName = l.User != null ? l.User.FullName : "Hệ thống",
                    userRole = l.User != null ? l.User.Role : "ADMIN",
                    actionType = l.ActionType,
                    quantityChange = l.QuantityChange,
                    note = l.Note,
                    createdAt = l.CreatedAt
                })
                .ToListAsync();

            return Ok(logs);
        }

        [HttpPost("adjust")]
        public async Task<IActionResult> AdjustStock([FromBody] StockAdjustRequest request)
        {
            var auth = _tokenService.ValidateRequest(Request);
            if (auth == null) return Unauthorized(new { message = "Vui lòng đăng nhập!" });
            if (auth.Role != "ADMIN")
                return StatusCode(403, new { message = "Truy cập bị từ chối: Chỉ Quản lý mới có quyền lập phiếu nhập/xuất kho!" });

            if (!ModelState.IsValid) return BadRequest(ModelState);

            var product = await _context.Products.FindAsync(request.ProductId);
            if (product == null) return NotFound(new { message = "Không tìm thấy sản phẩm!" });

            string action = request.ActionType.Trim().ToUpper();
            if (action != "IMPORT" && action != "EXPORT")
                return BadRequest(new { message = "Loại phiếu phải là IMPORT (Nhập) hoặc EXPORT (Xuất)!" });

            if (action == "EXPORT" && product.Quantity < request.Quantity)
            {
                return BadRequest(new { message = $"Số lượng tồn kho hiện tại ({product.Quantity}) không đủ để xuất {request.Quantity}!" });
            }

            int delta = action == "IMPORT" ? request.Quantity : -request.Quantity;
            product.Quantity += delta;
            product.UpdatedAt = DateTime.UtcNow;

            var log = new InventoryLog
            {
                ProductId = product.Id,
                ProductNameSnapshot = product.Name,
                UserId = auth.UserId,
                ActionType = action,
                QuantityChange = delta,
                Note = string.IsNullOrWhiteSpace(request.Note)
                    ? (action == "IMPORT" ? "Nhập hàng bổ sung vào kho" : "Xuất hàng bán lẻ/sỉ")
                    : request.Note.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.InventoryLogs.Add(log);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = action == "IMPORT"
                    ? $"Đã nhập thêm {request.Quantity} sản phẩm {product.Name}!"
                    : $"Đã xuất {request.Quantity} sản phẩm {product.Name}!",
                newQuantity = product.Quantity
            });
        }
    }
}
