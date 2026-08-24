# دكان — Build Tasks

Clear, ordered phases and tasks for building the دكان subscription platform & marketing website.

Source of truth: [`دكان — Production Master Prompt for AI Coding Agent.md`](دكان — Production Master Prompt for AI Coding Agent.md).

## How to Use This File

- Work through the phases **in order**. Do not skip ahead.
- Tick a task `[x]` only when it is actually done.
- **Do not mark a phase complete unless it builds and passes its verification gate.**
- Fix errors before moving to the next phase.
- Do not invent product functionality that is not documented in the master prompt.
- Keep the architecture simple and maintainable. No microservices, brokers, Kubernetes, Redis, CQRS frameworks, or SPA frameworks.

## Common Commands

```bash
dotnet restore
dotnet build
dotnet ef migrations add <Name>
dotnet ef database update
dotnet test
dotnet run
```

---

## Phase 1 — Foundation

**Reference:** §2, §3, §27, §28, §32, §33, §42-P1

### Project Structure

- [x] Create ASP.NET Core 10 MVC solution/project `Dukan.Web` (Razor Views, no SPA framework).
- [x] Create the folder structure:
  - `Areas/Admin/Controllers`, `Areas/Admin/Views`
  - `Controllers`, `Views` (Home, Subscription, Shared)
  - `Data` (+ `Configurations`, `Migrations`)
  - `Domain/Entities`, `Domain/Enums`, `Domain/Constants`
  - `Application/DTOs`, `Application/Interfaces`, `Application/Services`, `Application/Validators`
  - `Infrastructure/Services`, `Infrastructure/Identity`
  - `API/Controllers`
  - `wwwroot/css`, `wwwroot/js`, `wwwroot/images`

### Arabic RTL Base Layout

- [x] Add Bootstrap 5 + Bootstrap Icons (local or CDN, lightweight).
- [x] Build `_Layout.cshtml` with `lang="ar" dir="rtl"`.
- [x] Create Arabic site title/meta defaults.
- [x] Wire `wwwroot` CSS/JS bundles into the layout.
- [x] Confirm the site renders RTL correctly on mobile and desktop.

### Configuration

- [x] Use the Options pattern for settings.
- [x] Configure `ConnectionStrings` (SQL Server).
- [x] Create `ContactSettings` (PhoneNumber, WhatsAppNumber).
- [x] Create `LicenseSettings`.
- [x] Create `ApplicationUrl`.
- [x] Store sensitive values in user-secrets/environment variables — never hard-code secrets.

### Logging & Error Handling

- [x] Use `ILogger<T>` throughout.
- [x] Add a friendly 404 page.
- [x] Add a generic 500/error page (no stack traces exposed).
- [x] Add global exception handling that logs technical details server-side.
- [x] Add user-friendly validation error rendering.

### ✅ Phase 1 Verification Gate

- [x] `dotnet restore` succeeds.
- [x] `dotnet build` succeeds.
- [x] `dotnet run` starts and the Arabic RTL home page renders.

---

## Phase 2 — Database and Domain

**Reference:** §11, §12, §19, §25, §26, §35, §40, §42-P2

### Entities

- [x] `ApplicationUser` (ASP.NET Core Identity).
- [x] `Customer` — Id, FullName, Phone, WhatsAppNumber, Notes, CreatedAt, UpdatedAt.
- [x] `Plan` — Name, Duration, DurationUnit, Price, Currency, IsTrial, IsActive, DisplayOrder, Description.
- [x] `SubscriptionRequest` — Customer, Plan, requested fields, status.
- [x] `Subscription` — Customer, Plan, Status, StartDate, EndDate, LicenseKey + snapshots:
  - `PlanNameSnapshot`, `PriceSnapshot`, `CurrencySnapshot`, `DurationSnapshot`.
- [x] `ContactSettings` entity/config for configurable phone/WhatsApp.
- [x] `AuditLog` — Id, EntityName, EntityId, Action, Description, UserId, CreatedAt.

### Enums & Constants

- [x] `SubscriptionStatus` enum: Pending, Active, Expired, Cancelled, Rejected.
- [x] `DurationUnit` enum (Day, Week, Month, Year) for plans.
- [x] Constants for roles (e.g., `Admin`) and any repeated strings.

### EF Core Configuration

- [x] `ApplicationDbContext` for the Identity + domain entities.
- [x] GUID/UUID primary keys on domain entities.
- [x] Fluent API configurations for important relationships:
  - Customer → SubscriptionRequests
  - Customer → Subscriptions
  - Plan → SubscriptionRequests
  - Plan → Subscriptions
