# VoterSystem HTTP API Reference

This document describes the HTTP API currently implemented in `VoterSystem.WebAPI`.
It reflects the current controller and service code in the repository, including group management,
two-factor authentication, refresh tokens, and external-login handoff flows.

All routes below are relative to the API host, for example:

```text
https://your-api-host/api/v1/...
```

## General notes

### Authentication

The API uses JWT bearer authentication and also writes cookies on some login flows.

- Preferred for mobile and non-browser clients: `Authorization: Bearer <authToken>`
- Browser flow: cookies written by login or external-login token exchange

Cookie names currently used by the backend:

- `AuthToken`
- `RefreshToken`
- `id`

### Roles and policies

The backend distinguishes two roles:

- `User`
- `Admin`

Authorization is enforced both by controller attributes and by service-layer ownership checks.

- `AdminOnly` policy requires role claim `Admin`
- `UserOnly` policy requires role claim `User`

### Date and time format

All `DateTime` values should be sent as ISO 8601 timestamps, ideally in UTC.

Example:

```json
"2026-04-16T10:30:00Z"
```

Some endpoints accept a raw `DateTime` JSON literal as the request body, not an object.

### Error bodies

Most application-level errors are returned as:

```ts
type ErrorDto = {
  message: string;
  innerMessage: string | null;
};
```

Typical mappings:

- `400 Bad Request` -> `ErrorDto`
- `401 Unauthorized` -> `ErrorDto`
- `404 Not Found` -> `ErrorDto`
- `409 Conflict` -> `ErrorDto`
- `422 Unprocessable Entity` -> `ErrorDto`

Validation failures produced by ASP.NET model binding may instead return `ValidationProblemDetails`.

## Shared DTOs

These shapes are handwritten from the current code and are meant to match the implementation.

```ts
type Role = "User" | "Admin";

type ErrorDto = {
  message: string;
  innerMessage: string | null;
};

type TokensDto = {
  authToken: string;
  refreshToken: string; // GUID
  userId: string; // GUID
};

type RefreshTokenDto = {
  refreshToken: string; // GUID
};

type TwoFactorChallengeDto = {
  userId: string; // GUID
  message: string;
};

type TwoFactorVerificationRequestDto = {
  userId: string; // GUID
  code: string;
};

type UserRegisterRequestDto = {
  email: string;
  name: string;
  password: string;
};

type UserLoginRequestDto = {
  email: string;
  password: string;
};

type UserChangePasswordRequestDto = {
  oldPassword: string;
  newPassword: string;
};

type UserEmailConfirmRequestDto = {
  email: string;
  token: string; // base64-encoded token
};

type UserPasswordResetRequestDto = {
  email: string;
  token: string; // base64-encoded token
  newPassword: string;
};

type VoteChoiceRequestDto = {
  name: string;
  description: string | null;
};

type VoteChoiceDto = {
  choiceId: number;
  name: string;
  description: string | null;
  createdAt: string;
};

type VotingCreateRequestDto = {
  name: string;
  startsAt: string;
  endsAt: string;
  groupId: string | null; // GUID
};

type VotingDto = {
  votingId: number;
  name: string;
  createdAt: string;
  startsAt: string;
  endsAt: string;
  hasStarted: boolean;
  hasEnded: boolean;
  isOngoing: boolean;
  groupId: string | null;
  groupName: string | null;
  hasVoted: boolean | null;
  voteChoices: VoteChoiceDto[];
};

type VotingParticipationDto = {
  voting: VotingDto;
};

type UserDto = {
  id: string;
  name: string;
  email: string;
  emailConfirmed: boolean;
  twoFactorEnabled: boolean;
  role: Role;
  participations: VotingParticipationDto[];
};

type CreateGroupRequest = {
  name: string;
  description: string;
};

type GroupMemberDto = {
  userId: string;
  addedByUserId: string;
  createdAt: string;
  name: string | null;
  email: string | null;
};

type GroupDto = {
  groupId: string;
  creatorUserId: string;
  name: string;
  description: string;
  deletedAt: string | null;
  createdAt: string;
  members: GroupMemberDto[];
};

type VoteResultDto = {
  votingId: number;
  receipt: string;
};

type ChoiceResultDto = {
  choiceId: number;
  voteCount: number;
};

type VotingResultsDto = {
  choiceResults: ChoiceResultDto[];
};

type BallotDto = {
  voteChoice: VoteChoiceDto;
  voting: VotingDto;
  createdAt: string;
};

type ExternalLoginProvider = "Facebook" | "Google" | "Saml";
```

