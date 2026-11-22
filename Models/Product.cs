using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DukeShopBackend.Models
{
    public class Product
    {
        [Key]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm!")]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn danh mục!")]
        public int CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public Category? Category { get; set; }

        [Range(1, double.MaxValue, ErrorMessage = "Giá bán phải lớn hơn 0!")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho không được âm!")]
        public int Quantity { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<InventoryLog> InventoryLogs { get; set; } = new List<InventoryLog>();
    }

    public class ProductUpsertRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm!")]
        public string Name { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn danh mục hợp lệ!")]
        public int CategoryId { get; set; }

        [Range(1, double.MaxValue, ErrorMessage = "Giá bán phải lớn hơn 0!")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho không được âm!")]
        public int Quantity { get; set; }
    }
}
