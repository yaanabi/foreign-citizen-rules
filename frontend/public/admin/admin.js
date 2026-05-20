// Админ-панель: организации, целевые документы, правила.
import { api } from '/assets/api.js';
import { renderTopbar, showAlert, clearAlerts, escapeHtml } from '/assets/ui.js';

renderTopbar('admin');

// ---------- Табы ----------
const tabs = document.querySelectorAll('.tab');
const panels = document.querySelectorAll('.tab-panel');
tabs.forEach(t => t.addEventListener('click', () => {
  tabs.forEach(x => x.classList.remove('active'));
  panels.forEach(p => p.classList.remove('active'));
  t.classList.add('active');
  document.querySelector(`[data-panel="${t.dataset.tab}"]`).classList.add('active');
}));

// =====================================================================
// ОРГАНИЗАЦИИ
// =====================================================================
const orgForm    = document.getElementById('org-form');
const orgIdEl    = document.getElementById('org-id');
const orgName    = document.getElementById('org-name');
const orgAddress = document.getElementById('org-address');
const orgReset   = document.getElementById('org-reset');
const orgTitle   = document.getElementById('org-form-title');

function resetOrgForm() {
  orgIdEl.value = '';
  orgName.value = '';
  orgAddress.value = '';
  orgReset.hidden = true;
  orgTitle.textContent = 'Добавить организацию';
}
orgReset.addEventListener('click', resetOrgForm);

orgForm.addEventListener('submit', async (ev) => {
  ev.preventDefault();
  clearAlerts();
  const data = { name: orgName.value.trim(), address: orgAddress.value.trim() };
  try {
    if (orgIdEl.value) {
      await api.updateOrganization(orgIdEl.value, data);
      showAlert('ok', 'Организация обновлена');
    } else {
      await api.createOrganization(data);
      showAlert('ok', 'Организация создана');
    }
    resetOrgForm();
    await loadOrgs();
    await loadDocs();        // обновим чекбоксы в форме документов
    await loadRulesPicker(); // селекты в форме правил
  } catch (e) {
    showAlert('err', e.message || 'Ошибка сохранения');
  }
});

async function loadOrgs() {
  const host = document.getElementById('orgs-list');
  try {
    const list = await api.listOrganizations();
    if (!list.length) { host.innerHTML = '<div class="empty">Пока нет организаций.</div>'; return; }
    host.innerHTML = `
      <table class="data">
        <thead><tr><th>ID</th><th>Название</th><th>Адрес</th><th></th></tr></thead>
        <tbody>
          ${list.map(o => `
            <tr>
              <td class="mono">${o.id}</td>
              <td><b>${escapeHtml(o.name)}</b></td>
              <td>${escapeHtml(o.address)}</td>
              <td>
                <button class="btn btn-ghost btn-sm" data-edit-org='${JSON.stringify(o)}'>Изменить</button>
              </td>
            </tr>
          `).join('')}
        </tbody>
      </table>`;
    host.querySelectorAll('[data-edit-org]').forEach(b => b.addEventListener('click', () => {
      const o = JSON.parse(b.dataset.editOrg);
      orgIdEl.value = o.id;
      orgName.value = o.name;
      orgAddress.value = o.address;
      orgTitle.textContent = `Редактировать организацию #${o.id}`;
      orgReset.hidden = false;
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }));
  } catch (e) {
    host.innerHTML = `<div class="alert alert-err">${escapeHtml(e.message)}</div>`;
  }
}

// =====================================================================
// ЦЕЛЕВЫЕ ДОКУМЕНТЫ
// =====================================================================
const docForm   = document.getElementById('doc-form');
const docIdEl   = document.getElementById('doc-id');
const docName   = document.getElementById('doc-name');
const docOrgsEl = document.getElementById('doc-orgs-checkboxes');
const docReset  = document.getElementById('doc-reset');
const docTitle  = document.getElementById('doc-form-title');

function resetDocForm() {
  docIdEl.value = '';
  docName.value = '';
  docOrgsEl.querySelectorAll('input[type=checkbox]').forEach(c => c.checked = false);
  docReset.hidden = true;
  docTitle.textContent = 'Добавить целевой документ';
}
docReset.addEventListener('click', resetDocForm);

docForm.addEventListener('submit', async (ev) => {
  ev.preventDefault();
  clearAlerts();
  const orgIds = Array.from(docOrgsEl.querySelectorAll('input[type=checkbox]:checked')).map(c => Number(c.value));
  if (!orgIds.length) { showAlert('err', 'Отметьте хотя бы одну организацию'); return; }
  const data = { name: docName.value.trim(), organizationIds: orgIds };
  try {
    if (docIdEl.value) {
      await api.updateTargetDocument(docIdEl.value, data);
      showAlert('ok', 'Документ обновлён');
    } else {
      await api.createTargetDocument(data);
      showAlert('ok', 'Документ создан');
    }
    resetDocForm();
    await loadDocs();
    await loadRulesPicker();
  } catch (e) {
    showAlert('err', e.message || 'Ошибка сохранения');
  }
});

