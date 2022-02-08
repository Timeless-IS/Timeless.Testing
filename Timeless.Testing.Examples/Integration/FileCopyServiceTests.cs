using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Timeless.Testing.Examples.Integration
{
    public class FileCopyServiceTests
    {
        private readonly IServiceCollection services;

        public FileCopyServiceTests()
        {
            services = new ServiceCollection()

                .AddTransient<FileCopyService>()
                .AddTransient<IFileCopyCommand, FileCopyCommand>();
        }

        [Fact]
        public async Task TestWithCancellation()
        {
            var fileContent = Encoding.UTF8.GetBytes("Hello world");

            // always dispose CancellationTokenSource(s) used for timeouts
            using var src = new CancellationTokenSource(10000);

            await new TestBuilder(services)

                .GivenFolderIsEmpty("d:\\temp")
                .GivenFileExists("d:\\temp\\source.txt", fileContent)
                .WhenServiceIsInvoked("d:\\temp\\source.txt", "d:\\temp\\target.txt")
                .ThenFileIsCreated("d:\\temp\\target.txt", fileContent)

                .Run(src.Token);
        }
    }
}
