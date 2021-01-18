using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace azstorageaadauth
{
    public class CustomTokenExpiredException : Exception
    {
        public CustomTokenExpiredException(string message) : base(message)
        {
        }
    }
}
