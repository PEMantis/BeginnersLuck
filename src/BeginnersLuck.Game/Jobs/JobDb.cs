using System;
using System.Collections.Generic;
using BeginnersLuck.Game.Actors;

namespace BeginnersLuck.Game.Jobs;

public sealed class JobDb
{
    private readonly Dictionary<string, JobDef> _jobs = new();

    public JobDb(IEnumerable<JobDef> defs)
    {
        foreach (var d in defs)
            _jobs[d.Id] = d;
    }

    public JobDef Get(string id)
    {
        if (!_jobs.TryGetValue(id, out var job))
            throw new KeyNotFoundException($"JobDef not found: '{id}'");

        return job;
    }

    public bool TryGet(string id, out JobDef job)
        => _jobs.TryGetValue(id, out job!);

    public IEnumerable<JobDef> All => _jobs.Values;
}
