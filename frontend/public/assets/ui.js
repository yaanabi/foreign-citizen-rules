// Общие UI-утилиты для всех страниц
import { auth } from './api.js';

// Рендер верхней панели с навигацией.
export function renderTopbar(activePage) {
  const el = document.getElementById('topbar');
  if (!el) return;

  const citizen = auth.getCitizen();
  const isAuthed = auth.isAuthed();

  const links = [
    { href: '/index.html',   label: 'Главная',  key: 'home' },
    { href: '/roadmap.html', label: 'Дорожная карта', key: 'roadmap', requiresAuth: true },
    { href: '/profile.html', label: 'Профиль',  key: 'profile', requiresAuth: true },
    { href: '/admin/',       label: 'Админка',  key: 'admin' },
  ];

  const nav = links
    .filter(l => !l.requiresAuth || isAuthed)
    .map(l => `<a href="${l.href}" class="${l.key === activePage ? 'active' : ''}">${l.label}</a>`)
    .join('');

  const authBlock = isAuthed
    ? `<span class="user-chip">
         <span>${escapeHtml(citizen?.fullName || citizen?.email || 'Гражданин')}</span>
         <button id="logout-btn" title="Выйти">Выйти</button>
       </span>`
    : `<div class="gap-8">
         <a href="/login.html" class="btn btn-ghost btn-sm" style="color:rgba(255,255,255,0.85)">Войти</a>
         <a href="/register.html" class="btn btn-accent btn-sm">Регистрация</a>
       </div>`;

  el.innerHTML = `
    <a href="/index.html" class="brand">
      <span class="dot"></span>
      Foreign Citizen Rules
    </a>
    <nav>${nav}</nav>
    ${authBlock}
  `;

  const logoutBtn = document.getElementById('logout-btn');
  if (logoutBtn) {
    logoutBtn.addEventListener('click', () => {
      auth.clearToken();
      location.href = '/index.html';
    });
  }
}

// Алёрты внутри контейнера #alerts.
export function showAlert(kind, message) {
  const host = document.getElementById('alerts');
  if (!host) return;
  const node = document.createElement('div');
  node.className = `alert alert-${kind}`;
  node.textContent = message;
  host.innerHTML = '';
  host.appendChild(node);
}

export function clearAlerts() {
  const host = document.getElementById('alerts');
  if (host) host.innerHTML = '';
}

// Защита приватных страниц.
export function requireAuth() {
  if (!auth.isAuthed()) {
    location.href = '/login.html?next=' + encodeURIComponent(location.pathname);
    return false;
  }
  return true;
}

// Безопасный HTML.
export function escapeHtml(s) {
  if (s == null) return '';
  return String(s)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}

// Формат даты в человеческий вид.
export function fmtDate(iso) {
  if (!iso) return '—';
  const d = new Date(iso);
  if (isNaN(d.getTime())) return iso;
  return d.toLocaleDateString('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric' });
}

export function fmtDateTime(iso) {
  if (!iso) return '—';
  const d = new Date(iso);
  if (isNaN(d.getTime())) return iso;
  return d.toLocaleString('ru-RU', { dateStyle: 'short', timeStyle: 'short' });
}

// Сегодняшняя дата в формате YYYY-MM-DD (для <input type="date">)
export function todayIso() {
  const d = new Date();
  const pad = (n) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth()+1)}-${pad(d.getDate())}`;
}
