# دكان — Subscription Platform & Marketing Website

## Production Master Prompt for an AI Coding Agent

You are a senior full-stack .NET engineer and solution architect with 10+ years of experience building production-grade ASP.NET Core applications.

Your task is to design and implement a production-ready web platform for the Android application **"دكان"**.

The platform consists of three major parts:

1. A public Arabic landing/marketing website for the دكان application.
2. A customer subscription-request workflow.
3. A secure admin dashboard for managing plans, customers, subscription requests, and active subscriptions.

The existing Android application is called **دكان** and is an Android-only grocery/small-store management application.

The subscription/license validation mechanism does **not currently exist in the Android application**. Therefore, implement the web platform and the backend subscription/licensing foundation so that the Android application can integrate with it later through a secure API.

Do NOT invent features for the Android application that are not described in the provided product specification.

---

# 1. Product Context

دكان is a management system designed specifically for grocery stores and small shops.

The product is designed around real-world grocery-store workflows rather than generic accounting software.

Important product characteristics include:

- Fully offline operation.
- Sales continue when the Internet is unavailable.
- Synchronization between multiple devices in the same store.
- Arabic-first interface.
- Inventory management.
- Customer accounts and debts.
- Customer portal.
- Multiple packaging levels such as piece, carton, box, and kilogram.
- Weight-based selling.
- Different pricing for shelf and refrigerator sales.
- Inventory counting without closing the store.
- Profit and inventory reporting.
- Multiple payment methods.
- Audit/history and recovery mechanisms.

The provided product specification explicitly states that the current application does NOT include:

- Printed receipts.
- Thermal printer support.
- Tax invoices.
- Income-tax integration.
- Supplier/purchasing management.
- Multi-branch support.
- iOS application.
- Web version for subscribers.

Do not advertise or promise these capabilities.

The product specification states that the documented features are implemented in the existing application, not merely planned.

---

# 2. Technology Stack

Use the following technology stack unless there is a compelling technical reason to change it.

## Backend

- ASP.NET Core 10
- C#
- ASP.NET Core MVC
- Entity Framework Core 10
- ASP.NET Core Identity
- REST API for future Android integration
- preferred
- Dependency Injection
- Async/await
- Data Annotations and/or FluentValidation
- Built-in logging abstractions

## Frontend

Do NOT use React, Angular, Vue, or another SPA framework.

Use:

- Razor Views
- Bootstrap 5
- Modern JavaScript
- HTML5
- CSS3
- Bootstrap Icons or an equivalent lightweight icon library

Use unobtrusive AJAX/fetch where appropriate.

The UI must remain simple, fast, maintainable, and easy for another .NET developer to extend.

---

# 3. Language and UI Direction

The entire website and dashboard should be:

- Arabic
- RTL
- Mobile responsive
- Desktop responsive
- Clean
- Modern
- Professional
- Suitable for small-business owners

Arabic is the primary language.

Do not produce an English-first interface.

Use Arabic terminology consistently.

The design should feel like a modern SaaS product while remaining extremely simple.

Avoid excessive animations.

Avoid unnecessary gradients and visual complexity.

Prioritize:

- readability
- trust
- simplicity
- conversion
- speed
- accessibility

---

# 4. Project Architecture

Use a clean, maintainable architecture.

Recommended structure:

```text
Dukan.Web
│
├── Areas
│   └── Admin
│       ├── Controllers
│       └── Views
│
├── Controllers
│   ├── HomeController
│   ├── SubscriptionController
│   └── ...
│
├── Data
│   ├── ApplicationDbContext.cs
│   ├── Configurations
│   └── Migrations
│
├── Domain
│   ├── Entities
│   ├── Enums
│   └── Constants
│
├── Application
│   ├── DTOs
│   ├── Interfaces
│   ├── Services
│   └── Validators
│
├── Infrastructure
│   ├── Services
│   └── Identity
│
├── API
│   └── Controllers
│
├── Views
│   ├── Home
│   ├── Subscription
│   └── Shared
│
├── wwwroot
│   ├── css
│   ├── js
│   └── images
│
└── Program.cs
```

Keep MVC controllers thin.

