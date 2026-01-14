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
using AgroTechApp.ViewModels;


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
        public async Task<IActionResult> Index(int? pagina, string? buscar, string? sexo, string? estado, int? razaId)
        {
            try
            {
                var fincaId = GetFincaId();

                // Query base
                var query = _context.Animals
                    .Where(a => a.FincaId == fincaId)
                    .Include(a => a.Finca)
                    .Include(a => a.LoteAnimal)
                    .Include(a => a.Madre)
                    .Include(a => a.Padre)
                    .Include(a => a.Raza)
                    .AsQueryable();

                // Filtros
                if (!string.IsNullOrWhiteSpace(buscar))
                {
                    query = query.Where(a =>
                        a.Arete.Contains(buscar) ||
                        (a.Nombre != null && a.Nombre.Contains(buscar)));
                }

                if (!string.IsNullOrWhiteSpace(sexo))
                {
                    query = query.Where(a => a.Sexo == sexo);
                }

                if (!string.IsNullOrWhiteSpace(estado))
                {
                    query = query.Where(a => a.Estado == estado);
                }

                if (razaId.HasValue)
                {
                    query = query.Where(a => a.RazaId == razaId.Value);
                }

                // Contar total antes de paginar
                var totalRegistros = await query.CountAsync();

                // Más recientes primero (por ID descendente)
                query = query
                    .OrderBy(a => a.Estado == "Activo" ? 0 :
                                  a.Estado == "Vendido" ? 1 : 2)
                    .ThenByDescending(a => a.AnimalId);

                // PAGINACIÓN
                int registrosPorPagina = 10;
                int paginaActual = pagina ?? 1;
                int totalPaginas = (int)Math.Ceiling(totalRegistros / (double)registrosPorPagina);

                var animales = await query
                    .Skip((paginaActual - 1) * registrosPorPagina)
                    .Take(registrosPorPagina)
                    .ToListAsync();

                // ViewBags para la vista
                ViewBag.PaginaActual = paginaActual;
                ViewBag.TotalPaginas = totalPaginas;
                ViewBag.TotalRegistros = totalRegistros;
                ViewBag.BuscarTexto = buscar;
                ViewBag.SexoFiltro = sexo;
                ViewBag.EstadoFiltro = estado;
                ViewBag.RazaIdFiltro = razaId;

                // Dropdown de razas para filtro
                ViewBag.RazaId = new SelectList(_context.Razas, "RazaId", "Nombre");

                return View(animales);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado en AnimalesController.Index");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
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
                    _context.Animals
                        .Where(a => a.FincaId == fincaId &&
                               a.Sexo == "H" &&
                               a.Estado == "Activo")  
                        .OrderBy(a => a.Arete)
                        .ToList(),
                    "AnimalId", "Arete");

                ViewData["PadreId"] = new SelectList(
                    _context.Animals
                        .Where(a => a.FincaId == fincaId &&
                               a.Sexo == "M" &&
                               a.Estado == "Activo") 
                        .OrderBy(a => a.Arete)
                        .ToList(),
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

                // ✅ VALIDACIÓN: Validar que padres pertenecen a la misma finca
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

                // ✅ VALIDACIÓN: Validar que lote pertenece a la finca
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
                        // 1️⃣ CREAR EL ANIMAL
                        _context.Add(animal);
                        await _context.SaveChangesAsync(); // ✅ Guardar para obtener AnimalId

                        _logger.LogInformation(
                            "Animal creado: {AnimalId} - {Arete} (FincaId: {FincaId})",
                            animal.AnimalId, animal.Arete, fincaId);

                        // 2️⃣ REGISTRAR PESAJE INICIAL (si tiene PesoNacimiento)
                        if (animal.PesoNacimiento.HasValue && animal.PesoNacimiento.Value > 0)
                        {
                            var pesajeInicial = new Pesaje
                            {
                                AnimalId = animal.AnimalId,
                                Fecha = animal.FechaNacimiento.HasValue
                                    ? animal.FechaNacimiento.Value.ToDateTime(TimeOnly.MinValue)
                                    : DateTime.Today,
                                PesoKg = animal.PesoNacimiento.Value,
                                Observacion = "Peso inicial al registrar el animal"
                            };

                            _context.Pesajes.Add(pesajeInicial);
                            await _context.SaveChangesAsync();

                            _logger.LogInformation(
                                "Pesaje inicial registrado: Animal {AnimalId}, Peso: {Peso} kg, Fecha: {Fecha}",
                                animal.AnimalId, animal.PesoNacimiento.Value, pesajeInicial.Fecha);
                        }

                        // 3️⃣ REGISTRAR GASTO AUTOMÁTICO (si tiene costo de compra)
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

                        // 4️⃣ COMMIT DE TODA LA TRANSACCIÓN
                        await tx.CommitAsync();

                        // ✅ MENSAJE DE ÉXITO INTELIGENTE
                        var mensajeExito = "Animal creado exitosamente";

                        if (animal.PesoNacimiento.HasValue && animal.PesoNacimiento.Value > 0)
                        {
                            mensajeExito += $". Pesaje inicial registrado: {animal.PesoNacimiento.Value:N1} kg";
                        }

                        if (animal.CostoCompra.HasValue && animal.CostoCompra.Value > 0)
                        {
                            mensajeExito += $". Gasto de compra registrado: ₡{animal.CostoCompra.Value:N2}";
                        }

                        MostrarExito(mensajeExito);

                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        await tx.RollbackAsync();
                        _logger.LogError(ex, "Error al crear animal con pesaje inicial y/o gasto automático");
                        ModelState.AddModelError(string.Empty, "Ocurrió un error al crear el animal. Por favor, intente nuevamente.");
                    }
                }

                // ❌ SI HAY ERRORES, RECARGAR DROPDOWNS
                ViewData["LoteAnimalId"] = new SelectList(
                    _context.LoteAnimals.Where(l => l.FincaId == fincaId),
                    "LoteAnimalId", "Nombre", animal.LoteAnimalId);

                ViewData["RazaId"] = new SelectList(_context.Razas, "RazaId", "Nombre", animal.RazaId);

                ViewData["MadreId"] = new SelectList(
                    _context.Animals
                        .Where(a => a.FincaId == fincaId &&
                               a.Sexo == "H" &&
                               a.Estado == "Activo")
                        .OrderBy(a => a.Arete)
                        .ToList(),
                    "AnimalId", "Arete", animal.MadreId);

                ViewData["PadreId"] = new SelectList(
                    _context.Animals
                        .Where(a => a.FincaId == fincaId &&
                               a.Sexo == "M" &&
                               a.Estado == "Activo")
                        .OrderBy(a => a.Arete)
                        .ToList(),
                    "AnimalId", "Arete", animal.PadreId);

                return View(animal);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado en AnimalesController.Create");
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
                    _context.Animals
                        .Where(a => a.FincaId == fincaId &&
                               a.Sexo == "H" &&
                               a.Estado == "Activo") 
                        .OrderBy(a => a.Arete)
                        .ToList(),
                    "AnimalId", "Arete");

                ViewData["PadreId"] = new SelectList(
                    _context.Animals
                        .Where(a => a.FincaId == fincaId &&
                               a.Sexo == "M" &&
                               a.Estado == "Activo") 
                        .OrderBy(a => a.Arete)
                        .ToList(),
                    "AnimalId", "Arete");

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
                    _context.Animals
                        .Where(a => a.FincaId == fincaId &&
                               a.Sexo == "H" &&
                               a.Estado == "Activo")
                        .OrderBy(a => a.Arete)
                        .ToList(),
                    "AnimalId", "Arete");

                ViewData["PadreId"] = new SelectList(
                    _context.Animals
                        .Where(a => a.FincaId == fincaId &&
                               a.Sexo == "M" &&
                               a.Estado == "Activo")
                        .OrderBy(a => a.Arete)
                        .ToList(),
                    "AnimalId", "Arete");

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

                // INICIO DE TRANSACCIÓN
                using var tx = await _context.Database.BeginTransactionAsync();

                try
                {
                    // 1. Actualizar el animal
                    animal.Estado = "Vendido";
                    animal.PrecioVenta = PrecioVenta;
                    animal.FechaVenta = DateOnly.FromDateTime(FechaVenta ?? DateTime.UtcNow);

                    _context.Update(animal);
                    // NO GUARDAR AÚN - Esperar a que el servicio también agregue sus cambios

                    // 2. Registrar ingreso automático
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

                    // 3. AHORA SÍ guardamos todo junto (animal + ingreso)
                    await _context.SaveChangesAsync();

                    // 4. Commit de la transacción
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
                    // Rollback SOLO si algo falló
                    await tx.RollbackAsync();
                    _logger.LogError(ex, "Error al vender animal {AnimalId}", id);
                    MostrarError("Ocurrió un error al registrar la venta. Por favor, intente nuevamente.");
                    return RedirectToAction(nameof(Index));
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


        // ============================================================
        // AGREGAR ESTE MÉTODO AL AnimalesController
        // ============================================================

        // GET: Animales/Historial/5
        public async Task<IActionResult> Historial(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var fincaId = GetFincaId();

                // ========================================
                // 1️⃣ CARGAR EL ANIMAL CON TODAS SUS RELACIONES
                // ========================================
                var animal = await _context.Animals
                    .Where(a => a.FincaId == fincaId && a.AnimalId == id)
                    .Include(a => a.Finca)
                    .Include(a => a.Raza)
                    .Include(a => a.Madre)
                    .Include(a => a.Padre)
                    .Include(a => a.LoteAnimal)
                    .FirstOrDefaultAsync();

                if (animal == null)
                {
                    _logger.LogWarning($"Animal {id} no encontrado o no pertenece a finca {fincaId}");
                    return NotFound();
                }

                // ========================================
                // 2️⃣ CREAR EL VIEWMODEL
                // ========================================
                var vm = new AgroTechApp.ViewModels.AnimalHistorialVM
                {
                    Animal = animal
                };

                // ========================================
                // 3️⃣ CALCULAR EDAD
                // ========================================
                if (animal.FechaNacimiento.HasValue)
                {
                    var hoy = DateOnly.FromDateTime(DateTime.Today);
                    var nacimiento = animal.FechaNacimiento.Value;

                    var edad = hoy.DayNumber - nacimiento.DayNumber;
                    vm.DiasDeVida = edad;

                    // Formatear edad legible
                    if (edad < 30)
                    {
                        vm.EdadFormateada = $"{edad} días";
                    }
                    else if (edad < 365)
                    {
                        var meses = edad / 30;
                        var diasRestantes = edad % 30;
                        vm.EdadFormateada = diasRestantes > 0
                            ? $"{meses} meses y {diasRestantes} días"
                            : $"{meses} meses";
                    }
                    else
                    {
                        var años = edad / 365;
                        var mesesRestantes = (edad % 365) / 30;
                        vm.EdadFormateada = mesesRestantes > 0
                            ? $"{años} años y {mesesRestantes} meses"
                            : $"{años} años";
                    }
                }

                // ========================================
                // 4️⃣ HISTORIAL DE PESAJES Y ESTADÍSTICAS
                // ========================================
                vm.Pesajes = await _context.Pesajes
                    .Where(p => p.AnimalId == id)
                    .OrderBy(p => p.Fecha)
                    .ToListAsync();

                if (vm.Pesajes.Any())
                {
                    vm.PesoInicial = vm.Pesajes.First().PesoKg;
                    vm.PesoActual = vm.Pesajes.Last().PesoKg;
                    vm.GananciaTotal = vm.PesoActual - vm.PesoInicial;

                    // Calcular GDP (Ganancia Diaria Promedio)
                    if (vm.Pesajes.Count >= 2)
                    {
                        var primerPesaje = vm.Pesajes.First();
                        var ultimoPesaje = vm.Pesajes.Last();
                        var diasTranscurridos = (ultimoPesaje.Fecha - primerPesaje.Fecha).Days;

                        if (diasTranscurridos > 0)
                        {
                            vm.GananciaDiariaPromedio = vm.GananciaTotal / diasTranscurridos;
                        }

                        // Calcular mejor periodo de ganancia
                        decimal mejorGDP = 0;
                        string? mejorPeriodo = null;

                        for (int i = 1; i < vm.Pesajes.Count; i++)
                        {
                            var anterior = vm.Pesajes[i - 1];
                            var actual = vm.Pesajes[i];
                            var diasPeriodo = (actual.Fecha - anterior.Fecha).Days;

                            if (diasPeriodo > 0)
                            {
                                var gdpPeriodo = (actual.PesoKg - anterior.PesoKg) / diasPeriodo;

                                if (gdpPeriodo > mejorGDP)
                                {
                                    mejorGDP = gdpPeriodo;
                                    mejorPeriodo = $"{anterior.Fecha:dd/MM/yyyy} - {actual.Fecha:dd/MM/yyyy}";
                                }
                            }
                        }

                        vm.MejorGananciaDiaria = mejorGDP;
                        vm.MejorPeriodo = mejorPeriodo;
                    }
                }

                // ========================================
                // 5️⃣ HISTORIAL DE TRATAMIENTOS
                // ========================================
                vm.Tratamientos = await _context.Tratamientos
                    .Where(t => t.AnimalId == id)
                    .Include(t => t.TipoTrat)
                    .Include(t => t.Insumo)
                        .ThenInclude(i => i!.Unidad)
                    .OrderByDescending(t => t.Fecha)
                    .ToListAsync();

                vm.TotalTratamientos = vm.Tratamientos.Count;

                // Agrupar tratamientos por tipo
                vm.TratamientosPorTipo = vm.Tratamientos
                    .GroupBy(t => t.TipoTrat.Nombre)
                    .ToDictionary(g => g.Key, g => g.Count());

                // ========================================
                // 6️⃣ RESUMEN FINANCIERO
                // ========================================
                vm.CostoCompra = animal.CostoCompra;

                // Gastos del animal
                vm.Gastos = await _context.Gastos
                    .Where(g => g.AnimalId == id)
                    .Include(g => g.RubroGasto)
                    .OrderByDescending(g => g.Fecha)
                    .ToListAsync();

                vm.TotalGastos = vm.Gastos.Sum(g => g.Monto);

                // Ingresos del animal
                vm.Ingresos = await _context.Ingresos
                    .Where(i => i.AnimalId == id)
                    .Include(i => i.RubroIngreso)
                    .OrderByDescending(i => i.Fecha)
                    .ToListAsync();

                vm.TotalIngresos = vm.Ingresos.Sum(i => i.Monto);

                // Balance
                vm.BalanceFinanciero = vm.TotalIngresos - vm.TotalGastos;

                // ROI solo si está vendido y tiene costo de compra
                if (animal.Estado == "Vendido" && animal.CostoCompra.HasValue && animal.CostoCompra > 0)
                {
                    var ganancia = vm.BalanceFinanciero;
                    vm.ROI = (ganancia / animal.CostoCompra.Value) * 100;
                }

                // ========================================
                // 7️⃣ INDICADORES DE RENDIMIENTO DEL LOTE
                // ========================================
                if (animal.LoteAnimalId.HasValue)
                {
                    vm.Lote = animal.LoteAnimal;

                    // Contar animales en el lote
                    vm.AnimalesEnLote = await _context.Animals
                        .Where(a => a.LoteAnimalId == animal.LoteAnimalId &&
                                   a.Estado == "Activo" &&
                                   a.FincaId == fincaId)
                        .CountAsync();

                    // Calcular GDP promedio del lote
                    var animalesLote = await _context.Animals
                        .Where(a => a.LoteAnimalId == animal.LoteAnimalId &&
                                   a.Estado == "Activo" &&
                                   a.FincaId == fincaId)
                        .Include(a => a.Pesajes.OrderBy(p => p.Fecha))
                        .ToListAsync();

                    var gdpsLote = new List<decimal>();

                    foreach (var animalLote in animalesLote)
                    {
                        if (animalLote.Pesajes.Count >= 2)
                        {
                            var primerPesaje = animalLote.Pesajes.First();
                            var ultimoPesaje = animalLote.Pesajes.Last();
                            var diasTranscurridos = (ultimoPesaje.Fecha - primerPesaje.Fecha).Days;

                            if (diasTranscurridos > 0)
                            {
                                var ganancia = ultimoPesaje.PesoKg - primerPesaje.PesoKg;
                                var gdp = ganancia / diasTranscurridos;
                                gdpsLote.Add(gdp);
                            }
                        }
                    }

                    if (gdpsLote.Any())
                    {
                        vm.PromedioLote = gdpsLote.Average();

                        // Comparativa
                        if (vm.GananciaDiariaPromedio.HasValue && vm.PromedioLote.HasValue)
                        {
                            var diferencia = vm.GananciaDiariaPromedio.Value - vm.PromedioLote.Value;
                            var porcentajeDiferencia = (diferencia / vm.PromedioLote.Value) * 100;

                            if (porcentajeDiferencia > 10)
                            {
                                vm.ComparativaLote = $"Por encima del promedio ({porcentajeDiferencia:N1}%)";
                            }
                            else if (porcentajeDiferencia < -10)
                            {
                                vm.ComparativaLote = $"Por debajo del promedio ({Math.Abs(porcentajeDiferencia):N1}%)";
                                vm.TieneAlertaBajoRendimiento = true;
                                vm.MensajeAlerta = $"Este animal está ganando {Math.Abs(porcentajeDiferencia):N1}% menos peso que el promedio del lote.";
                            }
                            else
                            {
                                vm.ComparativaLote = "Dentro del rango normal del lote";
                            }
                        }
                    }

                    // Peso promedio del lote
                    var pesosActuales = new List<decimal>();
                    foreach (var animalLote in animalesLote)
                    {
                        var ultimoPeso = animalLote.Pesajes.OrderByDescending(p => p.Fecha).FirstOrDefault()?.PesoKg;
                        if (ultimoPeso.HasValue)
                        {
                            pesosActuales.Add(ultimoPeso.Value);
                        }
                    }

                    if (pesosActuales.Any())
                    {
                        vm.PesoPromedioLote = pesosActuales.Average();
                    }
                }

                // ========================================
                // 8️⃣ GENEALOGÍA Y CRÍAS
                // ========================================
                vm.Madre = animal.Madre;
                vm.Padre = animal.Padre;

                // Si es hembra, buscar sus crías
                if (animal.Sexo == "H")
                {
                    vm.Crias = await _context.Animals
                        .Where(a => a.MadreId == id && a.FincaId == fincaId)
                        .Include(a => a.Raza)
                        .OrderByDescending(a => a.FechaNacimiento)
                        .Take(10)
                        .ToListAsync();

                    vm.TotalCrias = vm.Crias.Count;
                }
                // Si es macho, buscar sus crías
                else if (animal.Sexo == "M")
                {
                    vm.Crias = await _context.Animals
                        .Where(a => a.PadreId == id && a.FincaId == fincaId)
                        .Include(a => a.Raza)
                        .OrderByDescending(a => a.FechaNacimiento)
                        .Take(10)
                        .ToListAsync();

                    vm.TotalCrias = vm.Crias.Count;
                }

                // ========================================
                // 9️⃣ ALERTA DE BAJO RENDIMIENTO (adicional)
                // ========================================
                if (!vm.TieneAlertaBajoRendimiento && vm.GananciaDiariaPromedio.HasValue)
                {
                    // Alerta si GDP es menor a 0.5 kg/día (ajustar según necesidad)
                    if (vm.GananciaDiariaPromedio.Value < 0.5m)
                    {
                        vm.TieneAlertaBajoRendimiento = true;
                        vm.MensajeAlerta = $"Ganancia diaria promedio baja ({vm.GananciaDiariaPromedio.Value:N2} kg/día). Se recomienda revisar alimentación y salud.";
                    }
                }

                return View(vm);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acceso no autorizado en AnimalesController.Historial");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
        }
    }
}