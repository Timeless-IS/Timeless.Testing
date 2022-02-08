using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Timeless.Testing.Examples
{
    public static class FileCopyServiceExtensions
    {
        #region Unit Tests

        public static TestBuilder GivenCommandThrows(this TestBuilder bld)
        {
            bld.Add((sp, tkn) =>
            {
                // arrange
                var mock = sp.GetRequiredService<Mock<IFileCopyCommand>>();

                mock.Setup(m => m.Execute(It.IsAny<string>(), It.IsAny<string>())).Throws<FileNotFoundException>();

                return Task.CompletedTask;
            });

            return bld;
        }

        public static TestBuilder GivenCommandReturns(this TestBuilder bld)
        {
            bld.Add((sp, tkn) =>
            {
                // arrange
                var mock = sp.GetRequiredService<Mock<IFileCopyCommand>>();

                mock.Setup(m => m.Execute(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

                return Task.CompletedTask;
            });

            return bld;
        }

        public static TestBuilder WhenServiceIsInvoked(this TestBuilder bld, string sourceFile, string targetFile)
        {
            bld.Add(async (sp, tkn) =>
            {
                // act
                var svc = sp.GetRequiredService<FileCopyService>();

                try
                {
                    await svc.Copy(sourceFile, targetFile);
                }
                catch (FileNotFoundException ex)
                {
                    ex.Should().NotBeNull();
                }
            });

            return bld;
        }

        public static TestBuilder ThenCommandIsInvoked(this TestBuilder bld)
        {
            bld.Add((sp, tkn) =>
            {
                // assert
                var mock = sp.GetRequiredService<Mock<IFileCopyCommand>>();

                mock.Verify(m => m.Execute(It.IsAny<string>(), It.IsAny<string>()), Times.Once);

                return Task.CompletedTask;
            });

            return bld;
        }

        #endregion

        #region Integration Tests

        public static TestBuilder GivenFileExists(this TestBuilder bld, string sourceFile, byte[] fileContent)
        {
            var testStep = new TestStep(async (sp, tkn) =>
            {
                await File.WriteAllBytesAsync(sourceFile, fileContent, tkn);
            });

            bld.Add(testStep);

            return bld;
        }

        public static TestBuilder ThenFileIsCreated(this TestBuilder bld, string targetFile, byte[] expectedContent)
        {
            var testStep = new TestStep(async (sp, tkn) =>
            {
                await Task.Delay(1500);

                var fileContent = await File.ReadAllBytesAsync(targetFile, tkn);

                fileContent.SequenceEqual(expectedContent).Should().BeTrue();
            });

            bld.Add(testStep);

            return bld;
        }

        public static TestBuilder GivenFileNotExists(this TestBuilder bld, string targetFile)
        {
            var testStep = new TestStep((sp, tkn) =>
            {
                if (File.Exists(targetFile))
                {
                    File.Delete(targetFile);
                }

                return Task.CompletedTask;
            });

            bld.Add(testStep);

            return bld;
        }

        public static TestBuilder GivenFolderIsEmpty(this TestBuilder bld, string folderPath)
        {
            var testStep = new TestStep((sp, tkn) =>
            {
                foreach (var f in Directory.GetFiles(folderPath))
                {
                    File.Delete(f);
                }

                return Task.CompletedTask;
            });

            bld.Add(testStep);

            return bld;
        }

        #endregion
    }
}
