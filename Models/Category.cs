using System.ComponentModel.DataAnnotations;

namespace DukeShopBackend.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên danh mục!")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(250)]
        public string Description { get; set; } = string.Empty;

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }

    public class CategoryRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập tên danh mục!")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(250)]
        public string Description { get; set; } = string.Empty;
    }
}
