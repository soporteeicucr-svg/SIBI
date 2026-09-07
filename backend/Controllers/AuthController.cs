using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth) => _auth = auth;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _auth.LoginAsync(request.Correo, request.Contrasena);
        if (response is null)
            return Unauthorized(new { mensaje = "Credenciales incorrectas o cuenta bloqueada." });
        return Ok(response);
    }

    [Authorize]
    [HttpPost("cambiar-contrasena")]
    public async Task<IActionResult> CambiarContrasena([FromBody] CambiarContrasenaRequest request)
    {
        var correo = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (correo.Equals("soporte.eic@ucr.ac.cr", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { mensaje = "La contraseña de esta cuenta no puede modificarse desde la aplicación." });

        var resultado = await _auth.CambiarContrasenaAsync(correo, request.ContrasenaActual, request.NuevaContrasena);
        return resultado switch
        {
            "incorrecta" => BadRequest(new { mensaje = "La contraseña actual es incorrecta." }),
            "debil"      => BadRequest(new { mensaje = "La nueva contraseña debe tener mínimo 6 caracteres, una mayúscula, una minúscula y un número." }),
            _            => NoContent()
        };
    }

}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       
