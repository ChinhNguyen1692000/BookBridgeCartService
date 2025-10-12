using CartService.Application.Interface;
using CartService.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace CartService.Api.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartServices _cartService;

        public CartController(ICartServices cartService)
        {
            _cartService = cartService;
        }

        [HttpGet("{customerId}")]
        public async Task<IActionResult> GetCart(string customerId)
        {
            var cart = await _cartService.GetCartAsync(customerId);
            return Ok(cart);
        }

        [HttpPost("/items")]
        public async Task<IActionResult> AddItem([FromBody] AddItemRequest item)
        {
            var updatedCart = await _cartService.AddItemAsync(item);
            return Ok(updatedCart);
        }

        [HttpDelete("{customerId}/items/")]
        public async Task<IActionResult> RemoveItem(RemoveItemRequest item)
        {
            var updatedCart = await _cartService.RemoveItemAsync(item);
            return Ok(updatedCart);
        }

        [HttpDelete("{customerId}")]
        public async Task<IActionResult> ClearCart(string customerId)
        {
            await _cartService.ClearCartAsync(customerId);
            return NoContent();
        }
    }
}