Business rules must not be implemented directly inside controllers.

Use services/application classes for:

- plan management
- subscription requests
- subscription activation
- subscription expiration
- license generation
- customer management
- notification preparation

---

# 5. Main Public Website

Create a professional Arabic landing page.

The landing page should be designed primarily to convert grocery-store owners into subscription requests.

## Hero Section

The hero should immediately communicate the biggest product advantage.

Suggested positioning:

> محلّك في جيبك — والإنترنت مش شرط.

Use the exact marketing idea from the product specification where appropriate, but improve the presentation and copywriting without inventing product capabilities.

Include:

- Product name: دكان
- Short supporting statement
- Primary CTA
- Secondary CTA
- Product visual/mockup area

Primary CTA:

> اطلب اشتراكك الآن

Secondary CTA:

> تعرّف على المزايا

The primary CTA should scroll to or open the subscription request form.

---

# 6. Landing Page Content

Build the landing page using the actual product capabilities.

Recommended sections:

## 6.1 Hero

Main message:

> محلّك في جيبك — والإنترنت مش شرط.

Supporting idea:

دكان يساعدك على إدارة البيع والمخزون وديون الزبائن والجرد وتقارير الربح، حتى عندما ينقطع الإنترنت.

CTA:

> اطلب اشتراكك الآن

WhatsApp CTA:

> تواصل معنا عبر واتساب

---

## 6.2 Core Benefits

Present the strongest differentiators.

Examples:

### يعمل بدون إنترنت

البيع والجرد والتقارير تستمر حتى عند انقطاع الإنترنت، وتتم المزامنة عند عودة الاتصال.

### أجهزتك تعمل معاً

يمكن لأجهزة المحل العمل معاً ومزامنة البيانات.

### حساب الزبون واضح

الزبون يستطيع الاطلاع على مشترياته ودفعاته والدين المتبقي دون الوصول إلى التكلفة أو هامش الربح.

### يفهم طريقة عمل البقالة

حبة، كرتونة، صندوق، كيلو، وبيع بالوزن.

### جرد بدون إغلاق المحل

يمكن إنشاء جلسات جرد مستقلة للمناطق أو الرفوف ومراجعة الفروقات قبل الاعتماد.

### تعرف ربحك

تقارير المبيعات، تكلفة البضاعة، الربح الإجمالي، المصاريف، وصافي الربح.

---

# 7. Feature Sections

Create visually attractive feature sections.

Group features logically.

## البيع اليومي

Include actual documented features such as:

- مسح الباركود بكاميرا الجوال
- تشغيل الفلاش تلقائياً في الفترة الليلية
- عدة باركودات للصنف
- نقدي / بنكي / دين
- حسم على مستوى الصنف
- تسجيل من اشترى
- تعديل البيعة المحفوظة

---

## المخزون والتسعير

Include:

- حبة
- كرتونة
- صندوق
- كيلو
- البيع بالوزن
- سعر لكل مستوى
- سعر الكمية
- سجل حركة الصنف
- دمج الأصناف
- تدقيق أخطاء التسعير

---

## الزبائن والديون

Include:

- حساب كامل للزبون
- أفراد العائلة على حساب واحد
- طرق تحصيل متعددة
- رسالة جاهزة بملخص الحساب
- الحسابات الداخلية

---

## بوابة الزبون

This is a major selling point.

Explain:

- دخول برقم الجوال ورمز
- رؤية المشتريات
- رؤية تفاصيل كل بيعة
- معرفة من قام بالشراء
- عدم رؤية التكلفة أو هامش الربح
- عدم تحميل بيانات المحل على جهاز الزبون

Do not imply that the customer portal is a separate mobile application.

---

## التقارير والأرباح

Include:

- المبيعات
- تكلفة البضاعة المباعة
- الربح الإجمالي
- المصاريف
- صافي الربح
- أفضل الأصناف
- أرباح الثلاجة
- قيمة المخزون
- الربح الكامن
- المصاريف حسب التصنيف
- توزيع طرق الدفع

---

## الجرد

Include:

- جلسات جرد بالمناطق
- العد بالمسح أو اليد
- العد بالوزن
- مراجعة الفروقات
- جلسة مرتبطة بجهازها
- سجل تصحيح الجرد

