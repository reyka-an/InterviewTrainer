using System.Collections.Concurrent;

namespace InterviewTrainer.Api.Services;

public class SessionStore
{
    private readonly ConcurrentDictionary<Guid, SessionState> _sessions = new();
    private readonly TimeSpan _ttl;

    public SessionStore(TimeSpan? ttl = null)
    {
        _ttl = ttl ?? TimeSpan.FromMinutes(30);
    }

    public SessionState CreateSession(IEnumerable<int> questionIds)
    {
        var list = questionIds.ToList();
        Shuffle(list);

        var state = new SessionState
        {
            Id = Guid.NewGuid(),
            Remaining = new Queue<int>(list),
            Correct = 0,
            Asked = 0,
            Finished = false,
            ExpiresAt = DateTimeOffset.UtcNow.Add(_ttl)
        };

        if (state.Remaining.Count > 0)
            state.CurrentQuestionId = state.Remaining.Dequeue();
        else
            state.Finished = true;

        _sessions[state.Id] = state;
        return state;
    }

    public bool TryGet(Guid id, out SessionState? state)
    {
        if (_sessions.TryGetValue(id, out var s))
        {
            if (s.ExpiresAt < DateTimeOffset.UtcNow)
            {
                _sessions.TryRemove(id, out _);
                state = null;
                return false;
            }
            s.ExpiresAt = DateTimeOffset.UtcNow.Add(_ttl);
            state = s;
            return true;
        }
        state = null;
        return false;
    }

    public bool Remove(Guid id) => _sessions.TryRemove(id, out _);

    private static void Shuffle<T>(IList<T> list)
    {
        var rng = Random.Shared;
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

public class SessionState
{
    public Guid Id { get; set; }
    public Queue<int> Remaining { get; set; } = new();
    public int? CurrentQuestionId { get; set; }
    public int Correct { get; set; }
    public int Asked { get; set; }
    public bool Finished { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
