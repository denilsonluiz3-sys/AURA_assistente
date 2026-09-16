(function () {
  if (document.getElementById('aura-reading-tools')) return;
  var toolbar = document.createElement('div');
  toolbar.id = 'aura-reading-tools';
  toolbar.innerHTML = '<strong>AURA</strong><button data-action="smaller">A−</button><button data-action="larger">A+</button><button data-action="contrast">◐</button><button data-action="focus">Foco</button><button data-action="close">×</button>';
  toolbar.style.cssText = 'position:fixed;z-index:2147483647;left:12px;bottom:12px;padding:8px;border-radius:12px;background:#171a28f2;color:#fff;font:600 13px sans-serif;box-shadow:0 3px 16px #0007';
  Array.from(toolbar.querySelectorAll('button')).forEach(function (button) {
    button.style.cssText = 'margin-left:5px;border:0;border-radius:7px;padding:6px 8px;background:#303752;color:#fff;font-weight:600';
    button.addEventListener('click', function () {
      var action = button.getAttribute('data-action');
      if (action === 'smaller') document.documentElement.style.fontSize = '90%';
      if (action === 'larger') document.documentElement.style.fontSize = '115%';
      if (action === 'contrast') document.documentElement.style.filter = document.documentElement.style.filter ? '' : 'contrast(1.25)';
      if (action === 'focus') Array.from(document.querySelectorAll('h1,h2,h3')).forEach(function (h) { h.style.outline = '2px solid #8b7cff'; });
      if (action === 'close') toolbar.remove();
    });
  });
  document.documentElement.appendChild(toolbar);
})();
