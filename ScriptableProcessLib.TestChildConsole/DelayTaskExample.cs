using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ScriptableProcessLib.TestChildConsole
{
    public class DelayTaskExample
    {

        public static int Run(string[] args)
        {
            var cancel_source = new CancellationTokenSource();
            var task_completion_source = new TaskCompletionSource<int>();
            cancel_source.Token.Register(() => task_completion_source.SetResult(0));
            cancel_source.CancelAfter(TimeSpan.FromSeconds(1));

            var wait_task = task_completion_source.Task;
            wait_task.Wait();

            return 0;
        }
    }
}
