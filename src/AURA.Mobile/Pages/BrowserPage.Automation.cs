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
                    script = "document.body ? document.body.innerText : ''";
                else
                {
                    string s = JsonSerializer.Serialize(selector);
                    script = $"(function(){{var e=document.querySelector({s});return e?e.innerText:'';}})()";
                }

                return await view.EvaluateJavaScriptAsync(script);
            });

        /// <summary>
        /// Returns a bounded, JSON-serializable accessibility-oriented DOM tree.
        /// Each node includes a stable per-read id, tag, text, common attributes,
        /// links/buttons/inputs and child nodes. The bounded traversal prevents a
        /// large page from exhausting the agent context.
        /// </summary>
        public Task<string> AutomationReadDomAsync(string? selector = null, CancellationToken ct = default) =>
            MainThread.InvokeOnMainThreadAsync(async () =>
            {
                ct.ThrowIfCancellationRequested();
                var view = _active?.View;
                if (view == null) return "{\"ok\":false,\"error\":\"browser unavailable\"}";

                string selectorJson = JsonSerializer.Serialize(selector ?? "body");
                const int maxNodes = 250;
                const int maxText = 500;
                string script = """
(function() {
  const root = document.querySelector(__SELECTOR_JSON__);
  if (!root) return JSON.stringify({ok:false,error:'selector not found'});
  let count = 0;
  const maxNodes = __MAX_NODES__;
  const maxText = __MAX_TEXT__;
  const ids = new WeakMap();
  function text(v) { return (v || '').replace(/\\s+/g,' ').trim().slice(0,maxText); }
  function attrs(e) {
    const out = {};
    for (const a of Array.from(e.attributes || [])) {
      if (out && Object.keys(out).length >= 24) break;
      out[a.name] = (a.value || '').slice(0,500);
    }
    return out;
  }
  function node(e) {
    if (!e || count >= maxNodes) return null;
    count++;
    const id = 'dom-' + count;
    ids.set(e,id);
    const tag = (e.tagName || '').toLowerCase();
    const role = e.getAttribute('role');
    const item = {
      id:id, tag:tag, text:text(e.innerText || e.textContent),
      attributes:attrs(e)
    };
    if (role) item.role = role;
    if (e.id) item.htmlId = e.id;
    if (e.getAttribute('name')) item.name = e.getAttribute('name');
    if (e.getAttribute('aria-label')) item.label = e.getAttribute('aria-label');
    if (tag === 'a') { item.href = e.href || e.getAttribute('href') || ''; item.kind='link'; }
    if (tag === 'button' || e.getAttribute('role') === 'button') item.kind='button';
    if (['input','textarea','select'].includes(tag)) {
      item.kind='input'; item.type=e.type || tag; item.value=(e.value || '').slice(0,500);
      item.placeholder=e.getAttribute('placeholder') || '';
      item.disabled=!!e.disabled;
    }
    const children=[];
    for (const child of Array.from(e.children || [])) {
      if (count >= maxNodes) break;
      const n=node(child); if (n) children.push(n);
    }
    if (children.length) item.children=children;
    return item;
  }
  const rootNode=node(root);
  const links=Array.from(root.querySelectorAll('a')).slice(0,100).map((e,i)=>({id:ids.get(e)||('link-'+(i+1)),text:text(e.innerText),href:e.href||e.getAttribute('href')||'',label:e.getAttribute('aria-label')||''}));
  const buttons=Array.from(root.querySelectorAll('button,[role=button]')).slice(0,100).map((e,i)=>({id:ids.get(e)||('button-'+(i+1)),text:text(e.innerText),label:e.getAttribute('aria-label')||'',disabled:!!e.disabled}));
  const inputs=Array.from(root.querySelectorAll('input,textarea,select')).slice(0,100).map((e,i)=>({id:ids.get(e)||('input-'+(i+1)),tag:(e.tagName||'').toLowerCase(),type:e.type||'',name:e.getAttribute('name')||'',placeholder:e.getAttribute('placeholder')||'',value:(e.value||'').slice(0,500),label:e.getAttribute('aria-label')||'',disabled:!!e.disabled}));
  return JSON.stringify({ok:true,url:location.href,title:document.title||'',selector:__SELECTOR_JSON__,nodeCount:count,truncated:count>=maxNodes,dom:rootNode,links:links,buttons:buttons,inputs:inputs});
})()
""";
                script = script
                    .Replace("__SELECTOR_JSON__", selectorJson, StringComparison.Ordinal)
                    .Replace("__MAX_NODES__", maxNodes.ToString(), StringComparison.Ordinal)
                    .Replace("__MAX_TEXT__", maxText.ToString(), StringComparison.Ordinal);
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
