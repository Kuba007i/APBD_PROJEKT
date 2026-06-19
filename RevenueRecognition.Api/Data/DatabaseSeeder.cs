using Microsoft.EntityFrameworkCore;
using RevenueRecognition.Api.Domain.Entities;
using RevenueRecognition.Api.Domain.Enums;

namespace RevenueRecognition.Api.Data;

public sealed class DatabaseSeeder
{
    private readonly AppDbContext _dbContext;

    public DatabaseSeeder(
        AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SeedAsync()
    {
        var adminExists =
            await _dbContext.Employees
                .AnyAsync(employee =>
                    employee.Login == "admin");

        if (!adminExists)
        {
            _dbContext.Employees.Add(
                new Employee
                {
                    Login = "admin",
                    PasswordHash =
                        BCrypt.Net.BCrypt.HashPassword(
                            "admin123!"),
                    Role = EmployeeRole.Admin
                });
        }

        var employeeExists =
            await _dbContext.Employees
                .AnyAsync(employee =>
                    employee.Login == "employee");

        if (!employeeExists)
        {
            _dbContext.Employees.Add(
                new Employee
                {
                    Login = "employee",
                    PasswordHash =
                        BCrypt.Net.BCrypt.HashPassword(
                            "employee123!"),
                    Role = EmployeeRole.Employee
                });
        }

        if (_dbContext.ChangeTracker.HasChanges())
        {
            await _dbContext.SaveChangesAsync();
        }
    }
}