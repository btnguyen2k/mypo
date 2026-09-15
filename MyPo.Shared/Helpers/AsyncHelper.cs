using Microsoft.Extensions.Logging;
using System.Reflection;

namespace MyPo.Shared.Helpers;
public sealed class AsyncHelper
{
	/// <summary>
	/// Waits for all background tasks to complete.
	/// </summary>
	/// <param name="tasks"></param>
	/// <param name="logger"></param>
	public static async void WaitForBackgroundTasks(ICollection<Task> tasks, ILogger? logger = default)
	{
		while (tasks.Count > 0)
		{
			var finishedTask = await Task.WhenAny(tasks);
			try { await finishedTask; }
			catch (Exception e)
			{
				logger?.LogError(e, "Error executing bootstrapper task.");
			}
			tasks.Remove(finishedTask);
		}
	}

    public static async Task WaitForBackgroundTasksAsync(IEnumerable<Task> tasks, ILogger? logger = default)
    {
        var remainingTasks = tasks.ToList();
        while (remainingTasks.Count > 0)
        {
            var finishedTask = await Task.WhenAny(remainingTasks);
            remainingTasks.Remove(finishedTask);

            try
            {
                await finishedTask;
            }
            catch (OperationCanceledException ex)
            {
                logger?.LogWarning(ex, "A bootstrapper task was cancelled.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error executing bootstrapper task.");
            }
        }
    }

	public static bool IsAsyncMethod(MethodInfo method) => method.ReturnType == typeof(Task)
		|| method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>);
}
