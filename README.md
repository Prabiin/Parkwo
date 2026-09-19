# ParkingApp Backend

A peer-to-peer parking marketplace API where users find parking spaces and land owners rent out their empty spaces. Built with ASP.NET Core 10, Clean Architecture, and PostGIS for geospatial queries.

## Change Log

### 2026-09-19 — Onboarding: phone from request, gender/DOB mandatory

- `PUT /profile` takes `FullName`, `PhoneNumber`, `Email`, `Gender`, `DateOfBirth`. The target row is found by the request phone (the OTP-time "dirty" row; UI shows it locked) — a changed/unknown number returns 404 — and must belong to the token owner (403 otherwise, blocking cross-account overwrites). Gender/DOB mandatory; `IsProfileComplete` = name + email + gender + DOB (picture stays optional).
- No migration (no schema change). Build passes.

### 2026-09-19 — Facility images (MinIO) + ratings & reviews + profile picture

- **Images are optional at onboarding, MinIO-backed.** One `parkingapp` bucket, two prefixes: `facility-images/` and `profile-images/` (S3/MinIO "folders" are key prefixes). `POST /facilities/{id}/images` (multipart, owner-only) and `POST /profile/picture` (single file, replaces the previous one — the old object is deleted best-effort). Only the public URL is persisted (`ParkingFacilityImages` rows / `Users.ProfileImageUrl`); jpeg/png/webp/gif ≤ 5 MB.
- **Compliance sees the evidence.** Facility list/detail responses (mobile + BackOffice) carry `ImageCount` and the image list — facilities with no images are the ones compliance spends manual review time on.
- **Rating stored on the table, reviews in their own table** (the standard Booking/Google pattern): `ParkingFacilities` gains `AverageRating` + `RatingCount`, recomputed transactionally on each accepted review; `ParkingFacilityReviews` holds one row per review (`FacilityId`, `AuthorId`, `Rating` 1–5, optional `Comment`), with a **unique `(FacilityId, AuthorId)`** constraint = one review per rider per facility.
- **Usage proof deferred to bookings (no visit table).** A dedicated `ParkingFacilityVisits` entity was considered and removed — an owner-recorded visit is gameable and becomes dead code once bookings arrive. Reviews currently require a `Verified` facility + the one-review rule; a `TODO(bookings)` in the handler marks where a completed-booking/payment check slots in.
- Migration `20260919113358_AddFacilityImagesReviewsAndProfilePicture` (images + reviews tables, rating columns, `Users.ProfileImageUrl`). Build + tests pass.

> If your local DB already applied the superseded `20260919112536_AddFacilityImagesReviewsAndVisits` migration, downgrade first: `dotnet ef database update AddParkingFacilityAndSpot --project ParkingApp.Infrastructure --startup-project ParkingApp`, then update normally (or drop the dev DB and re-apply all).

### 2026-09-19 — Parking facilities + parking spots (supply catalog)

Providers can now list their parking inventory immediately after their `ParkingProvider` profile exists — no approval gate. The compliance team reviews each facility (name, address, spot counts by vehicle type) through BackOffice before approving it.

- **New entities**: `ParkingFacility` (belongs to a `ParkingProvider`, `ApprovalStatus` Pending by default) and `ParkingSpot` (per spot: `SpotNumber`, `VehicleType` two/four-wheeler, `PricePerHourNpr`, `IsActive`). New tables `ParkingFacilities`, `ParkingSpots` (migration `20260919103951_AddParkingFacilityAndSpot`).
- `POST /facilities` (auth) — `CreateParkingFacilityCommand`; caller must **own the provider** (individual profile or an `Owner` member of the owning company). Facility starts `Pending`.
- `GET /facilities` (auth) — `GetMyParkingFacilitiesQuery`; facilities under my providers, each with `TwoWheelerCount` / `FourWheelerCount` + approval status.
- `GET /facilities/{id}` (auth) — `GetParkingFacilityByIdQuery`; full facility with its spots.
- `POST /facilities/{id}/spots` (auth) — `CreateParkingSpotsCommand`; **batch-add** spots with spot number, vehicle type and per-hour price. Spot numbers are unique per facility (request + DB conflict → 409).
- `GET /backoffice/facilities?approvalStatus=` and `GET /backoffice/facilities/{id}` — compliance review: lists with owner contact + spot counts; detail shows every spot and its pricing.
- `ProviderOwnership` helper centralizes the "owns this provider" check (individual `OwnerUserId` OR `Owner` role on the owning org).
- Build + tests pass. No booking/availability yet — that's the next phase.

### 2026-09-19 — Mobile endpoints for organizations + parking providers

Now a rider can create a **business (organization)** and register as a **parking provider** from the app — before this, only the BackOffice could *read* them.

- `POST /organizations` (auth) — `CreateOrganizationCommand`; caller becomes the **Owner** member (`UserOrganization` row with `Owner` role) and The organization is created with `ApprovalStatus = Pending` (BackOffice must verify before it can trade). Registration number is unique-checked (409 on conflict).
- `GET /organizations` (auth) — `GetMyOrganizationsQuery`; organizations the caller belongs to, each with the caller's `MyRole` + `MyRoleDescription`.
- `POST /parking-providers` (auth) — `CreateParkingProviderCommand`; two flavors driven by `ProviderType`:
  - **Individual** → profile attached to the caller (`OwnerUserId`), one per account (409 if the user already has one).
  - **Company** → `OrganizationId` required, and the caller must **own** that organization (`Owner` role, else 403); one provider per organization (409 if registered).
  - Provider starts `ApprovalStatus = Pending`.
- `GET /parking-providers` (auth) — `GetMyParkingProvidersQuery`; the caller's individual profile plus the profiles of organizations they belong to.
- Every response carries the enum value **and** its `[Description]` (`ApprovalStatusDescription`, `ProviderTypeDescription`, `MyRoleDescription`), consistent with the profile/vehicles convention.
- No schema change — no new migration. Build + tests pass.

### 2026-09-19 — BackOffice: approval workflow + admin console API

Introduced the **BackOffice** (Parkwo admin/compliance) side: organizations and parking providers must be **approved** before they can do business; riders do not (no KYC needed).

