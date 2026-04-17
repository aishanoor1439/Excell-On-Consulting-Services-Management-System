using Microsoft.EntityFrameworkCore;
using System;

namespace ExcellOnServices.Data
{
    public class DatabaseHandler
    {
        private static ApplicationDbContext _instance;
        private static readonly object _lock = new object();

        private DatabaseHandler() { }

        public static ApplicationDbContext GetContext(DbContextOptions<ApplicationDbContext> options)
        {
            lock (_lock)
            {
                // Check karein ke kya instance null hai ya Dispose ho chuka hai
                if (_instance == null || IsContextDisposed(_instance))
                {
                    var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                    optionsBuilder.UseSqlServer(@"Server=.\LAB;Database=ExcellOnDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=true");
                    
                    _instance = new ApplicationDbContext(optionsBuilder.Options);
                }
                return _instance;
            }
        }

        // Helper function to check if context is disposed
        private static bool IsContextDisposed(ApplicationDbContext context)
        {
            try
            {
                // Agar context disposed hai, toh ye property access karne par exception dega
                var test = context.Model; 
                return false;
            }
            catch (ObjectDisposedException)
            {
                return true;
            }
            catch (Exception)
            {
                return true;
            }
        }
    }
}