(function () {
  if (document.getElementById('aura-page-context')) return;
  var headings = document.querySelectorAll('h1,h2,h3').length;
  var links = document.links.length;
  var text = (document.body && document.body.innerText || '').trim().length;
  var panel = document.createElement('aside');
  panel.id = 'aura-page-context';
  panel.innerHTML = '<strong>AURA • Contexto</strong><br><span>Título: ' + (document.title || 'sem título').replace(/[<>]/g, '') + '</span><br><span>Seções: ' + headings + ' • Links: ' + links + ' • Texto: ' + text + ' caracteres</span>';
  panel.style.cssText = 'position:fixed;z-index:2147483646;right:12px;bottom:64px;max-width:300px;padding:12px 14px;border:1px solid #6c5ce7;border-radius:12px;background:#171528ee;color:#f4f1ff;font:13px/1.45 sans-serif;box-shadow:0 4px 18px #0007';
  document.documentElement.appendChild(panel);
})();