- `VerificationStatusEnum` renamed to **`ApprovalStatusEnum`** (`Pending = 1`, `Verified = 2`, `UnderReview = 3`, `Rejected = 4`) — it now applies to both `ParkingProvider` (column renamed `VerificationStatus` -> `ApprovalStatus`) and the new `Organization.ApprovalStatus`. Enums stay description-carrying; both are `Pending` by default (DB default).
- New `BackOfficeUser` entity (`UserName`, `FullName`, `Email`, `PasswordHash`, `IsActive`, `LastLoginAtUtc`) + table `BackOfficeUsers`, wired into `ApplicationDbContext` / `IApplicationDbContext`.
- **`POST /backoffice/auth/login`** (anonymous): password-based (no OTP) → issues a JWT with `Role = BackOffice` claim. Passwords are PBKDF2-hashed (`PasswordHasher`, SHA256, 100k iterations) — plain text is never stored.
- New CQRS queries: `GetRidersQuery` (optional `?vehicleType=`), `GetOrganizationsQuery`, `GetParkingProvidersQuery` (optional `?approvalStatus=`).
- New `BackOfficeApi` endpoint group: list riders / organizations / parking providers. All list endpoints require the **`BackOfficeOnly`** authorization policy (`Role = BackOffice`).
- **Seeded SuperAdmin** via migration (`BackOfficeUsers` seed): `SuperAdmin` / `superadmin@gmail.com` / `P@ssw0rd` (stored as a precomputed PBKDF2 hash, never plain).
- Migration `20260919100539_AddBackOfficeApprovalAndSeed` created. Build + tests pass.

### 2026-09-19 — Phone-first OTP auth (Pathao/InDrive-style) + vehicles

Replaced collect-everything-upfront registration/login with a phone-number-first, OTP-driven flow.

- `POST /auth/send-otp` replaces `/auth/register` + `/auth/login` — the client sends only `phoneNumber` (+ optional `channel`). **Purpose is derived server-side**: `Login` if the number exists, `Registration` if not — the client never sends or sees it, so no account-status leaks. Purpose/status/channel stay maintained on every OTP record.
- `POST /auth/verify-otp` sends no purpose either. It validates the latest pending OTP, then resolves the account **by number match**: unknown number → auto-create a phone-only account + login; known number → login. Response flags `isNewUser` **and** `isProfileComplete` so the app can route first-time users into the **onboarding flow (name/email)** vs straight to the dashboard.
- New `PUT /profile` (auth required, `UpdateProfileCommand`) — onboarding saves name/email here; `isProfileComplete` flips to true. `verify-otp` and `refresh-token` both return `isProfileComplete` so an unfinished onboarding is re-queued even when the user returns later.
- New `Vehicle` entity + `VehicleTypeEnum` (`TwoWheeler`, `FourWheeler`) and `POST /vehicles` (auth required) — vehicles are added **from inside the app (Vehicles tab)**, not the auth or onboarding flow.
- `User.FirstName`, `User.LastName`, `Email` are now nullable; new migration `MakeProfileFieldsOptional`. `AddVehicle` migration creates the `Vehicles` table.
- OTP code is returned as `DevCode` in the `send-otp` response for now (dev-friendly SMS stand-in). When a real SMS provider is wired, drop `DevCode` and return only the HTTP status.
- **Bug fix:** registered `IApplicationDbContext` in DI — it was never resolvable at runtime/design-time.
- **New:** `ICurrentUserService` (reads user id from the authenticated token) so commands like `CreateVehicle` know the caller.
- `GET /profile` (`GetProfileQuery`) — profile screen data: name, phone, `memberSince` (`CreatedAtUtc`), `isProfileComplete`, `hasVehicle`, plus booking/savings/rating fields (0 for now until Bookings/Payments land).
- `GET /vehicles` (`GetVehiclesQuery`) — the user's plates for the Vehicles tab, plus a `hasVehicle` flag.
- `POST /auth/logout` (`LogoutCommand`) — signout revokes the active refresh token server-side.
- `PUT /profile` now also updates **phone number** (`PhoneNumber` unique-checked), **Gender** (`GenderEnum`: Male/Female/Other) and **DateOfBirth** (`DateOnly`); user identity collapsed into a single **`FullName`** column (new migration `AddUserProfileDetails` migrates existing First/Middle/Last into FullName). DOB will later power birthday wishes.

### 2026-09-17 — Folder restructure (working-folder warm-up)

No business-logic changes today. The goal was to lock in the folder pattern the team will follow from now on.

- Adopted a **module-first** feature layout in the Application layer: `ParkingApp.Application/Auth/<Feature>/Commands/` (`Login`, `Register`, `RefreshToken`, `Otp`), replacing the previous flat `Features/<Feature>/Command/` shape. Handlers, validators, and response DTOs are co-located under each feature folder.
- Standardized on the plural `Commands/` folder name for command slices.
- Locked in the read/write convention: write operations under `Commands/`, GET requests under `Queries/`, and CRUD splits `Commands/` into `Create|Update|Delete/`. Every command/query carries its own FluentValidation validator + response DTO in the same folder.
- The API host lives in the `ParkingApp/` folder, hosting `ParkingApp.Api.csproj`.

**Cleanup noted during review (namespace/folder alignment — pending, non-blocking):**
- [ ] Namespaces still use `ParkingApp.Application.Features.*` (e.g. `ParkingApp.Application.Features.Login.Command`); realign to the folders, e.g. `ParkingApp.Application.Auth.Login.Commands`.
- [ ] `Auth/Otp/Command/` → rename to `Auth/Otp/Commands/` for consistency.
- [ ] Move `IOtpSender` and `ITokenService` from `Common/Interfaces/` → `Auth/Interfaces/`.
- [ ] Move `ISender`, `IRequestResult`, `IRequestResultHandler` from `Common/Interfaces/` → `Common/Cqrs/`.
- [ ] Move `JwtSettings` from `Common/` → a `Configuration/` folder.
- [ ] Move `AuthDbHelper` from `Common/Helpers/` → `Auth/Shared/`.

## Tech Stack

| Layer | Technology |
|---|---|
| Language | C# (.NET 10.0) |
| Web Framework | ASP.NET Core Web API |
| ORM | Entity Framework Core 10 |
| Database | PostgreSQL 17 + PostGIS 3.5 |
| Auth | JWT (access + refresh tokens) + OTP verification |
| Caching | Redis 7 (provisioned, not yet wired) |
| Storage | MinIO S3-compatible (provisioned, not yet wired) |
| Containerization | Docker Compose |
| Testing | xUnit 2.9 |

## Project Structure

```
ParkingApp/
├── ParkingApp/                      # API host (Program.cs, endpoint groups, middleware)
├── ParkingApp.Application/          # Feature logic (commands, handlers, validators, responses) + contracts
├── ParkingApp.Domain/               # Entities, base classes, enums (zero dependencies)
├── ParkingApp.Infrastructure/       # EF Core, auth/OTP services, CQRS sender, DI registration
├── ParkingApp.Tests/                # Unit tests
├── docker-compose.yml               # Local dev infrastructure
└── ParkingApp.slnx                  # Solution file
```

### Application Layer Folder Pattern (standard going forward)