- [x] Proper indexes (Phone/WhatsApp on Customer, LicenseKey on Subscription, Status/EndDate on Subscription).
- [x] No hard deletes where history must be preserved.

### Migrations & Seed

- [x] Create initial EF Core migration.
- [x] Seed the 5 plans (تجربة مجانية / شهر واحد / 3 أشهر / سنة واحدة / سنتان) with placeholder prices from config, `IsTrial` on the free trial.
- [x] Seed an initial development `Admin` user + `Admin` role via secure seed/config mechanism (no real production password committed).
- [x] Apply the migration.

### ✅ Phase 2 Verification Gate

- [x] `dotnet ef database update` applies cleanly.
- [x] Seed data exists in the database (5 plans, admin user in Admin role).
- [x] All relationships and indexes are present.

---

## Phase 3 — Public Website

**Reference:** §5, §6, §7, §8, §29, §30, §43, §42-P3

### Hero

- [x] Hero with product name دكان.
- [x] Main message: `محلّك في جيبك — والإنترنت مش شرط.`
- [x] Supporting statement (offline sales, inventory, debts, counting, profit reports).
- [x] Primary CTA: `اطلب اشتراكك الآن` → scrolls/opens subscription form.
- [x] Secondary CTA: `تعرّف على المزايا`.
- [x] WhatsApp CTA: `تواصل معنا عبر واتساب`.
- [x] Product visual/mockup area.

### Core Benefits

- [x] يعمل بدون إنترنت
- [x] أجهزتك تعمل معاً
- [x] حساب الزبون واضح
- [x] يفهم طريقة عمل البقالة
- [x] جرد بدون إغلاق المحل
- [x] تعرف ربحك

### Feature Sections

- [x] البيع اليومي — barcode scan, flash, multiple barcodes, cash/bank/debt, per-item discount, buyer tracking, editing saved sales.
- [x] المخزون والتسعير — piece/carton/box/kilo, weight selling, per-level price, quantity price, item movement log, item merging, price-error audit.
- [x] الزبائن والديون — full customer account, family members, multiple collection methods, ready account-summary message, internal accounts.
- [x] بوابة الزبون — phone+code login, view purchases/sale details/buyer, never expose cost/margin, no shop data on customer device.
- [x] التقارير والأرباح — sales, COGS, gross profit, expenses, net profit, top items, fridge profits, inventory value, latent profit, expense categories, payment methods.
- [x] الجرد — area-based sessions, scan/manual/weight counting, difference review, device-bound session, correction log.
- [x] المزامنة — `المحل واحد ولو تعددت الأجهزة.` (offline-first sync, no real-time claims).
- [x] الحماية من الخطأ البشري — trash/recovery, unmerge, created-by/edited-by, edit time, delete-or-reverse-stock options.

### Pricing Section

- [x] Render plans **from the database**, never hard-coded in views.
- [x] Free trial plan displayed as free (7 أيام).
- [x] Other plans show price/currency from DB (placeholders allowed).
- [x] Plans filtered to active ones only.
- [x] Pricing CTA: `اختر خطتك`.

### CTAs & Footer

- [x] Hero CTA: `اطلب اشتراكك الآن`.
- [x] Bottom CTA: `جاهز تبدأ مع دكان؟`.
- [x] WhatsApp CTA: `تحدث معنا عبر واتساب`.
- [x] Footer: دكان name, tagline, phone + WhatsApp (configurable), © {year} دكان / جميع الحقوق محفوظة.
- [x] No invented address/email/social links.

### SEO & Metadata

- [x] Title: `دكان — نظام إدارة البقالة والمتجر الصغير`.
- [x] Meta description (Arabic, from spec).
- [x] Open Graph metadata.
- [x] Semantic HTML + proper H1/H2 hierarchy.
- [x] Descriptive Arabic alt text on images.
- [x] No unsupported claims (no iOS, receipts, tax invoices, supplier/purchasing, multi-branch, online payments, web app for subscribers, etc.).

### ✅ Phase 3 Verification Gate

- [x] Landing page renders fully in Arabic RTL.
- [x] Responsive on mobile, tablet, and desktop (test with a phone viewport).
- [x] All CTAs lead to the correct targets.
- [x] Pricing comes from the database.

---

## Phase 4 — Subscription Request

**Reference:** §9, §10, §15, §42-P4

### Form & Validation

- [x] Arabic RTL subscription request form with fields:
  - الاسم الكامل *
  - رقم الهاتف *
  - رقم الواتساب *
  - الخطة المطلوبة * (dropdown of active plans only)
  - ملاحظات
