using System.IO;
using System.Threading.Tasks;

namespace Timeless.Testing.Examples
{
    public interface IFileCopyCommand
    {
        Task Execute(string source, string target);
    }

    public class FileCopyCommand : IFileCopyCommand
    {
        public async Task Execute(string source, string target)
        {
            var fileContent = await File.ReadAllBytesAsync(source);

            await File.WriteAllBytesAsync(target, fileContent);
        }
    }

    public class FileCopyService
    {
        private readonly IFileCopyCommand cmd;

        public FileCopyService(IFileCopyCommand cmd)
        {
            this.cmd = cmd;
        }

        public async Task Copy(string source, string target) => await cmd.Execute(source, target);
    }
}
