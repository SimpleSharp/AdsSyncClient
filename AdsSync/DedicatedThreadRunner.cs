using System.Collections.Concurrent;

namespace AdsSync
{
    /// <summary>
    /// Provides a dedicated thread for sequential execution of actions.
    /// </summary>
    /// <remarks>
    /// This class creates and manages a dedicated background thread on which all queued actions are executed
    /// sequentially. On Windows, the thread is configured as a single-threaded apartment (STA) thread.
    ///
    /// Use <see cref="InvokeAsync(Action)"/> when an action must be executed on the dedicated thread and the 
    /// caller needs to wait for its completion or handle exceptions raised by the action. The returned 
    /// <see cref="Task"/> completes after the action has finished.
    ///
    /// Use <see cref="BeginInvoke(Action)"/> when an action should be queued for execution without waiting for
    /// its completion. Exceptions raised by an action submitted with this method are not propagated to the caller.
    ///
    /// The queued actions are processed one after another and are never executed in parallel. This makes the 
    /// class suitable for thread-bound components or APIs that must always be accessed from the same thread,
    /// such as certain COM, ActiveX, or other STA-dependent components.
    ///
    /// The dedicated thread is started when an instance of this class is created. Call <see cref="Dispose"/> 
    /// when the instance is no longer needed. 
    /// </remarks>
    public class DedicatedThreadRunner : IDisposable
    {
        #region  private fields
        /// <summary> The dedicated thread used to process queued operations </summary>
        private readonly Thread thread;
        /// <summary> The queue containing the operations to be executed by the dedicated thread </summary>
        private readonly BlockingCollection<Func<Task>> queue = [];
        /// <summary> The cancellation token source used to stop the message loop </summary>
        private readonly CancellationTokenSource cts = new();
        /// <summary> Indicates whether the object has been disposed </summary>
        private bool disposed;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of this classes and starts the dedicated thread.
        /// </summary>
        public DedicatedThreadRunner()
        {
            thread = new Thread(RunMessageLoop)
            {
                IsBackground = true,
                Name = "Dedicated STA Runner",
            };
            if (OperatingSystem.IsWindows())
            {
                thread.SetApartmentState(ApartmentState.STA);
            }
            thread.Start();
        }
        #endregion

        #region public methods and tasks
        /// <summary>
        /// Queues an action for execution on the dedicated thread and returns a task that completes when finished.
        /// </summary>
        /// <param name="action"> The action to execute on the dedicated thread </param>
        /// <returns> A task representing the asynchronous completion of the action. </returns>
        /// <exception cref="ArgumentNullException"> Thrown when <paramref name="action"/> is null. </exception>
        public Task InvokeAsync(Action action)
        {
            ArgumentNullException.ThrowIfNull(action);
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            queue.Add(() =>
            {
                try
                {
                    action();
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }

                return Task.CompletedTask;
            }, cts.Token);

            return tcs.Task;
        }

        /// <summary>
        /// Queues a synchronous action for execution on the dedicated thread
        /// without waiting for it to complete.
        /// </summary>
        /// <param name="work"> The action to queue for execution. </param>
        /// <exception cref="ArgumentNullException"> Thrown when <paramref name="work"/> is <see langword="null"/>. </exception>
        public void BeginInvoke(Action work)
        {
            ArgumentNullException.ThrowIfNull(work);
            _ = InvokeAsync(() =>
            {
                work();
                return Task.CompletedTask;
            });
        }

        /// <summary>
        /// Releases all resources used by this instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region private methods and tasks
        /// <summary>
        /// Releases all resources used by this instance.
        /// </summary>
        /// <param name="disposing"> Disposes this istance if TRUE </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }
            if (disposing)
            {
                cts.Cancel();
                queue.CompleteAdding();
                if (!thread.Join(TimeSpan.FromSeconds(5)))
                {
                    thread.Interrupt();
                }
                cts.Dispose();
                queue.Dispose();
            }
            disposed = true;
        }

        /// <summary>
        /// Queues a synchronous function for execution on the dedicated thread and returns a task that completes with the function's result.
        /// </summary>
        /// <typeparam name="TResult"> The type of the result returned by the function </typeparam>
        /// <param name="func"> The function to execute on the dedicated thread </param>
        /// <returns> A task representing the asynchronous execution of the function. </returns>
        /// <exception cref="ArgumentNullException"> Thrown when <paramref name="func"/> is <see langword="null"/>. </exception>
        private async Task<TResult> InvokeAsync<TResult>(Func<TResult> func)
        {
            ArgumentNullException.ThrowIfNull(func);
            TResult result = default!;
            await InvokeAsync(() =>
            {
                result = func();
            }).ConfigureAwait(false);
            return result;
        }

        /// <summary>
        /// Runs the message loop that processes queued operations sequentially.
        /// </summary>
        private void RunMessageLoop()
        {
            try
            {
                foreach (var workItem in queue.GetConsumingEnumerable(cts.Token))
                {
                    var task = workItem();
                    task.GetAwaiter().GetResult();
                }
            }
            catch (OperationCanceledException)
            {
                //this excpetion occurs when stopping the synchronisation
            }
        }
        #endregion
    }
}