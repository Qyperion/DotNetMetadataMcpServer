using DotNetMetadataMcpServer;

namespace MetadataExplorerTest;

[TestFixture]
public class ProjectMetadataCacheTests
{
    private ProjectMetadataCache _cache;

    [SetUp]
    public void Setup()
    {
        _cache = new ProjectMetadataCache();
    }

    [Test]
    public void GetOrAdd_CallsFactoryOnce_ReturnsCachedOnSecondCall()
    {
        var callCount = 0;
        var metadata = new ProjectMetadata();

        ProjectMetadata Factory(string path)
        {
            Interlocked.Increment(ref callCount);
            return metadata;
        }

        var result1 = _cache.GetOrAdd("C:\\test\\project.csproj", Factory);
        var result2 = _cache.GetOrAdd("C:\\test\\project.csproj", Factory);

        Assert.That(result1, Is.SameAs(metadata));
        Assert.That(result2, Is.SameAs(metadata));
        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public void GetOrAdd_NormalizesPath_CaseInsensitive()
    {
        var callCount = 0;
        var metadata = new ProjectMetadata();

        ProjectMetadata Factory(string path)
        {
            Interlocked.Increment(ref callCount);
            return metadata;
        }

        var result1 = _cache.GetOrAdd("C:\\Test\\Project.csproj", Factory);
        var result2 = _cache.GetOrAdd("C:\\test\\project.csproj", Factory);

        Assert.That(result1, Is.SameAs(result2));
        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public void Invalidate_RemovesEntry_FactoryCalledAgain()
    {
        var callCount = 0;
        const string path = "C:\\test\\project.csproj";

        ProjectMetadata Factory(string p)
        {
            Interlocked.Increment(ref callCount);
            return new ProjectMetadata();
        }

        var result1 = _cache.GetOrAdd(path, Factory);
        Assert.That(callCount, Is.EqualTo(1));

        var invalidated = _cache.Invalidate(path);
        Assert.That(invalidated, Is.True);

        var result2 = _cache.GetOrAdd(path, Factory);
        Assert.That(callCount, Is.EqualTo(2));
        Assert.That(result2, Is.Not.SameAs(result1));
    }

    [Test]
    public void Invalidate_NonExistentPath_ReturnsFalse()
    {
        var result = _cache.Invalidate("C:\\nonexistent\\project.csproj");
        Assert.That(result, Is.False);
    }

    [Test]
    public void InvalidateAll_ClearsAllEntries()
    {
        var metadata1 = new ProjectMetadata();
        var metadata2 = new ProjectMetadata();

        _cache.GetOrAdd("C:\\test\\project1.csproj", _ => metadata1);
        _cache.GetOrAdd("C:\\test\\project2.csproj", _ => metadata2);

        _cache.InvalidateAll();

        var callCount = 0;
        _cache.GetOrAdd("C:\\test\\project1.csproj", _ =>
        {
            Interlocked.Increment(ref callCount);
            return new ProjectMetadata();
        });

        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public void GetOrAdd_ConcurrentAccess_FactoryCalledOnce()
    {
        var callCount = 0;
        const string path = "C:\\test\\project.csproj";
        var barrier = new Barrier(10);

        ProjectMetadata Factory(string p)
        {
            Interlocked.Increment(ref callCount);
            Thread.Sleep(50); // Simulate work
            return new ProjectMetadata();
        }

        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(() =>
        {
            barrier.SignalAndWait();
            return _cache.GetOrAdd(path, Factory);
        })).ToArray();

        Task.WaitAll(tasks);

        Assert.That(callCount, Is.EqualTo(1));

        var firstResult = tasks[0].Result;
        Assert.That(tasks.Select(t => t.Result), Is.All.SameAs(firstResult));
    }
}
