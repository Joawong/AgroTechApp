using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AgroTechApp.Models;
using AgroTechApp.Models.DB;
using AgroTechApp.Services;
using Microsoft.AspNetCore.Authorization;

namespace AgroTechApp.Controllers
{
    [Authorize]
    public class AnimalesController : BaseController
    {
        private readonly IFinanzasService _finanzasService;

        public AnimalesController(
            AgroTechDbContext context,
            IFinanzasService finanzasService,
            ILogger<AnimalesController> logger)
            : base(context, logger)
        {
            _finanzasService = finanzasService;
        }

        // GET: Animales
        public async Task<IActionResult> Index()
        {
            try
            {
                var fincaId = GetFincaId();

                var animals = await _context.Animals
                    .Where(a => a.FincaId == fincaId)
                    .Include(a => a.Finca)
                    .Include(a => a.LoteAnimal)
                    .Include(a => a.Madre)
                    .Include(a => a.Padre)
                    .Include(a => a.Raza)
                    .OrderByDescending(a => a.AnimalId)
                    .ToListAsync();

                return View(animals);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado en AnimalesController.Index");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error obteniendo finca del usuario");
                return ErrorView(ex.Message);
            }
        }

        // GET: Animales/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var fincaId = GetFincaId();

                var animal = await _context.Animals
                    .Where(a => a.FincaId == fincaId && a.AnimalId == id)
                    .Include(a => a.Finca)
                    .Include(a => a.LoteAnimal)
                    .Include(a => a.Madre)
                    .Include(a => a.Padre)
                    .Include(a => a.Raza)
                    .Include(a => a.Pesajes.OrderByDescending(p => p.Fecha))
                    .Include(a => a.Tratamientos)
                        .ThenInclude(t => t.TipoTrat)
                    .Include(a => a.Gastos)
                    .Include(a => a.Ingresos)
                    .FirstOrDefaultAsync();

                if (animal == null)
                {
                    _logger.LogWarning($"Animal {id} no encontrado o no pertenece a finca {fincaId}");
                    return NotFound();
                }

