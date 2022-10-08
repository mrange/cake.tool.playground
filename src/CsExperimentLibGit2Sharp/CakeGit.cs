using System;
using System.IO;
using Cake.Core;
using Cake.Core.IO;
using LibGit2Sharp;

namespace Cake.Core
{
  public enum Environment
  {
  }

  public class FileSystem
  {
    public bool Exist(DirectoryPath path)
    {
      return Directory.Exists(path.Path);
    }
  }

  public class ICakeContext
  {
    public Environment Environment => new();
    public FileSystem FileSystem => new();
  }
}

namespace Cake.Core.IO
{
  public record DirectoryPath(string Path)
  {
    public DirectoryPath MakeAbsolute(Environment env)
    {
      return new DirectoryPath(System.IO.Path.GetFullPath(Path));
    }
    public string FullPath => MakeAbsolute(default).Path;
  }
}

namespace Cake.Git.Extensions
{
    internal static class RepositoryExtensions
    {
        internal static void UseRepository(this ICakeContext context, DirectoryPath repositoryPath, Action<Repository> repositoryAction)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (repositoryPath == null)
            {
                throw new ArgumentNullException(nameof(repositoryPath));
            }

            if (repositoryAction == null)
            {
                throw new ArgumentNullException(nameof(repositoryAction));
            }

            var absoluteRepositoryPath = repositoryPath.MakeAbsolute(context.Environment);

            if (!context.FileSystem.Exist(absoluteRepositoryPath))
            {
                throw new DirectoryNotFoundException($"Failed to find {nameof(repositoryPath)}: {absoluteRepositoryPath}");
            }

            using (var repository = new Repository(absoluteRepositoryPath.FullPath))
            {
                repositoryAction(repository);
            }
        }

        internal static TResult UseRepository<TResult>(this ICakeContext context, DirectoryPath repositoryPath, Func<Repository, TResult> repositoryFunc)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (repositoryPath == null)
            {
                throw new ArgumentNullException(nameof(repositoryPath));
            }

            if (repositoryFunc == null)
            {
                throw new ArgumentNullException(nameof(repositoryFunc));
            }

            var absoluteRepositoryPath = repositoryPath.MakeAbsolute(context.Environment);

            if (!context.FileSystem.Exist(absoluteRepositoryPath))
            {
                throw new DirectoryNotFoundException($"Failed to find {nameof(repositoryPath)}: {absoluteRepositoryPath}");
            }

            using (var repository = new Repository(absoluteRepositoryPath.FullPath))
            {
                return repositoryFunc(repository);
            }
        }
    }
}