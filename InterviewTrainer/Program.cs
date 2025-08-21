using Microsoft.EntityFrameworkCore;
using InterviewTrainer.Api.Data;
using InterviewTrainer.Api.Endpoints;
using InterviewTrainer.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<SessionStore>();

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=app.db"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapQuestionEndpoints();
app.MapSessionEndpoints();
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();
