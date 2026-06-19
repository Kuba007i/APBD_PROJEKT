using Microsoft.EntityFrameworkCore;
using RevenueRecognition.Api.Domain.Entities;
using RevenueRecognition.Api.Domain.Enums;

namespace RevenueRecognition.Api.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Client> Clients => Set<Client>();

    public DbSet<IndividualClient> IndividualClients =>
        Set<IndividualClient>();

    public DbSet<CompanyClient> CompanyClients =>
        Set<CompanyClient>();
    
    public DbSet<Software> SoftwareProducts => 
        Set<Software>();

    public DbSet<Discount> Discounts => 
        Set<Discount>();
    
    public DbSet<Contract> Contracts => Set<Contract>();

    public DbSet<ContractPayment> ContractPayments => Set<ContractPayment>();
    
    public DbSet<Employee> Employees => Set<Employee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureClients(modelBuilder);
        ConfigureSoftware(modelBuilder);
        ConfigureDiscounts(modelBuilder);
        ConfigureContracts(modelBuilder);
        ConfigureContractPayments(modelBuilder);
        ConfigureEmployees(modelBuilder);
        
        SeedData(modelBuilder);
    }

    private static void ConfigureClients(ModelBuilder modelBuilder)
    {
        var client = modelBuilder.Entity<Client>();

        client.ToTable("Clients");

        client.HasKey(x => x.Id);

        client.Property(x => x.Address)
            .IsRequired()
            .HasMaxLength(200);

        client.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(150);

        client.Property(x => x.PhoneNumber)
            .IsRequired()
            .HasMaxLength(30);

        client.Property(x => x.IsDeleted)
            .HasDefaultValue(false);

        client.HasDiscriminator<string>("ClientType")
            .HasValue<IndividualClient>("Individual")
            .HasValue<CompanyClient>("Company");

        var individual = modelBuilder.Entity<IndividualClient>();

        individual.Property(x => x.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        individual.Property(x => x.LastName)
            .IsRequired()
            .HasMaxLength(100);

        individual.Property(x => x.Pesel)
            .IsRequired()
            .HasMaxLength(50);

        individual.HasIndex(x => x.Pesel)
            .IsUnique()
            .HasFilter("[Pesel] IS NOT NULL");

        var company = modelBuilder.Entity<CompanyClient>();

        company.Property(x => x.CompanyName)
            .IsRequired()
            .HasMaxLength(200);

        company.Property(x => x.Krs)
            .IsRequired()
            .HasMaxLength(20);

        company.HasIndex(x => x.Krs)
            .IsUnique()
            .HasFilter("[Krs] IS NOT NULL");
    }
    
    private static void ConfigureSoftware(ModelBuilder modelBuilder)
    {
        var software = modelBuilder.Entity<Software>();

        software.ToTable("SoftwareProducts");

        software.HasKey(x => x.Id);

        software.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(150);

        software.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(1000);

        software.Property(x => x.CurrentVersion)
            .IsRequired()
            .HasMaxLength(50);

        software.Property(x => x.Category)
            .IsRequired()
            .HasMaxLength(100);

        software.Property(x => x.YearlyLicensePrice)
            .HasPrecision(18, 2);

        software.HasIndex(x => x.Name);
    }
    
    private static void ConfigureDiscounts(ModelBuilder modelBuilder)
    {
        var discount = modelBuilder.Entity<Discount>();

        discount.ToTable("Discounts");

        discount.HasKey(x => x.Id);

        discount.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(150);

        discount.Property(x => x.Percentage)
            .HasPrecision(5, 2);

        discount.Property(x => x.ValidFrom)
            .IsRequired();

        discount.Property(x => x.ValidTo)
            .IsRequired();

        discount.Property(x => x.Target)
            .HasConversion<string>()
            .HasMaxLength(30);

        discount.HasOne(x => x.Software)
            .WithMany(x => x.Discounts)
            .HasForeignKey(x => x.SoftwareId)
            .OnDelete(DeleteBehavior.Restrict);
    }
    
    private static void ConfigureContracts(ModelBuilder modelBuilder)
    {
        var contract = modelBuilder.Entity<Contract>();

        contract.ToTable("Contracts", table =>
        {
            table.HasCheckConstraint(
                "CK_Contracts_Dates",
                "[EndDate] > [StartDate]");

            table.HasCheckConstraint(
                "CK_Contracts_AdditionalSupportYears",
                "[AdditionalSupportYears] BETWEEN 0 AND 3");

            table.HasCheckConstraint(
                "CK_Contracts_BasePrice",
                "[BasePrice] >= 0");

            table.HasCheckConstraint(
                "CK_Contracts_SupportCost",
                "[SupportCost] >= 0");

            table.HasCheckConstraint(
                "CK_Contracts_FinalPrice",
                "[FinalPrice] >= 0");

            table.HasCheckConstraint(
                "CK_Contracts_ProductDiscount",
                "[ProductDiscountPercentage] BETWEEN 0 AND 100");

            table.HasCheckConstraint(
                "CK_Contracts_ReturningCustomerDiscount",
                "[ReturningCustomerDiscountPercentage] BETWEEN 0 AND 100");
        });

        contract.HasKey(x => x.Id);

        contract.Property(x => x.SoftwareVersion)
            .IsRequired()
            .HasMaxLength(50);

        contract.Property(x => x.CreatedAt)
            .IsRequired();

        contract.Property(x => x.StartDate)
            .IsRequired();

        contract.Property(x => x.EndDate)
            .IsRequired();

        contract.Property(x => x.BasePrice)
            .HasPrecision(18, 2);

        contract.Property(x => x.SupportCost)
            .HasPrecision(18, 2);

        contract.Property(x => x.ProductDiscountPercentage)
            .HasPrecision(5, 2);

        contract.Property(x => x.ReturningCustomerDiscountPercentage)
            .HasPrecision(5, 2);

        contract.Property(x => x.FinalPrice)
            .HasPrecision(18, 2);

        contract.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        contract.Property(x => x.IsDeleted)
            .HasDefaultValue(false);

        contract.HasOne(x => x.Client)
            .WithMany(x => x.Contracts)
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        contract.HasOne(x => x.Software)
            .WithMany(x => x.Contracts)
            .HasForeignKey(x => x.SoftwareId)
            .OnDelete(DeleteBehavior.Restrict);

        contract.HasIndex(x => new
        {
            x.ClientId,
            x.SoftwareId,
            x.Status
        });
    }
    private static void ConfigureContractPayments(ModelBuilder modelBuilder)
    {
        var payment = modelBuilder.Entity<ContractPayment>();

        payment.ToTable("ContractPayments", table =>
        {
            table.HasCheckConstraint(
                "CK_ContractPayments_Amount",
                "[Amount] > 0");
        });

        payment.HasKey(x => x.Id);

        payment.Property(x => x.Amount)
            .HasPrecision(18, 2);

        payment.Property(x => x.PaidAt)
            .IsRequired();

        payment.Property(x => x.IsRefunded)
            .HasDefaultValue(false);

        payment.HasOne(x => x.Contract)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.ContractId)
            .OnDelete(DeleteBehavior.Restrict);

        payment.HasOne(x => x.Client)
            .WithMany(x => x.ContractPayments)
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        payment.HasIndex(x => x.ContractId);

        payment.HasIndex(x => x.ClientId);
    }
    
    private static void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Software>().HasData(
            new Software
            {
                Id = 1,
                Name = "FinTrack",
                Description = "System do zarządzania finansami przedsiębiorstwa.",
                CurrentVersion = "2.1",
                Category = "Finanse",
                YearlyLicensePrice = 12000m
            },
            new Software
            {
                Id = 2,
                Name = "EduManager",
                Description = "System do zarządzania placówką edukacyjną.",
                CurrentVersion = "1.5",
                Category = "Edukacja",
                YearlyLicensePrice = 8000m
            });

        modelBuilder.Entity<Discount>().HasData(
            new Discount
            {
                Id = 1,
                Name = "FinTrack License Promotion",
                Percentage = 10m,
                ValidFrom = new DateTime(
                    2025, 1, 1, 0, 0, 0,
                    DateTimeKind.Utc),
                ValidTo = new DateTime(
                    2030, 12, 31, 23, 59, 59,
                    DateTimeKind.Utc),
                Target = DiscountTarget.License,
                SoftwareId = 1
            },
            new Discount
            {
                Id = 2,
                Name = "FinTrack Premium Promotion",
                Percentage = 15m,
                ValidFrom = new DateTime(
                    2025, 1, 1, 0, 0, 0,
                    DateTimeKind.Utc),
                ValidTo = new DateTime(
                    2030, 12, 31, 23, 59, 59,
                    DateTimeKind.Utc),
                Target = DiscountTarget.License,
                SoftwareId = 1
            },
            new Discount
            {
                Id = 3,
                Name = "EduManager Subscription Promotion",
                Percentage = 8m,
                ValidFrom = new DateTime(
                    2025, 1, 1, 0, 0, 0,
                    DateTimeKind.Utc),
                ValidTo = new DateTime(
                    2030, 12, 31, 23, 59, 59,
                    DateTimeKind.Utc),
                Target = DiscountTarget.Subscription,
                SoftwareId = 2
            });
    }
    
    private static void ConfigureEmployees(ModelBuilder modelBuilder)
    {
        var employee = modelBuilder.Entity<Employee>();

        employee.ToTable("Employees");

        employee.HasKey(x => x.Id);

        employee.Property(x => x.Login)
            .IsRequired()
            .HasMaxLength(100);

        employee.Property(x => x.PasswordHash)
            .IsRequired()
            .HasMaxLength(200);

        employee.Property(x => x.Role)
            .HasConversion<string>()
            .HasMaxLength(30);

        employee.HasIndex(x => x.Login)
            .IsUnique();
    }
}