using InterviewTrainer.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InterviewTrainer.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Question> Questions => Set<Question>();
}