## Health

### `ANY /api/v1/health`

Short description: database-backed liveness/readiness check registered through ASP.NET health checks.

- Auth: none
- Query params: none
- Request body: none
- Responses:
  - `200 OK`, empty body when healthy
  - `503 Service Unavailable`, health-check response body when unhealthy
- Side effects:
  - Executes a lightweight database query against `Users`

Notes:

- The route is mapped with `app.UseHealthChecks("/api/v1/health")`.
- Clients should treat `GET /api/v1/health` as the intended form even though the middleware is not controller-based.

## Users

Base route: `/api/v1/users`

### `POST /api/v1/users/register`

Short description: create a local account with password login.

- Auth: none
- Query params: none
- Request body: `UserRegisterRequestDto`
- Responses:
  - `201 Created` -> `UserDto`
  - `400 Bad Request` -> `ErrorDto` or validation response
- Side effects:
  - Creates an ASP.NET Identity user
  - Generates and stores an initial refresh token
  - Adds the user to the matching Identity role
  - If there is no active admin yet, the first registered user becomes `Admin`; otherwise `User`

### `POST /api/v1/users/login`

Short description: authenticate with email and password.

- Auth: none
- Query params: none
- Request body: `UserLoginRequestDto`
- Responses:
  - `200 OK` -> `TokensDto`
  - `202 Accepted` -> `TwoFactorChallengeDto`
  - `401 Unauthorized` -> `ErrorDto`
  - `404 Not Found` -> `ErrorDto`
- Side effects:
  - On direct success, writes `AuthToken`, `RefreshToken`, and `id` cookies
  - On 2FA-required flow, generates an email token and attempts to send it by email
  - Updates ASP.NET Identity lockout counters through `PasswordSignInAsync`

### `POST /api/v1/users/login/2fa`

Short description: complete password login after receiving the email two-factor code.

- Auth: none
- Query params: none
- Request body: `TwoFactorVerificationRequestDto`
- Responses:
  - `200 OK` -> `TokensDto`
  - `400 Bad Request` -> `ErrorDto`
  - `404 Not Found` -> `ErrorDto`
- Side effects:
  - Verifies the email token for the supplied `userId`
  - Writes `AuthToken`, `RefreshToken`, and `id` cookies on success

### `GET /api/v1/users/all`

Short description: list active users, optionally filtered by username substring.

- Auth: authenticated user, but the service effectively restricts this to admins
- Query params:
  - `nameQuery?: string`
- Request body: none
- Responses:
  - `200 OK` -> `UserDto[]`
  - `401 Unauthorized` -> `ErrorDto`
- Side effects: none

Notes:

- Filtering is performed against normalized username values, not display names.

### `GET /api/v1/users`

Short description: get the currently authenticated user.

- Auth: authenticated user
- Query params: none
- Request body: none
- Responses:
  - `200 OK` -> `UserDto`
  - `404 Not Found` -> `ErrorDto`
- Side effects: none

### `GET /api/v1/users/{id}`

Short description: get a user by id.

- Auth: authenticated user
- Route params:
  - `id: Guid`
- Request body: none
- Responses:
  - `200 OK` -> `UserDto`
  - `401 Unauthorized` -> `ErrorDto`
  - `404 Not Found` -> `ErrorDto`
- Side effects: none

Notes:

- Admins can fetch any active user.
- Non-admins can only fetch themselves.

### `PUT /api/v1/users/change-password`

Short description: change the current user password.

- Auth: authenticated user
- Query params: none
- Request body: `UserChangePasswordRequestDto`
- Responses:
  - `200 OK`, empty body
  - `400 Bad Request` -> `ErrorDto`
  - `404 Not Found` -> `ErrorDto`
  - `409 Conflict` -> `ErrorDto`