---

## المزامنة

Highlight:

> المحل واحد ولو تعددت الأجهزة.

Explain that data entered during an Internet outage is synchronized after connectivity returns.

Do not claim real-time synchronization if the actual application specification does not guarantee it. Use the documented behavior.

---

## الحماية من الخطأ البشري

Include:

- سلة المحذوفات
- استرجاع البيانات
- التراجع عن دمج الأصناف
- تسجيل من أنشأ وعدّل
- وقت التعديل
- خيارات حذف الحركة أو عكس أثرها على المخزون

---

# 8. Pricing Section

Create a pricing section containing the five configurable plans:

1. تجربة مجانية — أسبوع
2. شهر واحد
3. 3 أشهر
4. سنة واحدة
5. سنتان

Prices are placeholders initially.

DO NOT hard-code prices in views.

Prices must come from the database.

Each plan must support:

```text
Name
Duration
DurationUnit
Price
Currency
IsTrial
IsActive
DisplayOrder
Description
```

Admin must be able to change all pricing information from the dashboard.

Example placeholder data may be:

```text
تجربة مجانية
7 أيام
مجاني

شهري
1 شهر
PLACEHOLDER

ربع سنوي
3 أشهر
PLACEHOLDER

سنوي
1 سنة
PLACEHOLDER

سنتان
2 سنة
PLACEHOLDER
```

Clearly structure the system so placeholder prices can later be replaced without code changes.

---

# 9. Subscription Request Form

Create a simple Arabic RTL subscription request form.

Fields:

```text
الاسم الكامل *
رقم الهاتف *
رقم الواتساب *
الخطة المطلوبة *
ملاحظات
```

No customer account is required.

The customer can submit the request anonymously.

Validation must happen:

- client side
- server side

Validate:

- required fields
- phone format
- WhatsApp format
- selected plan exists
- selected plan is active

Do not trust hidden form fields.

The selected PlanId must be validated server-side.

---

# 10. Subscription Request Workflow

The workflow should be:

```text
Customer
   ↓
Landing Page
   ↓
Select Plan
   ↓
Subscription Request
   ↓
Pending
   ↓
Admin Reviews
   ↓
Approve
   ↓
Subscription Activated
   ↓
License Generated
   ↓
Customer contacted via WhatsApp
```

Rejected requests should also be supported.

---

# 11. Subscription Statuses

Use an enum:

```csharp
public enum SubscriptionStatus
{
    Pending,
    Active,
    Expired,
    Cancelled,
    Rejected
}
```

Do not use a simple boolean such as:

```csharp
IsActive
```

for the subscription itself.

A subscription has a lifecycle.

---

# 12. Subscription Dates

When the admin activates a subscription:

- StartDate is automatically calculated/set.
- EndDate is calculated from the selected plan duration.

Examples:

```text
1 Week
7 days

1 Month
1 calendar month

3 Months
3 calendar months

1 Year
1 calendar year

2 Years
2 calendar years
```

Use proper date arithmetic.

Do not approximate one month as 30 days or one year as 365 days unless explicitly required.

Allow authorized administrators to override the start date when necessary.

The system should automatically recognize expired subscriptions.

Do not physically delete expired subscriptions.

---

# 13. License / Activation System

The Android app currently has no subscription validation mechanism.

Build the backend foundation for it.

Each active subscription should have a unique license/activation identifier.

Recommended conceptual model:

```text
Customer
   │
   ├── Phone
   └── WhatsApp
          │
          ▼
Subscription
   │
   ├── Plan
   ├── Status
   ├── StartDate
   ├── EndDate
   └── LicenseKey
```

Generate a cryptographically random license key.

Do not use sequential IDs as license keys.

Do not expose database primary keys as security credentials.

The license key should be unique.

Prepare an API endpoint for future Android integration.

For example:

```text
POST /api/v1/subscriptions/validate
```

Request:

```json
{
  "licenseKey": "..."
}
```

Response should contain only the information the Android application needs.

For example:

```json
{
  "valid": true,
  "status": "Active",
  "expiresAt": "..."
}
```

