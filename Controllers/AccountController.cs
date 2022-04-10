using System;
using System.Collections.Generic;
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
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.AspNetCore.Authorization;

namespace DiaryProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {

        private readonly ApplicationDbContext _context;
        private readonly ITokenService _tokenService;
        private UserManager<ApplicationUser> _userManager;
        public IConfiguration _configuration;
        private IMailService _mailService;
        public AccountController(UserManager<ApplicationUser> userManager, IConfiguration config, ApplicationDbContext context, ITokenService tokenService, IMailService mailService)
        {
            _userManager = userManager;
            _configuration = config;
            _context = context;
            _tokenService = tokenService;
            _mailService = mailService;

        }

        // /api/account/Register
        [HttpPost("Register")]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {

            ApplicationUser user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
                //return StatusCode(409);
                return BadRequest(new { message = "Email has already been used" });

            if (model == null)
                throw new NullReferenceException("Register Model is null");

            if (model.Password != model.ConfirmPassword)
                return BadRequest(new { message = "Confirm password did not match" });

            var identityUser = new ApplicationUser
            {
                Email = model.Email,
                UserName = model.Email,
            };

            var result = await _userManager.CreateAsync(identityUser, model.Password);

            if (result.Succeeded)
            {
                var confirmEmailToken = await _userManager.GenerateEmailConfirmationTokenAsync(identityUser);

                var encodedEmailToken = Encoding.UTF8.GetBytes(confirmEmailToken);
                var validEmailToken = WebEncoders.Base64UrlEncode(encodedEmailToken);

                string url = $"{_configuration["ClientUrl"]}/confirmEmail?userid={identityUser.Id}&token={validEmailToken}";

                MimeMessage message = new MimeMessage();
                MailboxAddress from = new MailboxAddress("Diary ","diary@diary.com");
                message.From.Add(from);
                MailboxAddress to = new MailboxAddress("User",model.Email);
                message.To.Add(to);
                message.Subject = "Diary - Please confirm your email";
                BodyBuilder bodyBuilder = new BodyBuilder();
                bodyBuilder.HtmlBody = "<h1>Hello "+ model.Email +",</h1> <p>Thank you for registering. Please confirm your email by <a href='" + url + "'>Clicking here</a></p> <p>This is an auto-generated email, please do not reply.</p> <p>From Diary</p>";
                bodyBuilder.TextBody = "Hello "+model.Email;
                message.Body = bodyBuilder.ToMessageBody();
                SmtpClient client = new SmtpClient();
                client.Connect("smtp.ethereal.email", 587, false);
                client.Authenticate("rex.hyatt30@ethereal.email", "vjTxKdqWJUFwErECYU");
                client.Send(message);
                client.Disconnect(true);
                client.Dispose();


                //await _mailService.SendEmailAsync(identityUser.Email, "Confirm your email", $"<h1>Welcome to Auth Demo</h1>" +
                //     $"<p>Please confirm your email by <a href='{url}'>Clicking here</a></p>");


                return Ok(new { message = "Registration successful! A confirmation link has been sent to " + model.Email });
            }

            return BadRequest(new { message = "Fail to register" });
        }


        // /api/account/confirmemail?userid&token
        [HttpGet("ConfirmEmail")]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
                return NotFound();

            ApplicationUser user = await _userManager.FindByIdAsync(userId);
            if (user == null)

                return BadRequest("User not found");

            var decodedToken = WebEncoders.Base64UrlDecode(token);
            string normalToken = Encoding.UTF8.GetString(decodedToken);

            var result = await _userManager.ConfirmEmailAsync(user, normalToken);

            if (result.Succeeded)
                return Ok("Email confirmed successfully!");