One folder per feature, grouped under its module (e.g. `Auth/`). The folder tree should make a feature's whole surface visible without opening files:

- `Commands/` — every **write** operation (POST / PUT / PATCH / DELETE)
- `Queries/` — every **read** operation (GET). If a simple action is still write-only, it lives under `Commands/` alone.
- **CRUD** — split `Commands/` into `Create/`, `Update/`, `Delete/` subfolders; the matching reads go under `Queries/`.
- Every command/query folder carries its own FluentValidation validator and response DTO next to the record + handler.

```
ParkingApp.Application/
├── Auth/                            # Feature module
│   ├── Otp/                           # OTP-driven auth (registration + login)
│   │   └── Command/
│   │       ├── SendOtpCommand.cs          # record + handler (co-located)
│   │       ├── SendOtpCommandValidator.cs
│   │       ├── SendOtpResponse.cs
│   │       ├── VerifyOtpCommand.cs
│   │       ├── VerifyOtpCommandValidator.cs
│   │       └── VerifyOtpResponse.cs
│   ├── RefreshToken/
│   │   └── Commands/                   # RefreshTokenCommand(.Validator/.Response).cs
│   └── <Feature>/                      # CRUD example (e.g. Profile) -- future
├── Profile/                        # Feature module
│   ├── Commands/
│   │   └── Update/
│   │       ├── UpdateProfileCommand.cs
│   │       ├── UpdateProfileCommandValidator.cs
│   │       └── UpdateProfileResponse.cs
│   └── Queries/
│       └── GetProfile/
│           ├── GetProfileQuery.cs
│           └── GetProfileResponse.cs
├── Vehicles/                        # Feature module
│   ├── Commands/
│   │   └── Create/
│   │       ├── CreateVehicleCommand.cs
│   │       ├── CreateVehicleCommandValidator.cs
│   │       └── CreateVehicleResponse.cs
│   └── Queries/
│       └── GetVehicles/
│           ├── GetVehiclesQuery.cs
│           └── GetVehiclesResponse.cs
│       ├── Commands/
│       │   ├── Create/
│       │   │   ├── Create<Feature>Command.cs
│       │   │   ├── Create<Feature>CommandValidator.cs
│       │   │   └── Create<Feature>Response.cs
│       │   ├── Update/
│       │   │   ├── Update<Feature>Command.cs
│       │   │   ├── Update<Feature>CommandValidator.cs
│       │   │   └── Update<Feature>Response.cs
│       │   └── Delete/
│       │       ├── Delete<Feature>Command.cs
│       │       ├── Delete<Feature>CommandValidator.cs
│       │       └── Delete<Feature>Response.cs
│       └── Queries/                    # read side; GET requests go here, not Commands
│           ├── Get<Feature>Query.cs
│           ├── Get<Feature>QueryValidator.cs
│           └── Get<Feature>Response.cs
├── Common/
│   ├── Helpers/                        # AuthDbHelper.cs (TODO: move to Auth/Shared/)
│   ├── Interfaces/                     # IApplicationDbContext, ISender, IRequestResult(+Handler), IOtpSender, ITokenService, ICurrentUserService
│   ├── JwtSettings.cs                  # (TODO: move to Configuration/)
│   └── Result.cs
└── ParkingApp.Application.csproj
```

### Architecture

Clean Architecture with dependency direction:

```
Domain <- Application <- Infrastructure <- Api
```

- **Domain**: Entities (`User`, `Otp`, `RefreshToken`), base classes, enums. No dependencies.
- **Application**: CQRS contracts (`IRequestResult<TRequest, TResponse>`, `IRequestResultHandler<TRequest, TResponse>`, `ISender`), module-first feature slices under `Auth/<Feature>/Commands/` (`Login`, `Register`, `VerifyOtp`, `RefreshToken` -- each folder contains the command record + handler, validator, and response DTO), `IApplicationDbContext`, `Result<T>` wrapper. Depends only on Domain + EF Core abstractions.
- **Infrastructure**: EF Core DbContext (`ApplicationDbContext`), CQRS `Sender` implementation, `ITokenService`/`IOtpSender` implementations, JWT config, DI registration. Depends on Application + Domain.
- **Api**: Minimal API endpoint groups (`Apis/AuthApi.cs`, `Infrastructure/EndpointGroupBase.cs`, `Infrastructure/EndpointGroupExtensions.cs`), `Program.cs`, middleware. Depends on all.

## Completed Work

### 1. Project Foundation

**Files:**
- `ParkingApp/Program.cs` -- Wired up `AddInfrastructure()`, added `UseAuthentication()` middleware
- `ParkingApp.Infrastructure/DependencyInjection.cs` -- Registers DbContext (with PostGIS), JWT auth, all services
- `ParkingApp.Infrastructure/Persistence/ApplicationDbContext.cs` -- DbSets for `Users`, `Otps`, `RefreshTokens`

### 2. Domain Entities

#### User (`ParkingApp.Domain/User.cs`)
| Property | Type | Constraints |
|---|---|---|
| FullName | string? | Optional (collected later via profile), max 200 |
| PhoneNumber | string | Required, max 20, **unique** |
| Email | string? | Optional (collected later via profile), max 255, **unique** |
| Gender | GenderEnum? | Male (1), Female (2), Other (3) |
| DateOfBirth | DateOnly? | `date`, birthday wishes later |
| IsPhoneVerified | bool | Default false |
| ProfileImageUrl | string? | Optional, max 500 (MinIO `profile-images/` URL, via `POST /profile/picture`) |
| RefreshTokens | ICollection\<RefreshToken\> | Navigation |

All entities inherit from `AuditableEntity` which provides `Id` (Guid via `Guid.CreateVersion7()`), `CreatedAtUtc`, `UpdatedAtUtc`, `CreatedBy`, `UpdatedBy`.

Enum convention: **all enums start at 1**. Every member carries a `[Description]` (e.g. `GenderEnum.Male -> "Male"`, `VehicleTypeEnum.TwoWheeler -> "Two wheeler"`). API responses keep the raw enum value (`Gender = 1`) **and** an adjacent human-readable field (`GenderDescription = "Male"`) via the `EnumExtensions.ToDescription()` helper.

#### Otp (`ParkingApp.Domain/Otp.cs`)
| Property | Type | Constraints |
|---|---|---|
| PhoneNumber | string | Private set, max 20 |
| Code | string | Private set, max 6 |
| Purpose | OtpTypeEnum | Registration, Login, ChangePhoneNumber, DeleteAccount |
| Status | OtpStatusEnum | Pending, Verified, Expired, Cancelled |
| Channel | OtpChannelEnum | Sms, Email |
| ExpiresAtUtc | DateTimeOffset | Required |
| VerifiedAtUtc | DateTimeOffset? | Optional |
| AttemptCount | int | Default 0 |

