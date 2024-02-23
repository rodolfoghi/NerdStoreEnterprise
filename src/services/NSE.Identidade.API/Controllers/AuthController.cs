using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NSE.Identidade.API.Models;

namespace NSE.Identidade.API.Controllers;

[ApiController]
[Route("api/identidade")]
public class AuthController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
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
        return Ok();

    }

    [HttpPost("autenticar")]
    public async Task<ActionResult> Login(UsuarioLogin usuarioLogin)
    {
        if (!ModelState.IsValid) return BadRequest();

        var result = await signInManager
            .PasswordSignInAsync(usuarioLogin.Email, usuarioLogin.Senha, false, true);

        if (!result.Succeeded) return BadRequest();

        return Ok();
    }
}