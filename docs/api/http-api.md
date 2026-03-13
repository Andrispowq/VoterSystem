# VoterSystem HTTP API Reference

This document describes the HTTP API currently implemented in `VoterSystem.WebAPI`.
It is written for client developers, especially the mobile client, so the focus is on:

- routes
- authentication requirements
- request query/body shapes
- response shapes
- current business rules and edge cases

All routes below are relative to the API host, for example:

```text
https://your-api-host/api/v1/...
```

## General Notes

### Authentication

The API uses JWT bearer authentication.

Clients can authenticate in either of these ways:

- `Authorization: Bearer <authToken>`
- auth cookie set by login (`auth_token`)

For mobile, prefer the bearer token.

### Roles

The backend distinguishes two roles:

- `User`
- `Admin`

Some endpoints are available to any authenticated user, while others are effectively restricted by role or ownership rules in the service layer.

### Date/time format

All date fields are `DateTime` values. Send and store them as ISO 8601 UTC timestamps, for example:

```json
"2026-03-13T10:30:00Z"
```

### Error responses

Most custom application errors are returned as plain string bodies:

- `400 Bad Request` -> string
- `401 Unauthorized` -> string
- `404 Not Found` -> string
- `409 Conflict` -> string
- `422 Unprocessable Entity` -> string

Validation failures from ASP.NET model binding may instead return `ValidationProblemDetails`.

## Shared Types

These type definitions are not generated from OpenAPI; they are handwritten to match the current code closely.

```ts
type Role = "User" | "Admin";

type TokensDto = {
  authToken: string;
  refreshToken: string; // GUID
  userId: string; // GUID
};

type TwoFactorChallengeDto = {
  challengeId: string; // GUID
  message: string;
};

type TwoFactorVerificationRequestDto = {
  challengeId: string; // GUID
  code: string; // 6-digit code sent by email
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
  token: string; // base64-encoded token string
};

type UserPasswordResetRequestDto = {
  email: string;
  token: string; // base64-encoded token string
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
  createdAt: string; // ISO datetime
};

type VotingCreateRequestDto = {
  name: string;
  startsAt: string; // ISO datetime
  endsAt: string; // ISO datetime
};

type VotingDto = {
  votingId: number;
  name: string;
  createdAt: string; // ISO datetime
  startsAt: string; // ISO datetime
  endsAt: string; // ISO datetime
  hasStarted: boolean;
  hasEnded: boolean;
  isOngoing: boolean;
  hasVoted: boolean | null; // null for admin responses; populated on GET /votings/{id}
  voteChoices: VoteChoiceDto[];
};

type VotingParticipationDto = {
  voting: VotingDto;
};

type UserDto = {
  id: string; // GUID
  name: string;
  email: string;
  emailConfirmed: boolean;
  twoFactorEnabled: boolean;
  role: Role;
  participations: VotingParticipationDto[];
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
  createdAt: string; // ISO datetime
};
```

## Health

### `ANY /api/v1/health`

Health check endpoint backed by the database. The implementation is not a controller action; it is mapped by ASP.NET health checks middleware.

- Auth: none
- Query params: none
- Body: none
- Success response: `200 OK`
- Failure response: `503 Service Unavailable`

Notes:

- The integration test currently calls this endpoint with `POST`, and the middleware accepts it.
- For clients and infrastructure, `GET /api/v1/health` is still the sensible default.

## Users

Base route: `/api/v1/users`

### `POST /api/v1/users/register`

Create a new user account.

- Auth: none
- Body: `UserRegisterRequestDto`
- Success:
  - `201 Created`
  - body: `UserDto`
- Error responses:
  - `400 Bad Request` if validation fails or the user cannot be created

Notes:

- If there are no admins yet, the first registered user becomes `Admin`.
- Otherwise new users are created as `User`.

### `POST /api/v1/users/login`

Authenticate with email and password.

