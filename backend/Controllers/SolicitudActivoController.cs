using System.Security.Claims;
using backend.Data;
using backend.DTOs;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SolicitudActivoController : ControllerBase
{
    private readonly SibiDbContext _db;
    private readonly HistorialService _historial;
    private readonly NotificacionService _notif;

    private static readonly string[] TiposPlacaValidos = { "Institucional", "Interno" };

    public SolicitudActivoController(SibiDbContext db, HistorialService historial, NotificacionService notif)
    {
        _db = db;
        _historial = historial;
        _notif = notif;
    }

    [HttpGet]
    [Authorize(Roles = "GTI,Administradora")]
    public async Task<IActionResult> Listar([FromQuery] string? estado)
    {
        var query = _db.SolicitudesActivo
            .Include(s => s.Categoria)
            .Include(s => s.Encargado)
            .Include(s => s.Solicitante)
            .Include(s => s.Revisor)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(estado))
            query = query.Where(s => s.Estado == estado);

        var lista = await query.OrderByDescending(s => s.FechaSolicitud).ToListAsync();
        return Ok(lista.Select(MapToDto));
    }

    [HttpGet("mis")]
    [Authorize(Roles = "JefaAdministrativa")]
    public async Task<IActionResult> ListarMias([FromQuery] string? estado)
    {
        var correo = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var query = _db.SolicitudesActivo
            .Include(s => s.Categoria)
            .Include(s => s.Encargado)
            .Include(s => s.Solicitante)
            .Include(s => s.Revisor)
            .Where(s => s.SolicitanteCorreo == correo);

        if (!string.IsNullOrWhiteSpace(estado))
            query = query.Where(s => s.Estado == estado);

        var lista = await query.OrderByDescending(s => s.FechaSolicitud).ToListAsync();
        return Ok(lista.Select(MapToDto));
    }

    [HttpGet("pendientes/count")]
    [Authorize(Roles = "GTI,Administradora")]
    public async Task<IActionResult> ContarPendientes()
    {
        var count = await _db.SolicitudesActivo.CountAsync(s => s.Estado == "Pendiente");
        return Ok(new { count });
    }

    [HttpPost]
    [Authorize(Roles = "JefaAdministrativa")]
    public async Task<IActionResult> Crear([FromBody] SolicitudActivoRequest request)
    {
        var error = await ValidarDatosAsync(request, idExcluir: null);
        if (error is not null) return BadRequest(new { mensaje = error });

        var correo = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var solicitud = new SolicitudActivo
        {
            SolicitanteCorreo = correo,
            Placa = request.Placa.Trim(),
            TipoPlaca = request.TipoPlaca,
            Marca = request.Marca,
            Modelo = request.Modelo,
            NumSerial = request.NumSerial,
            Articulo = request.Articulo,
            Observaciones = string.IsNullOrWhiteSpace(request.Observaciones) ? null : request.Observaciones,
            UbicacionActual = request.UbicacionActual,
            Estado = "Pendiente"
        };
        AplicarCategoriaYEncargado(solicitud, request);

        _db.SolicitudesActivo.Add(solicitud);
        await _db.SaveChangesAsync();

        var nombreSolicitante = User.FindFirstValue(ClaimTypes.Name);
        await _notif.NotificarGTIAdminAsync("solicitud_alta", "Nueva solicitud de alta de activo",
            $"{nombreSolicitante} solicitó registrar el activo {solicitud.Placa}.");

        return Ok(new { id = solicitud.Id });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "GTI,Administradora")]
    public async Task<IActionResult> Editar(int id, [FromBody] SolicitudActivoRequest request)
    {
        var solicitud = await _db.SolicitudesActivo.FindAsync(id);
        if (solicitud is null) return NotFound();
        if (solicitud.Estado != "Pendiente")
            return BadRequest(new { mensaje = "La solicitud ya fue procesada y no puede modificarse." });

        var error = await ValidarDatosAsync(request, idExcluir: id);
        if (error is not null) return BadRequest(new { mensaje = error });

        solicitud.Placa = request.Placa.Trim();
        solicitud.TipoPlaca = request.TipoPlaca;
        solicitud.Marca = request.Marca;
        solicitud.Modelo = request.Modelo;
        solicitud.NumSerial = request.NumSerial;
        solicitud.Articulo = request.Articulo;
        solicitud.Observaciones = string.IsNullOrWhiteSpace(request.Observaciones) ? null : request.Observaciones;
        solicitud.UbicacionActual = request.UbicacionActual;
        AplicarCategoriaYEncargado(solicitud, request);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/aprobar")]
    [Authorize(Roles = "GTI,Administradora")]
    public async Task<IActionResult> Aprobar(int id)
    {
        var solicitud = await _db.SolicitudesActivo
            .Include(s => s.Solicitante)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (solicitud is null) return NotFound();
        if (solicitud.Estado != "Pendiente")
            return BadRequest(new { mensaje = "La solicitud ya fue procesada." });

        // Revalidar contra el estado actual de la base (la placa pudo ocuparse mientras estuvo pendiente).
        // enAprobacion: no rechazamos si la categoría/encargado propuestos ya existen ahora —
        // ResolverCategoriaAsync / ResolverEncargadoAsync reutilizan la fila existente.
        var error = await ValidarDatosAsync(SolicitudComoRequest(solicitud), idExcluir: id, enAprobacion: true);
        if (error is not null) return BadRequest(new { mensaje = error });

        var correo = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 1) Materializar categoría/encargado propuestos (o reutilizar si ya existen ahora).
            var (categoriaId, catCreada) = await ResolverCategoriaAsync(solicitud);
            var (encargadoId, encCreado) = await ResolverEncargadoAsync(solicitud);

            // 2) Placa + Ubicacion + Activo
            if (!await _db.Placas.AnyAsync(p => p.Numero == solicitud.Placa))
                _db.Placas.Add(new Placa { Numero = solicitud.Placa, Tipo = solicitud.TipoPlaca });

            var ubicacion = new Ubicacion
            {
                Actual = solicitud.UbicacionActual,
                Anterior = solicitud.UbicacionActual,
                EncargadoActualId = encargadoId,
                EncargadoAnteriorId = encargadoId
            };
            _db.Ubicaciones.Add(ubicacion);
            await _db.SaveChangesAsync();

            var activo = new Activo
            {
                Placa = solicitud.Placa,
                Marca = solicitud.Marca,
                Modelo = solicitud.Modelo,
                NumSerial = solicitud.NumSerial,
                Articulo = solicitud.Articulo,
                CategoriaId = categoriaId,
                Observaciones = solicitud.Observaciones,
                UbicacionId = ubicacion.Id,
                Estado = "Activo"
            };
            _db.Activos.Add(activo);

            solicitud.Estado = "Aprobada";
            solicitud.RevisorCorreo = correo;
            solicitud.FechaResolucion = DateTime.Now;
            solicitud.PlacaCreada = activo.Placa;
            await _db.SaveChangesAsync();

            var solicitanteNombre = solicitud.Solicitante?.Nombre ?? solicitud.SolicitanteCorreo;
            var extra = new List<string>();
            if (catCreada is not null) extra.Add($"categoría '{catCreada}'");
            if (encCreado is not null) extra.Add($"encargado '{encCreado}'");
            var extraTxt = extra.Count > 0 ? $" Nuevo: {string.Join(", ", extra)}." : "";
            var desc = $"{activo.Marca} {activo.Modelo} agregado al inventario (alta solicitada por {solicitanteNombre}, aprobada por {User.FindFirstValue(ClaimTypes.Name)}).{extraTxt}";
            if (desc.Length > 250) desc = desc[..250];
            await _historial.RegistrarAsync(activo.Placa, correo, "Creacion", desc);

            await tx.CommitAsync();
            return NoContent();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    [HttpPost("{id}/rechazar")]
    [Authorize(Roles = "GTI,Administradora")]
    public async Task<IActionResult> Rechazar(int id, [FromBody] RechazarSolicitudRequest request)
    {
        var solicitud = await _db.SolicitudesActivo.FindAsync(id);
        if (solicitud is null) return NotFound();
        if (solicitud.Estado != "Pendiente")
            return BadRequest(new { mensaje = "La solicitud ya fue procesada." });

        var correo = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        solicitud.Estado = "Rechazada";
        solicitud.RevisorCorreo = correo;
        solicitud.FechaResolucion = DateTime.Now;
        solicitud.Comentario = request.Comentario;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void AplicarCategoriaYEncargado(SolicitudActivo s, SolicitudActivoRequest r)
    {
        var catNuevaNombre = string.IsNullOrWhiteSpace(r.CategoriaNuevaNombre) ? null : r.CategoriaNuevaNombre.Trim();
        s.CategoriaId          = catNuevaNombre is null ? r.CategoriaId : null;
        s.CategoriaNuevaNombre = catNuevaNombre;
        s.CategoriaNuevaIcono  = catNuevaNombre is null || string.IsNullOrWhiteSpace(r.CategoriaNuevaIcono)
            ? null : r.CategoriaNuevaIcono.Trim();

        var encNuevoNombre = string.IsNullOrWhiteSpace(r.EncargadoNuevoNombre) ? null : r.EncargadoNuevoNombre.Trim();
        s.EncargadoId          = encNuevoNombre is null ? r.EncargadoId : null;
        s.EncargadoNuevoNombre = encNuevoNombre;
        s.EncargadoNuevoRol    = encNuevoNombre is null || string.IsNullOrWhiteSpace(r.EncargadoNuevoRol)
            ? null : r.EncargadoNuevoRol.Trim();
    }

    private static SolicitudActivoRequest SolicitudComoRequest(SolicitudActivo s) => new(
        s.Placa, s.TipoPlaca, s.Marca, s.Modelo, s.NumSerial, s.Articulo,
        s.Observaciones, s.UbicacionActual,
        s.CategoriaId, s.CategoriaNuevaNombre, s.CategoriaNuevaIcono,
        s.EncargadoId, s.EncargadoNuevoNombre, s.EncargadoNuevoRol);

    /// <returns>(id de categoría resuelto, nombre si se creó una nueva)</returns>
    private async Task<(int id, string? creada)> ResolverCategoriaAsync(SolicitudActivo s)
    {
        if (s.CategoriaId.HasValue) return (s.CategoriaId.Value, null);

        var nombre = s.CategoriaNuevaNombre!.Trim();
        var existente = await _db.Categorias.FirstOrDefaultAsync(c => c.Nombre == nombre);
        if (existente is not null) return (existente.Id, null);

        var nueva = new Categoria { Nombre = nombre, Icono = s.CategoriaNuevaIcono };
        _db.Categorias.Add(nueva);
        await _db.SaveChangesAsync();
        return (nueva.Id, nombre);
    }

    /// <returns>(id de encargado resuelto, nombre si se creó uno nuevo)</returns>
    private async Task<(Guid id, string? creado)> ResolverEncargadoAsync(SolicitudActivo s)
    {
        if (s.EncargadoId.HasValue) return (s.EncargadoId.Value, null);

        var nombre = s.EncargadoNuevoNombre!.Trim();
        var rol = (s.EncargadoNuevoRol ?? "").Trim();
        var existente = await _db.Encargados.FirstOrDefaultAsync(e => e.Nombre == nombre && e.Rol == rol);
        if (existente is not null) return (existente.Id, null);

        var nuevo = new Encargado { Nombre = nombre, Rol = rol };
        _db.Encargados.Add(nuevo);
        await _db.SaveChangesAsync();
        return (nuevo.Id, nombre);
    }

    /// <summary>Valida los datos propuestos. Devuelve un mensaje de error o null si todo es válido.
    /// Con <paramref name="enAprobacion"/> se omite el rechazo por "la categoría/encargado propuesto ya existe"
    /// (al aprobar simplemente se reutiliza la fila existente).</summary>
    private async Task<string?> ValidarDatosAsync(SolicitudActivoRequest r, int? idExcluir, bool enAprobacion = false)
    {
        var placa = r.Placa?.Trim();
        if (string.IsNullOrWhiteSpace(placa))
            return "La placa es obligatoria.";
        if (!TiposPlacaValidos.Contains(r.TipoPlaca))
            return "El tipo de placa no es válido. Use 'Institucional' o 'Interno'.";
        if (string.IsNullOrWhiteSpace(r.Articulo)) return "El artículo es obligatorio.";
        if (string.IsNullOrWhiteSpace(r.Marca)) return "La marca es obligatoria.";
        if (string.IsNullOrWhiteSpace(r.Modelo)) return "El modelo es obligatorio.";
        if (string.IsNullOrWhiteSpace(r.NumSerial)) return "El número serial es obligatorio.";
        if (string.IsNullOrWhiteSpace(r.UbicacionActual)) return "La ubicación actual es obligatoria.";

        if (await _db.Activos.AnyAsync(a => a.Placa == placa))
            return "Ya existe un activo con esa placa en el inventario.";

        var pendienteDuplicada = await _db.SolicitudesActivo
            .AnyAsync(s => s.Placa == placa && s.Estado == "Pendiente" && (idExcluir == null || s.Id != idExcluir));
        if (pendienteDuplicada)
            return "Ya hay otra solicitud pendiente para esa placa.";

        // Categoría: exactamente una vía (existente o propuesta)
        var catNueva = string.IsNullOrWhiteSpace(r.CategoriaNuevaNombre) ? null : r.CategoriaNuevaNombre.Trim();
        if ((r.CategoriaId.HasValue) == (catNueva is not null))
            return "Indica una categoría existente o propón una nueva (una sola de las dos).";
        if (r.CategoriaId.HasValue && !await _db.Categorias.AnyAsync(c => c.Id == r.CategoriaId.Value))
            return "La categoría seleccionada no existe.";
        if (!enAprobacion && catNueva is not null && await _db.Categorias.AnyAsync(c => c.Nombre == catNueva))
            return $"Ya existe la categoría '{catNueva}'. Selecciónala de la lista en vez de proponerla.";

        // Encargado: exactamente una vía (existente o propuesto)
        var encNuevo = string.IsNullOrWhiteSpace(r.EncargadoNuevoNombre) ? null : r.EncargadoNuevoNombre.Trim();
        if ((r.EncargadoId.HasValue) == (encNuevo is not null))
            return "Indica un encargado existente o propón uno nuevo (una sola de las dos).";
        if (r.EncargadoId.HasValue && !await _db.Encargados.AnyAsync(e => e.Id == r.EncargadoId.Value))
            return "El encargado seleccionado no existe.";
        if (encNuevo is not null)
        {
            var rolNuevo = (r.EncargadoNuevoRol ?? "").Trim();
            if (rolNuevo.Length == 0)
                return "El encargado nuevo necesita un cargo o rol.";
            if (!enAprobacion && await _db.Encargados.AnyAsync(e => e.Nombre == encNuevo && e.Rol == rolNuevo))
                return $"Ya existe el encargado '{encNuevo}' ({rolNuevo}). Selecciónalo de la lista.";
        }

        return null;
    }

    private static SolicitudActivoDto MapToDto(SolicitudActivo s) => new(
        s.Id,
        s.Placa,
        s.TipoPlaca,
        s.Marca,
        s.Modelo,
        s.NumSerial,
        s.Articulo,
        s.Observaciones,
        s.UbicacionActual,
        s.CategoriaId,
        s.Categoria?.Nombre ?? "",
        s.CategoriaNuevaNombre,
        s.CategoriaNuevaIcono,
        s.EncargadoId,
        s.Encargado?.Nombre ?? "",
        s.EncargadoNuevoNombre,
        s.EncargadoNuevoRol,
        s.Solicitante?.Nombre ?? s.SolicitanteCorreo,
        s.SolicitanteCorreo,
        s.FechaSolicitud,
        s.Estado,
        s.Revisor?.Nombre,
        s.FechaResolucion,
        s.Comentario,
        s.PlacaCreada
    );
}
