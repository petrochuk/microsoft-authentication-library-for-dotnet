// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.OAuth2.Throttling
{
    /// <summary>
    /// Throttling occurs 
    /// </summary>
    internal class SingletonThrottlingManager : IThrottlingManager
    {
        #region Singleton
        private SingletonThrottlingManager() { }

        private static readonly Lazy<SingletonThrottlingManager> lazyPrivateCtor =
            new Lazy<SingletonThrottlingManager>(() => new SingletonThrottlingManager());

        public static SingletonThrottlingManager Instance { get { return lazyPrivateCtor.Value; } }
        #endregion

        public void RecordException(RequestContext requestContext, MsalServiceException ex)
        {
        }

        public void ThrottleIfNeeded(RequestContext requestContext)
        {
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

    }
}
