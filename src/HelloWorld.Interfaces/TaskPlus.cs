using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HelloWorld.Interfaces
{
    public static class TaskPlus
    {
        public static async Task<IEnumerable<Task>> ForEachAsync<TIn>(
            this IEnumerable<TIn> inputEnumerable,
            Func<TIn, Task> asyncProcessor,
            int maxDegreeOfParallelism)
        {
            var throttler = new SemaphoreSlim(maxDegreeOfParallelism, maxDegreeOfParallelism);

            var tasks = inputEnumerable.Select(async input =>
            {
                await throttler.WaitAsync();

                try
                {
                    await asyncProcessor(input);
                }
                finally
                {
                    throttler.Release();
                }

            }).ToArray();

            try
            {
                await Task.WhenAll(tasks);
            }
            catch 
            {
                // we are not interested in a single exception
                // which will be randomly chosen by Task.WhenAll
                // instead an array of all tasks with exceptions / results
                // is returned to the caller
            }

            return tasks;
        }
    }
}