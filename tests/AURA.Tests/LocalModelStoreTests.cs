using System.Text;
using AURA.AI.UniversalAI;
using Xunit;

namespace AURA.Tests;

public sealed class LocalModelStoreTests
{
    [Fact]
    public async Task ImportsGgufAtomicallyAndPersistsMetadata()
    {
        string root = Path.Combine(Path.GetTempPath(), "aura-models-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new LocalModelStore(root);
            byte[] content = Encoding.UTF8.GetBytes("fake gguf payload");
            var descriptor = await store.ImportAsync(
                new MemoryStream(content),
                new LocalModelDescriptor
                {
                    Id = LocalModelStore.RequiredModelId,
                    DisplayName = "Qwen Mini",
                    FileName = LocalModelStore.RequiredFileName
                });

            Assert.Equal(LocalModelStore.RequiredModelId, descriptor.Id);
            Assert.Equal(content.Length, descriptor.SizeBytes);
            Assert.False(string.IsNullOrWhiteSpace(descriptor.Sha256));
            Assert.True(File.Exists(store.GetModelPath(LocalModelStore.RequiredModelId)));
            Assert.Single(store.List());
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task SupportsMultipleSelectableGgufModels()
    {
        string root = Path.Combine(Path.GetTempPath(), "aura-models-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new LocalModelStore(root);
            await store.ImportAsync(new MemoryStream(new byte[] { 1 }), new LocalModelDescriptor
            {
                Id = "qwen-mini", DisplayName = "Qwen Mini", FileName = "qwen-mini.gguf"
            });
            await store.ImportAsync(new MemoryStream(new byte[] { 2, 3 }), new LocalModelDescriptor
            {
                Id = "phi-mini", DisplayName = "Phi Mini", FileName = "phi-mini.gguf"
            });

            Assert.Equal(2, store.List().Count);
            Assert.True(File.Exists(store.GetModelPath("phi-mini")));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task RejectsNonGgufAndTraversalNames()
    {
        string root = Path.Combine(Path.GetTempPath(), "aura-models-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new LocalModelStore(root);
            await Assert.ThrowsAsync<InvalidDataException>(() => store.ImportAsync(
                new MemoryStream(new byte[] { 1 }),
                new LocalModelDescriptor { Id = LocalModelStore.RequiredModelId, FileName = "model.bin" }));

            await Assert.ThrowsAsync<ArgumentException>(() => store.ImportAsync(
                new MemoryStream(new byte[] { 1 }),
                new LocalModelDescriptor { Id = "../escape", FileName = LocalModelStore.RequiredFileName }));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task ReimportReplacesOnlyTheSameModel()
    {
        string root = Path.Combine(Path.GetTempPath(), "aura-models-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new LocalModelStore(root);
            await store.ImportAsync(new MemoryStream(new byte[] { 1 }), new LocalModelDescriptor { Id = LocalModelStore.RequiredModelId, FileName = LocalModelStore.RequiredFileName });
            await store.ImportAsync(new MemoryStream(new byte[] { 2, 3 }), new LocalModelDescriptor { Id = LocalModelStore.RequiredModelId, FileName = LocalModelStore.RequiredFileName });

            Assert.Single(store.List());
            Assert.Equal(2, new FileInfo(store.GetModelPath(LocalModelStore.RequiredModelId)).Length);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
