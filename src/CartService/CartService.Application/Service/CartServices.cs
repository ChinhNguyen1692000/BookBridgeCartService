using CartService.Application.Interface;
using CartService.Application.Models;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CartService.Application.Service
{
    public class CartServices : ICartServices
    {
        private readonly IDatabase _redisDb;
        private readonly string _cartPrefix = "cart";
        public CartServices(IConnectionMultiplexer redisDb)
        {
            _redisDb = redisDb.GetDatabase();
        }
        private string GetKey(string customerId)
        {
            return _cartPrefix + customerId;
        }
        public async Task<Cart> GetCartAsync(string customerId)
        {
            var cart = await _redisDb.StringGetAsync(GetKey(customerId));
            if (cart.IsNullOrEmpty)
            {
                return new Cart { CustomerId = customerId };
            }
            return JsonSerializer.Deserialize<Cart>(cart);
        }
        public async Task<Cart> AddItemAsync(AddItemRequest item)
        {
            var cart = await GetCartAsync(item.CustomerId);
            if (cart == null)
            {
                cart = new Cart
                {
                    CustomerId = item.CustomerId,
                    Stores = new List<BookstoreItem>() // khởi tạo danh sách store
                };
            }

            // Tìm store trong cart
            var store = cart.Stores.FirstOrDefault(s => s.StoreId == item.StoreId);
            if (store == null)
            {
                // Nếu store chưa có, tạo mới
                store = new BookstoreItem
                {
                    StoreId = item.StoreId,
                    StoreName = item.StoreName,
                    Items = new List<CartItem>()
                };
                cart.Stores.Add(store);
            }

            // Tìm xem sách đã có trong store chưa
            var existItem = store.Items.FirstOrDefault(i => i.BookId == item.BookId);
            if (existItem != null)
            {
                existItem.Quantity += item.Quantity;
            }
            else
            {
                store.Items.Add(new CartItem
                {
                    BookId = item.BookId,
                    Quantity = item.Quantity,
                    BookImage = item.BookImage,
                    BookTitle = item.BookTitle,
                    UnitPrice = item.UnitPrice
                });
            }

            await SaveCartAsync(cart);
            return cart;
        }

        public async Task<Cart> RemoveItemAsync(RemoveItemRequest item)
        {
            var cart = await GetCartAsync(item.CustomerId);
            var existItem = cart.Stores.FirstOrDefault(s => s.StoreId == item.StoreId).Items.FirstOrDefault(i => i.BookId == item.BookId);
            var a = cart.Stores.FirstOrDefault(s => s.StoreId == item.StoreId).Items.Remove(existItem);
            await SaveCartAsync(cart);
            return cart;
        }

        public async Task ClearCartAsync(string customerId)
        {
            await _redisDb.KeyDeleteAsync(GetKey(customerId));
        }

        public async Task SaveCartAsync(Cart cart)
        {
            var data = JsonSerializer.Serialize(cart);

            // ⏱️ Tự xóa sau 1 phút
            await _redisDb.StringSetAsync(GetKey(cart.CustomerId), data, TimeSpan.FromMinutes(1));

            // 🕓 Tự xóa sau 2 tuần 
            // await _redisDb.StringSetAsync(GetKey(cart.CustomerId), data, TimeSpan.FromDays(14));
        }


    }
}
