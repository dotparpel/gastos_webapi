using System.Diagnostics;

namespace webapi.Extensions;

public static class Reflection
{
    public static string? MethodName(int frame = 1)
        {
            StackTrace stackTrace = new();

            if (frame > stackTrace.FrameCount)
                frame = stackTrace.FrameCount - 1;

            string? ret = stackTrace.GetFrame(frame)?.GetMethod()?.Name;

            return ret;
        }
}