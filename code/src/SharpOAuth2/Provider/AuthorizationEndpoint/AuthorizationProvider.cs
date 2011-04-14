﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.ServiceLocation;
using SharpOAuth2.Provider.AuthorizationEndpoint.Inspectors;
using SharpOAuth2.Globalization;
using System.Globalization;
using SharpOAuth2.Provider.Services;

namespace SharpOAuth2.Provider.AuthorizationEndpoint
{
    public class AuthorizationProvider : IAuthorizationProvider
    {
        readonly IAuthorizationServiceFactory ServiceFactory;

        public AuthorizationProvider(IAuthorizationServiceFactory serviceFactory)
        {
            ServiceFactory = serviceFactory;
        }
        private void InspectRequest(IAuthorizationContext context)
        {
            IEnumerable<IContextInspector<IAuthorizationContext>> inspectors = ServiceLocator.Current.GetAllInstances<IContextInspector<IAuthorizationContext>>();

            foreach (IContextInspector<IAuthorizationContext> inspector in inspectors)
                inspector.Inspect(context);
        }
        private void AssertResourceOwnerIdIsNotBlank(IAuthorizationContext context)
        {
            if (string.IsNullOrWhiteSpace(context.ResourceOwnerId))
                throw new OAuthFatalException(AuthorizationEndpointResources.ResourceOwnerNotIncluded);
        }
        private void AssertNoAuthorizationToken(IAuthorizationContext context)
        {
            if (context.Authorization != null)
                throw new OAuthFatalException(AuthorizationEndpointResources.AuthorizationContextContainsToken);
        }

        private void AssertIsClient(IAuthorizationContext context)
        {
            if (!ServiceFactory.ClientService.IsClient(context))
                throw Errors.UnauthorizedClient(context, context.Client);
        }

        private void AssertRedirectUriIsValid(IAuthorizationContext context)
        {
            if (!ServiceFactory.ClientService.ValidateRedirectUri(context))
                throw new OAuthFatalException(string.Format(CultureInfo.CurrentUICulture,
                    AuthorizationEndpointResources.InvalidRedirectUri, context.RedirectUri.ToString()));
        }
        #region IAuthorizationProvider Members

        public void CreateAuthorizationGrant(IAuthorizationContext context)
        {
            try
            {
                InspectRequest(context);
                AssertNoAuthorizationToken(context);
                AssertIsClient(context);
                AssertRedirectUriIsValid(context);
                AssertResourceOwnerIdIsNotBlank(context);
                
                if (context.ResponseType == Parameters.ResponseTypeValues.AccessToken)
                    throw Errors.UnsupportedResponseType(context, context.ResponseType);
                else
                {
                    AuthorizationGrantBase grant = ServiceFactory.TokenService.MakeAuthorizationGrant(context);
                    ServiceFactory.TokenService.ApproveAuthorizationGrant(grant, context.IsApproved);
                    context.Authorization = grant;
                }

                if (!context.IsApproved)
                    throw Errors.AccessDenied(context);
            }
            catch (OAuthErrorResponseException<IAuthorizationContext> ex)
            {
                context.Error = new ErrorResponse
                {
                    Error = ex.Error,
                    ErrorDescription = ex.Message,
                    ErrorUri = ex.ErrorUri
                };
            }
        }

        
        #endregion
    }
}
