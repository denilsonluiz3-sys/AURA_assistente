(function () {
  var old = document.getElementById('aura-security-guard');
  if (old) old.remove();
  var insecure = location.protocol !== 'https:';
  var passwordFields = document.querySelectorAll('input[type="password"]').length;
  var mixed = Array.from(document.querySelectorAll('[src], [href]')).filter(function (node) {
    var value = node.src || node.href || '';
    return value.indexOf('http://') === 0;
  }).length;
  var risks = [];
  if (insecure) risks.push('conexão sem HTTPS');
  if (passwordFields && insecure) risks.push('campo de senha em conexão insegura');
  if (mixed) risks.push(mixed + ' recurso(s) HTTP em página HTTPS');
  if (!risks.length) return;
  var notice = document.createElement('aside');
  notice.id = 'aura-security-guard';
  notice.setAttribute('role', 'alert');
  notice.innerHTML = '<strong>AURA Segurança</strong> ' + risks.join(' • ') + ' <button type="button" aria-label="Fechar">Fechar</button>';
  notice.style.cssText = 'display:block;box-sizing:border-box;width:100%;margin:0;padding:8px 12px;background:#fff4d6;color:#5c4300;border-bottom:1px solid #d6a936;font:600 13px/1.4 sans-serif;text-align:left';
  var close = notice.querySelector('button');
  close.style.cssText = 'float:right;border:0;background:transparent;color:#5c4300;text-decoration:underline;font-weight:600';
  close.addEventListener('click', function () { notice.remove(); });
  if (document.body) document.body.insertBefore(notice, document.body.firstChild);
})();
