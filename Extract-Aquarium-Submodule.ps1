#Requires -Version 5.1
<#
.SYNOPSIS
  Extract Aquarium from all-tasks-and-launcher into a new GitHub repository
  and replace the original folder with a Git submodule.

.PREREQUISITES
  Git with the "git subtree" command, GitHub CLI (gh), gh auth login.
  Requires permission to create repositories as the specified GitHub owner.

.EXAMPLE
  .\Extract-Aquarium-Submodule.ps1

.EXAMPLE
  .\Extract-Aquarium-Submodule.ps1 -UsePullRequest
#>

[CmdletBinding()]
param(
    [string]$Owner = 'illya228666',
    [string]$ParentRepository = 'all-tasks-and-launcher',
    [string]$AquariumRepository = 'Aquarium',
    [switch]$UsePullRequest
)

$ErrorActionPreference = 'Stop'
$SubPath = '07_Klassen_und_Objekte/Aquarium'
$ParentUrl = "https://github.com/$Owner/$ParentRepository.git"
$AquariumUrl = "https://github.com/$Owner/$AquariumRepository.git"
$TmpDir = Join-Path ([System.IO.Path]::GetTempPath()) ("aquarium-migration-" + [guid]::NewGuid().ToString('N'))

function Invoke-Checked {
    param([string]$Executable, [string[]]$CommandArguments)
    & $Executable @CommandArguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Executable $($CommandArguments -join ' ') returned exit code $LASTEXITCODE"
    }
}

try {
    foreach ($program in @('git', 'gh')) {
        if (-not (Get-Command $program -ErrorAction SilentlyContinue)) {
            throw "Required command '$program' is not installed or not on PATH."
        }
    }

    Invoke-Checked 'gh' @('auth', 'status')
    $LoggedInOwner = (& gh api user --jq '.login')
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($LoggedInOwner)) {
        throw 'Could not read the signed-in GitHub account.'
    }
    if ($LoggedInOwner.Trim() -ne $Owner) {
        throw "Signed in as '$LoggedInOwner', expected '$Owner'. Switch account with gh auth login."
    }

    # Configure Git credential helper for GitHub CLI authentication.
    Invoke-Checked 'gh' @('auth', 'setup-git')

    # Avoid accidentally overwriting an existing repository.
    $AlreadyExists = $false
    try {
        & gh repo view "$Owner/$AquariumRepository" --json name --jq '.name' *> $null
        $AlreadyExists = ($LASTEXITCODE -eq 0)
    }
    catch {
        # gh may turn an expected 404 into a terminating error in newer shells.
        $AlreadyExists = $false
    }
    if ($AlreadyExists) {
        throw "Repository $Owner/$AquariumRepository already exists. Refusing to overwrite it."
    }

    New-Item -Path $TmpDir -ItemType Directory | Out-Null
    Write-Host "Temporary workspace: $TmpDir"
    Invoke-Checked 'git' @('clone', '--branch', 'main', '--single-branch', $ParentUrl, (Join-Path $TmpDir 'parent'))
    Push-Location (Join-Path $TmpDir 'parent')
    try {
        $UserId = (& gh api user --jq '.id').Trim()
        if ($LASTEXITCODE -ne 0 -or $UserId -notmatch '^\d+$') {
            throw 'Could not read the GitHub user ID.'
        }
        Invoke-Checked 'git' @('config', 'user.name', $Owner)
        Invoke-Checked 'git' @('config', 'user.email', "$UserId+$Owner@users.noreply.github.com")

        $IsTracked = (& git ls-files -- $SubPath | Select-Object -First 1)
        if ($LASTEXITCODE -ne 0 -or -not $IsTracked) {
            throw "The source folder '$SubPath' was not found in the parent repository."
        }

        # Extract the directory as an independent Git history.
        # This also preserves the original authors/history of the folder.
        Write-Host "Extracting independent Aquarium history..."
        $AquariumSha = (& git subtree split "--prefix=$SubPath").Trim()
        if ($LASTEXITCODE -ne 0 -or $AquariumSha -notmatch '^[0-9a-f]{40}$') {
            throw 'git subtree split did not produce a valid commit.'
        }
        Write-Host "Standalone Aquarium commit: $AquariumSha"

        # Create a truly separate repository, initially empty.
        Invoke-Checked 'gh' @(
            'repo', 'create', "$Owner/$AquariumRepository",
            '--public',
            '--description', 'Standalone C# Aquarium calculator: core, console and WPF',
            '--disable-wiki'
        )

        # The very same commit becomes the submodule's pinned commit.
        Invoke-Checked 'git' @('push', $AquariumUrl, "${AquariumSha}:refs/heads/main")
        Invoke-Checked 'gh' @('repo', 'edit', "$Owner/$AquariumRepository", '--default-branch', 'main')

        # Replace the normal tracked directory with a genuine gitlink.
        if ($UsePullRequest) {
            $WorkBranch = "chore/aquarium-git-submodule"
            Invoke-Checked 'git' @('switch', '-c', $WorkBranch)
        }
        Invoke-Checked 'git' @('rm', '-r', '--', $SubPath)
        Invoke-Checked 'git' @('submodule', 'add', '-b', 'main', $AquariumUrl, $SubPath)

        $IndexEntry = (& git ls-files --stage -- $SubPath | Out-String).Trim()
        if ($IndexEntry -notmatch '^160000 [0-9a-f]{40} 0\s+') {
            throw 'The folder was not staged as a Git submodule (gitlink mode 160000).'
        }

        Invoke-Checked 'git' @('add', '--', '.gitmodules', $SubPath)
        Invoke-Checked 'git' @('commit', '-m', 'refactor: track standalone Aquarium repository as submodule')

        if ($UsePullRequest) {
            Invoke-Checked 'git' @('push', '-u', 'origin', $WorkBranch)
            Invoke-Checked 'gh' @(
                'pr', 'create',
                '--repo', "$Owner/$ParentRepository",
                '--base', 'main',
                '--head', $WorkBranch,
                '--title', 'Track Aquarium as a separate Git submodule',
                '--body', "Move Aquarium history into $Owner/$AquariumRepository and replace $SubPath with a real submodule. Clone with --recurse-submodules."
            )
        }
        else {
            # One-shot default: finish the parent repository migration.
            Invoke-Checked 'git' @('push', 'origin', 'main')
        }

        Write-Host ''
        Write-Host 'SUCCESS'
        Write-Host "Standalone repository: https://github.com/$Owner/$AquariumRepository"
        Write-Host "Parent repository:     https://github.com/$Owner/$ParentRepository"
        if ($UsePullRequest) {
            Write-Host 'Merge the newly created pull request to finish the parent migration.'
        }
        Write-Host 'For future clones: git clone --recurse-submodules <parent-repo-url>'
    }
    finally {
        Pop-Location
    }
}
catch {
    Write-Error $_
    Write-Host "If a repository has already been created, inspect it before retrying."
    Write-Host "Temporary workspace (if created): $TmpDir"
    exit 1
}
