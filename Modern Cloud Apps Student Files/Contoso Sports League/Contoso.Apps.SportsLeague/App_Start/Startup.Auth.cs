﻿using System;
using Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Threading.Tasks;
using Microsoft.Owin.Security.Notifications;
using Microsoft.IdentityModel.Protocols;
using System.Configuration;
using System.IdentityModel.Tokens;
using System.Web.Helpers;
using System.IdentityModel.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Contoso.Apps.SportsLeague.Web
{
    public partial class Startup
    {
        // App config settings
        private static string clientId =
        ConfigurationManager.AppSettings["ida:ClientId"];
        private static string aadInstance =
        ConfigurationManager.AppSettings["ida:AadInstance"];
        private static string tenant =
        ConfigurationManager.AppSettings["ida:Tenant"];
        private static string redirectUri =
        ConfigurationManager.AppSettings["ida:RedirectUri"];
        // B2C policy identifiers
        public static string SignUpPolicyId =
        ConfigurationManager.AppSettings["ida:SignUpPolicyId"];
        public static string SignInPolicyId =
        ConfigurationManager.AppSettings["ida:SignInPolicyId"];
        public static string ProfilePolicyId =
        ConfigurationManager.AppSettings["ida:UserProfilePolicyId"];
        public void ConfigureAuth(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);
            app.UseCookieAuthentication(new CookieAuthenticationOptions());
            // Configure OpenID Connect middleware for each policy
            app.UseOpenIdConnectAuthentication(CreateOptionsFromPolicy(SignUpPolicyId));
            app.UseOpenIdConnectAuthentication(CreateOptionsFromPolicy(ProfilePolicyId))
            ;
            app.UseOpenIdConnectAuthentication(CreateOptionsFromPolicy(SignInPolicyId));
            AntiForgeryConfig.UniqueClaimTypeIdentifier = ClaimTypes.NameIdentifier;
        }
        // Used for avoiding yellow-screen-of-death
          private OpenIdConnectAuthenticationOptions CreateOptionsFromPolicy(string policy)
        {
            return new OpenIdConnectAuthenticationOptions
            {
                // For each policy, give OWIN the policy-specific metadata address, and
                // set the authentication type to the id of the policy
                MetadataAddress = String.Format(aadInstance, tenant, policy),
                AuthenticationType = policy,
                // These are standard OpenID Connect parameters, with values pulled from web.config
                ClientId = clientId,
                RedirectUri = redirectUri,
                PostLogoutRedirectUri = redirectUri,
                Notifications = new OpenIdConnectAuthenticationNotifications()
                {
                    AuthenticationFailed = (notification) =>
                     {
                         notification.HandleResponse();
                         if (notification.Exception.Message == "access_denied")
                         {
                             notification.Response.Redirect("/");
                         }
                         else
                         {
                             notification.Response.Redirect("/Home/Error?message=" +
                            notification.Exception.Message);
                         }
                         return Task.FromResult(0);
                     }
                },
                Scope = "openid",
                ResponseType = "id_token",
                // This piece is optional - it is used for displaying the user's name in the navigation bar.
                TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name",
                },
            };
        }
    }
}