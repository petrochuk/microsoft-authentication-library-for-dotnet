﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal.Broker
{
    internal class BrokerSilentRequest
    {
        internal Dictionary<string, string> BrokerPayload { get; set; } = new Dictionary<string, string>();
        internal IBroker Broker { get; }
        private readonly AcquireTokenSilentParameters _silentParameters;
        private readonly AuthenticationRequestParameters _authenticationRequestParameters;
        private readonly IServiceBundle _serviceBundle;
        //private readonly AuthorizationResult _authorizationResult;

        internal BrokerSilentRequest(
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenSilentParameters acquireTokenSilentParameters,
            IServiceBundle serviceBundle,
            //AuthorizationResult authorizationResult,
            IBroker broker)
        {
            _authenticationRequestParameters = authenticationRequestParameters;
            _silentParameters = acquireTokenSilentParameters;
            _serviceBundle = serviceBundle;
            //_authorizationResult = authorizationResult;
            Broker = broker;
        }

        public async Task<MsalTokenResponse> SendTokenRequestToBrokerAsync()
        {
            _authenticationRequestParameters.RequestContext.Logger.Info(LogMessages.CanInvokeBrokerAcquireTokenWithBroker);

            return await SendAndVerifyResponseAsync().ConfigureAwait(false);
        }

        private async Task<MsalTokenResponse> SendAndVerifyResponseAsync()
        {
            CreateRequestParametersForBroker();

            MsalTokenResponse msalTokenResponse =
                await Broker.AcquireTokenUsingBrokerAsync(BrokerPayload).ConfigureAwait(false);

            ValidateResponseFromBroker(msalTokenResponse);
            return msalTokenResponse;
        }

        internal void CreateRequestParametersForBroker()
        {
            BrokerPayload.Clear();
            BrokerPayload.Add(BrokerParameter.Authority, _authenticationRequestParameters.Authority.AuthorityInfo.CanonicalAuthority);
            string scopes = EnumerableExtensions.AsSingleString(_authenticationRequestParameters.Scope);

            BrokerPayload.Add(BrokerParameter.Scope, scopes);
            BrokerPayload.Add(BrokerParameter.ClientId, _authenticationRequestParameters.ClientId);
            BrokerPayload.Add(BrokerParameter.CorrelationId, _authenticationRequestParameters.RequestContext.Logger.CorrelationId.ToString());
            BrokerPayload.Add(BrokerParameter.ClientVersion, MsalIdHelper.GetMsalVersion());
            BrokerPayload.Add(BrokerParameter.RedirectUri, _serviceBundle.Config.RedirectUri);

            string extraQP = string.Join("&", _authenticationRequestParameters.ExtraQueryParameters.Select(x => x.Key + "=" + x.Value));
            BrokerPayload.Add(BrokerParameter.ExtraQp, extraQP);

            BrokerPayload.Add(BrokerParameter.Username, _authenticationRequestParameters.Account?.Username ?? string.Empty);
            BrokerPayload.Add(BrokerParameter.ExtraOidcScopes, BrokerParameter.OidcScopesValue);
            BrokerPayload.Add(BrokerParameter.HomeAccountId, _silentParameters.Account.HomeAccountId.Identifier);
            BrokerPayload.Add(BrokerParameter.LocalAccountId, _silentParameters.Account.HomeAccountId.TenantId);
            BrokerPayload.Add(BrokerParameter.ForceRefresh, _silentParameters.ForceRefresh.ToString(CultureInfo.InvariantCulture));
        }

        internal void ValidateResponseFromBroker(MsalTokenResponse msalTokenResponse)
        {
            _authenticationRequestParameters.RequestContext.Logger.Info(LogMessages.CheckMsalTokenResponseReturnedFromBroker);
            if (msalTokenResponse.AccessToken != null)
            {
                _authenticationRequestParameters.RequestContext.Logger.Info(
                    LogMessages.BrokerResponseContainsAccessToken +
                    msalTokenResponse.AccessToken.Count());
                return;
            }
            else if (msalTokenResponse.Error != null)
            {
                _authenticationRequestParameters.RequestContext.Logger.Info(
                    LogMessages.ErrorReturnedInBrokerResponse(msalTokenResponse.Error));
                throw new MsalServiceException(msalTokenResponse.Error, MsalErrorMessage.BrokerResponseError + msalTokenResponse.ErrorDescription);
            }
            else
            {
                _authenticationRequestParameters.RequestContext.Logger.Info(LogMessages.UnknownErrorReturnedInBrokerResponse);
                throw new MsalServiceException(MsalError.BrokerResponseReturnedError, MsalErrorMessage.BrokerResponseReturnedError, null);
            }
        }
    }
}