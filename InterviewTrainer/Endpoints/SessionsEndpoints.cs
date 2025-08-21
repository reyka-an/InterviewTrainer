using InterviewTrainer.Api.Data;
using InterviewTrainer.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace InterviewTrainer.Api.Endpoints;

public static class SessionsEndpoints
{
    public static IEndpointRouteBuilder MapSessionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sessions");

        // Старт: создаём сессию, отдаём первый вопрос
        group.MapPost("start", async (AppDbContext db, SessionStore store, CancellationToken ct) =>
        {
            var ids = await db.Questions.AsNoTracking().Select(q => q.Id).ToListAsync(ct);
            if (ids.Count == 0)
                return Results.BadRequest(new { message = "Сначала добавьте вопросы." });

            var s = store.CreateSession(ids);

            var q = await db.Questions.AsNoTracking()
                .Where(x => x.Id == s.CurrentQuestionId)
                .Select(x => new QuestionView(x.Id, x.Text))
                .FirstAsync(ct);

            return Results.Ok(new StartResponse(s.Id, q, new Stats(s.Correct, s.Asked)));
        });

        // Текущий вопрос
        group.MapGet("{id:guid}/current", async (Guid id, AppDbContext db, SessionStore store, CancellationToken ct) =>
        {
            if (!store.TryGet(id, out var s) || s == null)
                return Results.NotFound(new { message = "Сессия не найдена." });

            if (s.Finished || s.CurrentQuestionId is null)
                return Results.Ok(new StepResponse(null, new Stats(s.Correct, s.Asked), true));

            var q = await db.Questions.AsNoTracking()
                .Where(x => x.Id == s.CurrentQuestionId.Value)
                .Select(x => new QuestionView(x.Id, x.Text))
                .FirstOrDefaultAsync(ct);

            return Results.Ok(new StepResponse(q, new Stats(s.Correct, s.Asked), s.Finished));
        });

        // Показать ответ к текущему вопросу
        group.MapGet("{id:guid}/current/answer", async (Guid id, AppDbContext db, SessionStore store, CancellationToken ct) =>
        {
            if (!store.TryGet(id, out var s) || s == null)
                return Results.NotFound(new { message = "Сессия не найдена." });

            if (s.CurrentQuestionId is null)
                return Results.BadRequest(new { message = "Вопросов больше нет." });

            var ans = await db.Questions.AsNoTracking()
                .Where(x => x.Id == s.CurrentQuestionId.Value)
                .Select(x => new AnswerView(x.Answer))
                .FirstOrDefaultAsync(ct);

            return ans is null ? Results.NotFound() : Results.Ok(ans);
        });

        // Ответ пользователя: правильный / неправильный. Переходим к следующему вопросу.
        group.MapPost("{id:guid}/answer", async (Guid id, AnswerRequest req, AppDbContext db, SessionStore store, CancellationToken ct) =>
        {
            if (!store.TryGet(id, out var s) || s == null)
                return Results.NotFound(new { message = "Сессия не найдена." });

            if (s.CurrentQuestionId is null)
                return Results.Ok(new StepResponse(null, new Stats(s.Correct, s.Asked), true));

            if (req.IsCorrect) s.Correct += 1;
            s.Asked += 1;

            if (s.Remaining.Count > 0)
            {
                s.CurrentQuestionId = s.Remaining.Dequeue();

                var q = await db.Questions.AsNoTracking()
                    .Where(x => x.Id == s.CurrentQuestionId.Value)
                    .Select(x => new QuestionView(x.Id, x.Text))
                    .FirstOrDefaultAsync(ct);

                return Results.Ok(new StepResponse(q, new Stats(s.Correct, s.Asked), false));
            }
            else
            {
                s.CurrentQuestionId = null;
                s.Finished = true;
                return Results.Ok(new StepResponse(null, new Stats(s.Correct, s.Asked), true));
            }
        });

        // Стоп: вернуть финальную статистику и удалить сессию
        group.MapPost("{id:guid}/stop", (Guid id, SessionStore store) =>
        {
            if (!store.TryGet(id, out var s) || s == null)
                return Results.NotFound(new { message = "Сессия не найдена." });

            var stats = new Stats(s.Correct, s.Asked);
            store.Remove(id);
            return Results.Ok(new { stats });
        });

        return app;
    }
}

public record QuestionView(int Id, string Text);
public record AnswerView(string Answer);
public record Stats(int Correct, int Asked);
public record StartResponse(Guid SessionId, QuestionView Question, Stats Stats);
public record AnswerRequest(bool IsCorrect);
public record StepResponse(QuestionView? Question, Stats Stats, bool Finished);
