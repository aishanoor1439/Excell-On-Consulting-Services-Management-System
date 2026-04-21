using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExcellOnServices.Data;
using ExcellOnServices.Models;
using Microsoft.AspNetCore.Authorization;

namespace ExcellOnServices.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // UPDATED: Ab ye Factory use karega
        public async Task<IActionResult> Dashboard()
        {
            // Factory se report object mangen
            var factory = new ReportFactory(_context);
            var report = factory.CreateReport("Dashboard");

            var model = await report.GetData();
            return View(model);
        }

        public async Task<IActionResult> LatePayments()
        {
            var factory = new ReportFactory(_context);
            var report = factory.CreateReport("LatePayments");

            var model = await report.GetData();
            ViewBag.ReportDate = DateTime.Now.ToString("yyyy-MM-dd");
            return View(model);
        }

        public async Task<IActionResult> ServiceUsage()
        {
            var factory = new ReportFactory(_context);
            var report = factory.CreateReport("ServiceUsage");

            var model = await report.GetData();
            return View(model);
        }

        public async Task<IActionResult> ClientSummary()
        {
            var clients = await _context.Clients.OrderBy(c => c.CompanyName).ToListAsync();
            var clientServices = await _context.ClientServices.Include(cs => cs.Service).ToListAsync();
            ViewBag.ClientServices = clientServices;
            return View(clients);
        }
    }

    // --- FACTORY PATTERN IMPLEMENTATION FOR IFFAT'S MODULE ---

    public interface IReport
    {
        Task<object> GetData();
    }

    public class DashboardReportGenerator : IReport
    {
        private readonly ApplicationDbContext _db;
        public DashboardReportGenerator(ApplicationDbContext db) => _db = db;

        public async Task<object> GetData()
        {
            var model = new DashboardReport
            {
                TotalServices = await _db.Services.CountAsync(),
                TotalDepartments = await _db.Departments.CountAsync(),
                TotalEmployees = await _db.Employees.CountAsync(),
                TotalClients = await _db.Clients.CountAsync(),
                TotalPayments = await _db.Payments.SumAsync(p => p.Amount),
                ActiveClients = await _db.Clients.CountAsync(c => c.IsActive),
                ActiveServices = await _db.Services.CountAsync(s => s.IsActive)
            };
            model.RecentPayments = await _db.Payments.Include(p => p.Client)
                                    .OrderByDescending(p => p.PaymentDate).Take(5).ToListAsync();
            return model;
        }
    }

    public class LatePaymentReportGenerator : IReport
    {
        private readonly ApplicationDbContext _db;
        public LatePaymentReportGenerator(ApplicationDbContext db) => _db = db;

        public async Task<object> GetData()
        {
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);
            var recentPaymentClientIds = await _db.Payments
                .Where(p => p.PaymentDate >= thirtyDaysAgo)
                .Select(p => p.ClientId).Distinct().ToListAsync();

            return await _db.Clients
                .Where(c => c.IsActive && !recentPaymentClientIds.Contains(c.Id))
                .ToListAsync();
        }
    }

    public class ServiceUsageReportGenerator : IReport
    {
        private readonly ApplicationDbContext _db;
        public ServiceUsageReportGenerator(ApplicationDbContext db) => _db = db;

        public async Task<object> GetData()
        {
            return await _db.ClientServices
                .Include(cs => cs.Service).Include(cs => cs.Client)
                .Where(cs => cs.IsActive).GroupBy(cs => cs.Service.Name)
                .Select(g => new ServiceUsageReport
                {
                    ServiceName = g.Key,
                    ClientCount = g.Count(),
                    TotalEmployees = g.Sum(cs => cs.NumberOfEmployees)
                }).OrderByDescending(r => r.ClientCount).ToListAsync();
        }
    }

    public class ReportFactory
    {
        private readonly ApplicationDbContext _db;
        public ReportFactory(ApplicationDbContext db) => _db = db;

        public IReport CreateReport(string type)
        {
            return type switch
            {
                "Dashboard" => new DashboardReportGenerator(_db),
                "LatePayments" => new LatePaymentReportGenerator(_db),
                "ServiceUsage" => new ServiceUsageReportGenerator(_db),
                _ => throw new ArgumentException("Invalid Report Type")
            };
        }
    }

    // --- VIEW MODELS ---
    public class DashboardReport
    {
        public int TotalServices { get; set; }
        public int TotalDepartments { get; set; }
        public int TotalEmployees { get; set; }
        public int TotalClients { get; set; }
        public decimal TotalPayments { get; set; }
        public int ActiveClients { get; set; }
        public int ActiveServices { get; set; }
        public List<Payment> RecentPayments { get; set; } = new List<Payment>();
        public Dictionary<string, int> ServiceUsage { get; set; } = new Dictionary<string, int>();
    }

    public class ServiceUsageReport
    {
        public string ServiceName { get; set; }
        public int ClientCount { get; set; }
        public int TotalEmployees { get; set; }
    }
}