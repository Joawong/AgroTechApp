using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgroTechApp.Models.DB
{
    public class UserFinca
    {
        public long UserFincaId { get; set; }        
        public long FincaId { get; set; }
        public string AspNetUserId { get; set; } = null!; // Relación a IdentityUser

        public virtual Finca Finca { get; set; } = null!;
    }

}
