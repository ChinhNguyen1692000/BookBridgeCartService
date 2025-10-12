using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Interface;
using OrderService.Application.Models;
using System.Threading.Tasks;

namespace OrderService.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderServices _service;

        public OrderController(IOrderServices service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(int page = 1, int pageSize = 10)
        {
            var result = await _service.GetAll(page, pageSize);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var order = await _service.GetById(id);
            if (order == null) return NotFound();
            return Ok(order);
        }

        [HttpGet("customer/{customerId}")]
        public async Task<IActionResult> GetByCustomer(string customerId, int page = 1, int pageSize = 10)
        {
            var result = await _service.GetOrderByCustomer(customerId, page, pageSize);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] OrderCreateRequest request)
        {
            var order = await _service.Create(request);
            return Ok(order);
        }

        [HttpPut("{id}/confirm")]
        public async Task<IActionResult> Confirm(int id)
        {
            var result = await _service.Confirm(id);
            return Ok(result);
        }

        [HttpPut("{id}/finish")]
        public async Task<IActionResult> Finish(int id)
        {
            var result = await _service.Finish(id);
            return Ok(result);
        }

        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            var result = await _service.Cancle(id);
            return Ok(result);
        }

        //----------------------------REDIS
        //[HttpPost("from-cart/{customerId}")]
        //public async Task<IActionResult> CreateFromCart(string customerId, [FromServices] CartService cartService)
        //{
        //    // 1️⃣ Lấy cart từ Redis
        //    var cart = await cartService.GetCartAsync(customerId);
        //    if (cart == null || !cart.Items.Any())
        //        return BadRequest("Cart is empty");

        //    // 2️⃣ Chuyển Cart → OrderCreateRequest
        //    var orderRequest = new OrderCreateRequest
        //    {
        //        CustomerId = customerId,
        //        ShippingAddress = "Default Address", // hoặc nhận từ FE
        //        TotalAmount = cart.TotalAmount,
        //        OrderItems = cart.Items.Select(i => new OrderItemCreateRequest
        //        {
        //            BookId = i.BookId,
        //            Quantity = i.Quantity,
        //            UnitPrice = i.UnitPrice,
        //            TotalPrice = i.TotalPrice
        //        }).ToList()
        //    };

        //    // 3️⃣ Gọi service tạo order
        //    var order = await _service.Create(orderRequest);

        //    // 4️⃣ Xóa giỏ hàng sau khi đặt
        //    await cartService.ClearCartAsync(customerId);

        //    return Ok(order);
        //}

    }
}
