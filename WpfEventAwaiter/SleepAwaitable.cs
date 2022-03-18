using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace WpfEventAwaiter;

public class SleepAwaitable
{
    private ManualResetValueTaskSourceCore<int> _core;

    public SleepAwaitable(int millisec)
    {
        ThreadPool.QueueUserWorkItem(
            _ =>
            {
                Thread.Sleep(millisec);
                _core.SetResult(0);
            });
    }

    public SleepAwaiter GetAwaiter() => new SleepAwaiter(this, _core.Version);


    private bool IsCompleted { get; set; }


    public readonly struct SleepAwaiter : ICriticalNotifyCompletion
    {
        private readonly SleepAwaitable _awaitable;
        private readonly short _token;

        public SleepAwaiter(SleepAwaitable awaitable, short token)
        {
            _awaitable = awaitable;
            _token = token;
        }

        public bool IsCompleted => _awaitable._core.GetStatus(_token) == ValueTaskSourceStatus.Succeeded;

        public void GetResult()
        {
            _awaitable._core.GetResult(_token);
        }

        public void OnCompleted(Action continuation) =>
            _awaitable._core.OnCompleted(h => ((Action?)h)?.Invoke(), continuation, _token,
                ValueTaskSourceOnCompletedFlags.FlowExecutionContext |
                ValueTaskSourceOnCompletedFlags.UseSchedulingContext);

        public void UnsafeOnCompleted(Action continuation) =>
            _awaitable._core.OnCompleted(h => ((Action?)h)?.Invoke(), continuation, _token,
                ValueTaskSourceOnCompletedFlags.UseSchedulingContext);
    }
}