Do not expose:

- internal customer information
- admin information
- pricing information
- sensitive database identifiers

The exact Android integration can be completed later.

---

# 14. Future Device Binding

Design the license model so that future device binding can be added.

Do not implement complex device fingerprinting unless explicitly required.

Leave room for:

```text
License
 ├── LicenseKey
 ├── SubscriptionId
 ├── Status
 ├── ActivatedAt
 ├── LastValidatedAt
 └── DeviceId (nullable)
```

For MVP, DeviceId may remain null.

---

# 15. WhatsApp Integration

The MVP does not need a WhatsApp API integration.

Instead, provide click-to-WhatsApp functionality.

Use a configurable WhatsApp number.

Configuration:

```text
ContactSettings
- PhoneNumber
- WhatsAppNumber
```

When the customer submits a subscription request, the success page should provide:

> تم إرسال طلب الاشتراك بنجاح.

and:

> تواصل معنا عبر واتساب لإكمال الإجراءات.

Create a WhatsApp link containing a useful prefilled message where technically appropriate.

Example message:

> مرحباً، أرسلت طلب اشتراك في دكان وأرغب في متابعة الطلب.

Do not hard-code the actual production phone number.

---

# 16. Admin Authentication

Use ASP.NET Core Identity.

Admin pages must require authentication.

Use role-based authorization.

Create:

```text
Admin
```

role.

All `/Admin/*` endpoints must require the Admin role.

Never rely only on hiding UI buttons for authorization.

Authorization must be enforced server-side.

---

# 17. Admin Dashboard

Create a clean Arabic RTL admin dashboard.

Main navigation:

```text
لوحة التحكم

الخطط
طلبات الاشتراك
العملاء
الاشتراكات
الإعدادات
```

---

# 18. Dashboard Overview

The dashboard home should show cards such as:

```text
إجمالي العملاء
الاشتراكات النشطة
طلبات الاشتراك الجديدة
الاشتراكات المنتهية
```

Also show:

- recent subscription requests
- recently activated subscriptions
- subscriptions expiring soon

Do not add meaningless charts simply to make the dashboard look complex.

---

# 19. Plan Management

Admin can:

- Create plan
- Edit plan
- Activate/deactivate plan
- Change price
- Change currency
- Change duration
- Change description
- Change display order
- Mark plan as trial

Do not allow deletion of a plan that has historical subscriptions.

Use soft deactivation instead.

Existing subscriptions must preserve their historical plan/pricing information.

This is important.

If a plan changes from $10 to $15, old subscriptions must not suddenly appear to have cost $15.

Therefore consider storing a snapshot:

```text
Subscription
- PlanId
- PlanNameSnapshot
- PriceSnapshot
- CurrencySnapshot
- DurationSnapshot
```

---

# 20. Customer Management

Customers are created automatically when a subscription request is submitted.

Customer fields:

```text
Customer
- Id
- FullName
- Phone
- WhatsAppNumber
- Notes
- CreatedAt
- UpdatedAt
```

A customer can have multiple subscription requests/subscriptions over time.

Do not create duplicate customers unnecessarily.

Use normalized phone/WhatsApp values where practical.

Customer details should show:

- customer information
- subscription history
- active subscription
- previous subscriptions
- pending requests

---

# 21. Subscription Requests

Admin list should support:

- Pending
- Approved/Active
- Rejected
- Cancelled
- Expired

Columns:

```text
Customer
Phone
Plan
Request Date
Status
Actions
```

Actions:

```text
View
Approve/Activate
Reject
Cancel
```

Use confirmation dialogs for destructive/status-changing actions.

---

# 22. Subscription Details

Subscription detail page should show:

```text
Customer
Plan
Status
Request Date
Start Date
End Date
License Key
Price
Currency
Admin Notes
```

Actions:

```text
Activate
Deactivate/Cancel
Renew
Regenerate License
```

Only implement actions that make sense for the current status.

Do not allow invalid state transitions.

---

# 23. Subscription State Machine

Implement clear business rules.

Examples:

```text
Pending
  ├── Activate → Active
  └── Reject → Rejected

Active
  ├── Expiration → Expired
  └── Cancel → Cancelled

Expired
  └── Renew → Active
```

