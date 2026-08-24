# دكان — نظام إدارة البقالة والمتجر الصغير

موقع تعريفي ومنصة اشتراكات لتطبيق **دكان** لإدارة البقالة والمتجر الصغير، مبنية بـ ASP.NET Core 10 (MVC) بدعم كامل للغة العربية (RTL).

يتيح الموقع للعميل تقديم طلب اشتراك (بدون حساب)، ويلتقط الطلبات في لوحة تحكم خاصة بالمدير لإدارتها وتفعيل الاشتراكات وتوليد رخص التشغيل، مع واجهة برمجية للتحقق من الرخص استعداداً للتطبيق الأندرويد.

## المزايا

- صفحة تسويقية عربية RTL تعرض الخطط مباشرة من قاعدة البيانات.
- نموذج طلب اشتراك بدون حساب مع تحقق في الطرفين (عميل وخادم) وتواصل واتساب.
- لوحة تحكم محمية بدور `Admin`:
  - لوحة رئيسية بالبطاقات وأحدث الطلبات والاشتراكات المنتهية قريباً.
  - إدارة الخطط (إنشاء / تعديل / تفعيل / إيقاف) مع سجل تدقيق لتغييرات الأسعار.
  - إدارة العملاء والطلبات والاشتراكات مع حالات وعمليات محددة بآلة حالات.
- محرك اشتراكات: تفعيل، إلغاء، تجديد (بسجل جديد محفوظ)، انتهاء تلقائي، رخص تشغيل آمنة وفريدة.
- واجهة برمجية للتحقق من الرخصة: `POST /api/v1/subscriptions/validate`.
- اختبارات آلية لأهم القواعد التجارية.

## المتطلبات

- .NET SDK 10.0+
- SQL Server (يُستخدم LocalDB افتراضياً في التطوير)
- أدوات EF Core CLI: `dotnet tool install --global dotnet-ef`

## البنية المعمارية

- `src/Dukan.Web` — التطبيق الرئيسي (MVC + Areas).
  - `Domain/` — الكيانات والثوابت.
  - `Application/` — الخدمات والواجهات وDTOs والإعدادات (Options pattern).
  - `Infrastructure/Services/` — الخدمات الخلفية.
  - `Data/` — `ApplicationDbContext` والتهيئة والهجرات والبذر.
  - `Areas/Admin/` — لوحة التحكم.
  - `Controllers/Api/` — واجهة التحقق من الرخصة.
- `tests/Dukan.Web.Tests` — اختبارات القواعد التجارية (xUnit v3 + EF Core InMemory + WebApplicationFactory).

## الإعداد

### 1) إعداد الاتصال بقاعدة البيانات

سلسلة الاتصال الافتراضية في `src/Dukan.Web/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=DukanDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

### 2) تطبيق الهجرات

```bash
dotnet ef database update
```

لإضافة هجرة جديدة عند تعديل الكيانات:

```bash
dotnet ef migrations add <Name>
```

### 3) تشغيل التطبيق

```bash
dotnet run --project src/Dukan.Web
```

عند أول تشغيل تُبذر الخطط الخمس الافتراضية من الإعدادات (تجربة مجانية 7 أيام، شهر واحد، 3 أشهر، سنة واحدة، سنتان — بأسعار تجريبية في `SeedData:Plans`).

## إعداد حساب المدير

لا تُحفظ كلمة مرور للمدير في الكود. عند التشغيل للتطوير، حدّدها عبر إعدادات البيئة أو User Secrets:

```bash
dotnet user-secrets set "SeedData:Admin:Password" "<كلمة مرور قوية>" --project src/Dukan.Web
```

عندها سيُنشأ مستخدم المدير (`admin@dukan.local` / `admin`) ويُضاف إلى دور `Admin` تلقائياً عند الإقلاع. استخدمها للدخول إلى `/Admin/Account/Login`.

> ملاحظة أمنية: لا تضع كلمة مرور حقيقية أبداً في `appsettings.json`. استخدم User Secrets أو متغيرات البيئة.

## الإعدادات الرئيسية

| المفتاح | الوصف |
| --- | --- |
| `ApplicationSettings:Url` | رابط الموقع الرسمي |
| `ContactSettings:PhoneNumber` / `WhatsAppNumber` | رقم الهاتف والواتساب القابلان للتكوين (يُستخدمان في الموقع وروابط الواتساب) |
| `LicenseSettings:KeyLength` / `KeyPrefix` | طول جسم الرخصة وبدايتها |
| `LicenseSettings:ApiKey` | مفتاح واجهة التحقق من الرخصة (استخدم قيمة سرية قوية في الإنتاج) |
| `SeedData:Plans` | الخطط الافتراضية (تُبذر مرة واحدة فقط) |
| `SeedData:Admin` | بيانات مستخدم المدير للتطوير |

## واجهة التحقق من الرخصة (API)

مخصصة للتطبيق الأندرويد مستقبلاً.

**المسار:** `POST /api/v1/subscriptions/validate`

**التأمين:** يُمرَّر مفتاح الواجهة في ترويسة `X-Api-Key`.

**الطلب:**

```json
{ "licenseKey": "DK-XXXX-XXXX" }
```

**الاستجابة** (`200`):

```json
{
  "valid": true,
  "status": "Active",
  "expiresAt": "2026-09-15T12:00:00Z"
}
```

- `valid: false` مع `status: "NotFound"` لرخصة غير معروفة.
- لا تُكشف بيانات العملاء أو الإدارة أو الأسعار في الاستجابة.
- تُسجَّل الطلبات في السجلات دون الرخص الكاملة (يكتفى بآخر الأحرف).

## التشغيل الآلي

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/Dukan.Web
```

