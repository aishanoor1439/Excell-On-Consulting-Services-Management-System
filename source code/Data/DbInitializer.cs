using ExcellOnServices.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace ExcellOnServices.Data
{
    public static class DbInitializer
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            // Singleton DatabaseHandler ka istemal
            using (var context = ExcellOnServices.Data.DatabaseHandler.GetContext(null))
            {
                // 1. Look for any services (Check if DB is already seeded)
                if (context.Services.Any())
                {
                    return; // DB has already been seeded, so exit
                }

                // 2. Add Services
                var services = new Service[]
                {
                    new Service { Name = "In-bound", Description = "Receive calls from customers", DailyChargePerEmployee = 4500, IsActive = true, CreatedDate = DateTime.Now },
                    new Service { Name = "Out-bound", Description = "Call customers for promotions", DailyChargePerEmployee = 6000, IsActive = true, CreatedDate = DateTime.Now },
                    new Service { Name = "Tele Marketing", Description = "Sales and marketing calls", DailyChargePerEmployee = 5500, IsActive = true, CreatedDate = DateTime.Now }
                };
                context.Services.AddRange(services);

                // 3. Add Departments
                var departments = new Department[]
                {
                    new Department { Name = "HR Management", Description = "Human Resources Department" },
                    new Department { Name = "Administration", Description = "Administrative Operations" },
                    new Department { Name = "Service", Description = "Service Delivery Department" },
                    new Department { Name = "Training", Description = "Employee Training Department" },
                    new Department { Name = "Internet Security", Description = "IT Security and Support" },
                    new Department { Name = "Auditors", Description = "Auditing and Compliance" }
                };
                context.Departments.AddRange(departments);

                // 4. Final Save
                context.SaveChanges();
            } // Context yahan band ho raha hai
        }
    }
}