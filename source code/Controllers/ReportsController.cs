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

        // GET: Reports/TestFactory - To verify Factory Pattern is working
        public IActionResult TestFactory()
        {
            var factory = new ReportFactory(_context);

            var dashboard = factory.CreateReport("Dashboard");
            var latePayments = factory.CreateReport("LatePayments");
            var serviceUsage = factory.CreateReport("ServiceUsage");
            var clientSummary = factory.CreateReport("ClientSummary");

            var allDifferent = dashboard.GetType() != latePayments.GetType() &&
                               latePayments.GetType() != serviceUsage.GetType() &&
                               serviceUsage.GetType() != clientSummary.GetType();

            return Json(new
            {
                factoryPatternWorking = allDifferent,
                dashboardType = dashboard.GetType().Name,
                latePaymentsType = latePayments.GetType().Name,
                serviceUsageType = serviceUsage.GetType().Name,
                clientSummaryType = clientSummary.GetType().Name,
                message = allDifferent ? "✅ Factory Pattern working correctly! All report types are different instances." : "❌ Factory Pattern failed"
            });
        }

        // UPDATED: Dashboard Report using Factory
        public async Task<IActionResult> Dashboard()
        {
            var factory = new ReportFactory(_context);
            var report = factory.CreateReport("Dashboard");

            var model = await report.GetData();
            return View(model);
        }

        // UPDATED: Late Payments Report using Factory
        public async Task<IActionResult> LatePayments()
        {
            var factory = new ReportFactory(_context);
            var report = factory.CreateReport("LatePayments");

            var model = await report.GetData();
            ViewBag.ReportDate = DateTime.Now.ToString("yyyy-MM-dd");
            return View(model);
        }

        // UPDATED: Service Usage Report using Factory
        public async Task<IActionResult> ServiceUsage()
        {
            var factory = new ReportFactory(_context);
            var report = factory.CreateReport("ServiceUsage");

            var model = await report.GetData();
            return View(model);
        }

        // UPDATED: Client Summary Report using Factory (Now Fixed)
        public async Task<IActionResult> ClientSummary()
        {
            var factory = new ReportFactory(_context);
            var report = factory.CreateReport("ClientSummary");
            var result = await report.GetData();

            dynamic data = result;
            ViewBag.ClientServices = data.ClientServices;
            return View(data.Clients);
        }
    }

    // ============================================================
    // FACTORY PATTERN IMPLEMENTATION
    // ============================================================

    // IReport Interface - All report generators must implement this
    public interface IReport
    {
        Task<object> GetData();
    }

    // Report 1: Dashboard Report Generator
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
                ActiveServices = await _db.Services.CountAsync(s => s.IsActive),
                ServiceUsage = new Dictionary<string, int>()
            };

            model.RecentPayments = await _db.Payments
                .Include(p => p.Client)
                .OrderByDescending(p => p.PaymentDate)
                .Take(5)
                .ToListAsync();

            // Populate ServiceUsage dictionary for the dashboard
            var services = await _db.Services.ToListAsync();
            foreach (var service in services)
            {
                var clientCount = await _db.ClientServices
                    .CountAsync(cs => cs.ServiceId == service.Id && cs.IsActive);
                model.ServiceUsage[service.Name] = clientCount;
            }

            return model;
        }
    }

    // Report 2: Late Payments Report Generator
    public class LatePaymentReportGenerator : IReport
    {
        private readonly ApplicationDbContext _db;
        public LatePaymentReportGenerator(ApplicationDbContext db) => _db = db;

        public async Task<object> GetData()
        {
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);
            var recentPaymentClientIds = await _db.Payments
                .Where(p => p.PaymentDate >= thirtyDaysAgo)
                .Select(p => p.ClientId)
                .Distinct()
                .ToListAsync();

            return await _db.Clients
                .Where(c => c.IsActive && !recentPaymentClientIds.Contains(c.Id))
                .ToListAsync();
        }
    }

    // Report 3: Service Usage Report Generator
    public class ServiceUsageReportGenerator : IReport
    {
        private readonly ApplicationDbContext _db;
        public ServiceUsageReportGenerator(ApplicationDbContext db) => _db = db;

        public async Task<object> GetData()
        {
            return await _db.ClientServices
                .Include(cs => cs.Service)
                .Include(cs => cs.Client)
                .Where(cs => cs.IsActive)
                .GroupBy(cs => cs.Service.Name)
                .Select(g => new ServiceUsageReport
                {
                    ServiceName = g.Key,
                    ClientCount = g.Count(),
                    TotalEmployees = g.Sum(cs => cs.NumberOfEmployees)
                })
                .OrderByDescending(r => r.ClientCount)
                .ToListAsync();
        }
    }

    // Report 4: Client Summary Report Generator (NEW - Fixed)
    public class ClientSummaryReportGenerator : IReport
    {
        private readonly ApplicationDbContext _db;
        public ClientSummaryReportGenerator(ApplicationDbContext db) => _db = db;

        public async Task<object> GetData()
        {
            var clients = await _db.Clients
                .OrderBy(c => c.CompanyName)
                .ToListAsync();

            var clientServices = await _db.ClientServices
                .Include(cs => cs.Service)
                .ToListAsync();

            return new { Clients = clients, ClientServices = clientServices };
        }
    }

    // ReportFactory - The heart of Factory Pattern
    // Decides which report object to create based on input type
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
                "ClientSummary" => new ClientSummaryReportGenerator(_db),
                _ => throw new ArgumentException($"Invalid Report Type: '{type}'. Valid types are: Dashboard, LatePayments, ServiceUsage, ClientSummary")
            };
        }
    }

    // ============================================================
    // VIEW MODELS
    // ============================================================

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