
# Configuring Auth0

## Set Up Auth0

Create a new account with [Auth0](https://auth0.com) and create a new project

Go to the `Settings` tab

Add `https://localhost:<YOUR_PORT_NUMBER>/callback` to `Allowed Callback URLs`

Add `https://localhost:<YOUR_PORT_NUMBER>/` to `Allowed Logout URLs`

## Link to Project

Open `appsettings.json` and add the `Auth0` block replacing `YOUR_AUTH0_DOMAIN` and `YOUR_CLIENT_ID` with the values from your project

``` json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Auth0": { // <- Add this
    "Domain": "YOUR_AUTH0_DOMAIN",
    "ClientId": "YOUR_CLIENT_ID"
  }
}
```

> Be sure to avoid pushing your ClientID!

## Useful Resources

* [Auth0 With Blazor](https://auth0.com/blog/what-is-blazor-tutorial-on-building-webapp-with-authentication/#Securing-the-Application-with-Auth0)
* [OpenID Connect Scopes](https://auth0.com/docs/get-started/apis/scopes/openid-connect-scopes)
