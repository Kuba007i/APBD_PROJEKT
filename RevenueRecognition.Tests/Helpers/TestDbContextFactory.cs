using Microsoft.EntityFrameworkCore;
using RevenueRecognition.Api.Data;

namespace RevenueRecognition.Tests.Helpers;

public static class TestDbContextFactory
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(
                $"RevenueRecognitionTests_{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }
}