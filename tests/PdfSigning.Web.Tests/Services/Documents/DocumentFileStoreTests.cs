using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using PdfSigning.Web.Services.Documents;

namespace PdfSigning.Web.Tests.Services.Documents;

public class DocumentFileStoreTests
{
    [Fact]
    public async Task SaveAsync_writes_private_files_outside_webroot_and_opens_them_back()
    {
        var root = CreateTempDirectory();
        var contentRoot = Path.Combine(root, "content");
        var webRoot = Path.Combine(root, "wwwroot");
        Directory.CreateDirectory(contentRoot);
        Directory.CreateDirectory(webRoot);

        var store = new DocumentFileStore(new FakeEnvironment(contentRoot, webRoot));
        var storageKey = store.CreateStorageKey("invoice.txt");

        await using (var source = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("secret pdf bytes")))
        {
            await store.SaveAsync(storageKey, source);
        }

        Assert.StartsWith("documents/", storageKey);
        Assert.EndsWith(".pdf", storageKey);
        Assert.True(File.Exists(Path.Combine(contentRoot, "App_Data", storageKey.Replace('/', Path.DirectorySeparatorChar))));
        Assert.False(File.Exists(Path.Combine(webRoot, storageKey.Replace('/', Path.DirectorySeparatorChar))));

        await using var opened = await store.OpenReadAsync(storageKey);
        Assert.NotNull(opened);
        using var reader = new StreamReader(opened!);
        Assert.Equal("secret pdf bytes", await reader.ReadToEndAsync());
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "pdfsigning-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class FakeEnvironment : IWebHostEnvironment
    {
        public FakeEnvironment(string contentRootPath, string webRootPath)
        {
            ContentRootPath = contentRootPath;
            WebRootPath = webRootPath;
        }

        public string ApplicationName { get; set; } = "PdfSigning.Web.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; }
        public string WebRootPath { get; set; }
        public string EnvironmentName { get; set; } = Environments.Development;
    }
}
