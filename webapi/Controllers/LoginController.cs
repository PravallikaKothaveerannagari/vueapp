﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Security.Claims;

namespace webapi.Controllers
{
    public class LoginRequest
    {
        public string username { get; set; }
        public string password { get; set; }
    }

    [ApiController]
    [Route("api/auth")]
    public class ApiAuthController : ControllerBase
    {
        private SignInManager<IdentityUser> SignInManager;
        private UserManager<IdentityUser> UserManager;
        private IConfiguration Configuration;

        public ApiAuthController(SignInManager<IdentityUser> signMgr, UserManager<IdentityUser> usrMgr,IConfiguration config)
        {
            SignInManager = signMgr;
            UserManager = usrMgr;
            Configuration = config;
        }

        [HttpPost("signin")]
        public async Task<IActionResult> ApiSignIn([FromBody] SigningCredentials creds)
        {
            var response = new Response();
            response.Message = "Invalid username or password";

            IdentityUser user = await UserManager.FindByNameAsync(creds.Username);
            if (user == null)
                return Ok(response);
            Microsoft.AspNetCore.Identity.SignInResult result = await SignInManager.CheckPasswordSignInAsync(user, creds.Password, false);

            if (result.Succeeded)
            {
                SecurityTokenDescriptor descriptor = new SecurityTokenDescriptor
                {
                    Subject = (await SignInManager.CreateUserPrincipalAsync(user)).Identities.First(),
                    Expires = DateTime.MaxValue,
                    SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                        Configuration["Jwt:Key"])),
                        SecurityAlgorithms.HmacSha256Signature)
                };
                JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
                SecurityToken secToken = new JwtSecurityTokenHandler().CreateToken(descriptor);
                response.Success = true;
                response.Message = "Successfully signed in";
                response.Token = handler.WriteToken(secToken);
            }
                
            return Ok(response);
        }

        [HttpPost("signup")]
        public async Task<IActionResult> ApiSignUp([FromBody] SingupCredentials creds)
        {
            IdentityUser user = new IdentityUser { UserName = creds.Username, Email = creds.Email };
            IdentityResult result = await UserManager.CreateAsync(user, creds.Password);

            var response = new Response();

            if (result.Succeeded)
            {
                SecurityTokenDescriptor descriptor = new SecurityTokenDescriptor
                {
                    Subject = (await SignInManager.CreateUserPrincipalAsync(user)).Identities.First(),
                    Expires = DateTime.MaxValue,
                    SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                        Configuration["Jwt:Key"])),
                        SecurityAlgorithms.HmacSha256Signature)
                };
                JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
                SecurityToken secToken = new JwtSecurityTokenHandler().CreateToken(descriptor);
                response.Success = true;
                response.Message = "Account is created";
                response.Token = handler.WriteToken(secToken);
            }
            else
                response.Message = result.Errors.ToString();
            return Ok(response);
        }
    }

    public class SigningCredentials
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class SingupCredentials
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class Response
    {
        public bool Success { get; set; } = false;
        public string Message { get; set; } = "";
        public string Token { get; set; } = "";
    }
}
