using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Microsoft.Extensions.ObjectPool;

namespace WpfEventAwaiter;

public class EventValueTaskSource<TTarget, TEventHandler, TEventArgs> : IValueTaskSource<TEventArgs>
    where TTarget : class
    where TEventHandler : Delegate
    where TEventArgs : EventArgs
{
    private readonly TEventHandler _eventHandler;
    private ManualResetValueTaskSourceCore<TEventArgs> _core;

    private TTarget? _target;
    private Action<TTarget, TEventHandler>? _removeHandler;

    private EventValueTaskSource()
    {
        _eventHandler = (TEventHandler)Delegate.CreateDelegate(typeof(TEventHandler), this, nameof(OnEvent));
    }

    public static EventValueTaskSource<TTarget, TEventHandler, TEventArgs> Create(
        TTarget target,
        Action<TTarget, TEventHandler> addHandler,
        Action<TTarget, TEventHandler> removeHandler)
    {
        var r = Pool.Get();
        r._core.Reset();
        addHandler(target, r._eventHandler);
        r._target = target;
        r._removeHandler = removeHandler;
        return r;
    }

    private void OnEvent(object? sender, TEventArgs e)
    {
        if (_target != null && _removeHandler != null)
        {
            _removeHandler(_target, _eventHandler);
            _target = null;
            _removeHandler = null;
        }

        _core.SetResult(e);
    }


    public ValueTask<TEventArgs> AsValueTask() => new ValueTask<TEventArgs>(this, _core.Version);

    TEventArgs IValueTaskSource<TEventArgs>.GetResult(short token)
    {
        try
        {
            return _core.GetResult(token);
        }
        finally
        {
            Pool.Return(this);
        }
    }

    ValueTaskSourceStatus IValueTaskSource<TEventArgs>.GetStatus(short token) => _core.GetStatus(token);

    void IValueTaskSource<TEventArgs>.OnCompleted(Action<object?> continuation, object? state, short token,
        ValueTaskSourceOnCompletedFlags flags) =>
        _core.OnCompleted(continuation, state, token, flags);


    private static readonly ObjectPool<EventValueTaskSource<TTarget, TEventHandler, TEventArgs>> Pool =
        new DefaultObjectPool<EventValueTaskSource<TTarget, TEventHandler, TEventArgs>>(new PooledObjectPolicy());

    private class PooledObjectPolicy : PooledObjectPolicy<EventValueTaskSource<TTarget, TEventHandler, TEventArgs>>
    {
        public override EventValueTaskSource<TTarget, TEventHandler, TEventArgs> Create() => new EventValueTaskSource<TTarget, TEventHandler, TEventArgs>();
        public override bool Return(EventValueTaskSource<TTarget, TEventHandler, TEventArgs> obj) => true;
    }
}