Do not allow:

```text
Rejected → Active
Cancelled → Active
```

without an explicit reactivation/renewal business operation.

Keep state transitions inside a service rather than controllers.

---

# 24. Renewals

Support renewal from the dashboard.

When renewing an existing customer:

- create a new subscription record
- preserve historical subscriptions
- do not overwrite the old subscription
- generate a new license if appropriate

Never destroy subscription history.

---

# 25. Database Entities

At minimum create:

```text
ApplicationUser
Customer
Plan
SubscriptionRequest
Subscription
```

Potential supporting entities:

```text
ContactSettings
AuditLog
```

Recommended relationships:

```text
Customer
   │
   ├── SubscriptionRequests
   │
   └── Subscriptions

Plan
   │
   ├── SubscriptionRequests
   │
   └── Subscriptions

SubscriptionRequest
   ├── Customer
   └── Plan

Subscription
   ├── Customer
   └── Plan
```

Use GUID/UUID primary keys unless there is a strong reason not to.

---

# 26. Auditability

Subscription activation/deactivation is important business activity.

Track:

```text
CreatedAt
CreatedBy
UpdatedAt
UpdatedBy
```

for important administrative entities.

For important subscription actions, consider an audit log:

```text
AuditLog
- Id
- EntityName
- EntityId
- Action
- Description
- UserId
- CreatedAt
```

At minimum record:

- activation
- cancellation
- rejection
- renewal
- plan price change

---

# 27. Security

Follow ASP.NET Core security best practices.

Requirements:

- HTTPS
- Anti-forgery protection for MVC POST forms
- ASP.NET Core Identity
- Role authorization
- Server-side validation
- Secure password hashing through Identity
- No passwords stored manually
- No sensitive information in URLs
- Parameterized EF Core queries
- Protection against overposting
- Validation of all IDs
- Proper error handling
- Do not expose stack traces in production
- Do not expose database connection strings
- Do not expose secrets in source control

Use DTO/ViewModel classes rather than binding entities directly from public forms.

---

# 28. Configuration

Use configuration/options for:

```text
ConnectionStrings
ContactSettings
WhatsAppNumber
PhoneNumber
ApplicationUrl
LicenseSettings
```

Use environment variables/user secrets for sensitive values.

Never hard-code production secrets.

---

# 29. Landing Page CTA

There should be multiple conversion opportunities.

Examples:

Hero:

> اطلب اشتراكك الآن

Pricing:

> اختر خطتك

Bottom CTA:

> جاهز تبدأ مع دكان؟

WhatsApp:

> تحدث معنا عبر واتساب

All CTAs should lead to the appropriate action.

---

# 30. Landing Page Footer

Footer should contain:

```text
دكان

نظام إدارة البقالة والمتجر الصغير.

الهاتف
واتساب

© {year} دكان
جميع الحقوق محفوظة.
```

Do not invent:

- physical address
- email
- social media accounts

unless provided later.

---

# 31. UX Requirements

The website should work well on:

- Mobile
- Tablet
- Desktop

The subscription form must be particularly mobile-friendly because many customers will access it through WhatsApp or a phone.

Use:

- large touch targets
- clear labels
- readable Arabic text
- obvious validation
- simple forms
- sticky CTA where appropriate

Avoid huge forms.

---

# 32. Error Handling

Use proper MVC error handling.

Implement:

- 404 page
- generic 500 page
- validation messages
- user-friendly errors

Do not expose technical exceptions to end users.

Log technical exceptions server-side.

---

# 33. Logging

Use `ILogger<T>`.

Log important events such as:

- subscription request created
- subscription activated
- subscription rejected
- subscription cancelled
- subscription renewed
- license generated
- license validation API request/result

Do not log:

- passwords
- sensitive credentials
- unnecessary personal information
- complete license keys where avoidable

---

# 34. API Versioning

Prepare API routes under:

```text
/api/v1/
```

Example:

```text
POST /api/v1/subscriptions/validate
```

The API should be designed primarily for future Android integration.

Do not build a large unnecessary API surface.

---

# 35. Seed Data

