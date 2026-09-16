(function () {
  if (document.getElementById('aura-demo-extension-banner')) return;
  var banner = document.createElement('div');
  banner.id = 'aura-demo-extension-banner';
  banner.textContent = 'Extensão privada AURA ativa';
  banner.style.cssText = 'position:fixed;z-index:2147483647;top:12px;right:12px;padding:10px 14px;border-radius:10px;background:#6c5ce7;color:white;font:600 14px sans-serif;box-shadow:0 4px 16px #0006';
  document.documentElement.appendChild(banner);
})();