Domain methods:
- `Otp.Create(phoneNumber, code, purpose, channel, expiresInMinutes)` -- Factory method
- `otp.Verify(code)` -- Validates code, increments attempts, marks verified or expired
- `otp.HasExceededMaxAttempts(maxAttempts = 5)` -- Check if locked out
- `otp.Cancel()` -- Cancels pending OTPs

#### RefreshToken (`ParkingApp.Domain/RefreshToken.cs`)
| Property | Type | Constraints |
|---|---|---|
| UserId | Guid | FK to User, cascade delete |
| Token | string | Private set, max 512, **unique** |
| ExpiresAtUtc | DateTimeOffset | Required |
| RevokedAtUtc | DateTimeOffset? | Optional |
| ReplacedByToken | string? | Optional, max 512 |
| RevokedReason | string? | Optional, max 256 |

Domain methods:
- `RefreshToken.Create(userId, token, expiresInDays)` -- Factory method
- `refreshToken.Revoke(reason, replacedByToken)` -- Marks as revoked
- `IsExpired`, `IsRevoked`, `IsActive` -- Computed properties

#### Vehicle (`ParkingApp.Domain/Vehicle.cs`)
| Property | Type | Constraints |
|---|---|---|
| VehicleType | VehicleTypeEnum | TwoWheeler (1), FourWheeler (2) |
| Name | string | Required, max 100 |
| VehicleNumber | string | Private set, max 20, normalized to uppercase |
| UserId | Guid | FK to User, cascade delete |
| User | User? | Navigation property |

Domain methods:
- `Vehicle.Create(userId, vehicleType, name, vehicleNumber)` -- Factory method; trims and uppercases the plate
- One user can own multiple vehicles (bike + car); plate validation lives in `CreateVehicleCommandValidator` (accepts Latin + Devanagari characters)

#### Organization (`ParkingApp.Domain/Organization.cs`)
A **business client** (e.g. an office renting parking for its employees). Created by a `User` (the owner), which ties into `UserOrganization` membership so one person can be an authorized person on multiple companies without changing their own identity.

| Property | Type | Constraints |
|---|---|---|
| Name | string | Required, max 200 |
| RegistrationNumber | string | Required, max 100, **unique** |
| ContactNumber | string | Required, max 20 |
| Address | string | Required, max 300 |
| OwnerUserId | Guid | FK to User, restrict delete |
| ApprovalStatus | ApprovalStatusEnum | Pending (1, default), Verified, UnderReview, Rejected |
| Members | ICollection\<UserOrganization\> | Navigation property |
| ParkingProvider | ParkingProvider? | 0..1 provider profile (company can be a provider) |

#### UserOrganization (`ParkingApp.Domain/UserOrganization.cs`)
Membership join table — links a `User` to an `Organization` with a role (composite PK `{ UserId, OrganizationId }`).

| Property | Type | Constraints |
|---|---|---|
| UserId | Guid | Part of composite PK, FK to User |
| OrganizationId | Guid | Part of composite PK, FK to Organization |
| Role | OrganizationRoleEnum | Owner (1), Admin (2), Monitor (3), default Monitor |
| JoinedAtUtc | DateTimeOffset | Required |

#### ParkingProvider (`ParkingApp.Domain/ParkingProvider.cs`)
The **supply side** — whoever runs a parking operation, whether a person (land owner, garage operator) or a company (a mall, fuel-pump chain). One profile per owner (unique `OwnerUserId`/`OwnerOrganizationId`); a `User` or `Organization` references it via navigation.

| Property | Type | Constraints |
|---|---|---|
| ProviderType | ProviderTypeEnum | Individual (1), Company (2) |
| ApprovalStatus | ApprovalStatusEnum | Pending (1, default), Verified, UnderReview, Rejected |
| OwnerUserId | Guid? | FK to User, cascade delete, **unique** |
| OwnerOrganizationId | Guid? | FK to Organization, cascade delete, **unique** |
| OwnerUser / OwnerOrganization | nav | Exactly one set (person or company) |

#### BackOfficeUser (`ParkingApp.Domain/BackOfficeUser.cs`)
An internal Parkwo admin/compliance account — password login only (no OTP). Seeded `SuperAdmin` account: `superadmin@gmail.com` / `P@ssw0rd`.

| Property | Type | Constraints |
|---|---|---|
| UserName | string | Required, max 100, **unique** |
| FullName | string | Required, max 200 |
| Email | string | Required, max 255, **unique** |
| PasswordHash | string | Required, max 512 (PBKDF2) |
| IsActive | bool | Default true |
| LastLoginAtUtc | DateTimeOffset? | Optional |

#### ParkingFacility (`ParkingApp.Domain/ParkingFacility.cs`)
A single parking location a provider runs. Created by the provider's owner; each facility carries an `ApprovalStatus` the compliance team verifies against (spot counts included).

| Property | Type | Constraints |
|---|---|---|
| ProviderId | Guid | FK to ParkingProvider, cascade delete |
| Name | string | Required, max 200, unique per provider |
| Description | string? | Optional, max 1000 |
| Address | string | Required, max 300 |
| Latitude / Longitude | double? | Optional GPS |
| ApprovalStatus | ApprovalStatusEnum | Pending (1, default), Verified, UnderReview, Rejected |
| AverageRating | double? | Denormalized aggregate, recomputed on each review |
| RatingCount | int | Denormalized review count, default 0 |
| Spots | ICollection\<ParkingSpot\> | Navigation |
| Images | ICollection\<ParkingFacilityImage\> | Navigation |
| Reviews | ICollection\<ParkingFacilityReview\> | Navigation |

#### ParkingSpot (`ParkingApp.Domain/ParkingSpot.cs`)
An individual bookable space inside a facility. Vehicle type + per-hour price are what riders will search on later.

| Property | Type | Constraints |
|---|---|---|
| FacilityId | Guid | FK to ParkingFacility, cascade delete |
| SpotNumber | string | Required, max 20, unique per facility (e.g. "B1") |
| VehicleType | VehicleTypeEnum | TwoWheeler (1), FourWheeler (2) |
| PricePerHourNpr | decimal | Required, numeric(10,2) |
| IsActive | bool | Default true |

#### ParkingFacilityImage (`ParkingApp.Domain/ParkingFacilityImage.cs`)
Optional onboarding evidence. Bytes live in MinIO (`parkingapp` bucket, `facilities/` prefix); the row stores the public URL + metadata so compliance can review thumbnails without touching storage.

| Property | Type | Constraints |
|---|---|---|
| FacilityId | Guid | FK to ParkingFacility, cascade delete |
| FileName | string | Required, max 255 (original name) |
| Url | string | Required, max 500 (public MinIO URL) |
| ContentType | string | Required, max 100 |
| SizeInBytes | long | Required |
| SortOrder | int | Required (upload order) |

