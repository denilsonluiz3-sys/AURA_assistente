using System;
using AURA.Core.Security;
namespace AURA.Mobile.Platforms.Android.WebView
{
    /// <summary>
    /// Abre downloads/arquivos (ex.: um link que dispara um download) no
    /// navegador/app externo do aparelho, já que o WebView embutido não tem
    /// gerenciador próprio de downloads.
    /// </summary>
    public sealed class AuraDownloadListener : Java.Lang.Object, global::Android.Webkit.IDownloadListener
    {
        public void OnDownloadStart(
            string? url,
            string? userAgent,
            string? contentDisposition,
            string? mimeType,
            long contentLength)
        {
            if (!WebSecurityPolicy.TryValidateHttpUrl(url, out _))
            {
                AURA.Mobile.AuraLog.Info("WebView: download bloqueado por URL inválida: " + WebSecurityPolicy.RedactForLog(url));
                return;
            }

            if (contentLength > 100L * 1024 * 1024 || IsExecutableMime(mimeType))
            {
                AURA.Mobile.AuraLog.Info("WebView: download bloqueado por tipo/tamanho: " + (mimeType ?? "?") + " / " + contentLength);
                return;
            }

            try
            {
                var context = global::Android.App.Application.Context;
                var uri = global::Android.Net.Uri.Parse(url!);
                if (uri == null)
                {
                    return;
                }

                var intent = new global::Android.Content.Intent(
                    global::Android.Content.Intent.ActionView,
                    uri);
                intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
                context.StartActivity(intent);

                AURA.Mobile.AuraLog.Info("WebView: download/recurso aberto externamente: " + WebSecurityPolicy.RedactForLog(url));
            }
            catch (System.Exception ex)
            {
                AURA.Mobile.AuraLog.Exception("WebView.Download", ex);
            }
        }
        private static bool IsExecutableMime(string? mimeType) =>
            mimeType is not null && (mimeType.Contains("android.package-archive", StringComparison.OrdinalIgnoreCase) ||
            mimeType.Contains("x-msdownload", StringComparison.OrdinalIgnoreCase) ||
            mimeType.Contains("x-sh", StringComparison.OrdinalIgnoreCase) ||
            mimeType.Contains("java-archive", StringComparison.OrdinalIgnoreCase) ||
            mimeType.Contains("dex", StringComparison.OrdinalIgnoreCase));
    }
}