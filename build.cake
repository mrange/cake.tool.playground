#nullable enable
#addin nuget:?package=Cake.Git&version=2.0.0

var target      = Argument("target", "GithubAction");
var repoPath    = "mrange/cake.tool.experiments.git";
var githubPat   = EnvironmentVariable("PAT_FOR_GITHUB");
var authRepoUri = $"https://{githubPat}@github.com/{repoPath}";

record BuildData(
        DirectoryPath RootPath
    ,   DirectoryPath GithubPath
    ,   DirectoryPath RepoPath
    ,   DirectoryPath ExportedModels
    ,   DirectoryPath RepoExportedModels
    );

Setup(ctx =>
    {
        var rootPath                = (DirectoryPath)ctx.Directory(".");
        var githubPath              = rootPath.Combine("github");
        var repoPath                = githubPath.Combine("repo");
        var exportedModelsPath      = rootPath.Combine("exported-models");
        var repoExportedModelsPath  = repoPath.Combine("exported-models");

        Information($"Root path                 : {rootPath}");
        Information($"Github path               : {githubPath}");
        Information($"Repo path                 : {repoPath}");
        Information($"Exported Models path      : {exportedModelsPath}");
        Information($"Repo Exported Model path  : {repoExportedModelsPath}");

        return new BuildData(
            rootPath
        ,   githubPath
        ,   repoPath
        ,   exportedModelsPath
        ,   repoExportedModelsPath
        );
    });

Task("InitRepo")
    .Does<BuildData>((ctx, bd) =>
{
    Information($"Creating if necessary {bd.GithubPath}");
    CreateDirectory(bd.GithubPath);
});

Task("CleanRepo")
    .IsDependentOn("InitRepo")
    .WithCriteria(c => HasArgument("rebuild"))
    .Does<BuildData>((ctx, bd) =>
{
    Information($"Cleaning {bd.GithubPath}");
    CleanDirectory(bd.GithubPath);
});

Task("CloneRepo")
    .IsDependentOn("CleanRepo")
    .WithCriteria<BuildData>((ctx, bd) => !DirectoryExists(bd.RepoPath))
    .Does<BuildData>((ctx, bd) =>
{
    Information($"Cloning repo {bd.RepoPath}");
    GitClone(authRepoUri, bd.RepoPath);
});

Task("UpdateRepo")
    .IsDependentOn("CloneRepo")
    .Does<BuildData>((ctx, bd) =>
{
    Information($"Creating if necessary {bd.RepoExportedModels}");
    CreateDirectory(bd.RepoExportedModels);

    Information($"Cleaning {bd.RepoExportedModels}");
    CleanDirectory(bd.RepoExportedModels);

    Information($"Copying models from {bd.ExportedModels} to {bd.RepoExportedModels}");
    CopyDirectory(bd.ExportedModels, bd.RepoExportedModels);
});

Task("PushRepo")
    .IsDependentOn("UpdateRepo")
    .Does<BuildData>((ctx, bd) =>
{
    Information($"Adding changes to repo {bd.RepoPath}");
    GitAddAll(bd.RepoPath);

    Information($"Creating commit in repo {bd.RepoPath}");
    try
    {
        var commit = GitCommit(
            bd.RepoPath
        ,   "mrange"
        ,   "marten_range@hotmailc.com"
        ,   "Automatic update of exported model"
        );
        var sha = commit.Sha;
        Information($"Commit created with SHA: {sha}");

        Information($"Changes detected... pushing repo: {bd.RepoPath}");
        GitPush(bd.RepoPath);
    }
    catch(LibGit2Sharp.EmptyCommitException)
    {
        Information($"No changes detected... skipping push to github");
    }
});

Task("GithubAction")
    .IsDependentOn("PushRepo")
    ;

RunTarget(target);