Create development seed data.

Plans:

```text
تجربة مجانية
7 أيام
IsTrial = true

شهر واحد
1 شهر
Placeholder Price

3 أشهر
3 أشهر
Placeholder Price

سنة واحدة
1 سنة
Placeholder Price

سنتان
2 سنة
Placeholder Price
```

Create an initial development admin user through a secure seed/configuration mechanism.

Do not commit a real production password.

---

# 36. Admin Dashboard UX

Use Bootstrap components:

- Cards
- Tables
- Badges
- Modals
- Dropdowns
- Alerts
- Forms

Use status badges:

```text
Pending
Active
Expired
Cancelled
Rejected
```

with Arabic labels.

Use clear colors semantically, but do not make the UI dependent on color alone.

---

# 37. JavaScript

Use modern JavaScript.

Do not introduce jQuery unless genuinely useful.

Use:

```text
fetch()
async/await
FormData
DOMContentLoaded
```

where appropriate.

JavaScript should enhance the application, not contain core business logic.

---

# 38. No Overengineering

This is a relatively small platform.

Do NOT introduce:

- Microservices
- Message brokers
- Kubernetes
- Redis unless required
- CQRS framework
- MediatR solely because it is fashionable
- Event sourcing
- Complex frontend frameworks
- unnecessary repository abstractions over EF Core

Prefer simple, understandable code.

---

# 39. Testing

Create tests for important business logic.

At minimum test:

## Plan

- duration validation
- inactive plans cannot receive requests

## Subscription

- activation
- expiration calculation
- cancellation
- renewal
- invalid state transitions

## License

- unique generation
- active subscription validation
- expired subscription validation
- cancelled subscription validation

## Customer

- duplicate handling where applicable

## Request

- required fields
- invalid plan
- inactive plan

Focus testing on business rules rather than trivial getters/setters.

---

# 40. Database Migrations

Create EF Core migrations.

The project must be runnable using:

```bash
dotnet restore
dotnet build
dotnet ef database update
dotnet run
```

Document migration commands in the README.

---

# 41. README

Create a professional README containing:

- Project overview
- Features
- Architecture
- Requirements
- Configuration
- Database setup
- Migration commands
- Running locally
- Admin login setup
- Seed data
- Deployment notes
- API overview
- Future Android integration

---

# 42. Development Process

Do NOT generate the entire project blindly in one pass.

Work in phases.

## Phase 1 — Foundation

Implement:

- ASP.NET Core MVC
- Bootstrap
- Arabic RTL layout
- EF Core
- Identity
- SQLSERVER
- project structure
- configuration
- logging
- global error handling

Verify the application builds and runs.

---

## Phase 2 — Database and Domain

Implement:

- Customer
- Plan
- SubscriptionRequest
- Subscription
- enums
- relationships
- configurations
- migrations
- seed data

Verify database creation and migrations.

---

## Phase 3 — Public Website

Implement:

- Landing page
- hero
- product benefits
- feature sections
- pricing
- CTA
- WhatsApp
- footer

Verify mobile responsiveness.

---

## Phase 4 — Subscription Request

Implement:

- subscription request form
- validation
- customer creation/update
- pending requests
- success page
- WhatsApp follow-up CTA

Test the entire workflow.

---

## Phase 5 — Admin Authentication

Implement:

- Identity
- Admin role
- admin login
- authorization
- protected admin area

---

## Phase 6 — Admin Dashboard

Implement:

- overview
- plan management
- customer management
- subscription requests
- subscription management

---

## Phase 7 — Subscription Engine

Implement:

- activation
- expiration
- cancellation
- renewal
- status transitions
- license generation

Add automated tests.

---

## Phase 8 — Android Integration Foundation

Implement:

```text
/api/v1/subscriptions/validate
```

Add secure license validation.

Do not attempt to modify the Android application unless its source code is explicitly provided.

---

## Phase 9 — Polish

Review:

- Arabic UX
- RTL
- responsiveness
- validation
- accessibility
- security
- performance
- logging
- error handling
- SEO
- metadata
- README

---

# 43. SEO

The public website should include:

