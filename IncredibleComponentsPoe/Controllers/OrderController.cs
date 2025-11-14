using Azure;
using Azure.Data.Tables;
using IncredibleComponentsPoe.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;

namespace IncredibleComponentsPoe.Controllers
{
    [Authorize] // all actions require login
    public class OrderController : Controller
    {
        private readonly TableClient _orderTable;
        private readonly TableClient _customerTable;
        private readonly TableClient _productTable;
        private readonly QueueService _queueService;

        public OrderController(IConfiguration config, QueueService queueService)
        {
            string conn = config.GetConnectionString("AzureTableStorage");

            _orderTable = new TableClient(conn, "Orders");
            _orderTable.CreateIfNotExists();

            _customerTable = new TableClient(conn, "Customers");
            _customerTable.CreateIfNotExists();

            _productTable = new TableClient(conn, "Products");
            _productTable.CreateIfNotExists();

            _queueService = queueService;
        }

        // GET: /Order
        public IActionResult Index()
        {
            var orders = _orderTable.Query<OrderEntity>().ToList();

            if (User.IsInRole("Admin"))
            {
                // Admin sees all orders
                foreach (var order in orders)
                    PopulateOrderDetails(order);

                return View("AdminIndex", orders);
            }
            else
            {
                // Customer sees only their own orders
                orders = orders.Where(o => o.CustomerUsername == User.Identity.Name).ToList();
                foreach (var order in orders)
                    PopulateOrderDetails(order);

                return View("UserIndex", orders);
            }
        }

        // GET: /Order/Details/{id}
        public IActionResult Details(string id)
        {
            try
            {
                var order = _orderTable.GetEntity<OrderEntity>("Orders", id).Value;

                // Customers cannot view others' orders
                if (!User.IsInRole("Admin") && order.CustomerUsername != User.Identity.Name)
                    return Forbid();

                PopulateOrderDetails(order);
                return View(order);
            }
            catch
            {
                return NotFound();
            }
        }

        #region Admin-only Actions

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewBag.Customers = _customerTable.Query<CustomerEntity>().ToList();
            ViewBag.Products = _productTable.Query<ProductEntity>().ToList();
            ViewBag.StatusOptions = new SelectList(new[] { "Processing", "Delivery" });
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderEntity model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Customers = _customerTable.Query<CustomerEntity>().ToList();
                ViewBag.Products = _productTable.Query<ProductEntity>().ToList();
                ViewBag.StatusOptions = new SelectList(new[] { "Processing", "Delivery" });
                return View(model);
            }

            decimal totalPrice = 0;
            var productKeys = model.ProductRowKeys.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var key in productKeys)
            {
                var product = _productTable.GetEntity<ProductEntity>("Products", key).Value;
                totalPrice += product.Price * model.Quantity;
            }

            model.TotalPrice = totalPrice;
            if (string.IsNullOrEmpty(model.Status))
                model.Status = "Processing";

            await _orderTable.AddEntityAsync(model);

            var orderReceivedMsg = new
            {
                EventType = "OrderReceived",
                OrderId = model.RowKey,
                TotalPrice = model.TotalPrice,
                Status = model.Status,
                Timestamp = DateTime.UtcNow
            };
            await _queueService.SendMessage(JsonSerializer.Serialize(orderReceivedMsg));

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Edit(string id)
        {
            try
            {
                var order = _orderTable.GetEntity<OrderEntity>("Orders", id).Value;
                ViewBag.Customers = _customerTable.Query<CustomerEntity>().ToList();
                ViewBag.Products = _productTable.Query<ProductEntity>().ToList();
                ViewBag.StatusOptions = new SelectList(new[] { "Processing", "Delivery" }, order.Status);
                return View(order);
            }
            catch { return NotFound(); }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string id, OrderEntity updatedOrder)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Customers = _customerTable.Query<CustomerEntity>().ToList();
                ViewBag.Products = _productTable.Query<ProductEntity>().ToList();
                ViewBag.StatusOptions = new SelectList(new[] { "Processing", "Delivery" }, updatedOrder.Status);
                return View(updatedOrder);
            }

            var existing = _orderTable.GetEntity<OrderEntity>("Orders", id).Value;
            existing.CustomerRowKey = updatedOrder.CustomerRowKey;
            existing.ProductRowKeys = updatedOrder.ProductRowKeys;
            existing.Quantity = updatedOrder.Quantity;
            existing.TotalPrice = updatedOrder.TotalPrice;
            existing.Status = updatedOrder.Status;

            _orderTable.UpdateEntity(existing, existing.ETag, TableUpdateMode.Replace);
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Delete(string id)
        {
            try
            {
                var order = _orderTable.GetEntity<OrderEntity>("Orders", id).Value;
                return View(order);
            }
            catch { return NotFound(); }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(string id)
        {
            try
            {
                _orderTable.DeleteEntity("Orders", id);
                return RedirectToAction(nameof(Index));
            }
            catch { return NotFound(); }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateStatus(string id, string Status)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();

            try
            {
                var existing = _orderTable.GetEntity<OrderEntity>("Orders", id).Value;
                existing.Status = Status;
                _orderTable.UpdateEntity(existing, existing.ETag, TableUpdateMode.Replace);
                return RedirectToAction("Index");
            }
            catch { return NotFound(); }
        }

        #endregion

        #region Helper
        private void PopulateOrderDetails(OrderEntity order)
        {
            try
            {
                var customer = _customerTable.GetEntity<CustomerEntity>("Customers", order.CustomerRowKey).Value;
                order.CustomerName = $"{customer.FirstName} {customer.LastName}";
            }
            catch { order.CustomerName = "Unknown"; }

            try
            {
                var productKeys = order.ProductRowKeys.Split(',', StringSplitOptions.RemoveEmptyEntries);
                order.ProductNames = string.Join(", ", productKeys.Select(id =>
                {
                    try { return _productTable.GetEntity<ProductEntity>("Products", id).Value.ProductName; }
                    catch { return "Unknown"; }
                }));
            }
            catch { order.ProductNames = "Unknown"; }
        }
        #endregion
    }
}