- Auth: none
- Body: `UserLoginRequestDto`
- Success:
  - `200 OK` with `TokensDto` when 2FA is not enabled
  - `202 Accepted` with `TwoFactorChallengeDto` when 2FA is enabled
- Error responses:
  - `401 Unauthorized` for invalid password or lockout
  - `404 Not Found` if the email does not exist

Notes:

- On success without 2FA, the server also sets the auth cookie.
- When 2FA is enabled, the user must complete `/login/2fa`.

### `POST /api/v1/users/login/2fa`

Finish login with the emailed 2FA code.

- Auth: none
- Body: `TwoFactorVerificationRequestDto`
- Success:
  - `200 OK`
  - body: `TokensDto`
- Error responses:
  - `401 Unauthorized` for invalid or expired challenge/code
  - `404 Not Found` if the challenged user no longer exists

Notes:

- The server also sets the auth cookie on success.

### `GET /api/v1/users/all`

Get all users.

- Auth: required
- Effective access: admin only
- Body: none
- Success:
  - `200 OK`
  - body: `UserDto[]`
- Error responses:
  - `401 Unauthorized` if unauthenticated or not allowed

### `GET /api/v1/users`

Get the currently authenticated user.

- Auth: required
- Body: none
- Success:
  - `200 OK`
  - body: `UserDto`
- Error responses:
  - `401 Unauthorized` if no valid token is sent
  - `404 Not Found` if the token resolves to a missing user

### `GET /api/v1/users/{id}`

Get a user by ID.

- Auth: required
- Path params:
  - `id: GUID`
- Success:
  - `200 OK`
  - body: `UserDto`
- Error responses:
  - `401 Unauthorized` if a non-admin requests someone else
  - `404 Not Found` if the user does not exist

Notes:

- Admins can fetch any user.
- Non-admins can only fetch themselves.

### `PUT /api/v1/users/change-password`

Change the current user password.

- Auth: required
- Body: `UserChangePasswordRequestDto`
- Success:
  - `200 OK`
  - empty body
- Error responses:
  - `400 Bad Request` if old and new password are the same or model validation fails
  - `401 Unauthorized` if unauthenticated
  - `404 Not Found` if the current user cannot be resolved
  - `409 Conflict` if Identity rejects the password change

### `PATCH /api/v1/users/two-factor/enable`

Enable email-based two-factor authentication for the current user.

- Auth: required
- Body: none
- Success:
  - `200 OK`
  - empty body
- Error responses:
  - `401 Unauthorized` if unauthenticated
  - `400 Bad Request` if enabling 2FA fails

Notes:

- The server sends a notification email after enabling 2FA.

### `POST /api/v1/users/confirm-email-request`

Request an email confirmation link for the current user.

- Auth: required
- Body: none
- Success:
  - `200 OK`
  - empty body
- Error responses:
  - `401 Unauthorized` if unauthenticated or the email is already confirmed
  - `404 Not Found` if the current user cannot be resolved

Notes:

- This endpoint sends an email containing a frontend link with a base64-encoded token.

### `POST /api/v1/users/confirm-email`

Confirm a user email address with a previously generated token.

- Auth: required
- Body: `UserEmailConfirmRequestDto`
- Success:
  - `200 OK`
  - empty body
- Error responses:
  - `400 Bad Request` if the token is not valid base64
  - `401 Unauthorized` if unauthenticated
  - `404 Not Found` if the user does not exist
  - `409 Conflict` if the token is rejected by Identity

Notes:

- `token` must be the base64-encoded value from the email link, not the raw Identity token.

### `POST /api/v1/users/reset-password-request`

Request a password reset email for a user email address.

- Auth: none
- Body: raw JSON string containing the email address
- Example body:

```json
"user@example.com"
```

- Success:
  - `200 OK`
  - empty body
- Error responses:
  - `404 Not Found` if the email does not belong to a user

Notes:

