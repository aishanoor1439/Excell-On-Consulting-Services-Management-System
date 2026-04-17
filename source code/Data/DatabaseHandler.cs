using Microsoft.EntityFrameworkCore;

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
                if (_instance == null)
                {
                    // Agar options null hain, toh default options create karein
                    if (options == null)
                    {
                        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                        // Aapki connection string yahan aayegi (Check appsettings.json)
                        optionsBuilder.UseSqlServer("Server=ELITEX840\\MSSQLSERVER01;Database=ExcellOnDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=true");
                        _instance = new ApplicationDbContext(optionsBuilder.Options);
                    }
                    else
                    {
                        _instance = new ApplicationDbContext(options);
                    }
                }
                return _instance;
            }
        }
    }
}