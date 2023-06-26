using System;

namespace IIS.FlexberrySampleLogging
{
    public class NoopDisposable : IDisposable
    {
        public static NoopDisposable Instance = new NoopDisposable();

        public void Dispose()
        {
        }
    }
}