- This is not a DTO object; it expects a bare JSON string.
- The server sends a frontend link with a base64-encoded reset token.

### `POST /api/v1/users/reset-password`

Reset the password using the emailed token.

- Auth: none
- Body: `UserPasswordResetRequestDto`
- Success:
  - `200 OK`
  - empty body
- Error responses:
  - `400 Bad Request` if the token is not valid base64
  - `404 Not Found` if the user does not exist
  - `409 Conflict` if Identity rejects the reset

Notes:

- `token` must be the base64-encoded value from the email link.

### `DELETE /api/v1/users/logout`

Log out the current user.

- Auth: required
- Body: none
- Success:
  - `200 OK`
  - empty body
- Error responses:
  - `401 Unauthorized` if unauthenticated

Notes:

- The auth cookie is deleted by the server.
- This does not invalidate already-issued JWTs on the client side.

### `PATCH /api/v1/users/promote?userId={guid}`

Promote a user to admin.

- Auth: required
- Effective access: admin only
- Query params:
  - `userId: GUID`
- Body: none
- Success:
  - `200 OK`
  - empty body
- Error responses:
  - `401 Unauthorized` if unauthenticated, non-admin, or attempting to promote self
  - `404 Not Found` if the target user does not exist
  - `400 Bad Request` if role updates fail

### `PATCH /api/v1/users/demote?userId={guid}`

Demote a user to normal user.

- Auth: required
- Effective access: admin only
- Query params:
  - `userId: GUID`
- Body: none
- Success:
  - `200 OK`
  - empty body
- Error responses:
  - `401 Unauthorized` if unauthenticated, non-admin, or attempting to demote self
  - `404 Not Found` if the target user does not exist
  - `400 Bad Request` if role updates fail

### `POST /api/v1/users/refresh-token`

Exchange a refresh token for a new auth token and a new refresh token.

- Auth: none
- Body: raw JSON string containing the refresh token GUID
- Example body:

```json
"7d130d58-e3f4-4c89-ae87-6fc7d0b8e9d1"
```

- Success:
  - `200 OK`
  - body: `TokensDto`
- Error responses:
  - `400 Bad Request` if the body is not a GUID string
  - `404 Not Found` if the refresh token is invalid

Notes:

- This endpoint also expects a bare JSON string, not an object.

## Votings

Base route: `/api/v1/votings`

### `GET /api/v1/votings`

Get all votings visible to the current user.

- Auth: required
- Body: none
- Success:
  - `200 OK`
  - body: `VotingDto[]`
- Error responses:
  - `401 Unauthorized` if unauthenticated

Notes:

- Both admins and users can access this.
- `hasVoted` is usually `null` in list responses.

### `GET /api/v1/votings/votable`

Get votings the current user has not yet participated in.

- Auth: required
- Effective access: `User` role only
- Body: none
- Success:
  - `200 OK`
  - body: `VotingDto[]`
- Error responses:
  - `401 Unauthorized` if unauthenticated
  - `403 Forbidden` if called by an admin

Notes:

- The current implementation filters only by participation, not by whether the voting has started or ended.

### `GET /api/v1/votings/voted`

Get votings the current user has already voted in.

- Auth: required
- Effective access: `User` role only
- Body: none
- Success:
  - `200 OK`
  - body: `VotingDto[]`
- Error responses:
  - `401 Unauthorized` if unauthenticated
  - `403 Forbidden` if called by an admin

### `GET /api/v1/votings/{id}`

Get a single voting by ID.

- Auth: required
- Path params:
  - `id: long`
- Success:
  - `200 OK`
  - body: `VotingDto`
- Error responses:
  - `401 Unauthorized` if unauthenticated or access is denied
  - `404 Not Found` if the voting does not exist

Notes:

- This is the only voting endpoint that explicitly populates `hasVoted`.

### `GET /api/v1/votings/{id}/results`

Get aggregated results for a voting.