#### ParkingFacilityReview (`ParkingApp.Domain/ParkingFacilityReview.cs`)
One row per rider review (the standard pattern: aggregate on the facility, bodies in a child table). Accepted only for a `Verified` facility; one review per rider per facility. A completed-booking check will gate this further once bookings exist (`TODO(bookings)` in the handler).

| Property | Type | Constraints |
|---|---|---|
| FacilityId | Guid | FK to ParkingFacility, cascade delete |
| AuthorId | Guid | FK to User, cascade delete |
| Rating | int | Required, 1–5 |
| Comment | string? | Optional, max 1000 |
| (FacilityId, AuthorId) | — | **Unique**: one review per rider per facility |

### 3. EF Core Migrations

- Initial migration generated (`20260916160931_InitialCreate`)
- Profile-optional migration generated (`20260919073552_MakeProfileFieldsOptional`) -- makes `Users` name/email nullable
- Vehicle migration generated (`20260919075251_AddVehicle`) -- creates `Vehicles` table
- Profile-details migration generated (`20260919084629_AddUserProfileDetails`) -- collapses First/Middle/LastName into `FullName`, adds `Gender` + `DateOfBirth`; existing names are migrated (not lost)
- Organization + membership + provider migration generated (`20260919094324_AddOrganizationAndParkingProvider`) -- creates `Organizations`, `UserOrganizations`, `ParkingProviders` tables
- BackOffice + approval migration generated (`20260919100539_AddBackOfficeApprovalAndSeed`) -- creates `BackOfficeUsers`, renames `ParkingProviders.VerificationStatus` -> `ApprovalStatus`, adds `Organizations.ApprovalStatus`, seeds SuperAdmin
- Parking facility + spot migration generated (`20260919103951_AddParkingFacilityAndSpot`) -- creates `ParkingFacilities` + `ParkingSpots` tables (provider-owned facilities; spots carry vehicle type + per-hour price)
- Facility images + reviews + profile picture migration generated (`20260919113358_AddFacilityImagesReviewsAndProfilePicture`) -- creates `ParkingFacilityImages`, `ParkingFacilityReviews` (unique per rider per facility); adds `AverageRating` + `RatingCount` to `ParkingFacilities`, `ProfileImageUrl` to `Users`
- Creates 12 tables: `Users`, `Otps`, `RefreshTokens`, `Vehicles`, `Organizations`, `UserOrganizations`, `ParkingProviders`, `BackOfficeUsers`, `ParkingFacilities`, `ParkingSpots`, `ParkingFacilityImages`, `ParkingFacilityReviews`
- PostGIS extension enabled
- Unique indexes on `Users.PhoneNumber`, `Users.Email`, `RefreshTokens.Token`
- FK index on `RefreshTokens.UserId`
- Fluent API configurations in `ParkingApp.Infrastructure/Persistence/Configurations/`

### 4. JWT Authentication

#### Configuration (`ParkingApp.Application/Common/JwtSettings.cs`, namespaced `ParkingApp.Application.Configuration`)
Settings loaded from `appsettings.json` under the `Jwt` section:
```json
{
  "Jwt": {
    "Secret": "...",
    "Issuer": "ParkingApp",
    "Audience": "ParkingApp.Mobile",
    "AccessTokenExpirationMinutes": 30,
    "RefreshTokenExpirationDays": 30
  }
}
```

JWT claims include: `sub` (user ID), `jti` (unique token ID), `email`, `phone_number`, `full_name`.

#### Token Service (`ParkingApp.Infrastructure/Auth/TokenService.cs`)
- `GenerateAccessToken(user)` -- Creates signed JWT with claims
- `GenerateRefreshToken()` -- Returns 64-byte random base64 string

### 5. CQRS Auth Commands

The auth flow is built with **CQRS**: every write operation is a command, dispatched to a dedicated handler. The infrastructure mirrors a MediatR-style `IRequestResult<TIn, TOut>` pattern.

**CQRS infrastructure (files in `ParkingApp.Application/Common/Interfaces/`, namespaced `ParkingApp.Application.Common.Cqrs`):**
- `IRequestResult<TRequest, TResponse>` -- Marker for commands/queries; the request type carries the response type
- `IRequestResultHandler<TRequest, TResponse>` -- Handler contract: `Task<Result<TResponse>> Handle(TRequest request, CancellationToken)`
- `ISender` (`Send`) -- Routes a request to its handler (unified for both commands and queries)

**Sender (`ParkingApp.Infrastructure/Cqrs/Sender.cs`)** resolves the matching handler from DI and invokes it. Each auth feature is a vertical slice under `ParkingApp.Application/Auth/<Feature>/Commands/` -- the positional-record command and its `<Command>Handler` class live together in one file, with a validator and response DTO alongside. Shared OTP/token DB helpers live in `ParkingApp.Application/Common/Helpers/AuthDbHelper.cs` (namespaced `Features.Shared`; to be moved to `Auth/Shared/`). Persistence in handlers goes through `IApplicationDbContext` (`ParkingApp.Application/Common/Interfaces/`).

**Feature slices (`ParkingApp.Application/Auth/<Feature>/Commands/`):**
- `SendOtp` -- `SendOtpCommand` + `SendOtpCommandHandler`, `SendOtpCommandValidator`, `SendOtpResponse` (one endpoint covers registration and login via `Purpose`)
- `VerifyOtp` -- `VerifyOtpCommand` + `VerifyOtpCommandHandler`, `VerifyOtpCommandValidator`, `VerifyOtpResponse`
- `RefreshToken` -- `RefreshTokenCommand` + `RefreshTokenCommandHandler`, `RefreshTokenCommandValidator`, `RefreshTokenResponse`

**Handlers:**
- `SendOtpCommandHandler` -- Derives purpose server-side (Login if the number exists, Registration otherwise), cancels old pending OTPs, generates + stores a new OTP (purpose/status/channel tracked), sends it via `IOtpSender`, returns `DevCode` for dev testing
- `VerifyOtpCommandHandler` -- Validates the latest pending OTP (max 5 attempts, expiry); then resolves the account by phone number: unknown number → creates a phone-only account (registration), known number → login; marks phone verified; issues JWT access + refresh tokens; sets `IsNewUser` for onboarding
- `RefreshTokenCommandHandler` -- Rotates refresh token; if a revoked token is reused, revokes ALL user tokens (theft detection)
- `CreateVehicleCommandHandler` -- (auth required) Registers a user's vehicle via `ICurrentUserService`; rejects duplicate number plates on the same account
- `UpdateProfileCommandHandler` -- (auth required) Completes the onboarding step; finds the OTP-time row by the request `PhoneNumber` (404 if changed/unknown), then requires it to belong to the token owner (403 on mismatch — blocks cross-account overwrites), saves name/email/gender/DOB (all mandatory), unique-checks email, returns `IsProfileComplete`.