- Side effects:
  - Updates the stored password hash

### `PATCH /api/v1/users/two-factor/enable`

Short description: enable email-based two-factor authentication for the current user.

- Auth: authenticated user
- Query params: none
- Request body: none
- Responses:
  - `200 OK`, empty body
  - `400 Bad Request` -> `ErrorDto`
  - `404 Not Found` -> `ErrorDto`
- Side effects:
  - Sets `TwoFactorEnabled = true` if it was previously disabled
  - Sends a confirmation email stating that 2FA was enabled

### `POST /api/v1/users/confirm-email-request`

Short description: request an email-confirmation link for the current user.

- Auth: authenticated user
- Query params: none
- Request body: none
- Responses:
  - `200 OK`, empty body
  - `401 Unauthorized` -> `ErrorDto`
  - `404 Not Found` -> `ErrorDto`
- Side effects:
  - Generates an email confirmation token
  - Base64-encodes the token
  - Sends an email containing a frontend confirmation link

Notes:

- The confirmation link currently points to `BlazorSettings.AdminPageUrl`.
- If the email is already confirmed, the endpoint returns `401 Unauthorized`.

### `POST /api/v1/users/confirm-email`

Short description: confirm an email address using a token previously sent by email.

- Auth: authenticated user
- Query params: none
- Request body: `UserEmailConfirmRequestDto`
- Responses:
  - `200 OK`, empty body
  - `400 Bad Request`, empty body if the token cannot be base64-decoded
  - `401 Unauthorized` -> `ErrorDto`
  - `404 Not Found` -> `ErrorDto`
  - `409 Conflict` -> `ErrorDto`
- Side effects:
  - Marks the target Identity email as confirmed when the token is valid

### `POST /api/v1/users/reset-password-request`

Short description: request a password-reset email for a user account.

- Auth: none
- Query params: none
- Request body:
  - raw JSON string containing the email address, for example `"user@example.com"`
- Responses:
  - `200 OK`, empty body
  - `404 Not Found` -> `ErrorDto`
- Side effects:
  - Generates a password reset token
  - Base64-encodes the token
  - Sends an email containing a frontend reset link

Notes:

- This endpoint does not accept an object wrapper; the body is a plain JSON string.

### `POST /api/v1/users/reset-password`

Short description: reset a password using a base64-encoded reset token.

- Auth: none
- Query params: none
- Request body: `UserPasswordResetRequestDto`
- Responses:
  - `200 OK`, empty body
  - `400 Bad Request`, empty body if the token cannot be base64-decoded
  - `404 Not Found` -> `ErrorDto`
  - `409 Conflict` -> `ErrorDto`
- Side effects:
  - Updates the stored password hash for the target user

### `DELETE /api/v1/users/logout`

Short description: sign out the current user.

- Auth: authenticated user
- Query params: none
- Request body: none
- Responses:
  - `200 OK`, empty body
  - `404 Not Found` -> `ErrorDto`
- Side effects:
  - Deletes `AuthToken`, `RefreshToken`, and `id` cookies
  - Calls `SignInManager.SignOutAsync()`

### `PATCH /api/v1/users/promote`

Short description: promote a user to the `Admin` role.

- Auth: `AdminOnly`
- Query params:
  - `userId: Guid`
- Request body: none
- Responses:
  - `200 OK`, empty body
  - `400 Bad Request` -> `ErrorDto`
  - `401 Unauthorized` -> `ErrorDto`
  - `404 Not Found` -> `ErrorDto`
- Side effects:
  - Updates the user entity `Role`
  - Removes previous Identity role assignments
  - Adds the `Admin` Identity role

Notes:

- An admin cannot promote or demote themselves through this service.

### `PATCH /api/v1/users/demote`

Short description: demote a user to the `User` role.

- Auth: `AdminOnly`
- Query params:
  - `userId: Guid`
- Request body: none
- Responses:
  - `200 OK`, empty body
  - `400 Bad Request` -> `ErrorDto`
  - `401 Unauthorized` -> `ErrorDto`
  - `404 Not Found` -> `ErrorDto`
