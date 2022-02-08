using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Timeless.Testing.Examples.Unit
{
    public class FileCopyServiceTests
    {
        private readonly IServiceCollection services;

        public FileCopyServiceTests()
        {
            services = new ServiceCollection()

                .AddTransient<FileCopyService>()

                .AddSingleton<Mock<IFileCopyCommand>>()
                .AddTransient<IFileCopyCommand>(sp =>
                {
                    var mock = sp.GetRequiredService<Mock<IFileCopyCommand>>();

                    return mock.Object;
                });
        }

        [Fact]
        public async Task SimpleTestWithMock()
        {
            var bld = new TestBuilder(services)

                .Add((sp, tkn) =>
                {
                    // arrange
                    var mock = sp.GetRequiredService<Mock<IFileCopyCommand>>();

                    mock.Setup(m => m.Execute(It.IsAny<string>(), It.IsAny<string>())).Throws<FileNotFoundException>();

                    return Task.CompletedTask;
                })
                .Add(async (sp, tkn) =>
                {
                    // act
                    var svc = sp.GetRequiredService<FileCopyService>();

                    try
                    {
                        await svc.Copy("source.txt", "target.txt");
                    }
                    catch (FileNotFoundException ex)
                    {
                        ex.Should().NotBeNull();
                    }
                })
                .Add((sp, tkn) =>
                {
                    // assert
                    var mock = sp.GetRequiredService<Mock<IFileCopyCommand>>();

                    mock.Verify(m => m.Execute(It.IsAny<string>(), It.IsAny<string>()), Times.Once);

                    return Task.CompletedTask;
                });

            await bld.Run();
        }

        [Fact]
        public async Task FluentTestWithMock()
        {
            await new TestBuilder(services)

                .GivenCommandThrows()
                .WhenServiceIsInvoked("source.txt", "target.txt")
                .ThenCommandIsInvoked()

                .Run();
        }
    }
}