### 6. API Endpoints

Endpoints are defined as **minimal API endpoint groups** (`ParkingApp/Apis/AuthApi.cs` inheriting `EndpointGroupBase` in `ParkingApp/Infrastructure/`). Each endpoint sends its command through `ISender` via `ExecuteCommand<TCommand, TResponse>`, which runs a registered FluentValidation validator (if any) and maps the `Result<T>` to a response. Groups are auto-discovered by `app.MapEndpoints()` in `Program.cs`.

| Method | Route | Request Body | Response | Auth |
|---|---|---|---|---|
| POST | `/api/auth/send-otp` | `SendOtpRequest` | `SendOtpResponse` | No |
| POST | `/api/auth/verify-otp` | `VerifyOtpRequest` | `VerifyOtpResponse` | No |
| POST | `/api/auth/refresh-token` | `RefreshTokenRequest` | `RefreshTokenResponse` | No |
| POST | `/api/auth/logout` | `LogoutRequest` | `LogoutResponse` | No |
| GET | `/api/profile` | — | `GetProfileResponse` | Bearer token |
| PUT | `/api/profile` | `UpdateProfileRequest` | `UpdateProfileResponse` | Bearer token |
| GET | `/api/vehicles` | — | `GetVehiclesResponse` | Bearer token |
| POST | `/api/vehicles` | `CreateVehicleRequest` | `CreateVehicleResponse` | Bearer token |
| GET | `/organizations` | — | `GetMyOrganizationsResponse` | Bearer token |
| POST | `/organizations` | `CreateOrganizationRequest` | `CreateOrganizationResponse` | Bearer token |
| GET | `/parking-providers` | — | `GetMyParkingProvidersResponse` | Bearer token |
| POST | `/parking-providers` | `CreateParkingProviderRequest` | `CreateParkingProviderResponse` | Bearer token |
| GET | `/facilities` | — | `GetMyParkingFacilitiesResponse` | Bearer token |
| POST | `/facilities` | `CreateParkingFacilityRequest` | `CreateParkingFacilityResponse` | Bearer token |
| GET | `/facilities/{facilityId}` | — | `GetParkingFacilityByIdResponse` | Bearer token |
| POST | `/facilities/{facilityId}/spots` | `CreateParkingSpotsRequest` | `CreateParkingSpotsResponse` | Bearer token |
| POST | `/facilities/{facilityId}/images` | multipart `files` | `{ Images[] }` | Bearer token |
| POST | `/facilities/{facilityId}/reviews` | `CreateParkingFacilityReviewRequest` | `CreateParkingFacilityReviewResponse` | Bearer token |
| GET | `/facilities/{facilityId}/reviews` | — | `GetParkingFacilityReviewsResponse` | Bearer token |
| POST | `/profile/picture` | multipart `file` | `{ ProfileImageUrl }` | Bearer token |
| POST | `/backoffice/auth/login` | `BackOfficeLoginRequest` | `BackOfficeLoginResponse` | No |
| GET | `/backoffice/riders` | `(?vehicleType=)` | `GetRidersResponse` | BackOffice bearer |
| GET | `/backoffice/organizations` | `(?approvalStatus=)` | `GetOrganizationsResponse` | BackOffice bearer |
| GET | `/backoffice/parking-providers` | `(?approvalStatus=)` | `GetParkingProvidersResponse` | BackOffice bearer |
| GET | `/backoffice/facilities` | `(?approvalStatus=)` | `GetParkingFacilitiesResponse` | BackOffice bearer |
| GET | `/backoffice/facilities/{facilityId}` | — | `GetParkingFacilityDetailResponse` | BackOffice bearer |

Note: BackOffice list endpoints use the **`BackOfficeOnly`** authorization policy (`Role = BackOffice` claim) — app-user tokens are rejected. BackOffice routes are `/backoffice/*` (no `/api` prefix); organization, parking-provider and facility routes are top-level (`/organizations`, `/parking-providers`, `/facilities`) using app-user Bearer tokens.

**Commands** (built from the request body via `FromRequest`):
```
SendOtpCommand      (PhoneNumber, Channel?)                                    -> SendOtpResponse     (Message, ExpiresAt, DevCode)
VerifyOtpCommand    (PhoneNumber, Code)                                       -> VerifyOtpResponse  (UserId, AccessToken, RefreshToken, AccessTokenExpiresAt, IsNewUser, IsProfileComplete)
RefreshTokenCommand (RefreshToken)                                            -> RefreshTokenResponse (UserId, AccessToken, RefreshToken, AccessTokenExpiresAt, IsProfileComplete)
LogoutCommand       (RefreshToken)                                            -> LogoutResponse     (Message)
UpdateProfileCommand(FullName, PhoneNumber, Email, Gender, DateOfBirth)            -> UpdateProfileResponse (FullName, PhoneNumber, Email, Gender, GenderDescription, DateOfBirth, IsProfileComplete)
CreateVehicleCommand(VehicleType, Name, VehicleNumber)                        -> CreateVehicleResponse (Id, VehicleType, VehicleTypeDescription, Name, VehicleNumber)
CreateOrganizationCommand (Name, RegistrationNumber, ContactNumber, Address)   -> CreateOrganizationResponse (Id, Name, RegistrationNumber, ContactNumber, Address, OwnerUserId, ApprovalStatus, ApprovalStatusDescription, MyRole, MyRoleDescription)
CreateParkingProviderCommand (ProviderType, OrganizationId?)                   -> CreateParkingProviderResponse (Id, ProviderType, ProviderTypeDescription, ApprovalStatus, ApprovalStatusDescription, OwnerUserId, OwnerOrganizationId)
CreateParkingFacilityCommand (ProviderId, Name, Description?, Address, Latitude?, Longitude?) -> CreateParkingFacilityResponse (Id, ProviderId, Name, Description, Address, Latitude, Longitude, ApprovalStatus, ApprovalStatusDescription, TwoWheelerCount, FourWheelerCount)
CreateParkingSpotsCommand (FacilityId, Spots[SpotNumber, VehicleType, PricePerHourNpr, IsActive?]) -> CreateParkingSpotsResponse (FacilityId, Spots[...], TwoWheelerCount, FourWheelerCount)
UploadImages (multipart files, owner-only, MinIO)                               -> { Images[Id, Url, FileName, ContentType, SizeInBytes, SortOrder] }
UploadProfilePicture (multipart file, replaces previous)                        -> { ProfileImageUrl }
CreateParkingFacilityReviewCommand (FacilityId, Rating 1-5, Comment?)           -> CreateParkingFacilityReviewResponse (Id, FacilityId, Rating, Comment, AuthorId, CreatedAtUtc, AverageRating, RatingCount) — requires Verified facility (booking check: TODO)
```

