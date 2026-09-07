namespace backend.DTOs;

/// <summary>Datos propuestos para un activo nuevo. Lo usa tanto la JefaAdministrativa
/// al crear la solicitud como Administradora/GTI al corregirla antes de aprobar.
///
/// Para la categoría se envía <b>o</b> <see cref="CategoriaId"/> (existente)
/// <b>o</b> <see cref="CategoriaNuevaNombre"/> (propuesta de creación). Lo mismo
/// para el encargado con <see cref="EncargadoId"/> vs. <see cref="EncargadoNuevoNombre"/>+<see cref="EncargadoNuevoRol"/>.</summary>
public record SolicitudActivoRequest(
    string Placa,
    string TipoPlaca,
    string Marca,
    string Modelo,
    string NumSerial,
    string Articulo,
    string? Observaciones,
    string UbicacionActual,
    int? CategoriaId,
    string? CategoriaNuevaNombre,
    string? CategoriaNuevaIcono,
    Guid? EncargadoId,
    string? EncargadoNuevoNombre,
    string? EncargadoNuevoRol
);

public record SolicitudActivoDto(
    int Id,
    string Placa,
    string TipoPlaca,
    string Marca,
    string Modelo,
    string NumSerial,
    string Articulo,
    string? Observaciones,
    string UbicacionActual,
    // Categoría: uno de los dos bloques según cómo se propuso
    int? CategoriaId,
    string CategoriaNombre,          // nombre de la categoría existente, o "" si es propuesta
    string? CategoriaNuevaNombre,    // != null cuando es una propuesta de categoría nueva
    string? CategoriaNuevaIcono,
    // Encargado
    Guid? EncargadoId,
    string EncargadoNombre,          // nombre del encargado existente, o "" si es propuesta
    string? EncargadoNuevoNombre,
    string? EncargadoNuevoRol,
    string SolicitanteNombre,
    string SolicitanteCorreo,
    DateTime FechaSolicitud,
    string Estado,
    string? RevisorNombre,
    DateTime? FechaResolucion,
    string? Comentario,
    string? PlacaCreada
);
