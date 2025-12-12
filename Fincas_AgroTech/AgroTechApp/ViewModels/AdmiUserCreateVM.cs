using System.ComponentModel.DataAnnotations;

namespace AgroTechApp.Models.ViewModels
{
    /// <summary>
    /// ViewModel para crear un nuevo usuario con Identity
    /// </summary>
    public class AdminUserCreateVM
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        [Display(Name = "Nombre completo")]
        public string Nombre { get; set; } = null!;

        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "La contraseña es requerida")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Debe confirmar la contraseña")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmPassword { get; set; } = null!;

        [Required(ErrorMessage = "Debe seleccionar un rol")]
        [Display(Name = "Rol")]
        public string RoleId { get; set; } = null!;

        [Display(Name = "Fincas asignadas")]
        public List<long> FincasSeleccionadas { get; set; } = new List<long>();

        [Display(Name = "Usuario activo")]
        public bool EmailConfirmed { get; set; } = true;
    }

    /// <summary>
    /// ViewModel para editar un usuario existente
    /// </summary>
    public class AdminUserEditVM
    {
        [Required]
        public string UserId { get; set; } = null!;

        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100)]
        [Display(Name = "Nombre completo")]
        public string Nombre { get; set; } = null!;

        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; } = null!;

        [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        [DataType(DataType.Password)]
        [Display(Name = "Nueva contraseña (dejar vacío para no cambiar)")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar nueva contraseña")]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        public string? ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un rol")]
        [Display(Name = "Rol")]
        public string RoleId { get; set; } = null!;

        [Display(Name = "Fincas asignadas")]
        public List<long> FincasSeleccionadas { get; set; } = new List<long>();

        [Display(Name = "Cuenta confirmada/activa")]
        public bool EmailConfirmed { get; set; }
    }

    /// <summary>
    /// ViewModel para asignar fincas a un usuario
    /// </summary>
    public class AdminAssignFincasVM
    {
        public string UserId { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;

        public List<FincaCheckboxVM> Fincas { get; set; } = new List<FincaCheckboxVM>();
    }

    /// <summary>
    /// ViewModel para checkbox de fincas
    /// </summary>
    public class FincaCheckboxVM
    {
        public long FincaId { get; set; }
        public string Nombre { get; set; } = null!;
        public string? Ubicacion { get; set; }
        public bool Activa { get; set; }
        public bool Seleccionada { get; set; }
    }

    /// <summary>
    /// ViewModel para la lista de usuarios con información completa
    /// </summary>
    public class AdminUserListItemVM
    {
        public string UserId { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public bool EmailConfirmed { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public int CantidadFincas { get; set; }
        public List<string> NombresFincas { get; set; } = new List<string>();
        public bool LockoutEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
    }

    /// <summary>
    /// ViewModel para detalles completos de un usuario
    /// </summary>
    public class AdminUserDetailsVM
    {
        public string UserId { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public bool EmailConfirmed { get; set; }
        public string? PhoneNumber { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public bool LockoutEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public int AccessFailedCount { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public List<FincaInfoVM> Fincas { get; set; } = new List<FincaInfoVM>();
    }

    /// <summary>
    /// ViewModel para información de fincas
    /// </summary>
    public class FincaInfoVM
    {
        public long FincaId { get; set; }
        public string Nombre { get; set; } = null!;
        public string? Ubicacion { get; set; }
        public bool Activa { get; set; }
    }

    /// <summary>
    /// ViewModel para selector de rol (dropdown)
    /// </summary>
    public class RoleSelectItemVM
    {
        public string RoleId { get; set; } = null!;
        public string RoleName { get; set; } = null!;
    }
}