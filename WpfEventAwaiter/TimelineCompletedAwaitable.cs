using System;
using System.Runtime.CompilerServices;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Extensions.ObjectPool;

namespace WpfEventAwaiter;

public class TimelineCompletedAwaitable
{
    private Action? _continuation;
    private Timeline? _timeline;

    private TimelineCompletedAwaitable()
    {
        
    }

    public static TimelineCompletedAwaitable Create(Timeline timeline)
    {
        var r = Pool.Get();
        r.SetTimeline(timeline);
        return r;
    }

    public TimelineCompletedAwaiter GetAwaiter() => new TimelineCompletedAwaiter(this);
    
    private void SetTimeline(Timeline timeline)
    {
        _continuation = null;
        IsCompleted = false;
        
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
    
    private void SetContinuation(Action continuation) => _continuation = continuation;
    
    private bool IsCompleted { get; set; }

    private void SetResult()
    {
        IsCompleted = true;
        _continuation?.Invoke();
        _continuation = null;
    }
    
    private static readonly ObjectPool<TimelineCompletedAwaitable> Pool =
        new DefaultObjectPool<TimelineCompletedAwaitable>(new PooledObjectPolicy());

    private class PooledObjectPolicy : PooledObjectPolicy<TimelineCompletedAwaitable>
    {
        public override TimelineCompletedAwaitable Create() => new TimelineCompletedAwaitable();
        public override bool Return(TimelineCompletedAwaitable obj) => true;
    }
    
    
    public readonly struct TimelineCompletedAwaiter : ICriticalNotifyCompletion
    {
        private readonly TimelineCompletedAwaitable _awaitable;

        public TimelineCompletedAwaiter(TimelineCompletedAwaitable awaitable)
        {
            _awaitable = awaitable;
        }

        public bool IsCompleted => _awaitable.IsCompleted;

        public void GetResult()
        {
            Pool.Return(_awaitable);
        }
        
        public void OnCompleted(Action continuation) => UnsafeOnCompleted(continuation);

        public void UnsafeOnCompleted(Action continuation)
        {
            if (IsCompleted)
            {
                continuation();
            }
            else
            {
                _awaitable.SetContinuation(continuation);
            }
        }
    }
}

