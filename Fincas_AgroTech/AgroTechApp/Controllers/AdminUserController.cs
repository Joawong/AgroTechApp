using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AgroTechApp.Data;
using AgroTechApp.Models.DB;
using AgroTechApp.Models.ViewModels;

namespace AgroTechApp.Controllers
{
    [Authorize(Roles = "Admin")] // Solo administradores
    public class AdminUsersController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AgroTechDbContext _agroContext;
        private readonly ILogger<AdminUsersController> _logger;

        public AdminUsersController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            AgroTechDbContext agroContext,
            ILogger<AdminUsersController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _agroContext = agroContext;
            _logger = logger;
        }

        // ============================================================================
        // GET: AdminUsers/Index - Lista de todos los usuarios
        // ============================================================================
        public async Task<IActionResult> Index()
        {
            try
            {
                var users = await _userManager.Users.ToListAsync();
                var userViewModels = new List<AdminUserListItemVM>();

                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);

                    // Obtener fincas asignadas
                    var fincas = await _agroContext.UserFincas
                        .Where(uf => uf.AspNetUserId == user.Id)
                        .Include(uf => uf.Finca)
                        .Select(uf => uf.Finca.Nombre)
                        .ToListAsync();

                    userViewModels.Add(new AdminUserListItemVM
                    {
                        UserId = user.Id,
                        UserName = user.UserName ?? "",
                        Email = user.Email ?? "",
                        EmailConfirmed = user.EmailConfirmed,
                        Roles = roles.ToList(),
                        CantidadFincas = fincas.Count,
                        NombresFincas = fincas,
                        LockoutEnabled = user.LockoutEnabled,
                        LockoutEnd = user.LockoutEnd
                    });
                }

                // Estadísticas para el dashboard
                ViewBag.TotalUsuarios = userViewModels.Count;
                ViewBag.UsuariosActivos = userViewModels.Count(u => u.EmailConfirmed);
                ViewBag.UsuariosInactivos = userViewModels.Count(u => !u.EmailConfirmed);
                ViewBag.TotalRoles = await _roleManager.Roles.CountAsync();

                return View(userViewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener la lista de usuarios");
                TempData["Error"] = "Error al cargar la lista de usuarios";
                return View(new List<AdminUserListItemVM>());
            }
        }

        // ============================================================================
        // GET: AdminUsers/Details/id - Ver detalles de un usuario
        // ============================================================================
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                    return NotFound();

                var roles = await _userManager.GetRolesAsync(user);

                // Obtener fincas asignadas con detalles completos
                var fincas = await _agroContext.UserFincas
                    .Where(uf => uf.AspNetUserId == user.Id)
                    .Include(uf => uf.Finca)
                    .Select(uf => new FincaInfoVM
                    {
                        FincaId = uf.Finca.FincaId,
                        Nombre = uf.Finca.Nombre,
                        Ubicacion = uf.Finca.Ubicacion,
                        Activa = uf.Finca.Activa
                    })
                    .ToListAsync();

                var vm = new AdminUserDetailsVM
                {
                    UserId = user.Id,
                    UserName = user.UserName ?? "",
                    Email = user.Email ?? "",
                    EmailConfirmed = user.EmailConfirmed,
                    PhoneNumber = user.PhoneNumber,
                    PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    LockoutEnabled = user.LockoutEnabled,
                    LockoutEnd = user.LockoutEnd,
                    AccessFailedCount = user.AccessFailedCount,
                    Roles = roles.ToList(),
                    Fincas = fincas
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener detalles del usuario {UserId}", id);
                TempData["Error"] = "Error al cargar los detalles del usuario";
                return RedirectToAction(nameof(Index));
            }
        }

        // ============================================================================
        // GET: AdminUsers/Create - Formulario para crear usuario
        // ============================================================================
        public async Task<IActionResult> Create()
        {
            try
            {
                await LoadSelectLists();
                return View(new AdminUserCreateVM());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar formulario de creación");
                TempData["Error"] = "Error al cargar el formulario";
                return RedirectToAction(nameof(Index));
            }
        }

        // ============================================================================
        // POST: AdminUsers/Create - Crear nuevo usuario
        // ============================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminUserCreateVM vm)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Crear usuario Identity
                    var user = new IdentityUser
                    {
                        UserName = vm.Email,
                        Email = vm.Email,
                        EmailConfirmed = vm.EmailConfirmed
                    };

                    var result = await _userManager.CreateAsync(user, vm.Password);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Usuario {Email} creado exitosamente", user.Email);

                        // Asignar rol
                        var role = await _roleManager.FindByIdAsync(vm.RoleId);
                        if (role != null)
                        {
                            await _userManager.AddToRoleAsync(user, role.Name!);
                            _logger.LogInformation("Rol {RoleName} asignado a {Email}", role.Name, user.Email);
                        }

                        // Asignar fincas
                        if (vm.FincasSeleccionadas != null && vm.FincasSeleccionadas.Any())
                        {
                            foreach (var fincaId in vm.FincasSeleccionadas)
                            {
                                var userFinca = new UserFinca
                                {
                                    AspNetUserId = user.Id,
                                    FincaId = fincaId,
                                    UserFincaId = 0 
                                };
                                _agroContext.UserFincas.Add(userFinca);
                            }
                            await _agroContext.SaveChangesAsync();
                            _logger.LogInformation("Fincas asignadas a {Email}", user.Email);
                        }

                        TempData["Success"] = $"Usuario {user.Email} creado exitosamente";
                        return RedirectToAction(nameof(Index));
                    }

                    // Agregar errores de Identity al ModelState
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }

                await LoadSelectLists(vm.RoleId, vm.FincasSeleccionadas);
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear usuario");
                ModelState.AddModelError(string.Empty, "Error al crear el usuario");
                await LoadSelectLists(vm.RoleId, vm.FincasSeleccionadas);
                return View(vm);
            }
        }

        // ============================================================================
        // GET: AdminUsers/Edit/id - Formulario para editar usuario
        // ============================================================================
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                    return NotFound();

                var roles = await _userManager.GetRolesAsync(user);
                var roleName = roles.FirstOrDefault();
                var role = roleName != null ? await _roleManager.FindByNameAsync(roleName) : null;

                // Obtener fincas asignadas
                var fincasIds = await _agroContext.UserFincas
                    .Where(uf => uf.AspNetUserId == user.Id)
                    .Select(uf => uf.FincaId)
                    .ToListAsync();

                var vm = new AdminUserEditVM
                {
                    UserId = user.Id,
                    Nombre = user.UserName ?? "",
                    Email = user.Email ?? "",
                    RoleId = role?.Id ?? "",
                    EmailConfirmed = user.EmailConfirmed,
                    FincasSeleccionadas = fincasIds
                };

                await LoadSelectLists(vm.RoleId, vm.FincasSeleccionadas);
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar formulario de edición para usuario {UserId}", id);
                TempData["Error"] = "Error al cargar el formulario de edición";
                return RedirectToAction(nameof(Index));
            }
        }

        // ============================================================================
        // POST: AdminUsers/Edit/id - Actualizar usuario
        // ============================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, AdminUserEditVM vm)
        {
            if (id != vm.UserId)
                return NotFound();

            try
            {
                if (ModelState.IsValid)
                {
                    var user = await _userManager.FindByIdAsync(id);
                    if (user == null)
                        return NotFound();

                    // Actualizar datos básicos
                    user.UserName = vm.Email;
                    user.Email = vm.Email;
                    user.EmailConfirmed = vm.EmailConfirmed;

                    var result = await _userManager.UpdateAsync(user);

                    if (result.Succeeded)
                    {
                        // Actualizar contraseña si se proporcionó una nueva
                        if (!string.IsNullOrWhiteSpace(vm.Password))
                        {
                            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                            await _userManager.ResetPasswordAsync(user, token, vm.Password);
                            _logger.LogInformation("Contraseña actualizada para {Email}", user.Email);
                        }

                        // Actualizar rol
                        var currentRoles = await _userManager.GetRolesAsync(user);
                        await _userManager.RemoveFromRolesAsync(user, currentRoles);

                        var newRole = await _roleManager.FindByIdAsync(vm.RoleId);
                        if (newRole != null)
                        {
                            await _userManager.AddToRoleAsync(user, newRole.Name!);
                            _logger.LogInformation("Rol actualizado a {RoleName} para {Email}", newRole.Name, user.Email);
                        }

                        // Actualizar fincas asignadas
                        var existingFincas = _agroContext.UserFincas.Where(uf => uf.AspNetUserId == user.Id);
                        _agroContext.UserFincas.RemoveRange(existingFincas);

                        if (vm.FincasSeleccionadas != null && vm.FincasSeleccionadas.Any())
                        {
                            foreach (var fincaId in vm.FincasSeleccionadas)
                            {
                                var userFinca = new UserFinca
                                {
                                    AspNetUserId = user.Id,
                                    FincaId = fincaId,
                                    UserFincaId = 0
                                };
                                _agroContext.UserFincas.Add(userFinca);
                            }
                        }

                        await _agroContext.SaveChangesAsync();

                        _logger.LogInformation("Usuario {Email} actualizado exitosamente", user.Email);
                        TempData["Success"] = $"Usuario {user.Email} actualizado exitosamente";
                        return RedirectToAction(nameof(Index));
                    }

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }

                await LoadSelectLists(vm.RoleId, vm.FincasSeleccionadas);
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar usuario {UserId}", id);
                ModelState.AddModelError(string.Empty, "Error al actualizar el usuario");
                await LoadSelectLists(vm.RoleId, vm.FincasSeleccionadas);
                return View(vm);
            }
        }

        // ============================================================================
        // GET: AdminUsers/AssignFincas/id - Formulario para asignar fincas
        // ============================================================================
        public async Task<IActionResult> AssignFincas(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                    return NotFound();

                var todasLasFincas = await _agroContext.Fincas
                    .OrderBy(f => f.Nombre)
                    .ToListAsync();

                var fincasAsignadas = await _agroContext.UserFincas
                    .Where(uf => uf.AspNetUserId == user.Id)
                    .Select(uf => uf.FincaId)
                    .ToListAsync();

                var vm = new AdminAssignFincasVM
                {
                    UserId = user.Id,
                    UserName = user.UserName ?? "",
                    Email = user.Email ?? "",
                    Fincas = todasLasFincas.Select(f => new FincaCheckboxVM
                    {
                        FincaId = f.FincaId,
                        Nombre = f.Nombre,
                        Ubicacion = f.Ubicacion,
                        Activa = f.Activa,
                        Seleccionada = fincasAsignadas.Contains(f.FincaId)
                    }).ToList()
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar formulario de asignación de fincas para usuario {UserId}", id);
                TempData["Error"] = "Error al cargar el formulario";
                return RedirectToAction(nameof(Index));
            }
        }

        // ============================================================================
        // POST: AdminUsers/AssignFincas/id - Guardar fincas asignadas
        // ============================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignFincas(AdminAssignFincasVM vm)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(vm.UserId);
                if (user == null)
                    return NotFound();

                // Limpiar fincas actuales
                var existingFincas = _agroContext.UserFincas.Where(uf => uf.AspNetUserId == user.Id);
                _agroContext.UserFincas.RemoveRange(existingFincas);

                // Asignar fincas seleccionadas
                var fincasSeleccionadas = vm.Fincas
                    .Where(f => f.Seleccionada)
                    .Select(f => f.FincaId)
                    .ToList();

                foreach (var fincaId in fincasSeleccionadas)
                {
                    var userFinca = new UserFinca
                    {
                        AspNetUserId = user.Id,
                        FincaId = fincaId,
                        UserFincaId = 0
                    };
                    _agroContext.UserFincas.Add(userFinca);
                }

                await _agroContext.SaveChangesAsync();

                _logger.LogInformation("Fincas asignadas exitosamente al usuario {Email}", user.Email);
                TempData["Success"] = $"Fincas asignadas exitosamente a {user.Email}";
                return RedirectToAction(nameof(Details), new { id = user.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al asignar fincas al usuario {UserId}", vm.UserId);
                TempData["Error"] = "Error al asignar las fincas";
                return View(vm);
            }
        }

        // ============================================================================
        // GET: AdminUsers/Delete/id - Confirmación de eliminación
        // ============================================================================
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                    return NotFound();

                var roles = await _userManager.GetRolesAsync(user);
                var fincas = await _agroContext.UserFincas
                    .Where(uf => uf.AspNetUserId == user.Id)
                    .Include(uf => uf.Finca)
                    .Select(uf => new FincaInfoVM
                    {
                        FincaId = uf.Finca.FincaId,
                        Nombre = uf.Finca.Nombre,
                        Ubicacion = uf.Finca.Ubicacion,
                        Activa = uf.Finca.Activa
                    })
                    .ToListAsync();

                var vm = new AdminUserDetailsVM
                {
                    UserId = user.Id,
                    UserName = user.UserName ?? "",
                    Email = user.Email ?? "",
                    EmailConfirmed = user.EmailConfirmed,
                    Roles = roles.ToList(),
                    Fincas = fincas
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar confirmación de eliminación para usuario {UserId}", id);
                TempData["Error"] = "Error al cargar la información del usuario";
                return RedirectToAction(nameof(Index));
            }
        }

        // ============================================================================
        // POST: AdminUsers/Delete/id - Eliminar usuario
        // ============================================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                    return NotFound();

                var email = user.Email;

                // Eliminar relaciones de fincas
                var userFincas = _agroContext.UserFincas.Where(uf => uf.AspNetUserId == user.Id);
                _agroContext.UserFincas.RemoveRange(userFincas);
                await _agroContext.SaveChangesAsync();

                // Eliminar usuario de Identity
                var result = await _userManager.DeleteAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Usuario {Email} eliminado exitosamente", email);
                    TempData["Success"] = $"Usuario {email} eliminado exitosamente";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    TempData["Error"] = error.Description;
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar usuario {UserId}", id);
                TempData["Error"] = "Error al eliminar el usuario";
                return RedirectToAction(nameof(Index));
            }
        }

        // ============================================================================
        // METODOS AUXILIARES PRIVADOS
        // ============================================================================

        /// <summary>
        /// Cargar SelectLists para el formulario
        /// </summary>
        private async Task LoadSelectLists(string? selectedRoleId = null, List<long>? selectedFincas = null)
        {
            // Roles de Identity
            var roles = await _roleManager.Roles
                .OrderBy(r => r.Name)
                .ToListAsync();

            ViewBag.Roles = new SelectList(roles, "Id", "Name", selectedRoleId);

            // Fincas activas
            var fincas = await _agroContext.Fincas
                .Where(f => f.Activa)
                .OrderBy(f => f.Nombre)
                .Select(f => new
                {
                    f.FincaId,
                    DisplayName = f.Nombre + (f.Ubicacion != null ? $" ({f.Ubicacion})" : "")
                })
                .ToListAsync();

            ViewBag.Fincas = fincas.Select(f => new SelectListItem
            {
                Value = f.FincaId.ToString(),
                Text = f.DisplayName,
                Selected = selectedFincas != null && selectedFincas.Contains(f.FincaId)
            }).ToList();
        }
    }
}