## ملاحظات النشر

- بدّل سلسلة الاتصال إلى خادم إنتاج، وطبق الهجرات قبل الإقلاع.
- اضبط `ContactSettings` و`LicenseSettings:ApiKey` و`ApplicationSettings:Url` لقيم الإنتاج.
- في الإنتاج تُفعّل تلقائياً: `UseHsts`، صفحة أخطاء عامة، و`SecurePolicy=Always` للكوكيز.
- يُنشأ المستخدم الملقّب `SeedData:Admin` في وضع التطوير فقط عبر إعدادات آمنة.

## النشر بـ Docker (جاهز للإنتاج)

المشروع مُحضّر للنشر بحاوية — `Dockerfile` متعدد المراحل + `docker-compose.yml` مع SQL Server.

### المتطلبات
- Docker Desktop + Docker Compose v2

### 1) إعداد البيئة
```bash
cp .env.example .env
# عدّل .env — SA_PASSWORD قوي (8+ أحرف، كبير/صغير/رقم/رمز)، ADMIN_PASSWORD
notepad .env
```

### 2) البناء والتشغيل (Orchestration)
```bash
docker compose up -d --build
# تابع السجلات
docker compose logs -f dukan-web
# تحقق من الصحة
curl http://localhost:5000/health
# لوحة التحكم
# http://localhost:5000/Admin/Account/Login
```

الخدمات:
- `dukan-web` — `src/Dukan.Web/Dockerfile` (SDK 10.0 → aspnet 10.0, USER app, 8080, HEALTHCHECK `curl /health`)
- `dukan-db` — `mcr.microsoft.com/mssql/server:2022-latest` مع healthcheck `sqlcmd` و volume `dukan-mssql-data`
- `dukan-net` bridge — يعتمد `dukan-web` على `dukan-db` healthy، يُطبّق الهجرات تلقائياً مع retry 10×3s.

### 3) متغيرات البيئة (تتجاوز appsettings.json)
| متغير | وصف |
|---|---|
| `ConnectionStrings__DefaultConnection` | يُضبط تلقائياً في compose → `Server=dukan-db,1433;...` |
| `SeedData__Admin__Password` | كلمة مرور المدير (إلزامي) |
| `SA_PASSWORD` / `ADMIN_PASSWORD` | من `.env` |
| `DISABLE_HTTPS_REDIRECT=true` | مُفعّل في compose لتجنب redirect على http 8080 |

### 4) إيقاف / تنظيف
```bash
docker compose down        # إيقاف
docker compose down -v     # حذف البيانات (volume)
docker compose ps
docker compose logs dukan-db
```

### 5) بناء ونشر الصورة منفردة
```bash
docker build -f src/Dukan.Web/Dockerfile -t dukan-web:latest .
docker tag dukan-web:latest registry.example.com/dukan-web:latest
docker push registry.example.com/dukan-web:latest
# أو عبر SDK بدون Dockerfile
dotnet publish src/Dukan.Web -c Release /t:PublishContainer
```

### 6) الإنتاج
- غيّر `SA_PASSWORD` و`ADMIN_PASSWORD` لقيم قوية، لا تضعها في git.
- للـ HTTPS خلف Nginx/Traefik: احذف `DISABLE_HTTPS_REDIRECT` واضبط شهادة.
- النسخ الاحتياطي: `docker volume` أو `sqlcmd` → backups.

## مستقبلاً: تطبيق أندرويد

واجهة `/api/v1/subscriptions/validate` هي الأساس لتفعيل الرخص داخل التطبيق. نموذج الرخصة يسمح لاحقاً بربط الجهاز (`DeviceId` يبقى `null` في هذه المرحلة).

## رخصة

جميع الحقوق محفوظة © دكان.