- Auth: required
- Path params:
  - `id: long`
- Success:
  - `200 OK`
  - body: `VotingResultsDto`
- Error responses:
  - `401 Unauthorized` if the caller is not allowed to see results
  - `404 Not Found` if the voting does not exist

Notes:

- Admins can always read results.
- The voting creator can read results.
- A normal user can read results only after they have voted in that voting.

### `POST /api/v1/votings`

Create a new voting.

- Auth: required
- Body: `VotingCreateRequestDto`
- Success:
  - `201 Created`
  - body: `VotingDto`
- Error responses:
  - `400 Bad Request` if:
    - `startsAt <= now`
    - `endsAt <= now + 1 day`
    - `startsAt + 1 day > endsAt`
    - model validation fails
  - `401 Unauthorized` if unauthenticated or role/ownership checks fail
  - `409 Conflict` if persistence fails

Notes:

- The current tests create votings as a normal `User`, so this is not admin-only.

### `PATCH /api/v1/votings/{id}/starts-at`

Update the start time of a voting that has not started yet.

- Auth: required
- Path params:
  - `id: long`
- Body: raw JSON datetime value
- Example body:

```json
"2026-03-20T12:00:00Z"
```

- Success:
  - `200 OK`
  - body: `VotingDto`
- Error responses:
  - `400 Bad Request` if:
    - the voting already started
    - `startsAt < now + 1 day`
    - `startsAt >= endsAt - 1 day`
  - `401 Unauthorized` if unauthenticated or not allowed to update
  - `404 Not Found` if the voting does not exist

### `PATCH /api/v1/votings/{id}/ends-at`

Update the end time of a voting that has not started yet.

- Auth: required
- Path params:
  - `id: long`
- Body: raw JSON datetime value
- Example body:

```json
"2026-03-25T12:00:00Z"
```

- Success:
  - `200 OK`
  - body: `VotingDto`
- Error responses:
  - `400 Bad Request` if:
    - the voting already started
    - `endsAt <= now + 1 day`
    - `endsAt <= startsAt + 1 day`
  - `401 Unauthorized` if unauthenticated or not allowed to update
  - `404 Not Found` if the voting does not exist

### `POST /api/v1/votings/{id}/start`

Start a voting immediately.

- Auth: required
- Path params:
  - `id: long`
- Body: none
- Success:
  - `200 OK`
  - body: `VotingDto`
- Error responses:
  - `400 Bad Request` if:
    - the voting has fewer than 2 choices
    - the voting already started
  - `401 Unauthorized` if unauthenticated or not allowed to update
  - `404 Not Found` if the voting does not exist

Notes:

- This sets `startsAt = DateTime.UtcNow`.

### `DELETE /api/v1/votings/{id}`

Delete a voting.

- Auth: required
- Path params:
  - `id: long`
- Body: none
- Success:
  - `200 OK`
  - empty body
- Error responses:
  - `401 Unauthorized` if unauthenticated or not allowed
  - `404 Not Found` if the voting does not exist
  - `409 Conflict` if deletion fails

### `GET /api/v1/votings/{id}/votes`

Get anonymous ballots for a voting.

- Auth: required
- Path params:
  - `id: long`
- Success:
  - `200 OK`
  - body: `BallotDto[]`
- Error responses:
  - `401 Unauthorized` if the caller is not allowed to see ballots
  - `404 Not Found` if the voting does not exist

Notes:

- Access rules are the same as for `/results`.

## Choices

Base route: `/api/v1/votings/{votingId}/choices`

### `GET /api/v1/votings/{votingId}/choices`

Get all choices for a voting.

- Auth: required
- Path params:
  - `votingId: long`
- Success:
  - `200 OK`
  - body: `VoteChoiceDto[]`
- Error responses:
  - `401 Unauthorized` if unauthenticated or access is denied
  - `404 Not Found` if the voting does not exist

