﻿using System;
using System.Threading.Tasks;

namespace MergeNow.Core.Utils
{
    public static class TaskUtils
    {
        public static void FireAsyncCatchErrors(this Task task, Action<Exception> errorHandler)
        {
            if (task == null)
            {
                return;
            }

            try
            {
                task.ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                errorHandler?.Invoke(ex);
            }
        }

        public static T FireAsyncCatchErrors<T>(this Task<T> task, Action<Exception> errorHandler)
        {
            if (task == null)
            {
                return default;
            }

            try
            {
                return task.ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                errorHandler?.Invoke(ex);
                return default;
            }
        }
    }
}