- meaningful page title
- meta description
- Open Graph metadata
- semantic HTML
- proper H1/H2 hierarchy
- descriptive image alt text
- Arabic metadata

Suggested title:

> دكان — نظام إدارة البقالة والمتجر الصغير

Suggested description:

> دكان يساعدك على إدارة مبيعاتك ومخزونك وديون زبائنك وجردك وتقارير أرباحك، حتى بدون إنترنت.

Do not make unsupported SEO claims such as "أفضل نظام في فلسطين" unless explicitly provided.

---

# 44. Performance

Keep the website lightweight.

Requirements:

- minimize unnecessary JavaScript
- optimize images
- lazy-load non-critical images
- avoid unnecessary libraries
- use caching where useful
- async database access
- pagination for admin tables

Do not load large frontend frameworks.

---

# 45. Accessibility

Use:

- proper labels
- semantic HTML
- keyboard navigation
- accessible buttons
- visible focus states
- sufficient contrast
- validation messages associated with fields

Do not rely solely on color to communicate status.

---

# 46. Important Product Truthfulness Rule

The marketing website must only advertise functionality actually documented for دكان.

Do not claim:

- iOS support
- web app for subscribers
- printed receipts
- tax invoices
- supplier management
- purchasing
- multi-branch support
- integrations that do not exist
- online payment
- features not present in the supplied specification

If a feature is not documented, do not invent it.

---

# 47. Important Subscription Rule

The website subscription system is separate from the Android application's existing functionality.

The Android app currently does not have subscription validation.

Therefore:

1. Build the subscription management backend.
2. Generate licenses.
3. Provide the validation API.
4. Make the API ready for Android integration.
5. Do not falsely claim that the Android app already validates licenses.
6. Clearly separate MVP web-platform functionality from future Android integration.

---

# 48. Definition of Done

The project is complete only when:

- Application builds successfully.
- Database migrations work.
- Admin can log in.
- Admin can manage plans.
- Admin can change plan prices.
- Customer can submit subscription request without an account.
- Customer can select only active plans.
- Admin can see requests.
- Admin can activate subscriptions.
- Subscription dates are calculated correctly.
- Admin can cancel/deactivate subscriptions.
- Expired subscriptions are recognized.
- Customers are maintained separately from requests.
- Subscription history is preserved.
- License keys are generated securely.
- License validation API works.
- Public landing page is responsive.
- Website is Arabic RTL.
- WhatsApp CTA works using configuration.
- Phone contact is configurable.
- Validation works client-side and server-side.
- Authorization is enforced server-side.
- Anti-forgery protection is enabled.
- Important business rules have tests.
- No production secrets are committed.
- README explains setup and deployment.

---

# 49. Coding Standards

Follow professional C# conventions.

Use:

- nullable reference types
- dependency injection
- async APIs
- cancellation tokens where appropriate
- meaningful names
- small focused methods
- clear services
- DTOs/ViewModels
- Fluent API for important EF relationships/configuration
- proper database indexes

Avoid:

- giant controllers
- giant service classes
- duplicated business logic
- magic strings
- magic numbers
- hard-coded prices
- hard-coded contact information
- entity binding directly from public forms
- unnecessary abstractions

---

# 50. Final Instruction to the AI Agent

Before writing significant code:

1. Inspect the existing repository.
2. Identify whether a project already exists.
3. Do not overwrite existing functionality blindly.
4. Preserve existing code when appropriate.
5. Identify the current .NET version.
6. Identify the existing database/provider.
7. Identify whether Identity already exists.
8. Explain the proposed implementation plan.
9. Implement one phase at a time.
10. Build and test after each major phase.
11. Fix errors before proceeding.
12. Do not mark a phase complete if it does not build.
13. Do not invent undocumented product functionality.
14. Keep the architecture simple and maintainable.
15. Prefer production-quality implementation over demo/prototype shortcuts.

At the end, provide:

- architecture summary
- database schema summary
- implemented features
- API endpoints
- configuration requirements
- migration commands
- test results
- known limitations
- recommended next steps for integrating the Android application with the subscription/license API.

The final product should feel like a **real production SaaS subscription platform for the دكان Android application**, not a generic CRUD demonstration.
