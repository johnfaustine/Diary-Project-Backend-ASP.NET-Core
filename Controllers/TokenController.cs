using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using DiaryProject.Data;
using DiaryProject.Models;
using DiaryProject.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DiaryProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly ITokenService _tokenService;
        private UserManager<ApplicationUser> _userManager;
        public IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public TokenController(UserManager<ApplicationUser> userManager, IConfiguration config, ApplicationDbContext context, ITokenService tokenService)
        {

            _userManager = userManager;
            _configuration = config;
            _context = context;
            _tokenService = tokenService;

        }
        //[HttpPost]
        //public async Task<IActionResult> Post(LoginViewModel model)
        //{

        //    var user = await _userManager.FindByEmailAsync(model.Email);

        //    if (user == null)
        //    {
        //        return BadRequest("Invalid email");
        //    }
        //    var result = await _userManager.CheckPasswordAsync(user, model.Password);
        //    if (!result)
        //        return BadRequest("Invalid password");

        //    var claims = new[] {
        //            new Claim("Email", model.Email),
        //            new Claim(ClaimTypes.NameIdentifier, user.Id),                
        //            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        //            new Claim(JwtRegisteredClaimNames.Iat, DateTime.UtcNow.ToString()),
        //            new Claim(JwtRegisteredClaimNames.Sub, _configuration["Jwt:Subject"])
        //           };

        //    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));

        //    var signIn = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        //    var token = new JwtSecurityToken(
        //        _configuration["Jwt:Issuer"],
        //        _configuration["Jwt:Audience"],
        //        claims: claims,
        //        expires: DateTime.UtcNow.AddDays(1),
        //        signingCredentials: signIn);

        //    return Ok(new JwtSecurityTokenHandler().WriteToken(token));

        //}

        // /api/token?token&refreshToken
        [HttpPost]
        public async Task<IActionResult> Refresh()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if(refreshToken == null)
                return BadRequest("No refresh token found");
            var user = _context.RefreshTokens.FirstOrDefault(u => u.RToken == refreshToken);
            if (user == null)
                return BadRequest("User not found for the refresh token");
            var usersClaims = new[]
          {
                new Claim(ClaimTypes.NameIdentifier, user.UserId)
            };
            var newJwtToken = _tokenService.GenerateAccessToken(usersClaims);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            user.RToken = newRefreshToken;
            user.RTExpirationDate = DateTime.UtcNow.AddMonths(6);
            await _context.SaveChangesAsync();


            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddMonths(6)
            };


            Response.Cookies.Append("refreshToken", newRefreshToken, cookieOptions);
            return new ObjectResult(new
            {
                token = newJwtToken,
                //refreshToken = newRefreshToken
            });
        }

    }
}
