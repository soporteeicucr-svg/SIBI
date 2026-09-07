namespace backend.Models;

/// <summary>
/// Propuesta de alta de un activo nuevo enviada por la JefaAdministrativa.
/// Debe ser aprobada o rechazada por Administradora o GTI; el revisor puede
/// corregir cualquier dato propuesto antes de aprobar. Al aprobarse se crea
/// el <see cref="Activo"/> real (junto con su Placa y Ubicacion).
///
/// La categoría y el encargado pueden venir de dos formas mutuamente excluyentes:
///  - una referencia a una fila existente (<see cref="CategoriaId"/> / <see cref="EncargadoId"/>), o
///  - una <b>propuesta de creación</b> (<see cref="CategoriaNuevaNombre"/> / <see cref="EncargadoNuevoNombre"/>+<see cref="EncargadoNuevoRol"/>),
///    que se materializa recién al aprobar la solicitud.
/// </summary>
public class SolicitudActivo
{
    public int Id { get; set; }
    public string SolicitanteCorreo { get; set; } = string.Empty;
    public DateTime FechaSolicitud { get; set; } = DateTime.Now;

    // Datos propuestos del activo
    public string Placa { get; set; } = string.Empty;
    public string TipoPlaca { get; set; } = "Institucional";
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public string NumSerial { get; set; } = string.Empty;
    public string Articulo { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public string UbicacionActual { get; set; } = string.Empty;

    // Categoría: existente O propuesta de creación (exactamente una)
    public int? CategoriaId { get; set; }
    public string? CategoriaNuevaNombre { get; set; }
    public string? CategoriaNuevaIcono { get; set; }

    // Encargado: existente O propuesta de creación (exactamente una)
    public Guid? EncargadoId { get; set; }
    public string? EncargadoNuevoNombre { get; set; }
    public string? EncargadoNuevoRol { get; set; }

    public string Estado { get; set; } = "Pendiente"; // Pendiente | Aprobada | Rechazada
    public string? RevisorCorreo { get; set; }
    public DateTime? FechaResolucion { get; set; }
    public string? Comentario { get; set; }
    public string? PlacaCreada { get; set; } // Placa del activo resultante tras la aprobación

    public Categoria? Categoria { get; set; }
    public Encargado? Encargado { get; set; }
    public Usuario Solicitante { get; set; } = null!;
    public Usuario? Revisor { get; set; }
}
