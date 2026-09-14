using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AURA.Modules.Loja;
using Xunit;

namespace AURA.Tests
{
    public class LockHelperTests
    {
        [Fact]
        public void TryAcquireLock_TimeoutExpires_ReturnsNull()
        {
            string root = Path.Combine(Path.GetTempPath(), "aura_lock_tests_" + Path.GetRandomFileName());
            Directory.CreateDirectory(root);
            try
            {
                string lockPath = Path.Combine(root, "test.lock");

                // acquire lock in background and hold it
                using (var holder = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                {
                    // Attempt to acquire with short timeout
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    var fs = LockHelper.TryAcquireLock(lockPath, TimeSpan.FromMilliseconds(200));
                    sw.Stop();

                    Assert.Null(fs);
                    Assert.True(sw.ElapsedMilliseconds >= 200);
                }
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        }

        [Fact]
        public void TryAcquireLock_WaitsAndSucceedsWhenLockReleased()
        {
            string root = Path.Combine(Path.GetTempPath(), "aura_lock_tests_" + Path.GetRandomFileName());
            Directory.CreateDirectory(root);
            try
            {
                string lockPath = Path.Combine(root, "test.lock");

                FileStream? holder = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

                // release the lock after a short delay on another thread
                Task.Run(() =>
                {
                    Thread.Sleep(150);
                    holder.Dispose();
                });

                var fs = LockHelper.TryAcquireLock(lockPath, TimeSpan.FromSeconds(2));
                try
                {
                    Assert.NotNull(fs);
                }
                finally
                {
                    fs?.Dispose();
                }
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        }
    }
}
