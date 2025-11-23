using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    /// <summary>
    /// A TaskScheduler that runs tasks on a single, long-lived thread.
    /// https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskscheduler?view=net-9.0
    /// https://stackoverflow.com/questions/30719366/run-work-on-specific-thread
    /// </summary>
    public class SingleThreadedTaskScheduler : TaskScheduler
    {
        private readonly CancellationToken cancellationToken;
        private readonly BlockingCollection<Task> taskQueue = new();

        private readonly Thread thread;

        public SingleThreadedTaskScheduler(CancellationToken cancellationToken)
        {
            this.cancellationToken = cancellationToken;
            this.thread = new Thread(ThreadProc);
            this.thread.Start();
        }

        /// <summary>
        /// Signal that no more tasks will be queued.
        /// </summary>
        public void Complete()
        {
            taskQueue.CompleteAdding();
        }

        /// <summary>
        /// Helper to schedule an action. If already on the correct thread, runs inline.
        /// </summary>
        /// <param name="action">Action to schedule</param>
        /// <returns>Task that completes when the action is finishes</returns>
        public Task Schedule(Action action)
        {
            // If we're on the correct thread, just run it inline.
            if (Thread.CurrentThread == thread)
            {
                action();
                return Task.CompletedTask;
            }

            return Task.Factory.StartNew(action, cancellationToken, TaskCreationOptions.None, this);
        }

        /// <summary>
        /// Helper to schedule an action. If already on the correct thread, runs inline.
        /// </summary>
        /// <param name="action">Action to schedule</param>
        /// <returns>Task that completes when the action is finishes</returns>
        public Task<T> Schedule<T>(Func<T> func)
        {
            // If we're on the correct thread, just run it inline.
            if (Thread.CurrentThread == thread)
            {
                return Task.FromResult(func());
            }

            return Task.Factory.StartNew(func, cancellationToken, TaskCreationOptions.None, this);
        }

        /// <summary>
        /// Helper to schedule an async function. If already on the correct thread, runs inline.
        /// </summary>
        /// <param name="func">Async function to schedule</param>
        /// <returns>Task that completes when the function finishes</returns>
        public Task<T> Schedule<T>(Func<Task<T>> func)
        {
            // If we're on the correct thread, just run it inline.
            if (Thread.CurrentThread == thread)
            {
                return func();
            }

            return Task.Factory.StartNew(func, cancellationToken, TaskCreationOptions.None, this).Unwrap();
        }

        private void ThreadProc()
        {
            try
            {
                foreach (var task in taskQueue.GetConsumingEnumerable(cancellationToken))
                {
                    TryExecuteTask(task);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown.
            }
        }

        protected override IEnumerable<Task>? GetScheduledTasks()
        {
            return taskQueue.ToArray();
        }

        protected override void QueueTask(Task task)
        {
            try
            {
                taskQueue.Add(task, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown.
            }
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            // var isOnCorrectThread = Thread.CurrentThread == thread;
            return false;
        }
    }
}