**Queries** (`GET`, no body):
```
GetProfileQuery     ()                                                        -> GetProfileResponse (FullName, PhoneNumber, Email, Gender, GenderDescription, DateOfBirth?, MemberSince, IsProfileComplete, HasVehicle, BookingsCount, AmountSavedInNpr, Rating)
GetVehiclesQuery    ()                                                        -> GetVehiclesResponse (Vehicles[Id, VehicleType, VehicleTypeDescription, Name, VehicleNumber], HasVehicle)
GetMyOrganizationsQuery ()                                                    -> GetMyOrganizationsResponse (Organizations[Id, Name, RegistrationNumber, ContactNumber, Address, ApprovalStatus, ApprovalStatusDescription, MyRole, MyRoleDescription])
GetMyParkingProvidersQuery ()                                                 -> GetMyParkingProvidersResponse (ParkingProviders[Id, ProviderType, ProviderTypeDescription, ApprovalStatus, ApprovalStatusDescription, OwnerUserId, OwnerOrganizationId])
GetMyParkingFacilitiesQuery ()                                                -> GetMyParkingFacilitiesResponse (Facilities[Id, ProviderId, Name, Description, Address, Latitude, Longitude, ApprovalStatus, ApprovalStatusDescription, TwoWheelerCount, FourWheelerCount])
GetParkingFacilityByIdQuery (FacilityId)                                      -> GetParkingFacilityByIdResponse (Facility[...] + Spots + Images, TwoWheelerCount, FourWheelerCount, AverageRating, RatingCount)
GetParkingFacilityReviewsQuery (FacilityId)                                   -> GetParkingFacilityReviewsResponse (FacilityId, AverageRating, RatingCount, Reviews[Id, Rating, Comment, AuthorId, AuthorFullName, CreatedAtUtc] newest-first)
GetRidersQuery      (VehicleType?)                                             -> GetRidersResponse (Users[Id, FullName, PhoneNumber, IsProfileComplete, Vehicles])
GetOrganizationsQuery (ApprovalStatus?)                                        -> GetOrganizationsResponse (Organizations[Id, Name, RegistrationNumber, ContactNumber, Address, ApprovalStatus, ApprovalStatusDescription])
GetParkingProvidersQuery (ApprovalStatus?)                                     -> GetParkingProvidersResponse (ParkingProviders[Id, ProviderType, ProviderTypeDescription, ApprovalStatus, ApprovalStatusDescription, Owner...])
GetParkingFacilitiesQuery (ApprovalStatus?)                                    -> GetParkingFacilitiesResponse (Facilities[Id, Name, Description, Address, ApprovalStatus, ApprovalStatusDescription, ProviderOwnerName, ProviderOwnerContactNumber, TwoWheelerCount, FourWheelerCount])
GetParkingFacilityDetailQuery (FacilityId)                                     -> GetParkingFacilityDetailResponse (Facility[...] + every Spot, TwoWheelerCount, FourWheelerCount)
```

**BackOffice commands** (Password-based auth — no OTP):
```
BackOfficeLoginCommand (UserNameOrEmail, Password)                             -> BackOfficeLoginResponse (AccessToken, AccessTokenExpiresAt, FullName, UserName, Email)
```

### 7. OTP Sender

`IOtpSender` interface (`ParkingApp.Application/Common/Interfaces/IOtpSender.cs`, namespaced `ParkingApp.Application.Auth.Interfaces`) with a single method:
- `SendAsync(phoneNumber, code, cancellationToken)`

Development implementation (`ParkingApp.Infrastructure/Auth/ConsoleOtpSender.cs`) logs OTP to console. Replace with Twilio/AWS SNS for production.

### 8. Result Pattern

`Result<T>` (`ParkingApp.Application/Common/Result.cs`) wraps all service responses:
- `Result<T>.Success(value)` -- Returns data with 200
- `Result<T>.Failure(error, statusCode)` -- Returns error with status code

### 9. Docker Compose

Services provisioned:
- **PostgreSQL 17 + PostGIS 3.5** -- `localhost:5432`
- **Redis 7** -- `localhost:6379` (not yet wired)
- **MinIO** -- `localhost:9000` (API), `localhost:9001` (Console) (not yet wired)

## Running Locally

```bash
# Start database
docker-compose up -d postgres

# Apply migrations
dotnet ef database update --project ParkingApp.Infrastructure --startup-project ParkingApp

# Run API
dotnet run --project ParkingApp
```

## Test Server (all-in-one: API + Postgres + MinIO)

```bash
# From the repo root (Docker Desktop must be running)
docker compose up -d --build

# Follow the API logs (migrations apply automatically at startup)
docker compose logs -f api
```

- API: `http://localhost:8080` (OpenAPI JSON at `/openapi/v1.json` in Development)
- Interactive API docs (Scalar, try-it included): `http://localhost:8080/scalar` — the mobile team can fill in requests and execute them there (paste the Bearer token from login into Authorize)
- MinIO console: `http://localhost:9001` (admin / admin12345); S3 endpoint `:9000`
- Logs: Serilog ships API logs to Elasticsearch (index `parkingapp-logs-YYYY.MM`); view/search in Kibana at `http://localhost:5601`. Console logging stays as fallback when ES is unreachable.
- The `api` service waits for postgres (healthcheck) and applies pending EF migrations on boot, so no manual `database update` is needed. The MinIO `parkingapp` bucket is auto-created on first image upload.
- Two hostnames matter for MinIO: inside compose the API uses `minio:9000`; phones/browsers open the stored `PublicBaseUrl` (`http://localhost:9000`). If the mobile team tests from other devices on your LAN, replace `localhost` with your machine's LAN IP in `PublicBaseUrl` and the API port accordingly.
- Tear down: `docker compose down` (add `-v` to also wipe DB + MinIO data).

## Hosting the Integration Server (for the mobile team)

Any Linux VPS with Docker works (2 vCPU / 4 GB RAM is plenty for integration).

