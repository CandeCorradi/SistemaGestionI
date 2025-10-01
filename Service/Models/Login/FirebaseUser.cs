using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.Login
{
    public class FirebaseUser
    {
        public string Uid { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string PhotoUrl { get; set; } = string.Empty;
        public bool EmailVerified { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
