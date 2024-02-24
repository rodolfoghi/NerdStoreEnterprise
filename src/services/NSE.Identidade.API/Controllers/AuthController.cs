﻿using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NSE.Identidade.API.Extensions;
using NSE.Identidade.API.Models;

namespace NSE.Identidade.API.Controllers;

[ApiController]
[Route("api/identidade")]
public class AuthController(
    SignInManager<IdentityUser> signInManager,
    UserManager<IdentityUser> userManager,
    IOptions<AppSettings> appSettings)
    : Controller
{
    [HttpPost("nova-conta")]
    public async Task<ActionResult> Registrar(UsuarioRegistro usuarioRegistro)
    {
        if (!ModelState.IsValid) return BadRequest();

        var user = new IdentityUser
        {
            UserName = usuarioRegistro.Email,
            Email = usuarioRegistro.Email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, usuarioRegistro.Senha);

        if (!result.Succeeded) return BadRequest();
        await signInManager.SignInAsync(user, false);
        return Ok(await GerarJwt(usuarioRegistro.Email));

    }

    [HttpPost("autenticar")]
    public async Task<ActionResult> Login(UsuarioLogin usuarioLogin)
    {
        if (!ModelState.IsValid) return BadRequest();

        var result = await signInManager
            .PasswordSignInAsync(usuarioLogin.Email, usuarioLogin.Senha, false, true);

        if (!result.Succeeded) return BadRequest();

        return Ok(await GerarJwt(usuarioLogin.Email));
    }

    private async Task<UsuarioRespostaLogin> GerarJwt(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        var claims = await userManager.GetClaimsAsync(user);
        var userRoles = await userManager.GetRolesAsync(user);

        claims.Add(new Claim(JwtRegisteredClaimNames.Sub, user.Id));
        claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));
        claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
        claims.Add(new Claim(JwtRegisteredClaimNames.Nbf, ToUnixEpochDate(DateTime.UtcNow).ToString()));
        claims.Add(
            new Claim(
                JwtRegisteredClaimNames.Iat,
                ToUnixEpochDate(DateTime.UtcNow).ToString(), ClaimValueTypes.Integer64));

        foreach (var userRole in userRoles)
        {
            claims.Add(new Claim("role", userRole));
        }

        var identityClaims = new ClaimsIdentity();
        identityClaims.AddClaims(claims);

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(appSettings.Value.Secret);
        var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = appSettings.Value.Emissor,
            Audience = appSettings.Value.ValidoEm,
            Subject = identityClaims,
            Expires = DateTime.UtcNow.AddHours(appSettings.Value.ExpiracaoHoras),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        });

        var encodedToken = tokenHandler.WriteToken(token);

        return new UsuarioRespostaLogin
        {
            AccessToken = encodedToken,
            ExpiresIn = TimeSpan.FromHours(appSettings.Value.ExpiracaoHoras).TotalSeconds,
            UsuarioToken = new UsuarioToken
            {
                Id = user.Id,
                Email = user.Email,
                Claims = claims.Select(c => new UsuarioClaim { Type = c.Type, Value = c.Value })
            }
        };
    }

    private static long ToUnixEpochDate(DateTime date)
        => (long)Math.Round((date.ToUniversalTime() - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)).TotalSeconds);
}