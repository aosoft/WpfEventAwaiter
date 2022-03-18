using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Sources;
using System.Windows.Media.Animation;
using Microsoft.Extensions.ObjectPool;

namespace WpfEventAwaiter;

public class TimelineCompletedValueTaskSourceAwaitable
{
    private Timeline? _timeline;
    private ManualResetValueTaskSourceCore<int> _core;

    private TimelineCompletedValueTaskSourceAwaitable()
    {
        
    }

    public static TimelineCompletedValueTaskSourceAwaitable Create(Timeline timeline)
    {
        var r = Pool.Get();
        r._core.Reset();
        r.SetTimeline(timeline);
        return r;
    }

    public TimelineCompletedAwaiter GetAwaiter() => new TimelineCompletedAwaiter(this, _core.Version);
    
    private void SetTimeline(Timeline timeline)
    {
        timeline.Completed += Timeline_OnCompleted;
        _timeline = timeline;
    }

    private void Timeline_OnCompleted(object? sender, EventArgs e)
    {
        if (_timeline != null)
        {
            _timeline.Completed -= Timeline_OnCompleted;
            _timeline = null;
        }
        
        SetResult();
    }

    private void SetResult() => _core.SetResult(0);
    
    private static readonly ObjectPool<TimelineCompletedValueTaskSourceAwaitable> Pool =
        new DefaultObjectPool<TimelineCompletedValueTaskSourceAwaitable>(new PooledObjectPolicy());

    private class PooledObjectPolicy : PooledObjectPolicy<TimelineCompletedValueTaskSourceAwaitable>
    {
        public override TimelineCompletedValueTaskSourceAwaitable Create() => new TimelineCompletedValueTaskSourceAwaitable();
        public override bool Return(TimelineCompletedValueTaskSourceAwaitable obj) => true;
    }
    
    
    public readonly struct TimelineCompletedAwaiter : ICriticalNotifyCompletion
    {
        private readonly TimelineCompletedValueTaskSourceAwaitable _awaitable;
        private readonly short _token;

        public TimelineCompletedAwaiter(TimelineCompletedValueTaskSourceAwaitable awaitable, short token)
        {
            _awaitable = awaitable;
            _token = token;
        }

        public bool IsCompleted => _awaitable._core.GetStatus(_token) == ValueTaskSourceStatus.Succeeded;

        public void GetResult()
        {
            _awaitable._core.GetResult(_token);
            Pool.Return(_awaitable);
        }

        public void OnCompleted(Action continuation) =>
            _awaitable._core.OnCompleted(static h => ((Action)h)?.Invoke(), continuation, _token,
                ValueTaskSourceOnCompletedFlags.FlowExecutionContext |
                ValueTaskSourceOnCompletedFlags.UseSchedulingContext);

        public void UnsafeOnCompleted(Action continuation) =>
            _awaitable._core.OnCompleted(static h => ((Action)h)?.Invoke(), continuation, _token,
                ValueTaskSourceOnCompletedFlags.UseSchedulingContext);
    }
}