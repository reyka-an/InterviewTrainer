using InterviewTrainer.Api.Data;
using InterviewTrainer.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InterviewTrainer.Api.Endpoints;

public static class QuestionsEndpoints
{
    public static IEndpointRouteBuilder MapQuestionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/questions");

        // Список с поиском и пагинацией
        group.MapGet("", async (
            AppDbContext db,
            CancellationToken ct,
            string? search,
            int page = 1,
            int pageSize = 20) =>
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            IQueryable<Question> query = db.Questions.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                // Ищем по подстроке в тексте вопроса
                query = query.Where(q => EF.Functions.Like(q.Text, $"%{search}%"));
            }

            var total = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(q => q.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(q => new QuestionDto(q.Id, q.Text, q.Answer, q.CreatedAt))
                .ToListAsync(ct);

            return Results.Ok(new Paged<QuestionDto>(items, total, page, pageSize));
        });

        // Получить по id
        group.MapGet("{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var q = await db.Questions.AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new QuestionDto(x.Id, x.Text, x.Answer, x.CreatedAt))
                .FirstOrDefaultAsync(ct);

            return q is null ? Results.NotFound() : Results.Ok(q);
        });

        // Создать вопрос
        group.MapPost("", async (CreateQuestionDto dto, AppDbContext db, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Text) || string.IsNullOrWhiteSpace(dto.Answer))
                return Results.BadRequest(new { message = "Поля 'text' и 'answer' обязательны." });

            var entity = new Question
            {
                Text = dto.Text.Trim(),
                Answer = dto.Answer.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            db.Questions.Add(entity);
            await db.SaveChangesAsync(ct);

            var result = new QuestionDto(entity.Id, entity.Text, entity.Answer, entity.CreatedAt);
            return Results.Created($"/api/questions/{entity.Id}", result);
        });

        // Обновить вопрос
        group.MapPut("{id:int}", async (int id, UpdateQuestionDto dto, AppDbContext db, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Text) || string.IsNullOrWhiteSpace(dto.Answer))
                return Results.BadRequest(new { message = "Поля 'text' и 'answer' обязательны." });

            var entity = await db.Questions.FindAsync(new object[] { id }, ct);
            if (entity is null) return Results.NotFound();

            entity.Text = dto.Text.Trim();
            entity.Answer = dto.Answer.Trim();

            await db.SaveChangesAsync(ct);

            var result = new QuestionDto(entity.Id, entity.Text, entity.Answer, entity.CreatedAt);
            return Results.Ok(result);
        });

        // Удалить вопрос
        group.MapDelete("{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var entity = await db.Questions.FindAsync(new object[] { id }, ct);
            if (entity is null) return Results.NotFound();

            db.Questions.Remove(entity);
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        });

        return app;
    }
}
public record CreateQuestionDto(string Text, string Answer);
public record UpdateQuestionDto(string Text, string Answer);
public record QuestionDto(int Id, string Text, string Answer, DateTime CreatedAt);
public record Paged<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
