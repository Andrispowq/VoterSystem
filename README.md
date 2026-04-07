# VoterSystem

This is an anonymous voting system, made up of a backend written in ASP.NET and a user and admin frontend, written in Blazor WebAssembly.

## Development

There are three types of branches:
- main: the final product, that is no longer under development as it's a complete version
- development: the latest stable version, that can still be under development
- feature branches (F-<feature>): active development, can be unstable

## Installation/running

To run the program, you can use the included docker compose yaml file to start the service. The commend ``docker compose --profile prod up --build`` runs the software, binding to the following ports: 
- backend: http://localhost:6900
- user frontend: http://localhost:6901
- admin frontend: http://localhost:6902
You need to copy the .env.example file into a new .env file, and fill out the environment variables needed to run the program.

If you want to run the components locally, you can also use the ``only-db`` profile. This configuration only starts the following services: postgres, redis, mailhog, smtp_relay and idp. After this, you can run the individual components via ``dotnet run``. However in this configuration the .env file is not available to be read by the dotnet environment, so you will need to provide these values in the user secrets file of the WebApi project. Here's the format you need to paste:

```{
  "DotEnv": [
    { "name": "API_HTTP", "value": "http://localhost:6900" },
    { "name": "API_HTTPS", "value": "https://localhost:6910" },
    { "name": "WEB_HTTP", "value": "http://localhost:6901" },
    { "name": "WEB_HTTPS", "value": "https://localhost:6911" },
    { "name": "ADMIN_HTTP", "value": "http://localhost:6902" },
    { "name": "ADMIN_HTTPS", "value": "https://localhost:6912" },
    { "name": "MOBILE_LINK", "value": "com.akmeczo.votersystem:/" },
    { "name": "DB_IP", "value": "db" },
    { "name": "DB_ROOT_PASSWORD", "value": "root" },
    { "name": "DB_USER", "value": "server_access" },
    { "name": "DB_PASSWORD", "value": "test_password" },
    { "name": "JWT_KEY", "value": "add it" },
    { "name": "VOTING_MASTER_KEY", "value": "add it" },
    { "name": "JWT_DOMAIN", "value": "voter-system.gov.hu" },
    { "name": "EMAIL_HOST", "value": "localhost" },
    { "name": "EMAIL_PORT", "value": "1025" },
    { "name": "EMAIL_SSL", "value": "false" },
    { "name": "EMAIL_USER", "value": "" },
    { "name": "EMAIL_PASSWORD", "value": "" },
    { "name": "EMAIL_FROM", "value": "no-reply@voter-system.gov.hu" },
    { "name": "REDIS_HOST", "value": "localhost" },
    { "name": "REDIS_PASSWORD", "value": "" },
    { "name": "RABBITMQ_DEFAULT_USER", "value": "default_user" },
    { "name": "RABBITMQ_DEFAULT_PASS", "value": "default_pass" },
    { "name": "OAUTH_GOOGLE_CLIENT_ID", "value": "add it" },
    { "name": "OAUTH_GOOGLE_CLIENT_SECRET", "value": "add it" },
    { "name": "OAUTH_FACEBOOK_CLIENT_ID", "value": "add it" },
    { "name": "OAUTH_FACEBOOK_CLIENT_SECRET", "value": "add it" },
    { "name": "SAML_ENABLED", "value": "true" },
    { "name": "SAML_RETURN_URL", "value": "https://localhost:6910" },
    { "name": "SAML_SIGN_REQUESTS", "value": "false" },
    { "name": "SAML_SIGNING_CERT_BASE64", "value": "" },
    { "name": "SAML_SIGNING_CERT_PASSWORD", "value": "" },
    { "name": "SAML_PUBLIC_ORIGIN", "value": "https://localhost:6910" },
    { "name": "SAML_SP_ENTITY_ID", "value": "https://localhost:6910/Saml2" },
    { "name": "SAML_METADATA_URL", "value": "http://localhost:8080/simplesaml/saml2/idp/metadata.php" },
    { "name": "SAML_IDP_ENTITY_ID", "value": "http://localhost:8080/simplesaml/saml2/idp/metadata.php" },
    { "name": "SAML_IDP_SSO", "value": "http://localhost:8080/simplesaml/saml2/idp/SSOService.php" },
    { "name": "SAML_IDP_SLO", "value": "http://localhost:8080/simplesaml/saml2/idp/SingleLogoutService.php" }
  ]
}```