- Side effects:
  - Updates the user entity `Role`
  - Removes previous Identity role assignments
  - Adds the `User` Identity role

### `POST /api/v1/users/refresh-token`

Short description: issue a fresh access token from a refresh token.

- Auth: none
- Query params: none
- Request body: `RefreshTokenDto`
- Responses:
  - `200 OK` -> `TokensDto`
  - `400 Bad Request` -> `ErrorDto`
  - `401 Unauthorized` -> `ErrorDto`
  - `404 Not Found` -> `ErrorDto`
- Side effects:
  - Looks up the user by refresh token
  - Issues a new JWT access token

Notes:

- The current implementation preserves the existing refresh token if one is already present; it does not rotate refresh tokens on refresh.

## External login

Base route: `/api/v1/users`

These endpoints support Google, Facebook, and SAML sign-in. They are primarily browser or mobile handoff endpoints, not conventional JSON API calls.

### `GET /api/v1/users/external-login/{provider}`

Short description: start an external authentication challenge.

- Auth: none
- Route params:
  - `provider: ExternalLoginProvider`
- Query params:
  - `frontend: "admin" | "user" | "mobile"`
- Request body: none
- Responses:
  - `307 Temporary Redirect` to the external provider challenge
  - `400 Bad Request` for invalid `frontend` or provider input
  - `401 Unauthorized`
  - `403 Forbidden`
- Side effects:
  - Creates authentication properties carrying the selected frontend target
  - Starts the external provider challenge

### `GET /api/v1/users/external-callback-google`

Short description: Google callback endpoint used by ASP.NET authentication middleware.

- Auth: none
- Query params: provider-managed
- Request body: none
- Responses:
  - nominal successful flow is handled by `TicketReceivedHandler`, which redirects to the frontend callback URL
  - if the action is reached directly, it returns `204 No Content`
- Side effects:
  - On the middleware-handled success path, external user linkage/login is performed and a temporary token handoff key may be created in Redis

### `GET /api/v1/users/external-callback-facebook`

Short description: Facebook callback endpoint used by ASP.NET authentication middleware.

- Auth: none
- Query params: provider-managed
- Request body: none
- Responses:
  - nominal successful flow is handled by `TicketReceivedHandler`, which redirects to the frontend callback URL
  - if the action is reached directly, it returns `204 No Content`
- Side effects:
  - On the middleware-handled success path, external user linkage/login is performed and a temporary token handoff key may be created in Redis

### `GET|POST /api/v1/users/external-callback-saml`

Short description: SAML callback endpoint that completes SAML authentication and redirects back to the frontend.

- Auth: none
- Query params/body: provider-managed SAML response data
- Responses:
  - `302 Found` redirect to frontend `/signin-callback?...`
  - redirect target may encode an error condition in query parameters instead of returning JSON
  - `401 Unauthorized`
  - `403 Forbidden`
  - `404 Not Found`
- Side effects:
  - Reads the external auth cookie from `IdentityConstants.ExternalScheme`
  - Resolves required claims
  - Creates or links the external user account
  - Issues tokens
  - Stores tokens in Redis behind a temporary GUID key for 5 minutes
  - Redirects to the frontend with `code`, `message`, and optional `key`

### `POST /api/v1/users/request-signin-tokens`

Short description: exchange a temporary external-login handoff key for actual tokens.

- Auth: none
- Query params:
  - `id: Guid`
- Request body: none
- Responses:
  - `200 OK` -> `TokensDto`
  - `400 Bad Request` -> `ErrorDto`
  - `404 Not Found` -> `ErrorDto`
- Side effects:
  - Loads tokens from Redis using the supplied key
  - Deletes the Redis entry after reading it
  - Writes `AuthToken`, `RefreshToken`, and `id` cookies

## Groups

Base route: `/api/v1/groups`

### `GET /api/v1/groups`

Short description: list groups the current user belongs to.

- Auth: authenticated user
- Query params: none
- Request body: none
- Responses:
  - `200 OK` -> `GroupDto[]`
  - `401 Unauthorized`
- Side effects: none

### `GET /api/v1/groups/{id}`

Short description: fetch a group by id.

