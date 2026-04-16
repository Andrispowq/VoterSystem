# SAML 2.0 Authentication

This document explains how to enable and verify SAML-based logins for the VoterSystem stack.

## Overview

The API now exposes SAML as an additional external provider next to Google and Facebook. The authentication handshake uses the ASP.NET Core `Sustainsys.Saml2` handler, so the normal `/api/v1/users/external-login/{provider}` route can now accept `Saml` and the web UIs show a "Login with SAML" button.

When a SAML response is accepted, the existing `TicketReceivedHandler` issues short-lived request keys that the front-end exchanges through `/api/v1/users/request-signin-tokens`, exactly like the other providers.

## Required Environment Variables

All SAML settings live under the `Authorisation:Saml` section in `VoterSystem.WebAPI/appsettings.json` and can be overridden via the following environment variables:

| Variable | Description |
| --- | --- |
| `SAML_ENABLED` | Set to `true` to register the SAML authentication handler. |
| `SAML_PUBLIC_ORIGIN` | Base HTTPS origin of the API (for example `https://localhost:6910`). Used to build the ACS URL when `SAML_RETURN_URL` is empty. |
| `SAML_SP_ENTITY_ID` | Service Provider entity ID that the IdP registers. Typically matches the API origin. |
| `SAML_IDP_ENTITY_ID` | Entity ID of the external IdP. |
| `SAML_METADATA_URL` | Optional metadata URL that the handler can download to resolve bindings and certificates. Leave empty to configure the IdP endpoints manually. |
| `SAML_IDP_SSO` | Required when metadata is not available. Absolute Single Sign-On endpoint of the IdP. |
| `SAML_IDP_SLO` | Optional Single Logout endpoint. Set even if metadata is available to override the URL provided there. |
| `SAML_RETURN_URL` | Absolute Assertion Consumer Service URL. When omitted, the backend uses `<SAML_PUBLIC_ORIGIN>/api/v1/users/external-callback-saml`. |
| `SAML_SIGN_REQUESTS` | Set to `true` when the IdP requires signed AuthnRequests. Requires a certificate. |
| `SAML_SIGNING_CERT_BASE64` | Base64-encoded PKCS12/PFX certificate that the backend uses to sign AuthnRequests. |
| `SAML_SIGNING_CERT_PASSWORD` | Optional password for the signing certificate. |

> The repo's `.env` file contains local defaults that match the SimpleSAMLphp IdP exposed in `docker-compose.yml` on `localhost:8080`.

## Running the Local IdP

The docker compose setup already defines an `idp` service:

```bash
docker compose up -d idp
```

The container exposes the metadata at `http://localhost:8080/simplesaml/saml2/idp/metadata.php`. The defaults in `.env` and `appsettings.json` point to that metadata and expect the API to be reachable over HTTPS at `https://localhost:6910` (the default Kestrel port in `launchSettings.json`).

If you add the API as an SP inside the IdP dashboard, ensure that:

- the entity ID matches `SAML_SP_ENTITY_ID`
- the ACS/Callback URL is `https://localhost:6910/api/v1/users/external-callback-saml`

## Triggering the Login Flow

With the IdP running and the environment variables set, initiate the login from either web front-end or directly via the API:

```bash
curl -I "https://localhost:6910/api/v1/users/external-login/Saml?frontend=user"
```

The API will respond with a `302` redirect to the IdP. After authentication, the handler validates the signed assertion, links or creates the user in ASP.NET Identity, and sends the browser back to `/signin-callback` with the token request key.

Admins can trigger the same flow by using `frontend=admin`, which changes the redirect destination after token issuance.

## Troubleshooting Tips

- A `400 Invalid provider option` response usually means the SAML handler is not enabled (`SAML_ENABLED` was false) or the scheme failed to register because of missing configuration.
- Check the API logs for `Authorisation:Saml` validation errors—they throw during startup when required fields are missing.
- To inspect the raw SAML response, temporarily set the `ASPNETCORE_ENVIRONMENT=Development` and enable verbose logging for `Sustainsys.Saml2` in `appsettings.Development.json`.
- Clock skew between the IdP and API hosts can cause token validation failures. The handler uses the default `Sustainsys` tolerances; adjust the IdP or host clocks if assertions are rejected immediately.
