using RevenueRecognition.Api.Domain.Enums;

namespace RevenueRecognition.Api.Domain.Entities;

public sealed class Employee
{
    public int Id { get; set; }

    public string Login { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public EmployeeRole Role { get; set; }
}