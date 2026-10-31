using Microsoft.EntityFrameworkCore;
using PropertyHub.DataAccess.Context;

namespace PropertyHub.UnitTests.Helpers
{
    public static class DbContextFactory
    {
        public static AppDbContext Create()
        {
            var options=new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
            return new AppDbContext(options);
        }
    }
}