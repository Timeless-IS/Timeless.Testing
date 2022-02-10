using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Timeless.Testing
{
    public interface ITestBuilder
    {
        TestBuilder Add(TestStep testStep);
        TestBuilder Add(Func<IServiceProvider, CancellationToken, Task> testStep);
        Task Run(CancellationToken tkn = default);
    }

    public sealed class TestBuilder : ITestBuilder
    {
        // TODO: docs + should ILogger and context be params of the test step delegate?

        private readonly IServiceCollection services;
        private readonly Queue<TestStep> testSteps;

        public TestBuilder(IServiceCollection services = null)
        {
            this.testSteps = new Queue<TestStep>();
            this.services = services ?? new ServiceCollection();
        }

        public TestBuilder Add(TestStep testStep)
        {
            testSteps.Enqueue(testStep);

            return this;
        }

        public TestBuilder Add(Func<IServiceProvider, CancellationToken, Task> testStep)
        {
            testSteps.Enqueue(testStep);

            return this;
        }

        public async Task Run(CancellationToken tkn = default)
        {
            var svc = services.BuildServiceProvider();

            while (testSteps.Count > 0)
            {
                var step = testSteps.Dequeue();

                if (step != null)
                {
                    tkn.ThrowIfCancellationRequested();

                    await step.Run(svc, tkn);
                }
            }
        }
    }
}
