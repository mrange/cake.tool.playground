using Cake.Core;
using Cake.Core.IO;
using Cake.Git.Extensions;

var ctx = new ICakeContext();
var repoPath = new DirectoryPath(@"C:\code\github\cake.tool.playground\github\repo");
ctx.UseRepository(repoPath, repo => 
  {
    repo.Network.Push(repo.Branches);
  });