using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ATEA_test.API.Models;
using ATEA_test.API.Services;
using static ATEA_test.API.AutoMapperSetup.AutoMapperConfig;

namespace ATEA_test.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IOrderService orderService, ILogger<OrderController> logger)
        {
            _logger = logger;
            _orderService = orderService;
        }

        [HttpPost]
        public async Task<IActionResult> SubmitOrder([FromBody] Order order, CancellationToken ct)
        {
            var (receipt, error) = await _orderService.ProcessOrderAsync(order, ct);

            if (receipt is not null)
                return Ok(receipt);

            return StatusCode(402, new { error });
        }

            [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
