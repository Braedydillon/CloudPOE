using Azure.Data.Tables;
using IncredibleComponentsPoe.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

[Authorize(Roles = "User")]
public class CartController : Controller
{
    private readonly TableClient _productTable;
    private readonly TableClient _orderTable;
    private const string SessionKeyCart = "_Cart";

    public CartController(IConfiguration config)
    {
        string conn = config.GetConnectionString("AzureTableStorage");
        _productTable = new TableClient(conn, "Products");
        _orderTable = new TableClient(conn, "Orders");
        _orderTable.CreateIfNotExists();
    }

    // Show cart
    public IActionResult Index()
    {
        var cart = GetCart();
        return View(cart);
    }

    // Add to cart
    public IActionResult AddToCart(string id)
    {
        var product = _productTable.GetEntity<ProductEntity>("Products", id).Value;
        var cart = GetCart();

        var existing = cart.FirstOrDefault(c => c.RowKey == id);
        if (existing != null)
            existing.Quantity++;
        else
            cart.Add(new CartItem { RowKey = id, ProductName = product.ProductName, Price = product.Price });

        SaveCart(cart);
        return RedirectToAction("Index", "Product");
    }

    // Remove from cart
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Remove(string id)
    {
        var cart = GetCart();
        var item = cart.FirstOrDefault(c => c.RowKey == id);
        if (item != null) cart.Remove(item);
        SaveCart(cart);
        return RedirectToAction(nameof(Index));
    }

    // Checkout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Checkout()
    {
        var cart = GetCart();
        if (!cart.Any())
        {
            TempData["Error"] = "Your cart is empty!";
            return RedirectToAction(nameof(Index));
        }

        var order = new OrderEntity
        {
            RowKey = Guid.NewGuid().ToString(),
            PartitionKey = "Orders",
            ProductRowKeys = string.Join(",", cart.Select(c => c.RowKey)),
            Quantity = cart.Sum(c => c.Quantity),
            TotalPrice = cart.Sum(c => c.Total),
            Status = "Processing",
            CustomerUsername = User.Identity.Name
        };

        _orderTable.AddEntity(order);
        HttpContext.Session.Remove(SessionKeyCart);

        TempData["Message"] = "Order placed successfully!";
        return RedirectToAction("Index", "Order");
    }

    private List<CartItem> GetCart()
    {
        var json = HttpContext.Session.GetString(SessionKeyCart);
        if (string.IsNullOrEmpty(json)) return new List<CartItem>();
        return JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>();
    }

    private void SaveCart(List<CartItem> cart)
    {
        HttpContext.Session.SetString(SessionKeyCart, JsonSerializer.Serialize(cart));
    }
}
