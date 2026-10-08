# ParkingApp Backend

A peer-to-peer parking marketplace API where users find parking spaces and land owners rent out their empty spaces. Built with ASP.NET Core 10, Clean Architecture, and PostGIS for geospatial queries.

## Change Log

### 2026-10-08 — Provider rates removed; Parkwo sets the price

- Facilities no longer carry per-type prices — `TwoWheelerPricePerHourNpr` / `FourWheelerPricePerHourNpr` columns, request fields and responses are gone (migration `20261008044802_RemoveProviderFacilityRates` drops both columns; existing bookings keep their snapshotted rate). Providers declare occupancy and area only at `POST /facilities`.
- The marketplace rate is central, config-driven, and set by Parkwo: new `ParkwoPricing` section (`TwoWheelerPricePerHourNpr` default 40, `FourWheelerPricePerHourNpr` default 100) with `ParkwoPricing__*` env passthrough in compose + `.env.example` for promos/pricing experiments.
- `POST /bookings` snapshots the configured rate onto the booking at create (same rule as before: the booking's own `PricePerHourNpr` drives the total and any overstay) — past bookings are never re-priced. `GET /facilities/nearby` still shows a per-type price, now the Parkwo rate rather than a facility-supplied one.

### 2026-10-06 — Single-gate scans, overstay settlement, config comments, rebuilt Figma flow

- **Single-gate scan endpoint.** `POST /gate/entry-exit` replaces `POST /gate/entry` + `POST /gate/exit`: the same QR is presented at one gate helmed by one phone — the booking's own state picks the direction via `GateRules.DirectionFor(Status)` (Confirmed/Active → Entry, Completed → Exit), so there is no entry/exit button a tired attendant can press wrong. New `ScanGateCommand`/`ScanGateResponse` under `Bookings/Commands/ScanGate`; the `ScanEntry/` + `ScanExit/` command folders are deleted and DI re-registered. The handler stays **SERIALIZABLE** — two taps must not both admit on the way in, nor both close and record an overstay on the way out.
- **Overstay settlement (kept out of the booking state machine).** New `OverstayPayment` entity + table (migration `20261006090604_AddOverstaySettlement`). Overstay pricing/grace (`OverstayPricing`, `LocalTime` helpers, config-driven) never touches the booking — the booking is already `Completed` and the space freed at the exit scan; the overstay is a separate debt. `POST /payments/overstay` (`CreateOverstayPaymentCommand`) starts the charge and its own callback `GET /payments/khalti/overstay/return` (AllowAnonymous, `ProcessOverstayPaymentCallbackCommand`) reconciles via `OverstayPaymentStatus.LoadAsync`/`UpdateRow`. New rider interrogations: `GET /bookings/{id}/exit-summary`, `GET /bookings/{id}/overstay/summary`, `GET /bookings/{id}/transactions` (merges prepaid + overstay payments). Amounts are snapshotted at the scan and flow as paise + pre-formatted local times.
- **Config use-site comments.** Values sourced from configuration are annotated at every usage site (Khalti/gateway config, JWT + back-office token services, pass settings, pricing, the summary endpoints) so callers always read the same source and never re-derive a constant.
- **Figma flow rebuilt as a single journey.** `design/figma` now hosts one booking→payment→gate plugin (book → pay → QR → staff entry scan → park → exit scan with the two-button "QR scanned"/"Continue" confirm → case closed or overstay branch), sized to the 3-page Figma Starter quota. The old 6-journey version is preserved in `design/_backup/old-full-flow/`. Import `design/figma/manifest.json` into Figma (Plugins → Development) and run "Parkwo Flow Designer".
- Build + tests pass (221/221).

### 2026-09-28 — Config env parity + license pipeline cleanup

- `ParkingStandards__*` env keys added to `docker-compose.yml` (test server now carries both `Upload__*` and `ParkingStandards__*`; on Render/AWS set the same keys as plain env — only secrets belong in a vault).
- License submission pipeline moved out of the API tier: `POST /licenses` only maps the multipart form to `CreateDrivingLicenseCommand` (raw strings + file records); parsing, `UploadValidation`, MinIO upload, duplicate checks and persistence all run in the handler. Shared helpers (`UploadValidation`, `LicenseCategoryParser`, `ApprovalTransitions`, `ParkingCapacity`) live in `Application/Common/Helpers`.
- No migration (no schema change). Build + tests pass.

### 2026-09-28 — Capacity counts replace spot rows (approval-gated increases)

- Deleted `ParkingSpot` entirely (entity, table, `POST /facilities/{id}/spots`, config, responses). Lots are no longer bay rows — a facility declares `twoWheelerOccupancy` + `fourWheelerOccupancy` (each ≥ 0, sum ≥ 1), nullable `landAreaSqM`, and per-type hourly prices (`twoWheelerPricePerHourNpr`, `fourWheelerPricePerHourNpr` > 0, replacing per-spot pricing for future billing). Migration `ReplaceSpotsWithOccupancy` drops `ParkingSpots` (existing bay data is dev-only).
- Capacity increases are approval-gated: `PUT /facilities/{id}/capacity` (owner, partial allowed) writes straight through for non-Verified lots, but stages merged totals into `pending*` columns for `Verified` lots — the app keeps serving current numbers. `PUT /backoffice/facilities/{id}/capacity-approval { approve, rejectionReason? }` flips pending live (reason required on reject, shown to owner). The lot stays `Verified` throughout.
- Compliance assist: `ParkingStandards` config (`twoWheelerAreaSqM: 2`, `fourWheelerAreaSqM: 12.5`, overridable via `ParkingStandards__*` env); BackOffice detail computes `estimatedAreaRequiredSqM` + `exceedsLandArea` from claimed occupancy vs declared area (advisory — layout varies). `ParkingCapacity` helper covered by 8 xUnit tests.
- Nearby/booking implications: `GET /facilities/nearby` filters `occupancy > 0` of the requested type and returns `available/occupancy/price` per type (`available == occupancy` until bookings subtract holds). Rider-facing copy becomes "7/12" per type. Seat-map stays a future `HasMarkedParkingLot` upgrade.
- Owner visibility: my list/detail carry `hasPendingCapacityChange` + the pending set. Build + tests pass (49/49).

### 2026-09-28 — Single approval per trust question (provider approval removed)

- `ParkingProvider` is no longer approvable: dropped `ApprovalStatus` + `RejectionReason` columns (migration `RemoveParkingProviderApproval`), deleted `PUT /backoffice/parking-providers/{id}/approval` + its command/validator, and removed the `?approvalStatus=` filter from `GET /backoffice/parking-providers` (now an unfiltered oversight list). Provider responses (mobile + BackOffice) no longer carry status fields.
- Rationale: the provider row carries no reviewable evidence — compliance reviews the combined lot (facility + owner info + photos + prices) on the facility detail screen. One operator can run many facilities; each lot is approved on its own evidence. Approvable entities are now exactly three: **Organization** (PAN body), **Facility** (lot quality), **DrivingLicense** (rider permit, verified for record — soft, no hard booking gate).
- Mobile effect: creating a provider returns immediately with no pending state; `GET /facilities` lists **all** facilities across the caller's providers with per-lot status. No migration backfill (columns dropped pre-launch). Build + tests pass.

### 2026-09-28 — BackOffice API split per area

- Deleted the `BackOfficeApi` monolith; now one group per area under `ParkingApp/Apis/BackOffice/` sharing a `BackOfficeGroup` base (`BackOfficeOnly` policy + init-code parsers): `Auth/BackOfficeAuthApi.cs` (`auth/login`, `init`; future `auth/logout`, `auth/change-password`), `Riders/`, `Organizations/`, `Providers/` (list only, no approval), `Facilities/`, `Licenses/` (each with its list/detail + `{id}/approval` PUT). Routes unchanged — auto-discovered via `MapEndpoints()`.
- No migration (no schema change). Build + tests pass.

### 2026-09-28 — BackOffice approve/decline flow (supply unblocked)

- New `PUT /backoffice/organizations/{id}/approval`, `PUT /backoffice/facilities/{id}/approval`, `PUT /backoffice/licenses/{id}/approval` (BackOffice-only; body `{ approvalStatus, rejectionReason? }`, route id wins via `request with`) → `Unit` (re-fetch the list). Until now the BackOffice was read-only, so nothing could ever leave `Pending` — nearby search would have returned zero lots. (Provider approval was cut same-day — see "Single approval" entry; approvable = org, facility, license.)
- Shared transition matrix (`ApprovalTransitions.IsAllowed`, 16 xUnit tests): `Pending → Verified|UnderReview|Rejected`; `UnderReview → Verified|Rejected`; `Rejected → UnderReview`; `Verified → UnderReview` (re-audit only); same-status and all other moves rejected with an explicit message.
- New nullable `RejectionReason` (max 500) on the approvable entities — required when rejecting (validator), cleared on verify, cleared on license resubmit. Surfaced in every related response (my orgs/facilities/license + all BackOffice lists/details) so owners see *why*.
- New `GET /backoffice/licenses?approvalStatus=` queue (license id, rider contact, number, categories, photo URLs, expiry) — licenses previously had no compliance surface at all.
- Migration `AddApprovalRejectionReason` (4 nullable columns, no backfill needed). Build + tests pass.

### 2026-09-28 — Nearby search + HasMarkedParkingLot flag

- New `ParkingFacility.HasMarkedParkingLot` (`bool`, default `false`) — set at `POST /facilities` via the UI toggle; surfaced in all facility responses (my list/detail, BackOffice list/detail). Later decides seat-map vs slot-count UI per facility.
- New `ParkingFacility.Location` (PostGIS `geography(Point, 4326)`, GiST-indexed, derived from lat/long at create) — `Latitude`/`Longitude` doubles stay the API contract; `Location` is query-only. Backfilled for existing rows in-migration.
- New `GET /facilities/nearby?latitude=&longitude=&radiusKm=&vehicleType=` (auth, nearest-first, max 50) with two modes: no `vehicleType` = **"parkings near me"** (pure spatial, `Verified` + has coords + within radius); with `vehicleType` (code `1`/`2`) = **"available spaces near me"** (adds `occupancy > 0` filter for that type). `radiusKm` default 5, max 20 (400 beyond); lat/lng range-validated.
- Response per lot: `distanceMeters`, rating, `imageCount` + `firstImageUrl`, `hasMarkedParkingLot`, and per type `available`/`occupancy`/`pricePerHourNpr` (`available == occupancy` until bookings land — overlapping holds subtract here via `TODO(bookings)`).
- `NetTopologySuite` referenced by Domain (geometry only) + Application; `UseNetTopologySuite` was already wired in DI.
- Migration `20260928151516_AddFacilityMarkedFlagAndLocation` (flag + geography column + backfill + GiST). Build + tests pass.

### 2026-09-28 — Upload limits moved to config (extensions jpg/jpeg/png)

- Deleted the `AllowedImageTypes` / `MaxImageBytes` constants duplicated in `ProfileApi`, `FacilityApi`, `LicenseApi`. New `UploadSettings` section (`MaxImageSizeInMB: 5`, `AllowedImageTypes: [jpg, jpeg, png]`) in `appsettings.Development.json` + `Upload__*` env passthrough in compose — override on the server later via `Upload__MaxImageSizeInMB` / `Upload__AllowedImageTypes__0..n` env vars, no code change. Code defaults match when the section is absent (no startup throw, unlike Jwt/Minio).
- Validation is now extension-based (`Path.GetExtension`, case-insensitive) + size check in one shared helper (`UploadValidation.ValidateImage`); MIME-type sniffing dropped. Note the type allowlist narrowed: **webp/gif no longer accepted** (was jpeg/png/webp/gif).
- No migration (no schema change). Build + tests pass.

### 2026-09-28 — Driving licenses + vehicle details (brand/model/color/category)

- New `VehicleCategoryEnum` (`Scooter = 1`, `Motorcycle = 2`, `CarJeepVan = 3`) — what the vehicle **is**. `VehicleTypeEnum` (`TwoWheeler`/`FourWheeler`) stays as the **spot-size** class; the two must agree (`Scooter`/`Motorcycle` ↔ `TwoWheeler`, `CarJeepVan` ↔ `FourWheeler`, enforced by `LicenseCoverage.MatchesSpotType` in the vehicle validator).
- New `LicenseCategoryEnum` (Nepal DoTM letters on the card: `K = 1` scooter/moped, `A = 2` motorcycle, `A1 = 3` heavy motorcycle, `B = 4` car/jeep/van) with a server-side coverage matrix (`LicenseCoverage.Covers`): Scooter ← K/A/A1, Motorcycle ← A/A1 (K rejected), Car/Jeep/Van ← B only. No cross two↔four coverage. 17 xUnit tests lock the matrix.
- New `DrivingLicense` entity (one row per user: `LicenseNumber` globally unique, `Categories` as Postgres `integer[]`, front/back photo URLs in MinIO `license-images/`, `ExpiryDate`, `ApprovalStatus` default `Pending`) + table `DrivingLicenses` (unique `UserId`, unique `LicenseNumber`, cascade delete).
- New endpoints (auth): `POST /licenses` (multipart: `licenseNumber`, `categories` comma-separated codes e.g. `"2,4"`, `expiryDate` `yyyy-MM-dd`, files `front`+`back` jpg/jpeg/png ≤ 5 MB (limits from `Upload` config — see Change Log 2026-09-28) — creates `Pending`; resubmission allowed only after `Rejected`, old photos deleted best-effort) → `Guid`; `GET /licenses` → license with category descriptions + approval status (404 when nothing submitted); `GET /licenses/init` → `{ licenseCategories[] }`. `GET /vehicles/init` now also returns `{ vehicleCategories[] }`.
- `Vehicle` gains `VehicleCategory`, `Brand`, `Model`, `Color` (all mandatory at `POST /vehicles`, surfaced in `GET /vehicles` with descriptions — for lost-bike identification). `POST /vehicles` additionally checks a **Verified** license: insufficient categories → 409 (no license / non-Verified license → allowed; the hard gate moves to bookings). The booking-time license gate + BackOffice license verification queue are next.
- Migration `20260928134240_AddDrivingLicenseAndVehicleDetails`. Build + tests pass.

### 2026-09-22 — Enum-as-request, dropdown init APIs, slim write responses

- `CreateVehicleCommand.VehicleType` changed from `string` to `VehicleTypeEnum` — the manual `Enum.TryParse` in the handler is gone; the validator moved from `IsEnumName` (string-only) to `IsInEnum()`, matching the existing `UpdateProfileCommand.Gender` precedent.
- `Program.cs` now registers `JsonStringEnumConverter` via `ConfigureHttpJsonOptions`, so enum names (`"TwoWheeler"`, `"Male"`, case-insensitive) bind from JSON bodies — plain ints (`1`, `2`) still bind, and numeric strings (`"1"`) bind too. This also fixes `PUT /profile` `"gender": "Male"`, which had the same latent issue.
- New common dropdown model `ListModel<T>` (`ParkingApp.Application/Common/Models/ListModel.cs`, `Code` = enum int value as string, `Text` = `[Description]` via `ToDescription()`) with `ListModel<T>.FromEnum()` factory.
- New auth-required init endpoints for dropdowns: `GET /profile/init` → `{ genders[] }`, `GET /vehicles/init` → `{ vehicleTypes[] }`, e.g. `{ "code": "1", "text": "Two wheeler" }`. Mobile shows `text`, posts back `code`.
- Write responses slimmed to ids — creates/updates no longer echo the request: `CreateVehicle` / `UpdateProfile` / `CreateOrganization` / `CreateParkingProvider` / `CreateParkingFacility` / `CreateParkingFacilityReview` → `Guid`; batch `CreateParkingSpots` → `Unit` (new `Application/Common/Unit.cs`; the client already has the `facilityId` from the URL). The 7 echo response DTOs were deleted and a dead re-query for spot counts (only used to build the echo) was removed from the spots handler. Mobile re-fetches display data via the existing `GET`s. Auth/OTP/login payloads untouched.
- Documented contracts: `DateOfBirth` is `DateOnly` — mobile sends `"yyyy-MM-dd"` (e.g. `"1998-06-15"`; no time component, other formats 400); `Vehicle.Name` is a free-text display label (`"Daily Dio"`) so riders can tell multiple plates apart — `VehicleType` drives spot matching, `VehicleNumber` is identity.
- No migration (no schema change). Build passes.

### 2026-09-19 — Onboarding: phone from request, gender/DOB mandatory

- `PUT /profile` takes `FullName`, `PhoneNumber`, `Email`, `Gender`, `DateOfBirth`. The target row is found by the request phone (the OTP-time "dirty" row; UI shows it locked) — a changed/unknown number returns 404 — and must belong to the token owner (403 otherwise, blocking cross-account overwrites). Gender/DOB mandatory; `IsProfileComplete` = name + email + gender + DOB (picture stays optional).
- No migration (no schema change). Build passes.

### 2026-09-19 — Facility images (MinIO) + ratings & reviews + profile picture

- **Images are optional at onboarding, MinIO-backed.** One `parkingapp` bucket, two prefixes: `facility-images/` and `profile-images/` (S3/MinIO "folders" are key prefixes). `POST /facilities/{id}/images` (multipart, owner-only) and `POST /profile/picture` (single file, replaces the previous one — the old object is deleted best-effort). Only the public URL is persisted (`ParkingFacilityImages` rows / `Users.ProfileImageUrl`); jpg/jpeg/png ≤ 5 MB (limits from `Upload` config — override via `Upload__*` env vars).
- **Compliance sees the evidence.** Facility list/detail responses (mobile + BackOffice) carry `ImageCount` and the image list — facilities with no images are the ones compliance spends manual review time on.
- **Rating stored on the table, reviews in their own table** (the standard Booking/Google pattern): `ParkingFacilities` gains `AverageRating` + `RatingCount`, recomputed transactionally on each accepted review; `ParkingFacilityReviews` holds one row per review (`FacilityId`, `AuthorId`, `Rating` 1–5, optional `Comment`), with a **unique `(FacilityId, AuthorId)`** constraint = one review per rider per facility.
- **Usage proof deferred to bookings (no visit table).** A dedicated `ParkingFacilityVisits` entity was considered and removed — an owner-recorded visit is gameable and becomes dead code once bookings arrive. Reviews currently require a `Verified` facility + the one-review rule; a `TODO(bookings)` in the handler marks where a completed-booking/payment check slots in.
- Migration `20260919113358_AddFacilityImagesReviewsAndProfilePicture` (images + reviews tables, rating columns, `Users.ProfileImageUrl`). Build + tests pass.

> If your local DB already applied the superseded `20260919112536_AddFacilityImagesReviewsAndVisits` migration, downgrade first: `dotnet ef database update AddParkingFacilityAndSpot --project ParkingApp.Infrastructure --startup-project ParkingApp`, then update normally (or drop the dev DB and re-apply all).

### 2026-09-19 — Parking facilities + parking spots (supply catalog)

Providers can now list their parking inventory immediately after their `ParkingProvider` profile exists — no approval gate. The compliance team reviews each facility (name, address, spot counts by vehicle type) through BackOffice before approving it.

- **New entities**: `ParkingFacility` (belongs to a `ParkingProvider`, `ApprovalStatus` Pending by default) and `ParkingSpot` (per spot: `SpotNumber`, `VehicleType` two/four-wheeler, `PricePerHourNpr`, `IsActive`). New tables `ParkingFacilities`, `ParkingSpots` (migration `20260919103951_AddParkingFacilityAndSpot`).
- `POST /facilities` (auth) — `CreateParkingFacilityCommand`; caller must **own the provider** (individual profile or an `Owner` member of the owning company). Facility starts `Pending`.
- `GET /facilities` (auth) — `GetParkingFacilitiesQuery`; facilities under my providers, each with `TwoWheelerCount` / `FourWheelerCount` + approval status.
- `GET /facilities/{id}` (auth) — `GetParkingFacilityByIdQuery`; full facility with its spots.
- `POST /facilities/{id}/spots` (auth) — `CreateParkingSpotsCommand`; **batch-add** spots with spot number, vehicle type and per-hour price. Spot numbers are unique per facility (request + DB conflict → 409).
- `GET /backoffice/facilities?approvalStatus=` and `GET /backoffice/facilities/{id}` — compliance review: lists with owner contact + spot counts; detail shows every spot and its pricing.
- `ProviderOwnership` helper centralizes the "owns this provider" check (individual `OwnerUserId` OR `Owner` role on the owning org).
- Build + tests pass. No booking/availability yet — that's the next phase.

### 2026-09-19 — Mobile endpoints for organizations + parking providers

Now a rider can create a **business (organization)** and register as a **parking provider** from the app — before this, only the BackOffice could *read* them.

- `POST /organizations` (auth) — `CreateOrganizationCommand`; caller becomes the **Owner** member (`UserOrganization` row with `Owner` role) and The organization is created with `ApprovalStatus = Pending` (BackOffice must verify before it can trade). Registration number is unique-checked (409 on conflict).
- `GET /organizations` (auth) — `GetOrganizationsQuery`; organizations the caller belongs to, each with the caller's `Role` + `RoleDescription`.
- `POST /parking-providers` (auth) — `CreateParkingProviderCommand`; two flavors driven by `ProviderType`:
  - **Individual** → profile attached to the caller (`OwnerUserId`), one per account (409 if the user already has one).
  - **Company** → `OrganizationId` required, and the caller must **own** that organization (`Owner` role, else 403); one provider per organization (409 if registered).
  - Provider starts `ApprovalStatus = Pending`.
- `GET /parking-providers` (auth) — `GetParkingProvidersQuery`; the caller's individual profile plus the profiles of organizations they belong to.
- Every response carries the enum value **and** its `[Description]` (`ApprovalStatusDescription`, `ProviderTypeDescription`, `RoleDescription`), consistent with the profile/vehicles convention.
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

## Business Logic (till now)

> Read-first section — skim this at the start of every session before touching code. If a flow isn't written here, it isn't decided yet.

**Parkwo in one line:** one app, one account, three hats — the same `User` is a **rider**, a **parking provider**, and a **company admin** at the same time, with no role or app switching (Pathao rider + pillion model). The hat is decided per action, not per login.

### Hat 1 — Rider (demand): register → find → book → pay → park → exit

1. OTP login/registration (phone-first), then onboarding (`PUT /profile`).
2. Register vehicle from the Vehicles tab (`GET /vehicles/init` → `POST /vehicles`: category Scooter/Motorcycle/CarJeepVan + brand/model/color for identification).
3. Submit driving license (`GET /licenses/init` → `POST /licenses` multipart with front/back photos; starts `Pending`). **Booking hard-gates on it**: a `Verified`, unexpired license whose categories cover the vehicle, else 403.
4. Find parking: `GET /facilities/nearby` (nearest-first, per type `available/occupancy/price`). Only `Verified` lots with capacity appear.
5. Book: `POST /bookings` with a UTC window (≥ 1 billable hour, hours round up, clamped 1–168). Availability is counted inside a **SERIALIZABLE** transaction against overlapping capacity-consuming bookings of the same vehicle type — two riders tapping "pay" at once cannot both win the last space. Creates an unpaid `PendingPayment` **hold** with a 10-minute `HoldExpiresAtUtc`, so an abandoned checkout releases the space automatically. An early exit frees the space immediately (`SpaceReleasedAtUtc`) and availability counts against the effective end, not the raw window end.
6. Pay: `GET /bookings/{id}/summary` (pre-payment confirmation; local times + NPR come from the server) → `POST /payments` (today Khalti: `mode` Redirect / DeepLink / FormPost). The gateway redirect alone is not enough — the app must also call `GET /payments/khalti/return` to force reconciliation, then poll until the booking leaves `PendingPayment`. The amount is always the server-side snapshot, never client-supplied.
7. Show the pass: `GET /bookings/{id}/pass` → `passToken` QR, valid across the booking window (400 while `PendingPayment`). `POST .../pass/rotate` prints a new token and invalidates every older QR (`PassNonce`).
8. Gate in/out: **one endpoint, `POST /gate/entry-exit`** (staff phone at the gate). The booking's own state picks the direction — `Confirmed`/`Active` → Entry (activates the stay, records `EnteredAtUtc`), `Completed` → Exit — so there is no mode button a tired attendant can press wrong. Both scans run SERIALIZABLE against double-tap races. Entry refusals say whether the window hasn't opened, has lapsed, or isn't paid; exit distinguishes already-closed / not-paid / never-checked-in.
9. Exit: the booking is `Completed`, the space is released at the scan (`SpaceReleasedAtUtc`), and the stay is measured from the actual entry scan (`EnteredAtUtc`), not the window (`actualStayMinutes`). If the stay exceeds the window beyond the grace, an overstay charge (`OverstayBillableHours`, `OverstayAmountPaisa`) is **snapshotted onto the booking in the same transaction as the exit** — one frozen number for staff, rider and gateway. No overstay → case closed, nothing owed.
10. Overstay is a **separate debt that never mutates the booking** (it is already `Completed`): the rider pays via `POST /payments/overstay` plus its own callback `GET /payments/khalti/overstay/return` (reconciled through `OverstayPaymentStatus`). **An unsettled overstay blocks new bookings (409)** — the same "unsettled" test `CreateOverstayPaymentCommand` uses, so the two can't disagree. `GET /bookings/{id}/transactions` merges prepaid + overstay payments into one history.

### Hat 2 — Provider (supply): land → provider → facility → verified

1. Rider with land calls `POST /parking-providers` (`Individual` — one per account, no org needed).
2. App immediately asks for the facility: `POST /facilities` (name, address, lat/long, description).
3. Occupancy/images follow at create (`twoWheelerOccupancy`, `fourWheelerOccupancy`, `landAreaSqM`, `POST .../images` multipart); later increases stage via `PUT .../capacity` pending compliance. The price is always Parkwo's (`ParkwoPricing`) — providers never propose one.
4. Compliance verifies in BackOffice (`Pending` → `Verified`); only then can riders book/review it.
5. Nothing about the `User` row changes — the rider just gains a provider profile.

### Hat 3 — Company (fleet demand): register → approval → employee subscriptions

1. Rider as company owner calls `POST /organizations` (name, registrationNumber unique, contactNumber, address); caller becomes `Owner`.
2. Compliance checks the profile from BackOffice; on approval the company can book parking **subscriptions for all its employees**.
3. Step 2's subscription model is **decided as direction, not designed** — no subscription/employee-booking code exists. Open questions before Phase 4: whose vehicle, whose wallet, who may book on the company's behalf (`Owner`? `Admin`?).

### Rules that must not break

- **One user, many profiles.** Hats are rows (`ParkingProvider`, `UserOrganization`), never a role switch on the user.
- **Phone is identity.** OTP-time row is found by request phone (`PUT /profile`: 404 on unknown number, 403 when the number isn't the token owner's); email is unique-checked.
- **Ownership gating.** Facility writes require `ProviderOwnership`: direct `OwnerUserId`, or `Owner` role on the owning org.
- **Approval gates supply, never demand.** Riders need no KYC; orgs/facilities trade only after `Verified`. Licenses are verified for record and gate bookings (`Verified` + covering categories + unexpired, else 403).
- **Uniqueness:** plate per account; `registrationNumber` global; `licenseNumber` global + one license per user; one `Individual` provider per user; one provider per org; one review per rider per facility (and only on `Verified` facilities).
- **Single gate, direction from state.** `POST /gate/entry-exit` derives Entry vs Exit from `BookingStatusEnum` (`GateRules.DirectionFor`) — the client never picks a mode. The same `passToken` scans in first, then out; `facilityId` in the body is an authorization check, not a decoration.
- **Overstay never touches the booking.** The booking is `Completed` and the space freed at exit; the overstay is a row in `OverstayPayments`. A paid/refunded overstay payment clears the debt; the "unsettled" test is identical in the create-booking gate and in `CreateOverstayPaymentCommand`.
- **SERIALIZABLE where races cost money.** Availability counting on create, and both gate scans — two taps must not both admit on entry, both close on exit, or both record an overstay.
- **Amounts are server-frozen snapshots.** Booking total at create, overstay charge at the exit scan — the client and gateway agree on one number; the gateway is never told a client-supplied amount.
- **The server clock is the billing clock.** UTC goes in, pre-formatted local time comes out. Skewed gate scanners cannot shorten a stay — `ScannedAtUtc`/`EnteredAtUtc` are server times.
- **Passes are re-issued, not edited.** `passToken` = signature + nonce; rotating kills every older QR; too-early, lapsed, unpaid and wrong-lot scans are refused with distinct messages.
- **Write responses are ids** (`Guid`/`Unit` — display data always comes from the `GET`s).

### Built vs planned (2026-10-06)

- Built: OTP auth, onboarding, vehicles (+ brand/model/color/category, dropdown inits), driving-license submission + license queue + **booking-time license gate**, org/provider/facility (occupancy)/images/reviews + capacity-approval, **nearby search**, **booking + 10-min hold + availability**, **QR pass + rotation**, **single-gate entry/exit scan**, **Khalti payment + return-reconcile + payment history**, **overstay settlement** (summary, payment, separate callback, new-booking block while unsettled).
- Planned: more payment gateways (eSewa/IME/Fonepay), company employee subscriptions + on-behalf booking rules, Redis-backed availability/locks, real SMS OTP.

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
| Channel | OtpChannelEnum | Always `Sms` — decided by `SendOtpCommandHandler`, not client-supplied |
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
| VehicleType | VehicleTypeEnum | TwoWheeler (1), FourWheeler (2) — spot-size class |
| VehicleCategory | VehicleCategoryEnum | Scooter (1), Motorcycle (2), CarJeepVan (3) — must agree with VehicleType |
| Name | string | Required, max 100 |
| VehicleNumber | string | Private set, max 20, normalized to uppercase |
| Brand | string | Required, max 100 (e.g. "Bajaj") |
| Model | string | Required, max 100 (e.g. "NS200") |
| Color | string | Required, max 50 (e.g. "white") |
| UserId | Guid | FK to User, cascade delete |
| User | User? | Navigation property |

Domain methods:
- `Vehicle.Create(userId, vehicleType, vehicleCategory, name, vehicleNumber, brand, model, color)` -- Factory method; trims and uppercases the plate
- One user can own multiple vehicles (bike + car); plate validation lives in `CreateVehicleCommandValidator` (accepts Latin + Devanagari characters)
- License coverage lives in `LicenseCoverage` (`ParkingApp.Domain/Common/Enums/LicenseCoverage.cs`): `Covers(held, vehicle)` (Scooter ← K/A/A1, Motorcycle ← A/A1, CarJeepVan ← B) and `MatchesSpotType(type, category)` (category must agree with spot size)

#### DrivingLicense (`ParkingApp.Domain/DrivingLicense.cs`)
One row per user (mirrors the DoTM smart card — categories accumulate over time). Starts `Pending`; compliance verifies via BackOffice (queue pending — next step).

| Property | Type | Constraints |
|---|---|---|
| UserId | Guid | FK to User, cascade delete, **unique** (one license per account) |
| LicenseNumber | string | Required, max 30, normalized to uppercase, **unique** globally |
| Categories | List\<LicenseCategoryEnum\> | Required, distinct (K/A/A1/B — Postgres `integer[]`) |
| FrontImageUrl / BackImageUrl | string | Required, max 500 (MinIO `license-images/` URLs) |
| ExpiryDate | DateOnly | Required, must be future (`yyyy-MM-dd`) |
| ApprovalStatus | ApprovalStatusEnum | Pending (1, default), Verified, UnderReview, Rejected |

Domain methods:
- `DrivingLicense.Create(userId, licenseNumber, categories, frontImageUrl, backImageUrl, expiryDate)` -- Factory method (Pending)
- `license.Resubmit(...)` -- Replaces card details after a rejection and resets to Pending

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
A single parking location a provider runs. Created by the provider's owner; each facility carries an `ApprovalStatus` the compliance team verifies against (claimed occupancy vs land area included). No bay rows — capacity is declared counts.

| Property | Type | Constraints |
|---|---|---|
| ProviderId | Guid | FK to ParkingProvider, cascade delete |
| Name | string | Required, max 200, unique per provider |
| Description | string? | Optional, max 1000 |
| Address | string | Required, max 300 |
| Latitude / Longitude | double? | Optional GPS (derives the PostGIS `Location` geography at create) |
| Location | Point? | Query-only PostGIS `geography(Point, 4326)`, GiST-indexed (nearby search) |
| HasMarkedParkingLot | bool | UI toggle at create (default false) — later decides seat-map vs slot-count UI |
| TwoWheelerOccupancy / FourWheelerOccupancy | int | Required, each ≥ 0, sum ≥ 1 (total claimable spaces per type) |
| LandAreaSqM | decimal? | Optional, > 0 when present (compliance plausibility base) |
| PendingTwoWheelerOccupancy / PendingFourWheelerOccupancy / PendingLandAreaSqM | — | Nullable staging columns for Verified-lot increases (live only after capacity approval) |
| ApprovalStatus | ApprovalStatusEnum | Pending (1, default), Verified, UnderReview, Rejected |
| AverageRating | double? | Denormalized aggregate, recomputed on each review |
| RatingCount | int | Denormalized review count, default 0 |
| Images | ICollection\<ParkingFacilityImage\> | Navigation |
| Reviews | ICollection\<ParkingFacilityReview\> | Navigation |

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
- Driving license + vehicle details migration generated (`20260928134240_AddDrivingLicenseAndVehicleDetails`) -- creates `DrivingLicenses` (unique `UserId`, unique `LicenseNumber`); adds `VehicleCategory`, `Brand`, `Model`, `Color` to `Vehicles` (existing rows default to `""`/`0` — re-register or wipe dev DBs)
- Facility marked-flag + location migration generated (`20260928151516_AddFacilityMarkedFlagAndLocation`) -- adds `HasMarkedParkingLot` (default false) + `Location` geography with GiST index (backfilled from lat/long for existing rows)
- Occupancy migration generated (`ReplaceSpotsWithOccupancy`) -- drops `ParkingSpots`; adds occupancy/prices/landArea/pending columns to `ParkingFacilities`
- Creates 12 tables: `Users`, `Otps`, `RefreshTokens`, `Vehicles`, `DrivingLicenses`, `Organizations`, `UserOrganizations`, `ParkingProviders`, `BackOfficeUsers`, `ParkingFacilities`, `ParkingFacilityImages`, `ParkingFacilityReviews`
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
- `CreateVehicleCommandHandler` -- (auth required) Registers a user's vehicle via `ICurrentUserService`; rejects duplicate number plates on the same account; validates `VehicleCategory` agrees with `VehicleType`; rejects (409) when the user holds a **Verified** license whose categories don't cover the vehicle (no/unverified license → allowed; hard gate moves to bookings)
- `CreateDrivingLicenseCommandHandler` -- (auth required) Submits the DoTM license (number unique-checked globally, expiry must be future); one active submission per account (409 when Pending/Verified); a `Rejected` license may be resubmitted (row reset to Pending, old photos deleted best-effort via `IFileStorage`)
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
| PUT | `/api/profile` | `UpdateProfileRequest` | `Guid` (user id; re-fetch via `GET /profile`) | Bearer token |
| GET | `/profile/init` | — | `{ Genders[Code, Text] }` | Bearer token |
| GET | `/api/vehicles` | — | `GetVehiclesResponse` | Bearer token |
| POST | `/api/vehicles` | `CreateVehicleRequest` | `Guid` (vehicle id; re-fetch via `GET /vehicles`) | Bearer token |
| GET | `/vehicles/init` | — | `{ VehicleTypes[Code, Text], VehicleCategories[Code, Text] }` | Bearer token |
| GET | `/licenses` | — | `DrivingLicenseResponse` (404 when nothing submitted) | Bearer token |
| POST | `/licenses` | multipart `licenseNumber, categories, expiryDate, front, back` | `Guid` (license id; re-fetch via `GET /licenses`) | Bearer token |
| GET | `/licenses/init` | — | `{ LicenseCategories[Code, Text] }` | Bearer token |
| GET | `/organizations` | — | `GetOrganizationsResponse` (user) | Bearer token |
| POST | `/organizations` | `CreateOrganizationRequest` | `Guid` (organization id) | Bearer token |
| GET | `/parking-providers` | — | `GetParkingProvidersResponse` (user) | Bearer token |
| POST | `/parking-providers` | `CreateParkingProviderRequest` | `Guid` (provider id) | Bearer token |
| GET | `/facilities` | — | `GetParkingFacilitiesResponse` (user) | Bearer token |
| GET | `/facilities/nearby?latitude=&longitude=&radiusKm=&vehicleType=` | — | `GetNearbyFacilitiesResponse` (distance + available/occupancy/price per type, nearest first) | Bearer token |
| POST | `/facilities` | `CreateParkingFacilityRequest` | `Guid` (facility id) | Bearer token |
| GET | `/facilities` | — | `GetParkingFacilitiesResponse` (user, with occupancy + pending flag) | Bearer token |
| GET | `/facilities/{facilityId}` | — | `GetParkingFacilityByIdResponse` (occupancy + pending set, no spot list) | Bearer token |
| PUT | `/facilities/{facilityId}/capacity` | `UpdateFacilityCapacityRequest` | `Unit` (`{}`; Verified lots stage to pending) | Bearer token |
| POST | `/facilities/{facilityId}/images` | multipart `files` | `{ Images[] }` | Bearer token |
| POST | `/facilities/{facilityId}/reviews` | `CreateParkingFacilityReviewRequest` | `Guid` (review id) | Bearer token |
| GET | `/facilities/{facilityId}/reviews` | — | `GetParkingFacilityReviewsResponse` | Bearer token |
| POST | `/profile/picture` | multipart `file` | `{ ProfileImageUrl }` | Bearer token |
| POST | `/backoffice/auth/login` | `BackOfficeLoginRequest` | `BackOfficeLoginResponse` | No |
| GET | `/backoffice/riders` | `(?vehicleType=)` | `GetRidersResponse` | BackOffice bearer |
| GET | `/backoffice/organizations` | `(?approvalStatus=)` | `GetOrganizationsResponse` (BackOffice) | BackOffice bearer |
| GET | `/backoffice/parking-providers` | — | `GetParkingProvidersResponse` (BackOffice oversight list, no approval) | BackOffice bearer |
| GET | `/backoffice/facilities` | `(?approvalStatus=)` | `GetParkingFacilitiesResponse` (BackOffice) | BackOffice bearer |
| GET | `/backoffice/facilities/{facilityId}` | — | `GetParkingFacilityDetailResponse` | BackOffice bearer |
| GET | `/backoffice/licenses` | `(?approvalStatus=)` | `GetLicensesResponse` (license queue with rider contact) | BackOffice bearer |
| PUT | `/backoffice/organizations/{id}/approval` | `UpdateOrganizationApprovalRequest` | `Unit` (`{}`; re-fetch list) | BackOffice bearer |
| PUT | `/backoffice/facilities/{id}/approval` | `UpdateParkingFacilityApprovalRequest` | `Unit` (`{}`; re-fetch list) | BackOffice bearer |
| PUT | `/backoffice/facilities/{id}/capacity-approval` | `UpdateFacilityCapacityApprovalRequest` | `Unit` (`{}`; re-fetch detail) | BackOffice bearer |
| PUT | `/backoffice/licenses/{id}/approval` | `UpdateDrivingLicenseApprovalRequest` | `Unit` (`{}`; re-fetch list) | BackOffice bearer |
| POST | `/gate/entry-exit` | `ScanGateCommand` (passToken, facilityId) | `ScanGateResponse` (direction, outcome, gate timestamps, overstay snapshot) | Bearer token |
| GET | `/bookings/{bookingId}/exit-summary` | — | exit + overstay summary (local times, NPR amounts) | Bearer token |
| GET | `/bookings/{bookingId}/overstay/summary` | — | overstay summary (minutes, grace, billable hours, rate, amount) | Bearer token |
| GET | `/bookings/{bookingId}/transactions` | — | prepaid + overstay payments merged | Bearer token |
| POST | `/payments/overstay` | `CreateOverstayPaymentRequest` | gateway mode/payload (overstay charge) | Bearer token |
| GET | `/payments/khalti/overstay/return` | `?pidx&status&amount` | overstay reconciliation result | No (AllowAnonymous) |

Note: BackOffice list endpoints use the **`BackOfficeOnly`** authorization policy (`Role = BackOffice` claim) — app-user tokens are rejected. BackOffice routes are `/backoffice/*` (no `/api` prefix); organization, parking-provider and facility routes are top-level (`/organizations`, `/parking-providers`, `/facilities`) using app-user Bearer tokens.

**Commands** (built from the request body via `FromRequest`):
```
SendOtpCommand      (PhoneNumber)                                              -> SendOtpResponse     (Message, ExpiresAt, DevCode)
VerifyOtpCommand    (PhoneNumber, Code)                                       -> VerifyOtpResponse  (UserId, AccessToken, RefreshToken, AccessTokenExpiresAt, IsNewUser, IsProfileComplete)
RefreshTokenCommand (RefreshToken)                                            -> RefreshTokenResponse (UserId, AccessToken, RefreshToken, AccessTokenExpiresAt, IsProfileComplete)
LogoutCommand       (RefreshToken)                                            -> LogoutResponse     (Message)
UpdateProfileCommand(FullName, PhoneNumber, Email, Gender, DateOfBirth)            -> Guid (user id; Gender is GenderEnum, DateOfBirth is DateOnly "yyyy-MM-dd")
CreateVehicleCommand(VehicleTypeEnum, VehicleCategoryEnum, Name, VehicleNumber, Brand, Model, Color) -> Guid (vehicle id; VehicleCategory must agree with VehicleType; Verified license with insufficient categories → 409)
CreateDrivingLicenseCommand(LicenseNumber, CategoriesRaw "2,4", ExpiryDateRaw yyyy-MM-dd, Front/Back file uploads) -> Guid (license id; parsing + MinIO upload + resubmission-after-Rejected all in the handler; API tier only maps the multipart form)
CreateOrganizationCommand (Name, RegistrationNumber, ContactNumber, Address)   -> Guid (organization id)
CreateParkingProviderCommand (ProviderType, OrganizationId?)                   -> Guid (provider id)
CreateParkingFacilityCommand (ProviderId, Name, Description?, Address, Latitude?, Longitude?, HasMarkedParkingLot, TwoWheelerOccupancy, FourWheelerOccupancy, LandAreaSqM?) -> Guid (facility id)
UpdateFacilityCapacityCommand (FacilityId, TwoWheelerOccupancy?, FourWheelerOccupancy?, LandAreaSqM?) -> Unit (Verified → pending columns; else live)
UploadImages (multipart files, owner-only, MinIO)                               -> { Images[Id, Url, FileName, ContentType, SizeInBytes, SortOrder] }
UploadProfilePicture (multipart file, replaces previous)                        -> { ProfileImageUrl }
CreateParkingFacilityReviewCommand (FacilityId, Rating 1-5, Comment?)           -> Guid (review id) — requires Verified facility (booking check: TODO)
```

**Queries** (`GET`, no body):
```
GetProfileQuery     ()                                                        -> GetProfileResponse (FullName, PhoneNumber, Email, Gender, GenderDescription, DateOfBirth?, MemberSince, IsProfileComplete, HasVehicle, BookingsCount, AmountSavedInNpr, Rating)
GetVehiclesQuery    ()                                                        -> GetVehiclesResponse (Vehicles[Id, VehicleType, VehicleTypeDescription, VehicleCategory, VehicleCategoryDescription, Name, VehicleNumber, Brand, Model, Color], HasVehicle)
GetDrivingLicenseQuery ()                                                     -> DrivingLicenseResponse (Id, LicenseNumber, Categories[], CategoryDescriptions[], FrontImageUrl, BackImageUrl, ExpiryDate, ApprovalStatus, ApprovalStatusDescription; 404 when nothing submitted)
GetOrganizationsQuery () [user]                                                  -> GetOrganizationsResponse (Organizations[Id, Name, RegistrationNumber, ContactNumber, Address, ApprovalStatus, ApprovalStatusDescription, Role, RoleDescription])
GetParkingProvidersQuery () [user]                                               -> GetParkingProvidersResponse (ParkingProviders[Id, ProviderType, ProviderTypeDescription, OwnerUserId, OwnerOrganizationId])
GetParkingFacilitiesQuery () [user]                                              -> GetParkingFacilitiesResponse (Facilities[Id, ..., Occupancy×2, LandAreaSqM, Prices×2, HasPendingCapacityChange, ...])
GetParkingFacilityByIdQuery (FacilityId)                                      -> GetParkingFacilityByIdResponse (Facility[...] + Images + pending set, no spot list)
GetNearbyFacilitiesQuery (Latitude, Longitude, RadiusKm? default 5/max 20, VehicleType?) -> GetNearbyFacilitiesResponse (Facilities[..., TwoWheeler{Available,Occupancy,Price}, FourWheeler{...}] nearest-first, max 50; available == occupancy until bookings)
GetParkingFacilityReviewsQuery (FacilityId)                                   -> GetParkingFacilityReviewsResponse (FacilityId, AverageRating, RatingCount, Reviews[Id, Rating, Comment, AuthorId, AuthorFullName, CreatedAtUtc] newest-first)
GetRidersQuery      (VehicleType?)                                             -> GetRidersResponse (Users[Id, FullName, PhoneNumber, IsProfileComplete, Vehicles])
GetOrganizationsQuery (ApprovalStatus?) [BackOffice]                              -> GetOrganizationsResponse (Organizations[Id, Name, RegistrationNumber, ContactNumber, Address, ApprovalStatus, ApprovalStatusDescription])
GetParkingProvidersQuery () [BackOffice]                                              -> GetParkingProvidersResponse (ParkingProviders[Id, ProviderType, ProviderTypeDescription, Owner...] — oversight list, no approval)
GetParkingFacilitiesQuery (ApprovalStatus?) [BackOffice]                          -> GetParkingFacilitiesResponse (Facilities[Id, ..., Occupancy×2, LandAreaSqM, Prices×2, HasPendingCapacityChange, ...])
GetParkingFacilityDetailQuery (FacilityId)                                     -> GetParkingFacilityDetailResponse (Facility[...] + Images + pending set + estimatedAreaRequiredSqM/exceedsLandArea)
```

**BackOffice commands** (Password-based auth — no OTP):
```
BackOfficeLoginCommand (UserNameOrEmail, Password)                             -> BackOfficeLoginResponse (AccessToken, AccessTokenExpiresAt, FullName, UserName, Email)
UpdateOrganizationApprovalCommand (OrganizationId, ApprovalStatus, RejectionReason?) -> Unit (transition-matrix enforced; reason required on Rejected)
UpdateParkingFacilityApprovalCommand (FacilityId, ApprovalStatus, RejectionReason?) -> Unit
UpdateFacilityCapacityApprovalCommand (FacilityId, Approve, RejectionReason?) -> Unit (approve flips pending live; reject clears with reason)
UpdateDrivingLicenseApprovalCommand (LicenseId, ApprovalStatus, RejectionReason?) -> Unit
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
- Interactive API docs (Swagger UI, try-it included): `http://localhost:8080/swagger` — the mobile team pastes the `accessToken` from login into **Authorize** once and every request then carries it
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

- API: `https://api.<domain>` — Swagger UI docs at `https://api.<domain>/swagger`
- Images served from `https://files.<domain>` (stored in DB URLs, openable on phones)
- MinIO console: `https://minio.<domain>`
- Caddy terminates TLS automatically (Let's Encrypt) — required, since mobile OSes reject plain-HTTP APIs.
- This sandbox intentionally runs `ASPNETCORE_ENVIRONMENT=Development` (Swagger UI + DevCode OTP visible) so the team can integrate without real SMS. It is **not** a production posture: rotate all secrets and add real SMS + locked-down config before any public launch.

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
2. **List pending work**: `GET /backoffice/organizations?approvalStatus=Pending`, `GET /backoffice/facilities?approvalStatus=Pending`, `GET /backoffice/licenses?approvalStatus=Pending`, `GET /backoffice/riders`, `GET /backoffice/parking-providers` (oversight, no approval) (all require the BackOffice token).
3. **Verify a facility**: `GET /backoffice/facilities/{id}` — the two/four-wheeler occupancy claims, area, uploaded images and ratings. No images → slower manual review.

**Provider catalog flow** (supply side):
1. Become a provider: `POST /parking-providers` (`Individual`, or `Company` + owned `organizationId`).
2. `POST /facilities` under your provider with occupancy, then optionally `POST /facilities/{id}/images` (multipart) — more evidence, faster compliance review. You never set a price: Parkwo's rate (`ParkwoPricing`) applies marketplace-wide. Later capacity increases via `PUT /facilities/{id}/capacity` (Verified lots stage to pending).
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