### `GET /api/v1/votings/{votingId}/choices/{choiceId}`

Get one choice by ID within a voting.

- Auth: required
- Path params:
  - `votingId: long`
  - `choiceId: long`
- Success:
  - `200 OK`
  - body: `VoteChoiceDto`
- Error responses:
  - `401 Unauthorized` if unauthenticated or access is denied
  - `404 Not Found` if the voting or choice does not exist

### `POST /api/v1/votings/{votingId}/choices`

Create a new choice for a voting.

- Auth: required
- Path params:
  - `votingId: long`
- Body: `VoteChoiceRequestDto`
- Success:
  - `201 Created`
  - body: `VoteChoiceDto`
- Error responses:
  - `400 Bad Request` if model validation fails
  - `401 Unauthorized` if:
    - unauthenticated
    - the current user did not create the voting
    - the voting already started
  - `404 Not Found` if the voting does not exist
  - `409 Conflict` if persistence fails

### `DELETE /api/v1/votings/{votingId}/choices/{choiceId}`

Delete a choice from a voting.

- Auth: required
- Path params:
  - `votingId: long`
  - `choiceId: long`
- Success:
  - `200 OK`
  - empty body
- Error responses:
  - `401 Unauthorized` if unauthenticated or the voting already started
  - `404 Not Found` if the voting or choice does not exist
  - `409 Conflict` if deletion fails

Notes:

- Unlike create, delete does not explicitly verify the caller is the voting creator in the controller/service path shown here. If ownership restrictions matter for the mobile client UX, verify this behavior before relying on it.

## Votes

Base route: `/api/v1/votes`

### `POST /api/v1/votes/cast-vote?choiceId={choiceId}`

Cast a vote for a choice.

- Auth: required
- Effective access: `User` role only
- Query params:
  - `choiceId: long`
- Body: none
- Success:
  - `200 OK`
  - body: `VoteResultDto`
- Error responses:
  - `401 Unauthorized` if:
    - unauthenticated
    - the caller is an admin
    - the caller is voting on a voting they created
  - `404 Not Found` if the choice does not exist
  - `409 Conflict` if the user already voted in that voting
  - `400 Bad Request` for unexpected persistence/processing errors

Notes:

- Successful voting also triggers a SignalR notification with updated aggregate results.
- The current implementation does not explicitly block voting before `startsAt` or after `endsAt` in this controller/service path. If the mobile client should enforce that, do it from `VotingDto` flags for now.

### `GET /api/v1/votes`

Get the authenticated user's voting participations.

- Auth: required
- Success:
  - `200 OK`
  - body: `VotingParticipationDto[]`
- Error responses:
  - `401 Unauthorized` if unauthenticated
  - `401 Unauthorized` if the caller is an admin

Notes:

- Admins cannot vote and cannot use this endpoint successfully.

## Real-time Endpoint

This is not part of the REST API, but it is implemented and relevant for clients that want live result updates.

### `GET /Hubs/VotesHub?access_token=<jwt>`

SignalR hub for vote result updates.

- Auth: required
- Transport: SignalR/WebSocket fallback stack
- Token transport:
  - standard bearer auth works for normal HTTP calls
  - for the hub, the JWT can also be passed as the `access_token` query parameter

Expected pushed payload:

```ts
type VotingUpdatedDto = {
  votingId: number;
  votingResults: VotingResultsDto;
};
```

## Current Quirks Worth Preserving In Client Code

- Some endpoints expect a raw JSON string body instead of an object:
  - `/api/v1/users/reset-password-request`
  - `/api/v1/users/refresh-token`
- The health endpoint is middleware-based and currently accepts methods besides `GET`.
- `GET /api/v1/votings/votable` does not currently mean "open for voting"; it means "not yet participated in".
- `POST /api/v1/votes/cast-vote` does not currently enforce voting window dates in the service path shown here.
- Error bodies are usually plain strings, not structured JSON error objects.
