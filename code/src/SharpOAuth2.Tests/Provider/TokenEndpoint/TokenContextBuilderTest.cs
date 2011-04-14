﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SharpOAuth2.Provider;
using SharpOAuth2.Provider.TokenEndpoint;
using System.Web;
using Moq;
using System.Collections.Specialized;

namespace SharpOAuth2.Tests.Provider.TokenEndpoint
{
    [TestFixture]
    public class TokenContextBuilderTest
    {

        private NameValueCollection MakeRequestValues(string redirectUrl)
        {
            NameValueCollection nvc = new NameValueCollection();

            nvc[Parameters.ClientId] = "123";
            nvc[Parameters.ClientSecret] = "client-secret";
            nvc[Parameters.ResourceOwnerPassword] = "owner-secret";
            nvc[Parameters.ResourceOwnerUsername] = "456";
            nvc[Parameters.AuthroizationCode] = "auth-code";
            nvc[Parameters.GrantType] = Parameters.GrantTypeValues.AuthorizationCode;
            nvc[Parameters.Scope] = "create delete";
            nvc[Parameters.RefreshToken] = "refresh-token";
            nvc[Parameters.RedirectUri] = redirectUrl;
            return nvc;
        }
        [Test, ExpectedException(typeof(NotSupportedException))]
        public void CreateContextViaUri()
        {
            IContextBuilder<ITokenContext> builder = new TokenContextBuilder();
            builder.FromUri((Uri)null);
        }

        [Test, ExpectedException(typeof(NotSupportedException))]
        public void CreateContextViaUrl()
        {
            IContextBuilder<ITokenContext> builder = new TokenContextBuilder();
            builder.FromUri((string)null);
        }

        [Test, ExpectedException(typeof(OAuthFatalException))]
        public void CreateContextFromGetRequest()
        {
            Mock<HttpRequestBase> mckRequest = new Mock<HttpRequestBase>();
            mckRequest.SetupGet(x => x.HttpMethod).Returns("GET");

            IContextBuilder<ITokenContext> builder = new TokenContextBuilder();
            builder.FromHttpRequest(mckRequest.Object);
        }

        [Test]
        public void CreateContextFromPostRequest()
        {
            Mock<HttpRequestBase> mckRequest = new Mock<HttpRequestBase>();
            mckRequest.SetupGet(x => x.HttpMethod).Returns("POST");
            mckRequest.SetupGet(x => x.Form).Returns(MakeRequestValues("http://www.mysite.com/callback"));

            IContextBuilder<ITokenContext> builder = new TokenContextBuilder();

            ITokenContext context = builder.FromHttpRequest(mckRequest.Object);

            Assert.AreEqual("123", context.Client.ClientId);
            Assert.AreEqual("client-secret", context.Client.ClientSecret);
            Assert.AreEqual("owner-secret", context.Password);
            Assert.AreEqual("456", context.Username);
            Assert.AreEqual("auth-code", context.AuthorizationCode);
            Assert.AreEqual(Parameters.GrantTypeValues.AuthorizationCode, context.GrantType);
            Assert.AreEqual("refresh-token", context.RefreshToken);
            Assert.AreEqual(new Uri("http://www.mysite.com/callback"), context.RedirectUri);

        }
    }
}
