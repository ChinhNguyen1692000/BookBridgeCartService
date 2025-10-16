using CartService.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CartService.Application.Interface
{
    public interface ICartServices
    {
        Task<Cart> GetCartAsync(string customerId);
        Task<Cart> AddItemAsync(AddItemRequest item);
        Task<Cart> RemoveItemAsync(RemoveItemRequest item);

        Task ClearCartAsync(string customerId);
        Task SaveCartAsync(Cart cart);
    }
}
