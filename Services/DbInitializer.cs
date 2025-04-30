using Microsoft.EntityFrameworkCore;
using Zoom.Models;
using Zoom.Services.Interfaces;

namespace Zoom.Services
{
    public class DbInitializer : IDbInitializer
    {
        private readonly ZoomContext _context;

        public DbInitializer(ZoomContext context)
        {
            _context = context;
        }

        public void Initialize()
        {
            // Ini akan buat database kalau belum ada, dan update migration kalau belum up-to-date
            _context.Database.Migrate();
        }
    }
}
