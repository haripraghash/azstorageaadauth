using Azure.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace azstorageaadauth
{
    public class CustomTokenCredential : TokenCredential
    {
        private AccessToken _token;
        private DateTimeOffset expiresOn;

        public CustomTokenCredential(string token, DateTimeOffset expiresOn) : this(new AccessToken(token, expiresOn))
        {
            // We can catch this exception in the calling code, then to determine if the token needs to be renewed
            if(expiresOn < DateTimeOffset.UtcNow)
            {
                throw new CustomTokenExpiredException("Token Expired. Renew it silently");
            }
        }

        public CustomTokenCredential(AccessToken token)
        {
            _token = token;
        }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return _token;
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new ValueTask<AccessToken>(_token);
        }
    }
}
