#nullable enable
#addin nuget:?package=Cake.Git&version=2.0.0

const string EnvKey     = "PAT_FOR_GITHUB";
const string RepoPath   = "mrange/cake.tool.experiments.git";

// The github Personal Access Token (PAT)
// Careful not to log this
var githubPat   = EnvironmentVariable(EnvKey);
var target      = Argument("target", "GithubAction");

var repoUri     = $"https://github.com/{RepoPath}";
// Careful not to log this
var authRepoUri = $"https://{githubPat}@github.com/{RepoPath}";

record BuildData(
        DirectoryPath RootPath
    ,   DirectoryPath GithubPath
    ,   DirectoryPath RepoPath
    ,   DirectoryPath ExportedModels
    ,   DirectoryPath RepoExportedModels
    );

Setup(ctx =>
    {
        if (string.IsNullOrWhiteSpace(githubPat))
        {
            throw new Exception($"Environment variable {EnvKey} must configured a personal access token that change do changes to: {RepoPath}");
        }
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
    Information($"Cloning repo {repoUri} into {bd.RepoPath}");
    GitClone(repoUri, bd.RepoPath);
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

    if (GitHasUncommitedChanges(bd.RepoPath))
    {
        Information($"Creating commit in repo {bd.RepoPath}");
        var commit = GitCommit(
            bd.RepoPath
        ,   "mrange"
        ,   "marten_range@hotmail.com"
        ,   "Automatic update of exported model"
        );
        var sha = commit.Sha;
        Information($"Commit created with SHA: {sha}");

        // GitPush don't work with Github PAT, so invoke git manually
        Information($"Changes detected... pushing repo: {bd.RepoPath} to {repoUri}");
        var ec = StartProcess(
            "git"
        ,   new ProcessSettings()
            {
                Arguments           = $"push {authRepoUri}"
            ,   WorkingDirectory    = bd.RepoPath
            });
        if (ec != 0)
        {
            throw new Exception($"Push repo: {bd.RepoPath} to {repoUri} failed with: {ec}");
        }
    }
    else
    {
        Information($"No changes detected... skipping push to github");
    }
});

Task("GithubAction")
    .IsDependentOn("PushRepo")
    ;

RunTarget(target);