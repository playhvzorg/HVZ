# Initial Setup

## Use Gmail

The default configuration is to use `smtp.gmail.com` as the smtp provider, if you are choosing to use a different smtp
provider this section does not apply.

> It is recommended that you create a new gmail account to use for testing instead of using your personal gmail account

You need to create an app password for your gmail account in order to use smtp.

1. Navigate to your account settings
2. Under security, enable 2-step verification
3. After enabling 2-step verification, return to the security tab
4. Select app passwords
5. Select mail
6. Selct your device
7. Hit generate
8. Copy the generated password and save it for later

## Configure Email Service

You need to set the `EmailId` and `Password` fields for `EmailServiceOptions`
with [user secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-7.0%27).

``` bash
dotnet user-secrets set "EmailServiceOptions:EmailId" {your email}
```

> Replace {your email} with the email account you wish to use wrapped in double quotes
>
> ex: `"example@gmail.com"`

``` bash
dotnet user-secrets set "EmailServiceOptions:Password" {your password}
```

> Replace {your password} with the password for the email account wrapped in double quotes
>
> ex: `"abcd1234"`
