# Fortnox Authentication Provider

<Ingress>
Allow users to sign in with Fortnox and access the Fortnox API on behalf of the user.
</Ingress>

## Overview

Fortnox is a cloud-based platform for accounting, invoicing, and business administration, widely used by businesses in Sweden. The Fortnox authentication provider enables secure OAuth-based sign-in, allowing users to grant your application access to their Fortnox data and services.

## Getting Your Fortnox Configuration

Before using Fortnox with Ivy, you'll need to register an integration and obtain your configuration values:

### Step 1: Register Your Integration

1. **Get access** to the [Fortnox Developer Portal](https://www.fortnox.se/developer/developer-portal)
2. **Navigate** to **Developer Portal > Integrations**
3. **Create** an integration and give it a name

### Step 2: Edit Your Integration

1. On the **OAuth** page, add the [scopes](https://www.fortnox.se/developer/guides-and-good-to-know/scopes) that your application needs.
1. On the **OAuth** page, set the **Redirect URI** to `http://localhost:5010/webhook` during development and `https://<your-domain>/webhook` in production.

### Step 3: Get Your Configuration Values

From your integrations **OAuth** page, copy these values:

- **Client ID**
- **Client Secret**

## Adding Authentication

To set up Fortnox Authentication with Ivy, run the following command and choose `Fortnox` when asked to select an auth provider:

```terminal
>ivy auth add
```

You will be prompted to provide the following Fortnox configuration:

- **Client ID**
- **Client Secret**

Your credentials will be stored securely in .NET user secrets. Ivy then finishes configuring your application automatically:

1. Adds the `Ivy.Auth.Fortnox` package to your project.
2. Adds `server.UseAuth<FortnoxAuthProvider>(c => c.UseFortnox());` to your `Program.cs`.
3. Adds `Ivy.Auth.Fortnox` to your global usings.

### Advanced Configuration

#### Connection Strings

To skip the interactive prompts, you can provide configuration via a connection string:

```terminal
>ivy auth add --provider Fortnox --connection-string "FORTNOX_CLIENT_ID=your-client-id;FORTNOX_CLIENT_SECRET=your-client-secret"
```

For a list of connection string parameters, see [Configuration Parameters](#configuration-parameters) below.

#### Manual Configuration

When deploying an Ivy project without using `ivy deploy`, your local .NET user secrets are not automatically transferred. In that case, you can configure Fortnox Auth by setting environment variables or .NET user secrets. See [Configuration Parameters](#configuration-parameters) below.

> **Note:** If configuration is present in both .NET user secrets and environment variables, Ivy will use the values in **.NET user secrets over environment variables**.

For more information, see [Authentication Overview](Overview.md).

#### Configuration Parameters

The following parameters are supported via connection string, environment variables, or .NET user secrets:

- **FORTNOX_CLIENT_ID**: Required. Your application's client ID.
- **FORTNOX_CLIENT_SECRET**: Required. Your application's client secret.

## Authentication Flow

1. User clicks a login button in your application
2. User is redirected to Fortnox with appropriate parameters
3. User authenticates with their Fortnox credentials
4. Fortnox redirects back to your application with authorization code
5. Ivy exchanges the authorization code for access and ID tokens
6. The user is authenticated with their Fortnox identity
7. Your Ivy app can make API requests to Fortnox on behalf of the user

## Security Best Practices

- **Always use HTTPS** in production environments
- **Implement proper logout** to clear sessions
- **Never commit secrets** to source control

## Troubleshooting

### Common Issues

**The client credentials are invalid**
- Verify Client ID and Client Secret are correct and match your Fortnox application

**The redirect URI provided is missing or does not match**
- Verify redirect URIs in Fortnox match your application URLs exactly
- Check for case sensitivity in URLs
- Ensure HTTPS is used in production environments

## Related Documentation

- [Authentication Overview](Overview.md)
- [Auth0 Provider](Auth0.md)
- [Microsoft Entra Provider](MicrosoftEntra.md)
- [Fortnox Developer Documentation](https://www.fortnox.se/developer)