            return BadRequest("Fail to confirm");

        }

        // /api/account/Login
        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginViewModel model)
        {

            ApplicationUser user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return BadRequest(new { message = "There is no user with that email" });

            var checkPasswordResult = await _userManager.CheckPasswordAsync(user, model.Password);

            if (!checkPasswordResult)
                return BadRequest(new { message = "Wrong password!" });

            var usersClaims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            var jwtToken = _tokenService.GenerateAccessToken(usersClaims);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddMonths(6)
            };

            //var cookieOptionsHttpOnlyFalse = new CookieOptions
            //{
            //    HttpOnly = false,
            //    Expires = DateTime.UtcNow.AddMonths(6)
            //};

            //if (user.RefreshToken == null)
            //{
            //    var refreshToken = _tokenService.GenerateRefreshToken();

            //    user.RefreshToken = refreshToken;
            //    user.RTExpirationDate = DateTime.UtcNow.AddMonths(6);
            //    await _userManager.UpdateAsync(user);

            //    Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
            //    return new ObjectResult(new
            //    {
            //        token = jwtToken,
            //        refreshToken = refreshToken
            //    });
            //}
            //if (user.RefreshToken != null && DateTime.UtcNow > user.RTExpirationDate)
            //{
            //    var refreshToken = _tokenService.GenerateRefreshToken();

            //    user.RefreshToken = refreshToken;

            //    user.RTExpirationDate = DateTime.UtcNow.AddMonths(6); ;

            //    await _userManager.UpdateAsync(user);

            //    Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
            //    return new ObjectResult(new
            //    {
            //        token = jwtToken,
            //        refreshToken = refreshToken
            //    });
            //}

            var refreshToken = _tokenService.GenerateRefreshToken();

            var r = new RefreshToken();
            r.UserId = user.Id.ToString();
            r.RToken = refreshToken;
            r.RTExpirationDate = DateTime.UtcNow.AddMonths(6);
            _context.RefreshTokens.Add(r);
            await _context.SaveChangesAsync();

            Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
            //Response.Cookies.Append("x", "", cookieOptionsHttpOnlyFalse);
            return new ObjectResult(new
            {
                token = jwtToken,
            });

        }

        [Authorize]
        // /api/account/Logout
        [HttpGet("Logout")]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (refreshToken == null)
                return BadRequest("No refresh token found");

            var r = _context.RefreshTokens.FirstOrDefault(r => r.RToken == refreshToken);
            if (r == null)
            {
                return NotFound();
            }

            _context.RefreshTokens.Remove(r);
            await _context.SaveChangesAsync();
            Response.Cookies.Delete("refreshToken");
            return Ok("Done");
        }

        // /api/account/ForgetPassword?email
        [HttpPost("ForgetPassword")]
        public async Task<IActionResult> ForgetPassword(string email)
        {
            ApplicationUser user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return BadRequest("Email is not registered");

            var PasswordResetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedPasswordResetToken = Encoding.UTF8.GetBytes(PasswordResetToken);
            var validToken = WebEncoders.Base64UrlEncode(encodedPasswordResetToken);

            //string url = $"{_configuration["AppUrl"]}/api/account/ResetPassword?email={email}&token={validToken}";
            string url = $"{_configuration["ClientUrl"]}/resetPassword?email={email}&token={validToken}";


            MimeMessage message = new MimeMessage();
            MailboxAddress from = new MailboxAddress("Diary ", "diary@diary.com");
            message.From.Add(from);
            MailboxAddress to = new MailboxAddress("User", email);
            message.To.Add(to);
            message.Subject = "Diary - Password Reset";
            BodyBuilder bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = "<h1>Hello " + email + ",</h1> <p>Please reset your password by <a href='" + url + "'>Clicking here</a></p> <p>This is an auto-generated email, please do not reply.</p> <p>From Diary</p>";
            bodyBuilder.TextBody = "Hello " + email;
            message.Body = bodyBuilder.ToMessageBody();
            SmtpClient client = new SmtpClient();
            client.Connect("smtp.ethereal.email", 587, false);
            client.Authenticate("rex.hyatt30@ethereal.email", "vjTxKdqWJUFwErECYU");
            client.Send(message);
            client.Disconnect(true);
            client.Dispose();

            //await _mailService.SendEmailAsync(email, "Reset Password", "<h1>Follow the instructions to reset your password</h1>" +
            //    $"<p>To reset your password <a href='{url}'>Click here</a></p>");

            return Ok("Reset password URL has been sent to " + email);
        }

        // /api/account/ResetPassword?email&token
        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword(string email, string token, ResetPasswordViewModel model)
        {
            ApplicationUser user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return BadRequest("Email is not registered");

            if (model.NewPassword != model.ConfirmPassword)
                return BadRequest("Confirm password did not match");

            var decodedToken = WebEncoders.Base64UrlDecode(token);
            string normalToken = Encoding.UTF8.GetString(decodedToken);

            IdentityResult result = await _userManager.ResetPasswordAsync(user, normalToken, model.NewPassword);

            if (result.Succeeded)
                return Ok("New password has been saved successfuly!");

            return BadRequest("Password reset failed");

        }

    }
}
