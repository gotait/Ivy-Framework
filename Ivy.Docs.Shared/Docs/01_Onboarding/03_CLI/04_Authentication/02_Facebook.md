# Facebook Authentication Provider

<Ingress>
Allow users to sign in with Facebook and access the Facebook Graph API on behalf of the user.
</Ingress>

## Getting Your Facebook App Configuration

Before using Facebook with Ivy, you'll need to register an app.

### Step 1: Register Your App

1. **Register** as a [Facebook Developer](https://developers.facebook.com/async/registration)
2. **Navigate** to your [Facebook Apps](https://developers.facebook.com/apps/)
3. **Create** an app and fill all required fields. *About use cases*: If you only need login you can choose `Create an app without a use case`. If you need additional API access choose your use case.

### Step 2: Get Your Configuration Values

Go to **App Settings > Basic** and copy the following values:

- **App ID**
- **App secret**

## Adding Authentication

To set up Facebook Authentication with Ivy, run the following command and choose `Facebook` when asked to select an auth provider:

```terminal
>ivy auth add
```

You will be prompted to provide the following Facebook configuration:

- **App ID**
- **App secret**

Your credentials will be stored securely in .NET user secrets. Ivy then finishes configuring your application automatically:

1. Adds the `Ivy.Auth.Facebook` package to your project.
2. Adds `server.UseAuth<FacebookAuthProvider>(c => c.UseFacebook());` to your `Program.cs`.
3. Adds `Ivy.Auth.Facebook` to your global usings.

### Advanced Configuration

#### Connection Strings

To skip the interactive prompts, you can provide configuration via a connection string:

```terminal
>ivy auth add --provider Facebook --connection-string "FACEBOOK_APP_ID=your-app-id;FACEBOOK_APP_SECRET=your-app-secret"
```

For a list of connection string parameters, see [Configuration Parameters](#configuration-parameters) below.

#### Manual Configuration

When deploying an Ivy project without using `ivy deploy`, your local .NET user secrets are not automatically transferred. In that case, you can configure Facebook Auth by setting environment variables or .NET user secrets. See [Configuration Parameters](#configuration-parameters) below.

> **Note:** If configuration is present in both .NET user secrets and environment variables, Ivy will use the values in **.NET user secrets over environment variables**.

For more information, see [Authentication Overview](Overview.md).

#### Configuration Parameters

The following parameters are supported via connection string, environment variables, or .NET user secrets:

- **Facebook:AppId**: Required. Your application's app ID.
- **Facebook:AppSecret**: Required. Your application's app secret.

## Authentication Flow

1. User clicks a login button in your application
2. User is redirected to Facebook with appropriate parameters
3. User authenticates with their Facebook credentials
4. Facebook redirects back to your application with authorization code
5. Ivy exchanges the authorization code for an access token
6. The user is authenticated with their Facebook identity
7. Your Ivy app can make API requests to the Facebook Graph API on behalf of the user

## Security Best Practices

- **Always use HTTPS** in production environments
- **Implement proper logout** to clear sessions
- **Never commit secrets** to source control

## Troubleshooting

## Related Documentation

- [Authentication Overview](Overview.md)
- [Auth0 Provider](Auth0.md)
- [Microsoft Entra Provider](MicrosoftEntra.md)
- [Facebook Developer Documentation](https://developers.facebook.com/docs/facebook-login)