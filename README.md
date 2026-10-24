# PlanYourFriday

**Платформа для спільного планування подій у групі друзів.** Кожен позначає, коли він вільний, система знаходить найкращий час, група голосує, і подія сама з'являється в Google Calendar у всіх.

---

## Як це працює

```mermaid
flowchart LR
    A[Створення події] --> B[Запрошення друзів]
    B --> C[Збір вільного часу]
    C --> D[Голосування]
    D --> E[Подію підтверджено]
    E --> F[Google Calendar<br/>усіх учасників]

    classDef planning fill:#2e6fd1,stroke:#2e6fd1,color:#fff
    classDef voting fill:#8e44ad,stroke:#8e44ad,color:#fff
    classDef done fill:#1f8a5b,stroke:#1f8a5b,color:#fff
    class A,B,C planning
    class D voting
    class E,F done
```

1. **Створення події.** Організатор задає назву, вікно (коли подія може відбутися), тривалість і, за бажанням, дедлайн.
2. **Запрошення.** Посиланням або на email. Учасник заходить через свій Google-акаунт.
3. **Збір вільного часу.** Кожен позначає вільні проміжки на календарі: вручну, імпортом з Google Calendar або одним кліком за своїми «звичними годинами».
4. **Голосування.** Система пропонує до 6 найкращих варіантів, навіть якщо повного збігу немає (і показує, хто не може). Організатор вибирає варіанти й запускає голосування. Кожен відповідає **Підходить / Можливо / Не можу** (2 / 1 / 0 балів).
5. **Підтвердження.** Організатор фіксує час, додає місце, посилання й нотатки. Подія потрапляє в календарі учасників: організатор як власник, решта як гості.

## Можливості

| Розділ | Що там |
| --- | --- |
| **Головна** | Найближча подія, «твій хід» (де чекають на тебе), план на 7 днів |
| **Події** | Усі події з пошуком, вкладками за етапом (зокрема окремо минулі й скасовані), фільтром за роллю, сортуванням |
| **Сторінка події** | Календар вільного часу, найкращі варіанти, голосування, деталі події, скасування й видалення, учасники й запрошення |
| **Календар** | Усі плани з усіх подій: місяць, тиждень, список |
| **Ідеї** | Опитування серед друзів «що робимо?»: варіанти, голосування, переможець одразу стає подією. Нічия — крутимо колесо |
| **Друзі** | Додавання за посиланням, пошук за @ніком, підказки з людей, з якими вже щось планували. Друзів можна одразу додати в подію чи опитування |
| **Сповіщення** | Запрошення, старт голосування, дедлайни, підтвердження, зміни та скасування, заявки в друзі, опитування. Важливі ще й на email — за налаштуваннями |
| **Wheel** | Колесо ідей, якщо не знаєте, чим зайнятися. Результат одразу перетворюється на план |
| **Налаштування** | Профіль і @нік, підключення Google Calendar, звичний вільний час, email-сповіщення, тема, формат часу, експорт і видалення акаунта |

**Кольори означають етап**, однаково по всьому сайту:

| Колір | Етап |
| --- | --- |
| Синій | Збір вільного часу |
| Фіолетовий | Голосування |
| Зелений | Подію підтверджено |
| Сірий | Минуло або скасовано |
| Помаранчевий | Твій вільний час і те, що чекає на тебе |

## Архітектура

Модульний моноліт:

- **SPA** (`web/`): інтерфейс.
- **API App** (`src/PlanYourFriday.Api`): HTTP-запити, вхід через Google, бізнес-логіка подій, вільного часу й голосування.
- **Worker App** (`src/PlanYourFriday.Worker`): фонова робота — листи з outbox, синхронізація підтверджених подій у Google Calendar кожного учасника, нагадування про дедлайни й події, підсумки опитувань. Збій SMTP чи Google API не ламає запит користувача.
- **PostgreSQL**: дані, включно з таблицею outbox.

Шари бекенду: `Api` / `Worker` → `Application` (бізнес-правила й інтерфейси) → `Domain` (сутності), а `Infrastructure` реалізує інтерфейси (БД, Google, SMTP).

## Стек

| Частина | Технології |
| --- | --- |
| Frontend | React, TypeScript, Vite, React Router, TanStack Query, FullCalendar, Lucide |
| Backend | C#, ASP.NET Core Web API, .NET Worker Service (.NET 10) |
| База даних | PostgreSQL, Entity Framework Core |
| Інтеграції | Google OAuth 2.0, Google Calendar API, SMTP |

## Запуск

**Frontend** (потрібен Node.js 20.19+ або 22.12+):

```bash
cd web
npm install
npm run dev        # http://localhost:5173
npm run format     # Prettier; CI перевіряє через npm run format:check
npm run lint       # oxlint
```

За замовчуванням фронтенд працює з **мок-API** прямо в браузері, тож бекенд не потрібен. Щоб підключити справжній API, створіть `web/.env.development.local` з `VITE_USE_MOCKS=false` і запустіть бекенд: Vite проксіює `/api` на `http://localhost:5131`.

**Backend** (потрібні .NET 10 SDK і Docker):

```bash
cp .env.example .env                              # лише перший раз
docker compose up -d postgres mailpit             # лише PostgreSQL і Mailpit
dotnet run --project src/PlanYourFriday.Api       # http://localhost:5131, міграції застосовуються самі
dotnet run --project src/PlanYourFriday.Worker    # листи, Google Calendar, нагадування
```

