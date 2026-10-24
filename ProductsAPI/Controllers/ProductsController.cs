
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductsApi.Models;


namespace ProductsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private static readonly List<Product> _products = new()
        {
            new Product
            {
                Id = 1,
                Name = "Keyboard",
                Description = "USB Keyboard",
                Price = 750,
                StockQuantity = 20
            },
            new Product
            {
                Id = 2,
                Name = "Mouse",
                Description = "Wireless Mouse",
                Price = 450,
                StockQuantity = 15
            }
        };

        private static int _nextId = 3;

        public record ProductInput(
            string Name,
            string Description,
            decimal Price,
            int StockQuantity);

       
        [AllowAnonymous]
        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(_products);
        }

        [AllowAnonymous]
        [HttpGet("{id:int}")]
        public IActionResult GetById(int id)
        {
            var product = _products.FirstOrDefault(p => p.Id == id);

            return product == null ? NotFound() : Ok(product);
        }

        
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult Create(ProductInput input)
        {
            if (string.IsNullOrWhiteSpace(input.Name) ||
                input.Price < 0 ||
                input.StockQuantity < 0)
            {
                return BadRequest("Invalid product details.");
            }

            var product = new Product
            {
                Id = _nextId++,
                Name = input.Name,
                Description = input.Description,
                Price = input.Price,
                StockQuantity = input.StockQuantity
            };

            _products.Add(product);

            return StatusCode(201, product);
        }

        
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public IActionResult Update(int id, ProductInput input)
        {
            var product = _products.FirstOrDefault(p => p.Id == id);

            if (product == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(input.Name) ||
                input.Price < 0 ||
                input.StockQuantity < 0)
            {
                return BadRequest("Invalid product details.");
            }

            product.Name = input.Name;
            product.Description = input.Description;
            product.Price = input.Price;
            product.StockQuantity = input.StockQuantity;

            return Ok(product);
        }

    
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            var product = _products.FirstOrDefault(p => p.Id == id);

            if (product == null)
                return NotFound();

            _products.Remove(product);

            return NoContent();
        }
    }
}