- [x] Anonymous submission — no customer account required.
- [x] Client-side validation (required, phone/WhatsApp format).
- [x] Server-side validation (required, phone/WhatsApp format, plan exists, plan is active).
- [x] Do not trust hidden form fields — PlanId validated server-side.
- [x] Use DTO/ViewModel for the form (no entity binding).
- [x] Anti-forgery token on the form.

### Service Logic

- [x] `SubscriptionRequestService` handles creation (thin controller).
- [x] Find-or-create customer using normalized Phone/WhatsApp (no duplicates).
- [x] Request starts as `Pending`.
- [x] Log request creation.

### Success Page & WhatsApp

- [x] Success page: `تم إرسال طلب الاشتراك بنجاح.`
- [x] WhatsApp follow-up CTA: `تواصل معنا عبر واتساب لإكمال الإجراءات.`
- [x] WhatsApp link (configurable number) with prefilled message: `مرحباً، أرسلت طلب اشتراك في دكان وأرغب في متابعة الطلب.`
- [x] No hard-coded production phone number.

### ✅ Phase 4 Verification Gate

- [x] Full workflow tested: submit → pending → success page → WhatsApp link works.
- [x] Invalid/inactive plan rejected server-side.
- [x] Duplicate customer submissions reuse the customer record.

---

## Phase 5 — Admin Authentication

**Reference:** §16, §27, §42-P5

- [x] ASP.NET Core Identity wired with cookie auth.
- [x] `Admin` role exists.
- [x] Admin login page (Arabic RTL).
- [x] Logout.
- [x] All `/Admin/*` endpoints require `[Authorize(Roles = "Admin")]`.
- [x] Authorization enforced server-side — never rely on hidden UI alone.
- [x] Secure cookie settings (HttpOnly, SameSite, HTTPS in production).

### ✅ Phase 5 Verification Gate

- [x] Logging in as admin reaches the dashboard.
- [x] Unauthenticated access to `/Admin/*` is redirected/blocked.
- [x] Non-admin users cannot access `/Admin/*` even if they type the URL.

---

## Phase 6 — Admin Dashboard

**Reference:** §17, §18, §19, §20, §21, §22, §36, §42-P6

### Overview

- [x] Dashboard home with cards: إجمالي العملاء، الاشتراكات النشطة، طلبات الاشتراك الجديدة، الاشتراكات المنتهية.
- [x] Recent subscription requests list.
- [x] Recently activated subscriptions.
- [x] Subscriptions expiring soon.
- [x] No meaningless charts.

### Plan Management

- [x] Create plan.
- [x] Edit plan (name, price, currency, duration, description, display order, trial flag).
- [x] Activate/deactivate plan (soft — no deletion of plans with history).
- [x] Prevent deletion of a plan that has historical subscriptions.
- [x] Changing price/duration must not alter existing subscriptions (snapshots preserved).
- [x] Log plan price changes to the audit log.

### Customer Management

- [x] Customer list (paged).
- [x] Customer detail: info, pending requests, active subscription, previous subscriptions, full history.
- [x] Customer edit (notes etc.).

### Subscription Requests

- [x] Request list with columns: Customer, Phone, Plan, Request Date, Status, Actions.
- [x] Filter by status (Pending, Approved/Active, Rejected, Cancelled, Expired).
- [x] Pagination.
- [x] Actions: View, Approve/Activate, Reject, Cancel.
- [x] Confirmation dialogs for status-changing/destructive actions.
- [x] Log approve/reject/cancel.

### Subscription Management

- [x] Subscription detail page: Customer, Plan, Status, Request Date, Start Date, End Date, License Key, Price, Currency, Admin Notes.
- [x] Actions: Activate, Cancel/Deactivate, Renew, Regenerate License.
- [x] Only show actions valid for the current status (no invalid transitions in UI).
- [x] Admin notes field.

### ✅ Phase 6 Verification Gate

- [x] Admin can perform each management flow end-to-end.
- [x] Plan price change does not affect historical subscriptions.
- [x] Status actions are guarded by the state machine.

---

## Phase 7 — Subscription Engine

**Reference:** §12, §13, §23, §24, §39, §42-P7

### State Machine (in a service, not controllers)

- [x] `Pending` → `Active` (Activate) or `Rejected` (Reject).
- [x] `Active` → `Expired` (expiration) or `Cancelled` (Cancel).
- [x] `Expired` → `Active` only via explicit renewal.
- [x] Forbid `Rejected → Active` and `Cancelled → Active` without an explicit renewal/reactivation operation.
- [x] Central `SubscriptionService` owning all transitions.

### Activation & Dates

- [x] On activation: StartDate set automatically (admin may override).
- [x] EndDate computed from plan duration using calendar arithmetic (weeks/months/years — not fixed 30/365 days).
- [x] Auto-detect expired subscriptions (status flipped to Expired).
- [x] Never physically delete expired subscriptions.

