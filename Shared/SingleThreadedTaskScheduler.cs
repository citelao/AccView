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
        /// Helper to schedule an action.
        /// </summary>
        /// <param name="action">Action to schedule</param>
        /// <returns>Task that completes when the action is finishesd</returns>
        public Task Schedule(Action action)
        {
            return Task.Factory.StartNew(action, cancellationToken, TaskCreationOptions.None, this);
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
            throw new NotImplementedException();
        }
    }
}
