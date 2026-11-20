using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using HospitalManagementSystem.Services;

namespace HospitalManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ExpensesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;

        public ExpensesController(ApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IActionResult> Index(string? category, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Expenses.AsQueryable();
            if (!string.IsNullOrWhiteSpace(category)) query = query.Where(e => e.Category == category);
            if (fromDate.HasValue) query = query.Where(e => e.ExpenseDate >= fromDate.Value);
            if (toDate.HasValue) query = query.Where(e => e.ExpenseDate <= toDate.Value);

            var expenses = await query.OrderByDescending(e => e.ExpenseDate).ToListAsync();
            var today = DateTime.Today;

            ViewBag.TodayTotal = expenses.Where(e => e.ExpenseDate == today).Sum(e => e.Amount);
            ViewBag.MonthTotal = expenses.Where(e => e.ExpenseDate.Month == today.Month && e.ExpenseDate.Year == today.Year).Sum(e => e.Amount);
            ViewBag.GrandTotal = expenses.Sum(e => e.Amount);
            ViewBag.Category = category;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(expenses);
        }

        public IActionResult Create() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Expense expense)
        {
            if (ModelState.IsValid)
            {
                expense.EnteredBy = User.Identity?.Name;
                expense.CreatedAt = DateTime.UtcNow;
                _context.Expenses.Add(expense);
                await _context.SaveChangesAsync();
                await _audit.LogAsync($"Expense added: {expense.Description} ₹{expense.Amount}", "Expenses");
                TempData["Success"] = "Expense recorded.";
                return RedirectToAction(nameof(Index));
            }
            return View(expense);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var expense = await _context.Expenses.FindAsync(id);
            if (expense == null) return NotFound();
            return View(expense);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Expense expense)
        {
            if (id != expense.ExpenseId) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(expense);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Expense updated.";
                return RedirectToAction(nameof(Index));
            }
            return View(expense);
        }
    }
}