### Renewals

- [x] Renew creates a new subscription record (preserves history, never overwrites old).
- [x] New license generated on renewal where appropriate.
- [x] Log renewal.

### License Generation

- [x] Cryptographically random, unique license key (not sequential IDs, not DB PKs).
- [x] License model leaves room for future device binding: `LicenseKey, SubscriptionId, Status, ActivatedAt, LastValidatedAt, DeviceId (nullable)` — DeviceId stays null for MVP.
- [x] Log license generation (avoid logging full keys where possible).

### Automated Tests

- [x] **Plan:** duration validation; inactive plans cannot receive requests.
- [x] **Subscription:** activation, expiration calculation, cancellation, renewal, invalid state transitions.
- [x] **License:** unique generation; validation for active / expired / cancelled subscriptions.
- [x] **Customer:** duplicate handling.
- [x] **Request:** required fields, invalid plan, inactive plan.
- [x] Focus on business rules, not getters/setters.

### ✅ Phase 7 Verification Gate

- [x] `dotnet test` passes all business-rule tests.
- [x] Activation computes correct EndDate for week/month/3-month/year/2-year plans.
- [x] Expired subscriptions are recognized without manual intervention.
- [x] Renewal preserves history.

---

## Phase 8 — Android Integration Foundation

**Reference:** §13, §14, §34, §42-P8

- [x] API routes under `/api/v1/`.
- [x] `POST /api/v1/subscriptions/validate` accepting `{ "licenseKey": "..." }`.
- [x] Response contains only Android-required info, e.g. `{ "valid": true, "status": "Active", "expiresAt": "..." }`.
- [x] Do not expose customer info, admin info, pricing, or sensitive DB identifiers.
- [x] Secure the endpoint appropriately (this is the future Android licensing API).
- [x] Log validation API requests/results (no full license keys where avoidable).
- [x] API tests: active, expired, cancelled, and unknown/invalid license keys.
- [x] Do not modify the Android application (no source provided).

### ✅ Phase 8 Verification Gate

- [x] Validate endpoint works for all license states.
- [x] Response contains only the documented fields.
- [x] Tests green.

---

## Phase 9 — Polish & Definition of Done

**Reference:** §31, §41, §44, §45, §48, §49, §42-P9

### Quality Review

- [x] Arabic UX review across public site + admin (labels, messages, terminology).
- [x] RTL correctness on all pages.
- [x] Responsiveness on mobile/tablet/desktop (form especially).
- [x] Large touch targets, clear labels, sticky CTA where appropriate.
- [x] Validation messages associated with fields (client + server).
- [x] Accessibility: semantic HTML, keyboard navigation, focus states, contrast, no color-only status.
- [x] Security review: anti-forgery, overposting protection, ID validation, no secrets/connection strings/stack traces exposed, parameterized queries.
- [x] Performance: pagination on admin tables, lazy-load non-critical images, minimize JS, caching where useful.
- [x] SEO/metadata confirmed.
- [x] README written (§41): overview, features, architecture, requirements, configuration, database setup, migration commands, running locally, admin login setup, seed data, deployment notes, API overview, future Android integration.
- [x] Coding standards: nullable reference types, DI, async, cancellation tokens, small services, DTOs/ViewModels, Fluent API, indexes, no magic strings/numbers, no hard-coded prices/contacts.

### Final Definition of Done (from §48)

- [x] Application builds successfully.
- [x] Database migrations work.
- [x] Admin can log in.
- [x] Admin can manage plans.
- [x] Admin can change plan prices.
- [x] Customer can submit subscription request without an account.
- [x] Customer can select only active plans.
- [x] Admin can see requests.
- [x] Admin can activate subscriptions.
- [x] Subscription dates are calculated correctly.
- [x] Admin can cancel/deactivate subscriptions.
- [x] Expired subscriptions are recognized.
- [x] Customers are maintained separately from requests.
- [x] Subscription history is preserved.
- [x] License keys are generated securely.
- [x] License validation API works.
- [x] Public landing page is responsive.
- [x] Website is Arabic RTL.
- [x] WhatsApp CTA works using configuration.
- [x] Phone contact is configurable.
- [x] Validation works client-side and server-side.
- [x] Authorization is enforced server-side.
- [x] Anti-forgery protection is enabled.
- [x] Important business rules have tests.
- [x] No production secrets are committed.
- [x] README explains setup and deployment.

### ✅ Final Gate

- [x] `dotnet restore` / `dotnet build` / `dotnet test` / `dotnet run` all green.
- [x] Full manual walkthrough of the customer journey + admin workflows passes.
