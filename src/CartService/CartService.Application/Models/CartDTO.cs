using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CartService.Application.Models
{
    public class CartDTO
    {

    }
    public class Cart
    {
        public string CustomerId { get; set; }  // Mã user, dùng làm key Redis
        public List<BookstoreItem> Stores { get; set; } = new List<BookstoreItem>();
    }
    public class BookstoreItem
    {
        public int StoreId { get; set; }
        public string? StoreName { get; set; }
        public List<CartItem> Items { get; set; } = new List<CartItem>();

    }
    public class CartItem
    {
        public int BookId { get; set; }
        public string? BookTitle { get; set; }
        public string? BookImage { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
    public class AddItemRequest
    {
        public string CustomerId { get; set; }  // Mã user, dùng làm key Redis
        public int StoreId { get; set; }
        public string? StoreName { get; set; }
        public int BookId { get; set; }
        public string? BookTitle { get; set; }
        public string? BookImage { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
    public class RemoveItemRequest
    {
        public string CustomerId { get; set; }  // Mã user, dùng làm key Redis
        public int StoreId { get; set; }
        public int BookId { get; set; }

    }
}