async function loadDocs() {
  // Подгружаем чекбоксы организаций в форму документа
  const orgs = await api.listOrganizations();
  docOrgsEl.innerHTML = orgs.length
    ? orgs.map(o => `
        <label class="checkbox-row">
          <input type="checkbox" value="${o.id}" />
          <span>${escapeHtml(o.name)} <span class="muted"> - ${escapeHtml(o.address)}</span></span>
        </label>`).join('')
    : '<div class="muted">Сначала добавьте организации.</div>';

  // Список документов
  const host = document.getElementById('docs-list');
  try {
    const list = await api.listTargetDocuments();
    if (!list.length) { host.innerHTML = '<div class="empty">Пока нет документов.</div>'; return; }
    host.innerHTML = `
      <table class="data">
        <thead><tr><th>ID</th><th>Название</th><th>Организации</th><th></th></tr></thead>
        <tbody>
          ${list.map(d => `
            <tr>
              <td class="mono">${d.id}</td>
              <td><b>${escapeHtml(d.name)}</b></td>
              <td>${(d.organizations || []).map(o => escapeHtml(o.name)).join(', ') || '<span class="muted">—</span>'}</td>
              <td>
                <button class="btn btn-ghost btn-sm" data-edit-doc='${JSON.stringify(d)}'>Изменить</button>
              </td>
            </tr>
          `).join('')}
        </tbody>
      </table>`;
    host.querySelectorAll('[data-edit-doc]').forEach(b => b.addEventListener('click', () => {
      const d = JSON.parse(b.dataset.editDoc);
      docIdEl.value = d.id;
      docName.value = d.name;
      const ids = new Set((d.organizations || []).map(o => o.id));
      docOrgsEl.querySelectorAll('input[type=checkbox]').forEach(c => c.checked = ids.has(Number(c.value)));
      docTitle.textContent = `Редактировать документ #${d.id}`;
      docReset.hidden = false;
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }));
  } catch (e) {
    host.innerHTML = `<div class="alert alert-err">${escapeHtml(e.message)}</div>`;
  }
}

// =====================================================================
// ПРАВИЛА
// =====================================================================
const profilesList = document.getElementById('profiles-list');
const ruleForm = document.getElementById('rule-form');

function addProfileBlock() {
  const idx = profilesList.children.length + 1;
  const block = document.createElement('div');
  block.className = 'card';
  block.style.background = 'var(--bg-muted)';
  block.innerHTML = `
    <div class="section-title">
      <h3 style="margin:0">Профиль #${idx}</h3>
      <button type="button" class="btn btn-ghost btn-sm" data-remove-profile>✕ Удалить</button>
    </div>
    <div class="form-row">
      <div class="field">
        <label>Дни пребывания</label>
        <input type="number" min="1" required data-k="stayDays" placeholder="30" />
      </div>
      <div class="field">
        <label>Цели пребывания (через запятую)</label>
        <input type="text" required data-k="stayPurposes" placeholder="Трудовая деятельность, Иная" />
      </div>
    </div>
    <div class="field">
      <label>Гражданства (через запятую)</label>
      <input type="text" data-k="citizenships" placeholder="Беларусь, Казахстан; пусто - любая страна (при выборе всех остальных граждан)" />
    </div>
    <label class="gap-8" style="font-weight: normal;">
      <input type="checkbox" data-k="isFallback" />
      <span>Для всех остальных иностранных граждан</span>
    </label>
    <div class="field">
      <label>Свойства (опционально)</label>
      <div data-props></div>
      <button type="button" class="btn btn-secondary btn-sm mt-8" data-add-prop>+ Свойство</button>
    </div>
  `;
  block.querySelector('[data-remove-profile]').addEventListener('click', () => block.remove());
  block.querySelector('[data-add-prop]').addEventListener('click', () => addPropRow(block.querySelector('[data-props]')));
  profilesList.appendChild(block);
}

function addPropRow(host) {
  const row = document.createElement('div');
  row.className = 'props';
  row.innerHTML = `
    <input type="text" placeholder="Имя" data-pk="name" />
    <input type="text" placeholder="Значение" data-pk="value" />
    <button type="button" class="btn btn-ghost btn-sm">✕</button>
  `;
  row.querySelector('button').addEventListener('click', () => row.remove());
  host.appendChild(row);
}

document.getElementById('add-profile').addEventListener('click', addProfileBlock);
addProfileBlock(); // один по умолчанию

