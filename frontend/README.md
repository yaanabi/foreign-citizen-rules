# Frontend

Учебный фронтенд для проекта **Foreign Citizen Rules**.
Чистый HTML + CSS + Vanilla JS, без сборщика. Раздаётся через nginx,
который заодно проксирует запросы `/api/*` на сервис `backend` в той же
docker-compose сети — поэтому CORS на бэке настраивать не нужно.

## Структура

```
frontend/
├── nginx.conf           # конфиг nginx с proxy_pass на backend:8080
├── public/              # всё, что отдаётся пользователю
│   ├── index.html       # лендинг
│   ├── login.html       # вход
│   ├── register.html    # регистрация
│   ├── profile.html     # личный кабинет
│   ├── roadmap.html     # запрос дорожной карты + история
│   ├── admin/
│   │   ├── index.html   # админ-панель (3 таба)
│   │   └── admin.js
│   └── assets/
│       ├── styles.css   # общие стили
│       ├── api.js       # клиент бэкенда
│       └── ui.js        # рендер топбара, алёрты, утилиты
└── README.md            # этот файл
```

## Запуск

Из корня репозитория:

```bash
docker compose up --build
```

После старта:
- Фронт: <http://localhost:3000>
- Swagger бэка через nginx: <http://localhost:3000/swagger/>
- Бэк напрямую: <http://localhost:5000>

## Как разрабатывать

`public/` смонтирована в контейнер nginx **в read-only режиме**. Любые правки
HTML/CSS/JS подхватываются мгновенно — пересобирать контейнер не нужно,
просто обновите страницу в браузере (Ctrl+F5 если кеш).

Правка `nginx.conf` требует перезапуска контейнера:

```bash
docker compose restart frontend
```

## Заметки

- Токен хранится в `localStorage` (`fcr.token`), кладётся в `Authorization: Bearer <token>`.
- API возвращает JSON в PascalCase (`PropertyNamingPolicy = null` в `Program.cs`).
  Клиент в `api.js` автоматически нормализует ключи в camelCase, чтобы
  весь остальной код был однообразным.
- Если запускать фронт без nginx-прокси (например, открыв `index.html` файлом),
  бэк нужно будет настроить с CORS. Через docker-compose это не требуется.
