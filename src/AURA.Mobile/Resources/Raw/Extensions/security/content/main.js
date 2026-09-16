(function () {
  if (document.getElementById('aura-security-guard')) return;
  var insecure = location.protocol !== 'https:';
  var passwordFields = document.querySelectorAll('input[type="password"]').length;
  var mixed = Array.from(document.querySelectorAll('[src], [href]')).filter(function (node) {
    var value = node.src || node.href || '';
    return value.indexOf('http://') === 0;
  }).length;
  var label = insecure ? 'AURA Segurança: conexão não criptografada' : 'AURA Segurança: HTTPS ativo';
  if (passwordFields && insecure) label += ' • senha em HTTP';
  if (mixed) label += ' • conteúdo misto';
  var badge = document.createElement('div');
  badge.id = 'aura-security-guard';
  badge.textContent = label;
  badge.title = 'Campos de senha: ' + passwordFields + ' | Recursos HTTP: ' + mixed;
  badge.style.cssText = 'position:fixed;z-index:2147483647;top:12px;right:12px;padding:9px 12px;border-radius:10px;background:' + (insecure || mixed ? '#b23b3b' : '#237a55') + ';color:#fff;font:600 13px sans-serif;box-shadow:0 3px 14px #0006';
  document.documentElement.appendChild(badge);
})();
