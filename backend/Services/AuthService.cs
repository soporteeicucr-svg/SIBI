using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using backend.Data;
using backend.DTOs;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace backend.Services;

public class AuthService
{
    private readonly SibiDbContext _db;
    private readonly IConfiguration _config;
    private readonly NotificacionService _notif;

    public AuthService(SibiDbContext db, IConfiguration config, NotificacionService notif)
    {
        _db = db;
        _config = config;
        _notif = notif;
    }

    public async Task<LoginResponse?> LoginAsync(string correo, string contrasena)
    {
        var usuario = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.Correo == correo && u.Activo);

        if (usuario is null) return null;

        const string cuentaPrincipal = "soporte.eic@ucr.ac.cr";
        bool esCuentaPrincipal = usuario.Correo.Equals(cuentaPrincipal, StringComparison.OrdinalIgnoreCase);

        if (!esCuentaPrincipal && usuario.IntentosFallidos >= 3) return null;

        if (!BCrypt.Net.BCrypt.Verify(contrasena, usuario.Contrasena))
        {
            if (!esCuentaPrincipal)
            {
                usuario.IntentosFallidos++;
                await _db.SaveChangesAsync();
                if (usuario.IntentosFallidos == 3)
                    await _notif.NotificarAdminAsync("cuenta_bloqueada",
                        "Cuenta bloqueada",
                        $"La cuenta de {usuario.Nombre} ({usuario.Correo}) fue bloqueada tras 3 intentos fallidos de contraseña.");
            }
            return null;
        }

        usuario.IntentosFallidos = 0;
        await _db.SaveChangesAsync();

        return new LoginResponse(
            GenerarToken(usuario),
            usuario.Correo,
            usuario.Nombre,
            usuario.Permisos,
            usuario.EsContrasenaTemporal
        );
    }

    public async Task<string> CambiarContrasenaAsync(string correo, string actual, string nueva)
    {
        var usuario = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.Correo == correo && u.Activo);

        if (usuario is null) return "incorrecta";

        // Usuarios con contraseña temporal ya verificaron al hacer login; no se exige la actual.
        if (!usuario.EsContrasenaTemporal && !BCrypt.Net.BCrypt.Verify(actual, usuario.Contrasena))
            return "incorrecta";

        if (!ValidarFuerzaContrasena(nueva))
            return "debil";

        usuario.Contrasena = BCrypt.Net.BCrypt.HashPassword(nueva);
        usuario.EsContrasenaTemporal = false;
        await _db.SaveChangesAsync();
        return "ok";
    }

    // Mínimo 6 chars, al menos una mayúscula, minúscula y número (regla del mockup)
    public static bool ValidarFuerzaContrasena(string contrasena) =>
        contrasena.Length >= 6 &&
        Regex.IsMatch(contrasena, @"[A-Z]") &&
        Regex.IsMatch(contrasena, @"[a-z]") &&
        Regex.IsMatch(contrasena, @"[0-9]");

    private string GenerarToken(Usuario usuario)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Correo),
            new Claim(ClaimTypes.Name, usuario.Nombre),
            new Claim(ClaimTypes.Role, usuario.Permisos)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(double.Parse(_config["Jwt:ExpireHours"]!)),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

}