```bash
# 1. On the server: install Docker, open ports 22/80/443
# 2. Point DNS A records at the server: api.<domain>, files.<domain>, minio.<domain>
# 3. Clone + configure:
git clone git@github.com:Prabiin/Parkwo.git && cd Parkwo
cp .env.example .env && nano .env   # fill domains + secrets
# 4. Launch:
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

- API: `https://api.<domain>` — Scalar docs at `https://api.<domain>/scalar`
- Images served from `https://files.<domain>` (stored in DB URLs, openable on phones)
- MinIO console: `https://minio.<domain>`
- Caddy terminates TLS automatically (Let's Encrypt) — required, since mobile OSes reject plain-HTTP APIs.
- This sandbox intentionally runs `ASPNETCORE_ENVIRONMENT=Development` (Scalar + DevCode OTP visible) so the team can integrate without real SMS. It is **not** a production posture: rotate all secrets and add real SMS + locked-down config before any public launch.

## Testing Auth

Use the `ParkingApp.http` file or Postman:

1. **Request OTP**: `POST /api/auth/send-otp` with just the phone number (+ optional `channel`). The code is returned as `DevCode` in the response (dev stand-in for SMS).
2. **Verify**: `POST /api/auth/verify-otp` with phone + code. Check `isNewUser` / `isProfileComplete` — a phone-only account is auto-created on first use.
3. **Onboard**: if `isNewUser` (or `!isProfileComplete`), the app routes to the user form: phone number auto-populated from the OTP step and locked, user fills full name, email, gender and date of birth (all mandatory) → `PUT /profile` with all five fields. `IsProfileComplete` = name + email + gender + DOB present (picture stays optional).
4. **Add vehicle from the app**: from the Vehicles tab, `POST /api/vehicles` with vehicle type + number plate (Bearer token). Vehicle-dependent actions prompt "Add vehicle first" until at least one plate exists.
5. **Use the access token** in `Authorization: Bearer <token>` header
6. **Refresh**: `POST /api/auth/refresh-token` when access token expires (returns `isProfileComplete` so unfinished onboarding is re-queued)

**BackOffice** (admin console):
1. **Login**: `POST /backoffice/auth/login` with `SuperAdmin` + `P@ssw0rd` -> returns a BackOffice bearer token.
2. **List pending work**: `GET /backoffice/organizations?approvalStatus=Pending`, `GET /backoffice/parking-providers?approvalStatus=Pending`, `GET /backoffice/facilities?approvalStatus=Pending`, `GET /backoffice/riders` (all require the BackOffice token).
3. **Verify a facility**: `GET /backoffice/facilities/{id}` — every spot number, vehicle type, price, the two/four-wheeler counts, uploaded images and ratings. No images → slower manual review.

**Provider catalog flow** (supply side):
1. Become a provider: `POST /parking-providers` (`Individual`, or `Company` + owned `organizationId`).
2. `POST /facilities` under your provider, then `POST /facilities/{id}/spots` to add spots (batch, two/four-wheeler + price).
3. Optionally `POST /facilities/{id}/images` (multipart) — more evidence, faster compliance review.
4. `GET /facilities` shows your inventory with spot counts, image counts, ratings and `ApprovalStatus`; compliance sees the same via BackOffice.
5. Riders review once the facility is `Verified`: `POST /facilities/{id}/reviews` (one per rider; a completed-booking gate follows with bookings).

## Branching & Environments

- `main` — frozen release line. Do not commit here directly.
- `release/dev` — integration branch. All work lands here via PR.
- `feature/<what>` — checked out from `release/dev`, e.g. `feature/facility-search`. Commit + push the branch, open a PR against `release/dev`.
- Merging into `release/dev` triggers the `release-dev` workflow: `dotnet test`, then builds the API image and pushes `ghcr.io/prabiin/parkwo:dev` (+ `dev-<sha>`) to GitHub Container Registry. The hosted dev env pulls `:dev`.
- Later: `release/sit`, `release/uat` branches with their own workflows once development completes.

## Roadmap (Plans — focus on ONE item at a time)

### Phase 0 — Release what works today (unblocks the mobile team)

- [ ] **Set up Git + GitHub** — repo is not under version control yet; two GitHub accounts on this machine (office + personal) → set up SSH keys + `~/.ssh/config`, `git init`, add `.gitignore`, push to GitHub (Render deploys from GitHub).
- [ ] **Free Postgres with PostGIS** (hosted DB has no 30-day expiry) — Supabase or Neon free tier; Project connection string once created.
- [ ] **Apply migrations to hosted DB** — `dotnet ef database update` against the hosted connection string.
- [ ] **Deploy API to Render** (free web service) — env vars via `ConnectionStrings__DefaultConnection`, `Jwt__Secret`, `Jwt__Issuer`, `Jwt__Audience`; confirm app listens on Render's `PORT`.
- [ ] **Keep-alive monitor** — free UptimeRobot ping every ~10 min (avoids the 15-min idle + cold-start lag for the mobile team).
- [ ] Global error handling middleware
- [ ] Production JWT secret management (env var, never committed)
- [ ] Confirm base URL + auth flow with the mobile team's APK

### Phase 1 — Auth & profile

- [ ] User profile endpoints (get / update profile)
- [ ] Real SMS/email OTP sender (Integration behind `IOtpSender`)
- [ ] Rate limiting (OTP resend/verify abuse)

### Phase 2 — Parking spaces (landlord)

- [ ] Domain models: `ParkingSpace`, `ParkingZone`
- [ ] Landlord CRUD API (`Commands/Create|Update|Delete/` + `Queries/` pattern)
- [ ] Accept `lat`/`long` from mobile (map stays client-side)
- [ ] Photo/video upload via MinIO presigned URLs
- [ ] Spot QR generation (entry/exit scan)

### Phase 3 — Search & discovery (driver)

- [ ] Nearby parking search (PostGIS spatial queries)
- [ ] Nearby landmarks
- [ ] Recommendations

### Phase 4 — Bookings & billing

- [ ] Booking/reservation flow (see below for Redis)
- [ ] Parking history
- [ ] Parking time & amount calculation (server-side)
- [ ] Real-time rider-to-spot distance (SignalR or polling; math in PostGIS)
- [ ] Payment gateways (eSewa / Khalti / IME Pay / Fonepay) + webhooks
- [ ] Payment QRs

### Phase 5 — Scale & hardening

- [ ] Redis caching + booking availability locks
- [ ] Unit tests (xUnit)
- [ ] Production hardening review

## Key Design Decisions

1. **Dual-role users** -- No separate landlord/tenant roles. A single `User` who creates a `ParkingSpace` listing becomes a landlord. All users are drivers.
2. **OTP for everything** -- Both registration and login use OTP. No passwords. Mobile-first UX.
3. **Refresh token rotation** -- Each refresh request invalidates the old token and issues a new one. Prevents replay attacks.
4. **Theft detection** -- If a revoked refresh token is reused, all tokens for that user are revoked.
5. **Clean Architecture + CQRS** -- Domain has zero dependencies. Write operations are CQRS commands handled in Infrastructure; read operations (queries) will follow the same `IQuery<T>`/dispatcher pattern.