- Auth: authenticated user
- Route params:
  - `id: Guid`
- Request body: none
- Responses:
  - `200 OK` -> `GroupDto`
  - `401 Unauthorized` -> `ErrorDto`
  - `404 Not Found` -> `ErrorDto`
- Side effects: none

Notes:

- Access is limited to admins and group members.

### `POST /api/v1/groups`

Short description: create a new group.

- Auth: `AdminOnly`
- Query params: none
- Request body: `CreateGroupRequest`
- Responses:
  - `201 Created` -> `GroupDto`
  - `400 Bad Request` -> `ErrorDto` or validation response
  - `401 Unauthorized` -> `ErrorDto`
- Side effects:
  - Creates the group
  - Automatically inserts the creator as the first group member

### `DELETE /api/v1/groups/{id}`

Short description: soft-delete a group.

- Auth: `AdminOnly`
- Route params:
  - `id: Guid`
- Request body: none
- Responses:
  - `200 OK`, empty body
  - `400 Bad Request` -> `ErrorDto`
  - `401 Unauthorized` -> `ErrorDto`
  - `404 Not Found` -> `ErrorDto`
- Side effects:
  - Sets `DeletedAt` on the group instead of hard-deleting it

### `POST /api/v1/groups/{id}/members/{userId}`

Short description: add a user to a group.

- Auth: `AdminOnly`
- Route params:
  - `id: Guid`
  - `userId: Guid`
- Request body: none
- Responses:
  - `200 OK`, empty body
  - `400 Bad Request` -> `ErrorDto`
  - `401 Unauthorized` -> `ErrorDto`
  - `404 Not Found` -> `ErrorDto`
- Side effects:
  - Inserts a `GroupMembers` row with the acting admin as `AddedByUserId`

### `DELETE /api/v1/groups/{id}/members/{userId}`

Short description: remove a user from a group.

- Auth: `AdminOnly`
- Route params:
  - `id: Guid`
  - `userId: Guid`
- Request body: none
- Responses:
  - `200 OK`, empty body
  - `400 Bad Request` -> `ErrorDto`
  - `401 Unauthorized` -> `ErrorDto`
  - `404 Not Found` -> `ErrorDto`
- Side effects:
  - Deletes the matching group membership row

## Votings

Base route: `/api/v1/votings`

### `GET /api/v1/votings`

Short description: list votings visible to the current caller.

- Auth: authenticated user
- Query params: none
- Request body: none
- Responses:
  - `200 OK` -> `VotingDto[]`
  - `401 Unauthorized` -> `ErrorDto`
- Side effects: none

Notes:

- Admins receive all votings.
- Non-admins only receive votings they created, filtered by group visibility.

### `GET /api/v1/votings/votable`

Short description: list votings the current `User` can still vote on.

- Auth: `UserOnly`
- Query params: none
- Request body: none
- Responses:
  - `200 OK` -> `VotingDto[]`
  - `401 Unauthorized`
- Side effects: none

Notes:

- Excludes votings created by the current user.
- Excludes votings already voted on by the current user.
- Requires group access if the voting is group-scoped.
- Requires the voting to be ongoing and to have at least two choices.

### `GET /api/v1/votings/voted`

Short description: list votings the current `User` has already voted on.

- Auth: `UserOnly`
- Query params: none
- Request body: none
- Responses:
  - `200 OK` -> `VotingDto[]`
  - `401 Unauthorized`
- Side effects: none

### `GET /api/v1/votings/{id}`

Short description: fetch one voting with choice data.

- Auth: authenticated user
- Route params:
  - `id: long`
- Request body: none
- Responses:
  - `200 OK` -> `VotingDto`
  - `401 Unauthorized` -> `ErrorDto`
  - `404 Not Found` -> `ErrorDto`
- Side effects: none

Notes:

- Group-scoped votings require membership unless the caller is an admin.
- The controller always computes `hasVoted` for the current caller; in practice admins will normally see `false`, not `null`.

### `GET /api/v1/votings/{id}/results`

Short description: fetch aggregated voting results by choice.

- Auth: authenticated user
- Route params:
  - `id: long`
