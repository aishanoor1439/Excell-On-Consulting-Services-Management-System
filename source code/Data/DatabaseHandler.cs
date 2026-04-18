using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;

namespace ExcellOnServices.Data
{
    public class DatabaseHandler
    {
        private static ApplicationDbContext _instance;
        private static readonly object _lock = new object();
        private static IConfiguration _configuration;
        private static bool _isDisposed = false;

        private DatabaseHandler() { }

        public static void Initialize(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public static ApplicationDbContext GetContext()
        {
            if (_configuration == null)
                throw new InvalidOperationException("DatabaseHandler not initialized. Call Initialize() first.");

            lock (_lock)
            {
                // If instance is disposed or null, create new one
                if (_instance == null || _isDisposed)
                {
                    var connectionString = _configuration.GetConnectionString("DefaultConnection");
                    var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                    optionsBuilder.UseSqlServer(connectionString);

                    _instance = new ApplicationDbContext(optionsBuilder.Options);
                    _isDisposed = false;
                }
                return _instance;
            }
        }

        public static void Reset()
        {
            lock (_lock)
            {
                if (_instance != null && !_isDisposed)
                {
                    _instance.Dispose();
                    _isDisposed = true;
                }
                _instance = null;
            }
        }

        // Call this if you accidentally disposed the context
        public static void MarkAsDisposed()
        {
            _isDisposed = true;
        }
    }
}