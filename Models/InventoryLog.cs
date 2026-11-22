using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DukeShopBackend.Models
{
    public class InventoryLog
    {
        [Key]
        public int Id { get; set; }

        public string? ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }

        [MaxLength(150)]
        public string ProductNameSnapshot { get; set; } = string.Empty;

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [Required]
        [MaxLength(30)]
        public string ActionType { get; set; } = "IMPORT";

        public int QuantityChange { get; set; }

        [MaxLength(300)]
        public string Note { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class StockAdjustRequest
    {
        [Required(ErrorMessage = "Vui lòng chọn sản phẩm!")]
        public string ProductId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn loại phiếu (IMPORT hoặc EXPORT)!")]
        public string ActionType { get; set; } = "IMPORT";

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng nhập/xuất phải lớn hơn 0!")]
        public int Quantity { get; set; }

        [MaxLength(300)]
        public string Note { get; set; } = string.Empty;
    }
}