function collectProfiles() {
  return Array.from(profilesList.children).map(block => {
    const props = Array.from(block.querySelectorAll('.props')).map(r => ({
      name:  r.querySelector('[data-pk=name]').value.trim(),
      value: r.querySelector('[data-pk=value]').value.trim(),
    })).filter(p => p.name);
    return {
      stayDays:     Number(block.querySelector('[data-k=stayDays]').value),
      isFallback:   block.querySelector('[data-k=isFallback]').checked,
      stayPurposes: block.querySelector('[data-k=stayPurposes]').value.split(',').map(s => s.trim()).filter(Boolean),
      citizenships: block.querySelector('[data-k=citizenships]').value.split(',').map(s => s.trim()).filter(Boolean),
      properties:   props,
    };
  });
}

function validateProfiles(profiles) {
  for (const [index, profile] of profiles.entries()) {
    const number = index + 1;
    if (!profile.stayDays) {
      return `Профиль #${number}: укажите дни пребывания`;
    }
    if (!profile.stayPurposes.length) {
      return `Профиль #${number}: укажите хотя бы одну цель пребывания`;
    }
    if (!profile.isFallback && !profile.citizenships.length && !profile.properties.length) {
      return `Профиль #${number}: укажите гражданство или добавьте хотя бы одно свойство`;
    }
  }
  return null;
}

ruleForm.addEventListener('submit', async (ev) => {
  ev.preventDefault();
  clearAlerts();
  const profiles = collectProfiles();
  if (!profiles.length) { showAlert('err', 'Добавьте хотя бы один профиль'); return; }
  const profileError = validateProfiles(profiles);
  if (profileError) { showAlert('err', profileError); return; }
  const data = {
    name: document.getElementById('rule-name').value.trim(),
    roadmapVersion: document.getElementById('rule-version').value.trim(),
    targetDocumentId: Number(document.getElementById('rule-document').value),
    guidance: {
      description: document.getElementById('rule-desc').value.trim(),
    },
    profiles,
  };
  try {
    await api.createRule(data);
    showAlert('ok', 'Правило создано');
    ruleForm.reset();
    profilesList.innerHTML = '';
    addProfileBlock();
    await loadRules();
  } catch (e) {
    showAlert('err', e.message || 'Ошибка создания правила');
  }
});

async function loadRulesPicker() {
  // Селект документов в форме правил.
  const sel = document.getElementById('rule-document');
  const docs = await api.listTargetDocuments();
  sel.innerHTML = docs.length
    ? '<option value="">— выберите —</option>' + docs.map(d =>
        `<option value="${d.id}">${escapeHtml(d.name)} (#${d.id})</option>`).join('')
    : '<option value="">— сначала создайте документ —</option>';
}

async function loadRules() {
  const host = document.getElementById('rules-list');
  try {
    const list = await api.listRules();
    if (!list.length) { host.innerHTML = '<div class="empty">Пока нет правил.</div>'; return; }
    host.innerHTML = list.map(r => `
      <div class="card">
        <div class="section-title">
          <h3 style="margin:0">${escapeHtml(r.name)} <span class="badge">v ${escapeHtml(r.roadmapVersion || '—')}</span></h3>
          <span class="hint mono">id #${r.id}</span>
        </div>
        <div class="form-row">
          <div>
            <div class="muted">Целевой документ</div>
            <b>${escapeHtml(r.targetDocument?.name || '—')}</b>
          </div>
          <div>
            <div class="muted">Организации</div>
            ${(r.targetDocument?.organizations || []).map(o => escapeHtml(o.name)).join(', ') || '<span class="muted">—</span>'}
          </div>
        </div>
        <div class="mt-16">
          <div class="muted">Руководство</div>
          ${escapeHtml(r.guidance?.description || '—')}
        </div>
        <div class="mt-16">
          <div class="muted mb-8">Профили (${(r.profiles || []).length})</div>
          ${(r.profiles || []).map(p => `
            <div class="step">
              <div class="step-num">≡</div>
              <div>
                <b>${p.stayDays} дн.</b> ·
                ${p.isFallback ? '<span class="badge warn">для остальных</span> · ' : ''}
                цели: ${(p.stayPurposes || []).map(escapeHtml).join(', ') || '—'} ·
                гражданства: ${(p.citizenships || []).map(escapeHtml).join(', ') || '—'}
                ${(p.properties || []).length
                  ? `<div class="step-meta mt-8">Свойства: ${p.properties.map(pr =>
                       `<code>${escapeHtml(pr.name)}=${escapeHtml(pr.value ?? '')}</code>`).join(' · ')}</div>`
                  : ''}
              </div>
            </div>
          `).join('')}
        </div>
      </div>
    `).join('');
  } catch (e) {
    host.innerHTML = `<div class="alert alert-err">${escapeHtml(e.message)}</div>`;
  }
}

// Загрузка всего на старте.
(async () => {
  await loadOrgs();
  await loadDocs();
  await loadRulesPicker();
  await loadRules();
})();
