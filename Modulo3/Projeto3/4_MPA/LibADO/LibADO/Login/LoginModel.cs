using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibADO.Login;

namespace LibADO.Login
{
    public class LoginModel
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Senha { get; set; }
        public string Nome { get; set; }
        public string UserRole { get; set; }
        public string? Status { get; set; }
    }
}
