using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Accelerant.Core.Exceptions;
using Accelerant.DataTransfer.Models;
using Accelerant.Services.Mongo;
using Accelerant.WebAPI.Models;
using Accelerant.WebAPI.Models.Call;
using Accelerant.WebAPI.Models.Return;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Accelerant.WebAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private ApplicationSettings applicationSettings;

        public UsersController(ApplicationSettings applicationSettings)
        {
            this.applicationSettings = applicationSettings;
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("add")]
        public IActionResult Add([FromBody]UserAddModel user)
        {
            var userDto = new User
            {
                Id = null,
                Name = user.Name,
                Password = user.Password
            };

            try
            {
                return Ok(new AddUserReturnModel
                {
                    Name = ServiceFactory.UsersService.Add(userDto).Name
                });
            }
            catch(UserAlreadyExistsException e)
            {
                return BadRequest(e);
            }
            catch(Exception e)
            {
                return BadRequest(e);
            }
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("authenticate")]
        public IActionResult Authenticate(UserAuthenticationModel authModel)
        {
            try
            {
                var user = ServiceFactory.UsersService.Authenticate(authModel.Name, authModel.Password);

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(applicationSettings.Authentication.Secret);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                        new Claim(ClaimTypes.Name, user.Id.ToString())
                    }),
                    Expires = DateTime.UtcNow.AddDays(7),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                // return basic user info (without password) and token to store client side
                return Ok(new
                {
                    Id = user.Id.Value,
                    Username = user.Name,
                    Token = tokenString
                });
            }
            catch(InvalidAuthenticationDataException e)
            {
                return BadRequest(e);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }
    }
}