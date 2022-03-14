using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using System.Windows.Media.Animation;
using Microsoft.Extensions.ObjectPool;

namespace WpfEventAwaiter;

public class TimelineCompletedValueTaskSource : IValueTaskSource
{
    private ManualResetValueTaskSourceCore<int> _core;
    private Timeline? _timeline;

    private TimelineCompletedValueTaskSource()
    {
        
    }

    public static TimelineCompletedValueTaskSource Create(Timeline timeline)
    {
        var r = Pool.Get();
        r._core.Reset();
        r._timeline = timeline;
        timeline.Completed += r.Timeline_OnCompleted;
        return r;
    }

    private void Timeline_OnCompleted(object? sender, EventArgs e)
    {
        if (_timeline != null)
        {
            _timeline.Completed -= Timeline_OnCompleted;
            _timeline = null;
        }
        
        _core.SetResult(0);
    }
    
    
    
    public ValueTask ToValueTask() => new ValueTask(this, _core.Version);
    
    void IValueTaskSource.GetResult(short token)
    {
        try
        {
            _core.GetResult(token);
        }
        finally
        {
            Pool.Return(this);
        }
    }

    ValueTaskSourceStatus IValueTaskSource.GetStatus(short token) => _core.GetStatus(token);

    void IValueTaskSource.OnCompleted(Action<object?> continuation, object? state, short token,
        ValueTaskSourceOnCompletedFlags flags) =>
        _core.OnCompleted(continuation, state, token, flags);

    
    private static readonly ObjectPool<TimelineCompletedValueTaskSource> Pool =
        new DefaultObjectPool<TimelineCompletedValueTaskSource>(new PooledObjectPolicy());

    private class PooledObjectPolicy : PooledObjectPolicy<TimelineCompletedValueTaskSource>
    {
        public override TimelineCompletedValueTaskSource Create() => new TimelineCompletedValueTaskSource();
        public override bool Return(TimelineCompletedValueTaskSource obj) => true;
    }
}