Листи з локального запуску не йдуть назовні, а потрапляють у Mailpit: <http://localhost:8025>.

**Без ключів Google** у режимі розробки замість Google відкривається форма тестового входу: вводиш будь-який email і заходиш. Так зручно перевіряти кількома «друзями» в різних браузерах.

Для входу через Google створіть OAuth Client ID (тип Web application) у Google Cloud Console з двома redirect URI:

- `http://localhost:5173/api/auth/signin-google` (вхід);
- `http://localhost:5173/api/calendar-connections/google/callback` (підключення календаря).

Увімкніть **Google Calendar API** (APIs & Services → Library), а в OAuth consent screen → Data Access додайте scopes `.../auth/calendar.readonly` і `.../auth/calendar.events`. Ключі збережіть у user-secrets (не в git); API і Worker мають спільний `UserSecretsId`, тож достатньо одного разу:

```bash
dotnet user-secrets set "Authentication:Google:ClientId" "<client-id>" --project src/PlanYourFriday.Api
dotnet user-secrets set "Authentication:Google:ClientSecret" "<client-secret>" --project src/PlanYourFriday.Api
```

**Тести:** `dotnet test`. Інтеграційні тести (`tests/PlanYourFriday.IntegrationTests`) самі піднімають PostgreSQL у Docker, тож Docker має бути запущений. Вони звіряють кожну відповідь API з типами SPA у `web/src/api/types.ts`, тому якщо фронтенд і бекенд розійдуться, тест це покаже.

### Docker

Увесь застосунок (SPA, API, Worker, PostgreSQL, Mailpit) запускається однією командою, без .NET і Node.js на машині. Потрібен лише Docker.

**1. Google Cloud** (якщо потрібен вхід через Google; без нього працює тестовий вхід). У тому ж OAuth client, що й вище, додайте ще два Authorized redirect URI:

- `http://localhost:8080/api/auth/signin-google`;
- `http://localhost:8080/api/calendar-connections/google/callback`.

Поки OAuth consent screen у статусі Testing, додайте свій акаунт у **Test users**. JavaScript origins не потрібні: вхід відбувається на сервері.

**2. `.env`.** Скопіюйте `cp .env.example .env` і впишіть ключі Google. user-secrets у контейнер не потрапляють, тож значення ті самі, що там (`dotnet user-secrets list --project src/PlanYourFriday.Api`).

| Змінна | За замовчуванням | Що це |
| --- | --- | --- |
| `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET` | порожні | ключі OAuth; порожні означає тестовий вхід |
| `APP_PORT` | `8080` | порт сайту; якщо змінили, змініть і в redirect URI |
| `APP_ENVIRONMENT` | `Development` | `Production` вимикає тестовий вхід (тоді ключі Google обов'язкові) |
| `SMTP_HOST`, `SMTP_PORT`, `SMTP_ENABLE_SSL`, `SMTP_USERNAME`, `SMTP_PASSWORD`, `SMTP_FROM` | порожні | справжня пошта (приклад для Gmail у `.env.example`); порожні означає Mailpit. Хост, порт і SSL задавайте разом |
| `POSTGRES_*` | `planyourfriday` | база даних; після першого запуску не змінюйте, або `docker compose down -v` |

**3. Запуск:**

```bash
docker compose up -d --build   # http://localhost:8080, листи в http://localhost:8025
docker compose down            # зупинити (дані лишаються; -v видаляє базу)
```

Після змін у `.env` достатньо `docker compose up -d`, після змін у коді потрібен `--build`.

Сайт працює на `http://localhost:8080`. Якщо відкрити `http://127.0.0.1:8080` (так робить посилання на порт у Docker Desktop), nginx перенаправить на `localhost`: Google приймає лише зареєстровані redirect URI з `localhost`, а cookie входу прив'язана до хоста.

Порядок запуску як у production: PostgreSQL → `migrate` (застосовує міграції й завершується, тож у `docker compose ps -a` він `Exited (0)`) → API і Worker → SPA.

## Структура

```text
PlanYourFriday/
├── web/                              SPA
├── src/
│   ├── PlanYourFriday.Api/           HTTP API
│   ├── PlanYourFriday.Worker/        фонова обробка outbox
│   ├── PlanYourFriday.Application/   бізнес-логіка, інтерфейси
│   ├── PlanYourFriday.Domain/        доменні сутності
│   └── PlanYourFriday.Infrastructure/ БД, Google, SMTP
└── tests/
    ├── PlanYourFriday.UnitTests/         домен і сервіси (NSubstitute)
    └── PlanYourFriday.IntegrationTests/  API + PostgreSQL у Docker, контракт із SPA
```

## Команда

| | Роль | Зона відповідальності |
| --- | --- | --- |
| **Бублик Світлана** | Business Analyst, Frontend Engineer | C4 та UML-діаграми, SPA |
| **Забродін Максим** | Backend Engineer, DevOps Engineer | API, сервіси подій, запрошень і пошуку часу, схема БД, розгортання |
| **Хаймик Тетяна** | Backend Engineer, QA Engineer | Google OAuth і Calendar, Worker та outbox, SMTP, тести |
