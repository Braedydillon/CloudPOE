using Azure;
using Azure.Data.Tables;
using IncredibleComponentsPoe.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IncredibleComponentsPoe.Controllers
{
    public class ProductController : Controller
    {
        private readonly TableClient _tableClient;
        private readonly BlobService _blobService;

        public ProductController(IConfiguration config, BlobService blobService)
        {
            string connectionString = config.GetConnectionString("AzureTableStorage");
            _tableClient = new TableClient(connectionString, "Products");
            _tableClient.CreateIfNotExists();
            _blobService = blobService;
        }

        // All logged-in users can view products
        [Authorize]
        public IActionResult Index()
        {
            var products = _tableClient.Query<ProductEntity>().ToList();

            if (User.IsInRole("Admin"))
                return View("AdminIndex", products);
            else
                return View("UserIndex", products);
        }

        [Authorize(Roles = "User")]
        public IActionResult UserIndex()
        {
            var products = _tableClient.Query<ProductEntity>().ToList();
            return View(products); // must have Views/Product/UserIndex.cshtml
        }


        // All logged-in users can view details
        [Authorize]
        public IActionResult Details(string id)
        {
            try
            {
                var product = _tableClient.GetEntity<ProductEntity>("Products", id).Value;
                return View(product);
            }
            catch { return NotFound(); }
        }

        // Admin only: Create product
        [Authorize(Roles = "Admin")]
        public IActionResult Create() => View();

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductEntity product, IFormFile imageFile)
        {
            if (!ModelState.IsValid) return View(product);

            product.RowKey = Guid.NewGuid().ToString();
            product.PartitionKey = "Products";

            if (imageFile != null && imageFile.Length > 0)
                product.ImageUrl = await _blobService.UploadFileAsync(imageFile, "products");

            await _tableClient.AddEntityAsync(product);
            return RedirectToAction(nameof(Index));
        }

        // Admin only: Edit
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(string id)
        {
            try { return View(_tableClient.GetEntity<ProductEntity>("Products", id).Value); }
            catch { return NotFound(); }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string id, ProductEntity updatedProduct)
        {
            if (!ModelState.IsValid) return View(updatedProduct);

            try
            {
                var existing = _tableClient.GetEntity<ProductEntity>("Products", id).Value;
                existing.ProductName = updatedProduct.ProductName;
                existing.ProductDescription = updatedProduct.ProductDescription;
                existing.Price = updatedProduct.Price;
                existing.Stock = updatedProduct.Stock;

                _tableClient.UpdateEntity(existing, existing.ETag, TableUpdateMode.Replace);
                return RedirectToAction(nameof(Index));
            }
            catch { return NotFound(); }
        }

        // Admin only: Delete
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(string id)
        {
            try { return View(_tableClient.GetEntity<ProductEntity>("Products", id).Value); }
            catch { return NotFound(); }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(string id)
        {
            try
            {
                _tableClient.DeleteEntity("Products", id);
                return RedirectToAction(nameof(Index));
            }
            catch { return NotFound(); }
        }
    }
}
