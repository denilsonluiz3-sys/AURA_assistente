using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace AURA.Mobile.Pages
{
    public partial class BrowserPage
    {
        public bool AutomationAvailable => _active != null;

        public Task<bool> AutomationOpenAsync(string url, CancellationToken ct = default) =>
            MainThread.InvokeOnMainThreadAsync(() =>
            {
                ct.ThrowIfCancellationRequested();
                if (!Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri) ||
                    (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                    return false;

                if (!_initialized)
                {
                    _initialized = true;
                    NewTab(HomeUrl());
                    ApplySettings();
                }

                LoadInActive(uri.ToString());
                return _active != null;
            });

        public Task<string> AutomationReadAsync(string? selector = null, CancellationToken ct = default) =>
            MainThread.InvokeOnMainThreadAsync(async () =>
            {
                ct.ThrowIfCancellationRequested();
                var view = _active?.View;
                if (view == null) return string.Empty;

                string script;
                if (string.IsNullOrWhiteSpace(selector))
                {
                    script = "document.body ? document.body.innerText : ''";
                }
                else
                {
                    string s = JsonSerializer.Serialize(selector);
                    script = $"(function(){{var e=document.querySelector({s});return e?e.innerText:'';}})()";
                }

                return await view.EvaluateJavaScriptAsync(script);
            });

        public Task<bool> AutomationClickAsync(string selector, CancellationToken ct = default) =>
            EvaluateActionAsync(selector, "click");

        public Task<bool> AutomationTypeAsync(string selector, string text, CancellationToken ct = default) =>
            MainThread.InvokeOnMainThreadAsync(async () =>
            {
                ct.ThrowIfCancellationRequested();
                var view = _active?.View;
                if (view == null || string.IsNullOrWhiteSpace(selector)) return false;
                string s = JsonSerializer.Serialize(selector);
                string value = JsonSerializer.Serialize(text ?? string.Empty);
                string script = $"(function(){{var e=document.querySelector({s});if(!e)return false;e.focus();if('value' in e)e.value={value};else e.textContent={value};e.dispatchEvent(new Event('input',{{bubbles:true}}));e.dispatchEvent(new Event('change',{{bubbles:true}}));return true;}})()";
                string result = await view.EvaluateJavaScriptAsync(script);
                return result == "true" || result == "True";
            });

        public Task<bool> AutomationScrollAsync(int pixels, CancellationToken ct = default) =>
            MainThread.InvokeOnMainThreadAsync(async () =>
            {
                ct.ThrowIfCancellationRequested();
                var view = _active?.View;
                if (view == null) return false;
                string script = $"window.scrollBy(0,{pixels}); true;";
                string result = await view.EvaluateJavaScriptAsync(script);
                return result == "true" || result == "True";
            });

        public Task<bool> AutomationBackAsync(CancellationToken ct = default) =>
            MainThread.InvokeOnMainThreadAsync(() =>
            {
                ct.ThrowIfCancellationRequested();
                if (_active?.View.CanGoBack != true) return false;
                _active.View.GoBack();
                return true;
            });

        public Task<bool> AutomationForwardAsync(CancellationToken ct = default) =>
            MainThread.InvokeOnMainThreadAsync(() =>
            {
                ct.ThrowIfCancellationRequested();
                if (_active?.View.CanGoForward != true) return false;
                _active.View.GoForward();
                return true;
            });

        public async Task<bool> AutomationWaitAsync(int milliseconds, CancellationToken ct = default)
        {
            if (milliseconds < 0 || milliseconds > 30000) return false;
            await Task.Delay(milliseconds, ct).ConfigureAwait(false);
            return true;
        }

        public Task<string?> AutomationScreenshotAsync(CancellationToken ct = default) =>
            MainThread.InvokeOnMainThreadAsync(() =>
            {
                ct.ThrowIfCancellationRequested();
#if ANDROID
                var webView = ActivePlatformView();
                if (webView == null || webView.Width <= 0 || webView.Height <= 0) return (string?)null;

                string path = Path.Combine(FileSystem.CacheDirectory, $"aura-browser-{DateTime.UtcNow:yyyyMMddHHmmssfff}.png");
                using var bitmap = global::Android.Graphics.Bitmap.CreateBitmap(webView.Width, webView.Height, global::Android.Graphics.Bitmap.Config.Argb8888);
                using var canvas = new global::Android.Graphics.Canvas(bitmap);
                webView.Draw(canvas);
                using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
                bitmap.Compress(global::Android.Graphics.Bitmap.CompressFormat.Png, 100, stream);
                return File.Exists(path) ? path : null;
#else
                return (string?)null;
#endif
            });

        private Task<bool> EvaluateActionAsync(string selector, string action) =>
            MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var view = _active?.View;
                if (view == null || string.IsNullOrWhiteSpace(selector)) return false;
                string s = JsonSerializer.Serialize(selector);
                string script = $"(function(){{var e=document.querySelector({s});if(!e)return false;e.{action}();return true;}})()";
                string result = await view.EvaluateJavaScriptAsync(script);
                return result == "true" || result == "True";
            });
    }
}