                return View(animal);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // GET: Animales/Create
        public IActionResult Create()
        {
            try
            {
                var fincaId = GetFincaId();

                ViewData["LoteAnimalId"] = new SelectList(
                    _context.LoteAnimals.Where(l => l.FincaId == fincaId),
                    "LoteAnimalId", "Nombre");

                ViewData["RazaId"] = new SelectList(_context.Razas, "RazaId", "Nombre");

                ViewData["MadreId"] = new SelectList(
                    _context.Animals.Where(a => a.FincaId == fincaId && a.Sexo == "H"),
                    "AnimalId", "Arete");

                ViewData["PadreId"] = new SelectList(
                    _context.Animals.Where(a => a.FincaId == fincaId && a.Sexo == "M"),
                    "AnimalId", "Arete");

                return View();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // POST: Animales/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AnimalId,Arete,Nombre,Sexo,RazaId,FechaNacimiento,PesoNacimiento,Estado,MadreId,PadreId,LoteAnimalId,CostoCompra")] Animal animal)
        {
            try
            {
                var fincaId = GetFincaId();
                animal.FincaId = fincaId;

                ModelState.Remove("FincaId");
                ModelState.Remove("Finca");
                ModelState.Remove("Raza");
                ModelState.Remove("Madre");
                ModelState.Remove("Padre");
                ModelState.Remove("LoteAnimal");

                // Validar que padres pertenecen a la misma finca
                if (animal.MadreId.HasValue)
                {
                    var madre = await _context.Animals
                        .FirstOrDefaultAsync(a => a.AnimalId == animal.MadreId && a.FincaId == fincaId);
                    if (madre == null)
                    {
                        ModelState.AddModelError("MadreId", "La madre seleccionada no pertenece a su finca");
                    }
                }

                if (animal.PadreId.HasValue)
                {
                    var padre = await _context.Animals
                        .FirstOrDefaultAsync(a => a.AnimalId == animal.PadreId && a.FincaId == fincaId);
                    if (padre == null)
                    {
                        ModelState.AddModelError("PadreId", "El padre seleccionado no pertenece a su finca");
                    }
                }

                // Validar que lote pertenece a la finca
                if (animal.LoteAnimalId.HasValue)
                {
                    var lote = await _context.LoteAnimals
                        .FirstOrDefaultAsync(l => l.LoteAnimalId == animal.LoteAnimalId && l.FincaId == fincaId);
                    if (lote == null)
                    {
                        ModelState.AddModelError("LoteAnimalId", "El lote seleccionado no pertenece a su finca");
                    }
                }

                if (ModelState.IsValid)
                {
                    using var tx = await _context.Database.BeginTransactionAsync();

                    try
                    {
                        // Crear el animal
                        _context.Add(animal);
                        await _context.SaveChangesAsync();

                        // Si tiene costo de compra, registrar gasto automatico
                        if (animal.CostoCompra.HasValue && animal.CostoCompra.Value > 0)
                        {
                            string nombreAnimal = string.IsNullOrWhiteSpace(animal.Nombre)
                                ? animal.Arete
                                : $"{animal.Arete} - {animal.Nombre}";

                            await _finanzasService.RegistrarGastoCompraAnimal(
                                fincaId: fincaId,
                                animalId: animal.AnimalId,
                                nombreAnimal: nombreAnimal,
                                costoCompra: animal.CostoCompra.Value,
                                fecha: DateTime.UtcNow
                            );

                            _logger.LogInformation(
                                "Gasto automático registrado para compra de animal {AnimalId} por {Costo}",
                                animal.AnimalId, animal.CostoCompra.Value);
                        }

                        await tx.CommitAsync();

                        if (animal.CostoCompra.HasValue && animal.CostoCompra.Value > 0)
                        {
                            MostrarExito($"Animal creado exitosamente. Gasto de compra registrado: ₡{animal.CostoCompra.Value:N2}");
                        }
                        else
                        {
                            MostrarExito("Animal creado exitosamente");
                        }

                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        await tx.RollbackAsync();
                        _logger.LogError(ex, "Error al crear animal con gasto automático");
                        ModelState.AddModelError(string.Empty, "Ocurrió un error al crear el animal.");
                    }
                }

                // Recargar dropdowns si hay error
                ViewData["LoteAnimalId"] = new SelectList(
                    _context.LoteAnimals.Where(l => l.FincaId == fincaId),
                    "LoteAnimalId", "Nombre", animal.LoteAnimalId);

                ViewData["RazaId"] = new SelectList(_context.Razas, "RazaId", "Nombre", animal.RazaId);

                ViewData["MadreId"] = new SelectList(
                    _context.Animals.Where(a => a.FincaId == fincaId && a.Sexo == "H"),
                    "AnimalId", "Arete", animal.MadreId);

                ViewData["PadreId"] = new SelectList(
                    _context.Animals.Where(a => a.FincaId == fincaId && a.Sexo == "M"),
                    "AnimalId", "Arete", animal.PadreId);

                return View(animal);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // GET: Animales/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var fincaId = GetFincaId();

                var animal = await _context.Animals
                    .Where(a => a.FincaId == fincaId && a.AnimalId == id)
                    .FirstOrDefaultAsync();

                if (animal == null)
                {
                    _logger.LogWarning($"Animal {id} no encontrado o no pertenece a finca {fincaId}");
                    return NotFound();
                }

                ViewData["LoteAnimalId"] = new SelectList(
                    _context.LoteAnimals.Where(l => l.FincaId == fincaId),
                    "LoteAnimalId", "Nombre", animal.LoteAnimalId);

                ViewData["RazaId"] = new SelectList(_context.Razas, "RazaId", "Nombre", animal.RazaId);

                ViewData["MadreId"] = new SelectList(
                    _context.Animals.Where(a => a.FincaId == fincaId && a.Sexo == "H"),
                    "AnimalId", "Arete", animal.MadreId);

                ViewData["PadreId"] = new SelectList(
                    _context.Animals.Where(a => a.FincaId == fincaId && a.Sexo == "M"),
                    "AnimalId", "Arete", animal.PadreId);

                return View(animal);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // POST: Animales/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("AnimalId,FincaId,Arete,Nombre,Sexo,RazaId,FechaNacimiento,PesoNacimiento,Estado,MadreId,PadreId,LoteAnimalId,CostoCompra")] Animal animal)
        {
            if (id != animal.AnimalId)
            {
                return NotFound();
            }

            try
            {
                var fincaId = GetFincaId();

                ModelState.Remove("FincaId");
                ModelState.Remove("Finca");
                ModelState.Remove("Raza");
                ModelState.Remove("Madre");
                ModelState.Remove("Padre");
                ModelState.Remove("LoteAnimal");

                // Validar que el animal pertenece a la finca del usuario
                ValidarAcceso(animal.FincaId);

                if (ModelState.IsValid)
                {
                    try
                    {
                        _context.Update(animal);
                        await _context.SaveChangesAsync();
                        MostrarExito("Animal actualizado exitosamente");
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!AnimalExists(animal.AnimalId, fincaId))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                    return RedirectToAction(nameof(Index));
                }

                ViewData["LoteAnimalId"] = new SelectList(
                    _context.LoteAnimals.Where(l => l.FincaId == fincaId),
                    "LoteAnimalId", "Nombre", animal.LoteAnimalId);

                ViewData["RazaId"] = new SelectList(_context.Razas, "RazaId", "Nombre", animal.RazaId);

                ViewData["MadreId"] = new SelectList(
                    _context.Animals.Where(a => a.FincaId == fincaId && a.Sexo == "H"),
                    "AnimalId", "Arete", animal.MadreId);

                ViewData["PadreId"] = new SelectList(
                    _context.Animals.Where(a => a.FincaId == fincaId && a.Sexo == "M"),
                    "AnimalId", "Arete", animal.PadreId);

                return View(animal);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ============================================================
        // GET: Animales/Vender/5
        // ============================================================
        public async Task<IActionResult> Vender(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var fincaId = GetFincaId();

                var animal = await _context.Animals
                    .Where(a => a.FincaId == fincaId && a.AnimalId == id)
                    .Include(a => a.Raza)
                    .Include(a => a.Pesajes.OrderByDescending(p => p.Fecha))
                    .FirstOrDefaultAsync();

                if (animal == null)
                {
                    return NotFound();
                }

                // Validar que el animal este activo
                if (animal.Estado != "Activo")
                {
                    MostrarError($"No se puede vender un animal con estado '{animal.Estado}'");
                    return RedirectToAction(nameof(Index));
                }

                // Obtener ultimo peso
                var ultimoPeso = animal.Pesajes.FirstOrDefault()?.PesoKg;

                ViewBag.UltimoPeso = ultimoPeso;
                ViewBag.NombreAnimal = string.IsNullOrWhiteSpace(animal.Nombre)
                    ? animal.Arete
                    : $"{animal.Arete} - {animal.Nombre}";
                ViewBag.CostoCompra = animal.CostoCompra;

                return View(animal);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // ============================================================
        // POST: Animales/Vender/5
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Vender(long id, decimal PrecioVenta, DateTime? FechaVenta, decimal? PesoVenta, string? Observacion)
        {
            try
            {
                var fincaId = GetFincaId();

                var animal = await _context.Animals
                    .Where(a => a.FincaId == fincaId && a.AnimalId == id)
                    .FirstOrDefaultAsync();

                if (animal == null)
                {
                    return NotFound();
                }

                // Validar que el animal este activo
                if (animal.Estado != "Activo")
                {
                    MostrarError($"No se puede vender un animal con estado '{animal.Estado}'");
                    return RedirectToAction(nameof(Index));
                }

                // Validar precio de venta
                if (PrecioVenta <= 0)
                {
                    ModelState.AddModelError("PrecioVenta", "El precio de venta debe ser mayor a cero");
                    return View(animal);
                }

                using var tx = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Actualizar el animal
                    animal.Estado = "Vendido";
                    animal.PrecioVenta = PrecioVenta;
                    animal.FechaVenta = DateOnly.FromDateTime(FechaVenta ?? DateTime.UtcNow);

                    _context.Update(animal);
                    await _context.SaveChangesAsync();

                    // Registrar ingreso automatico
                    string nombreAnimal = string.IsNullOrWhiteSpace(animal.Nombre)
                        ? animal.Arete
                        : $"{animal.Arete} - {animal.Nombre}";

                    await _finanzasService.RegistrarIngresoVentaAnimal(
                        fincaId: fincaId,
                        animalId: animal.AnimalId,
                        nombreAnimal: nombreAnimal,
                        precioVenta: PrecioVenta,
                        fecha: FechaVenta ?? DateTime.UtcNow,
                        pesoVenta: PesoVenta
                    );

                    await tx.CommitAsync();

                    _logger.LogInformation(
                        "Animal {AnimalId} vendido por {Precio}. Ingreso automático registrado.",
                        animal.AnimalId, PrecioVenta);

                    // Calcular rentabilidad si tiene costo
                    if (animal.CostoCompra.HasValue)
                    {
                        var ganancia = PrecioVenta - animal.CostoCompra.Value;
                        var roi = (ganancia / animal.CostoCompra.Value) * 100;

                        MostrarExito($"Animal vendido exitosamente por ₡{PrecioVenta:N2}. " +
                                   $"Ganancia: ₡{ganancia:N2} (ROI: {roi:N1}%)");
                    }
                    else
                    {
                        MostrarExito($"Animal vendido exitosamente por ₡{PrecioVenta:N2}. Ingreso registrado.");
                    }

                    return RedirectToAction(nameof(Details), new { id = animal.AnimalId });
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    _logger.LogError(ex, "Error al vender animal {AnimalId}", id);
                    ModelState.AddModelError(string.Empty, "Ocurrió un error al registrar la venta.");
                    return View(animal);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // GET: Animales/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var fincaId = GetFincaId();

                var animal = await _context.Animals
                    .Where(a => a.FincaId == fincaId && a.AnimalId == id)
                    .Include(a => a.Finca)
                    .Include(a => a.LoteAnimal)
                    .Include(a => a.Madre)
                    .Include(a => a.Padre)
                    .Include(a => a.Raza)
                    .FirstOrDefaultAsync();

                if (animal == null)
                {
                    return NotFound();
                }

                // Advertencia si esta vendido
                if (animal.Estado == "Vendido")
                {
                    ViewBag.AdvertenciaVendido = true;
                }

                return View(animal);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        // POST: Animales/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            try
            {
                var fincaId = GetFincaId();

                var animal = await _context.Animals
                    .Where(a => a.FincaId == fincaId && a.AnimalId == id)
                    .FirstOrDefaultAsync();

                if (animal == null)
                {
                    return NotFound();
                }

                // no permitir eliminar animales vendidos
                if (animal.Estado == "Vendido")
                {
                    MostrarError("No se puede eliminar un animal vendido. Si es necesario, cambie su estado primero.");
                    return RedirectToAction(nameof(Index));
                }

                using var tx = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Eliminar gasto de compra si existe
                    if (animal.CostoCompra.HasValue)
                    {
                        await _finanzasService.EliminarGastoDeCompraAnimal(id);
                    }

                    // Eliminar el animal
                    _context.Animals.Remove(animal);
                    await _context.SaveChangesAsync();

                    await tx.CommitAsync();

                    MostrarExito("Animal eliminado correctamente");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    _logger.LogError(ex, "Error al eliminar animal {AnimalId}", id);
                    MostrarError("Ocurrió un error al eliminar el animal.");
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }

        private bool AnimalExists(long id, long fincaId)
        {
            return _context.Animals.Any(e => e.AnimalId == id && e.FincaId == fincaId);
        }
    }
}