public class FolderWatcherService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private List<FileSystemWatcher> _watchers;
    private List<string> _foldersToWatch;

    public FolderWatcherService(IServiceScopeFactory serviceScopeFactory, IWebHostEnvironment webHostEnvironment)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _webHostEnvironment = webHostEnvironment;

        _watchers = [];
        _foldersToWatch = [];
        var rootPath = Path.Combine(_webHostEnvironment.ContentRootPath, Constant.RootFolder);

        _foldersToWatch.Add(Path.Combine(rootPath, Constant.OfficeFolder));
        _foldersToWatch.Add(Path.Combine(rootPath, Constant.HealthCheckFolder));
        _foldersToWatch.Add(Path.Combine(rootPath, Constant.PersonFolder));
        _foldersToWatch.Add(Path.Combine(rootPath, Constant.ResultFolder));
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var folderPath in _foldersToWatch)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            ConfigureFileSystemWatcher(folderPath);
        }
        return Task.CompletedTask;
    }

    private void ConfigureFileSystemWatcher(string folderPath)
    {
        var watcher = new FileSystemWatcher
        {
            Path = folderPath,
            EnableRaisingEvents = true,
            NotifyFilter = NotifyFilters.FileName
                        | NotifyFilters.CreationTime
                        | NotifyFilters.LastWrite
                        | NotifyFilters.Attributes
                        | NotifyFilters.DirectoryName
                        | NotifyFilters.Size,
            Filter = "*.csv",
            IncludeSubdirectories = true
        };
        watcher.Created += async (sender, e) =>
        {
            using var scope = _serviceScopeFactory.CreateScope();
            await HandleFolderEvent(folderPath, scope.ServiceProvider);
        };

        _watchers.Add(watcher);
    }

    private async ValueTask HandleFolderEvent(string folderPath, IServiceProvider serviceProvider)
    {
        switch (folderPath)
        {
            case var path when path.Contains(Constant.OfficeFolder):
                var officeService = serviceProvider.GetRequiredService<IOfficeService>();
                await officeService.ImportListOfficeAsync();
                break;
            case var path when path.Contains(Constant.HealthCheckFolder):
                var healthCheckService = serviceProvider.GetRequiredService<IHealthCheckService>();
                await healthCheckService.ImportListHeathCheckAsync();
                break;
            case var path when path.Contains(Constant.PersonFolder):
                var personService = serviceProvider.GetRequiredService<IPersonService>();
                await personService.ImportListPersonAsync();
                break;
            case var path when path.Contains(Constant.ResultFolder):
                var resultService = serviceProvider.GetRequiredService<IResultService>();
                await resultService.ImportListResultAsync();
                break;
            default:
                break;
        }
    }

    public override void Dispose()
    {
        foreach (var watcher in _watchers)
        {
            watcher.Dispose();
        }
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}