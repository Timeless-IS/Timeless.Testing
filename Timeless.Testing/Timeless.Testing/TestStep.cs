using System;
using System.Threading;
using System.Threading.Tasks;

namespace Timeless.Testing
{
    public class TestStep
    {
        public TestStep(Func<IServiceProvider, CancellationToken, Task> run)
        {
            Run = run ?? throw new ArgumentNullException("Test step delegate cannot be null");
        }

        public Func<IServiceProvider, CancellationToken, Task> Run { get; }

        public static implicit operator TestStep(Func<IServiceProvider, CancellationToken, Task> value)
        {
            return new TestStep(value);
        }
    }
}