- Request body: none
- Responses:
  - `200 OK` -> `VotingResultsDto`
  - `401 Unauthorized` -> `ErrorDto`
  - `404 Not Found` -> `ErrorDto`
- Side effects: none

Notes:

- Access is allowed to admins, the voting creator, and users who have already voted on that voting.

### `POST /api/v1/votings`

Short description: create a new voting.

- Auth: authenticated user, but the service only allows non-admin creators
- Query params: none
- Request body: `VotingCreateRequestDto`
- Responses:
  - `201 Created` -> `VotingDto`
  - `400 Bad Request` -> `ErrorDto` or validation response
  - `401 Unauthorized` -> `ErrorDto`
  - `404 Not Found` -> `ErrorDto` when the supplied `groupId` does not exist
  - `409 Conflict` -> `ErrorDto`
- Side effects:
  - Creates a `Voting` row with a generated salt

Notes:

- `groupId` is optional.
- If `groupId` is present, the creator must be a member of that group.
- `startsAt` must be in the future.
- `endsAt` must be at least one day in the future and at least one day after `startsAt`.

### `PATCH /api/v1/votings/{id}/starts-at`

Short description: change the scheduled start time of a voting that has not started yet.

- Auth: authenticated user
- Route params:
  - `id: long`
- Request body:
  - raw JSON `DateTime` literal, for example `"2026-05-01T08:00:00Z"`
- Responses:
  - `200 OK` -> `VotingDto`
  - `400 Bad Request` -> string or `ErrorDto`
  - `401 Unauthorized` -> `ErrorDto`
  - `404 Not Found` -> `ErrorDto`
- Side effects:
  - Updates `StartsAt`

Notes:

- Rejects updates if the voting has already started.
- Rejects `startsAt` values that are less than one day from now or not at least one day before the current `EndsAt`.

### `PATCH /api/v1/votings/{id}/ends-at`

Short description: change the scheduled end time of a voting that has not started yet.

- Auth: authenticated user
- Route params:
  - `id: long`
- Request body:
  - raw JSON `DateTime` literal
- Responses:
  - `200 OK` -> `VotingDto`
  - `400 Bad Request` -> string or `ErrorDto`
  - `401 Unauthorized` -> `ErrorDto`
  - `404 Not Found` -> `ErrorDto`
- Side effects:
  - Updates `EndsAt`

Notes:

- Rejects updates if the voting has already started.
- Rejects `endsAt` values that are not more than one day after now and one day after `StartsAt`.

### `POST /api/v1/votings/{id}/start`

Short description: start a voting immediately.

- Auth: authenticated user
- Route params:
  - `id: long`
- Request body: none
- Responses:
  - `200 OK` -> `VotingDto`
  - `400 Bad Request` -> string or `ErrorDto`
  - `401 Unauthorized` -> `ErrorDto`
  - `404 Not Found` -> `ErrorDto`
- Side effects:
  - Sets `StartsAt = DateTime.UtcNow`

Notes:

- Requires at least two vote choices.
- Rejects already-started votings.

### `DELETE /api/v1/votings/{id}`

Short description: delete a voting that has not started yet.

- Auth: authenticated user
- Route params:
  - `id: long`
- Request body: none
- Responses:
  - `200 OK`, empty body
  - `400 Bad Request` -> `ErrorDto`
  - `401 Unauthorized` -> `ErrorDto`
  - `404 Not Found` -> `ErrorDto`
  - `409 Conflict` -> `ErrorDto`
- Side effects:
  - Removes the voting row

Notes:

- Admins and creators may delete.
- Started votings cannot be deleted.

### `GET /api/v1/votings/{id}/votes`

Short description: fetch the anonymous ballots for a voting.

- Auth: authenticated user
- Route params:
  - `id: long`
- Request body: none
- Responses:
  - `200 OK` -> `BallotDto[]`
  - `401 Unauthorized` -> `ErrorDto`
  - `404 Not Found` -> `ErrorDto`
- Side effects: none

Notes:

- Access is allowed to admins, the voting creator, and users who have already voted on that voting.

## Vote choices

Base route: `/api/v1/votings/{votingId}/choices`

