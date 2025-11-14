using Azure;
using Azure.Data.Tables;
using IncredibleComponentsPoe.Models;
using IncredibleComponentsPOE.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IncredibleComponentsPoe.Controllers
{
    [Authorize(Roles = "Admin")] // Only logged-in users with Admin role can access
    public class CustomerController : Controller
    {
        private readonly IncredibleComponentPoe _context;
        private readonly TableClient _customerTable;

        public CustomerController(IncredibleComponentPoe context, IConfiguration config)
        {
            _context = context;
            string conn = config.GetConnectionString("AzureTableStorage");
            _customerTable = new TableClient(conn, "Customers");
            _customerTable.CreateIfNotExists();
        }

        public async Task<IActionResult> Index()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User newUser)
        {
            if (!ModelState.IsValid) return View(newUser);

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            var tableCustomer = new CustomerEntity
            {
                PartitionKey = "Customer",
                RowKey = newUser.Id.ToString(),
                FirstName = newUser.Username,
              
            };
            await _customerTable.AddEntityAsync(tableCustomer);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, User updatedUser)
        {
            if (!ModelState.IsValid) return View(updatedUser);

            _context.Users.Update(updatedUser);
            await _context.SaveChangesAsync();

            var tableCustomer = new CustomerEntity
            {
                PartitionKey = "Customer",
                RowKey = updatedUser.Id.ToString(),
                FirstName = updatedUser.Username,
           
            };
            await _customerTable.UpsertEntityAsync(tableCustomer);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            await _customerTable.DeleteEntityAsync("Customer", id.ToString());

            return RedirectToAction(nameof(Index));
        }
    }
}
