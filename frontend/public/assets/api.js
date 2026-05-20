// Универсальный клиент API
// База — относительная: все запросы идут на свой же origin,
// nginx проксирует /api/* на backend:8080. CORS не нужен.

const TOKEN_KEY = 'fcr.token';
const CITIZEN_KEY = 'fcr.citizen';

export const auth = {
  getToken()    { return localStorage.getItem(TOKEN_KEY); },
  setToken(t)   { localStorage.setItem(TOKEN_KEY, t); },
  clearToken()  { localStorage.removeItem(TOKEN_KEY); localStorage.removeItem(CITIZEN_KEY); },
  getCitizen()  { try { return JSON.parse(localStorage.getItem(CITIZEN_KEY)); } catch { return null; } },
  setCitizen(c) { localStorage.setItem(CITIZEN_KEY, JSON.stringify(c)); },
  isAuthed()    { return !!localStorage.getItem(TOKEN_KEY); },
};

// Нормализатор ключей: первая буква в нижний регистр на всех уровнях.
// Бэк возвращает JSON в PascalCase (PropertyNamingPolicy = null в Program.cs),
// но фронт удобнее писать в camelCase.
function camelize(obj) {
  if (Array.isArray(obj)) return obj.map(camelize);
  if (obj !== null && typeof obj === 'object') {
    const out = {};
    for (const k of Object.keys(obj)) {
      const ck = k.length ? k[0].toLowerCase() + k.slice(1) : k;
      out[ck] = camelize(obj[k]);
    }
    return out;
  }
  return obj;
}

async function request(method, path, body) {
  const headers = { 'Accept': 'application/json' };
  const token = auth.getToken();
  if (token) headers['Authorization'] = `Bearer ${token}`;

  const init = { method, headers };
  if (body !== undefined) {
    headers['Content-Type'] = 'application/json';
    init.body = JSON.stringify(body);
  }

  const res = await fetch(path, init);
  const text = await res.text();
  let data = null;
  if (text) {
    try { data = JSON.parse(text); } catch { data = text; }
  }
  data = camelize(data);

  if (!res.ok) {
    const err = new Error(
      (data && data.message) || `HTTP ${res.status} ${res.statusText}`
    );
    err.status = res.status;
    err.body = data;
    throw err;
  }
  return data;
}

export const api = {
  // Граждане
  registerCitizen: (data) => request('POST', '/api/v1/citizens/register', data),
  login:           (data) => request('POST', '/api/v1/citizens/login', data),
  me:              ()     => request('GET',  '/api/v1/citizens/me'),
  updateMe:        (data) => request('PUT',  '/api/v1/citizens/me', data),
  createRoadmap:   (data) => request('POST', '/api/v1/citizens/me/roadmaps', data),
  listRoadmaps:    ()     => request('GET',  '/api/v1/citizens/me/roadmaps'),

  // Справочники
  stayPurposes:    ()     => request('GET',  '/api/v1/reference/stay-purposes'),
  citizenships:    ()     => request('GET',  '/api/v1/reference/citizenships'),
  profileProperties: ()   => request('GET',  '/api/v1/reference/profile-properties'),

  // Организации
  listOrganizations:   ()        => request('GET',  '/api/v1/organizations'),
  createOrganization:  (data)    => request('POST', '/api/v1/organizations', data),
  updateOrganization:  (id, d)   => request('PUT',  `/api/v1/organizations/${id}`, d),

  // Целевые документы
  listTargetDocuments:  ()       => request('GET',  '/api/v1/target-documents'),
  createTargetDocument: (data)   => request('POST', '/api/v1/target-documents', data),
  updateTargetDocument: (id, d)  => request('PUT',  `/api/v1/target-documents/${id}`, d),

  // Правила
  listRules:  (version)  => request('GET', '/api/v1/rules' + (version ? `?roadmapVersion=${encodeURIComponent(version)}` : '')),
  getRule:    (id)       => request('GET', `/api/v1/rules/${id}`),
  createRule: (data)     => request('POST', '/api/v1/rules', data),
};