### `GET /api/v1/votings/{votingId}/choices`

Short description: list all choices for a voting.

- Auth: authenticated user
- Route params:
  - `votingId: long`
- Request body: none
- Responses:
  - `200 OK` -> `VoteChoiceDto[]`
  - `401 Unauthorized` -> `ErrorDto`
  - `404 Not Found` -> `ErrorDto`
- Side effects: none

Notes:

- Access depends on the caller being allowed to access the parent voting.

### `GET /api/v1/votings/{votingId}/choices/{choiceId}`

Short description: fetch one choice inside a voting.

- Auth: authenticated user
- Route params:
  - `votingId: long`
  - `choiceId: long`
- Request body: none
- Responses:
  - `200 OK` -> `VoteChoiceDto`
  - `401 Unauthorized` -> `ErrorDto`
  - `404 Not Found` -> `ErrorDto`
- Side effects: none

### `POST /api/v1/votings/{votingId}/choices`

Short description: add a new choice to a voting.

- Auth: authenticated user
- Route params:
  - `votingId: long`
- Request body: `VoteChoiceRequestDto`
- Responses:
  - `201 Created` -> `VoteChoiceDto`
  - `400 Bad Request` -> `ErrorDto` or validation response
  - `401 Unauthorized` -> `ErrorDto`
  - `404 Not Found` -> `ErrorDto`
  - `409 Conflict` -> `ErrorDto`
- Side effects:
  - Inserts a new `VoteChoice` row

Notes:

- Only the creator of the voting can add choices.
- Choices cannot be added after the voting has started.

### `DELETE /api/v1/votings/{votingId}/choices/{choiceId}`

Short description: delete a choice from a voting.

- Auth: authenticated user
- Route params:
  - `votingId: long`
  - `choiceId: long`
- Request body: none
- Responses:
  - `200 OK`, empty body
  - `401 Unauthorized` -> `ErrorDto`
  - `404 Not Found` -> `ErrorDto`
  - `409 Conflict` -> `ErrorDto`
- Side effects:
  - Deletes the `VoteChoice` row

Notes:

- Only the creator of the voting can delete choices.
- Choices cannot be deleted after the voting has started.

## Votes

Base route: `/api/v1/votes`

### `POST /api/v1/votes/cast-vote`

Short description: cast a vote for one choice.

- Auth: `UserOnly`
- Query params:
  - `choiceId: long`
- Request body: none
- Responses:
  - `200 OK` -> `VoteResultDto`
  - `400 Bad Request` -> `ErrorDto`
  - `401 Unauthorized` -> `ErrorDto`
  - `404 Not Found` -> `ErrorDto`
  - `409 Conflict` -> `ErrorDto`
- Side effects:
  - Inserts a `VotingParticipation`
  - Inserts an `AnonymousBallot`
  - Increments `VoteChoice.VoteCount`
  - Persists all changes in a single save
  - Broadcasts updated aggregated results through SignalR

Notes:

- Admins cannot vote.
- Users cannot vote on their own votings.
- Group-scoped votings require membership in the group.
- A user may only vote once per voting.
- The voting must currently be ongoing.

### `GET /api/v1/votes/`

Short description: list the current user voting participations.

- Auth: authenticated user
- Query params: none
- Request body: none
- Responses:
  - `200 OK` -> `VotingParticipationDto[]`
  - `401 Unauthorized` -> `ErrorDto`
- Side effects: none

Notes:

- Admins are rejected by the service with `401 Unauthorized`.

## Real-time

### `GET /Hubs/VotesHub?access_token=<jwt>`

Short description: authenticated SignalR hub that broadcasts updated aggregate voting results.

- Auth: authenticated user
- Query params:
  - `access_token?: string` for SignalR connections that pass the JWT in the query string
- Request body: none
- Responses:
  - SignalR/WebSocket negotiation response rather than a REST JSON body
- Side effects:
  - Successful calls to `POST /api/v1/votes/cast-vote` trigger broadcasts to all connected clients

Expected pushed payload:

```ts
type VotingUpdatedDto = {
  votingId: number;
  votingResults: VotingResultsDto;
};
```
