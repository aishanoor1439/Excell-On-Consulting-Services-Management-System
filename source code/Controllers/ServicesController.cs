using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExcellOnServices.Data;
using ExcellOnServices.Models;
using Microsoft.AspNetCore.Authorization;

namespace ExcellOnServices.Controllers
{
    [Authorize]
    public class ServicesController : Controller
    {
        // Yahan 'readonly' hata diya hai taake disposed hone par refresh kiya ja sakay
        private ApplicationDbContext _context;

        public ServicesController()
        {
            // Initial instance fetch
            _context = DatabaseHandler.GetContext(null);
        }

        // Helper method: Jo har action se pehle check karega ke context 'alive' hai ya nahi
        private void RefreshContext()
        {
            _context = DatabaseHandler.GetContext(null);
        }

        // GET: Services
        public IActionResult Index()
        {
            RefreshContext(); // Aakhri touch: Connection check
            if (_context == null) return NotFound("Database connection error.");
            
            var services = _context.Services.AsNoTracking().ToList();
            return View(services);
        }

        // GET: Services/Details/5
        public IActionResult Details(int? id)
        {
            RefreshContext();
            if (id == null) return NotFound();

            var service = _context.Services
                .AsNoTracking()
                .FirstOrDefault(m => m.Id == id);
                
            if (service == null) return NotFound();

            return View(service);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create([Bind("Id,Name,Description,DailyChargePerEmployee,CreatedDate,IsActive")] Service service)
        {
            RefreshContext();
            if (ModelState.IsValid)
            {
                if (service.CreatedDate == default) service.CreatedDate = DateTime.Now;

                _context.Add(service);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(service);
        }

        public IActionResult Edit(int? id)
        {
            RefreshContext();
            if (id == null) return NotFound();

            var service = _context.Services.Find(id);
            if (service == null) return NotFound();
            
            return View(service);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, [Bind("Id,Name,Description,DailyChargePerEmployee,CreatedDate,IsActive")] Service service)
        {
            RefreshContext();
            if (id != service.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(service);
                    _context.SaveChanges();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ServiceExists(service.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(service);
        }

        public IActionResult Delete(int? id)
        {
            RefreshContext();
            if (id == null) return NotFound();

            var service = _context.Services
                .AsNoTracking()
                .FirstOrDefault(m => m.Id == id);
                
            if (service == null) return NotFound();

            return View(service);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            RefreshContext();
            var service = _context.Services.Find(id);
            if (service != null)
            {
                _context.Services.Remove(service);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ServiceExists(int id)
        {
            RefreshContext();
            return _context.Services.Any(e => e.Id == id);
        }
    }
}