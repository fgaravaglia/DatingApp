using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DatingApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        #region Fields 

        private readonly UserManager<User>  _userManager;
        private readonly SignInManager<User> _signinManager;
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;

        #endregion

        public AuthController(IConfiguration config, IMapper mapper, 
                            UserManager<User> userManager, SignInManager<User> signinManager)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signinManager = signinManager ?? throw new ArgumentNullException(nameof(signinManager));
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserForRegisterDto userForRegisterDto)
        {
            var userToCreate = _mapper.Map<User>(userForRegisterDto);

            var result = await this._userManager.CreateAsync(userToCreate, userForRegisterDto.Password);

            var userToReturn = _mapper.Map<UserForDetailedDto>(userToCreate);

            if(result.Succeeded)
            {
                return CreatedAtRoute("GetUser", 
                                    new {controller = "Users", id = userToCreate.Id}, 
                                    userToReturn);
            }

            return BadRequest(result.Errors);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserForLoginDto userForLoginDto)
        {
            // get user by username then check password
            bool lockoutUserOnFailure = false;
            var user = await this._userManager.FindByNameAsync(userForLoginDto.Username);
            if(user == null)
                return BadRequest("Unable to login the user");

            var result = await this._signinManager.CheckPasswordSignInAsync(user, userForLoginDto.Password, lockoutUserOnFailure);
            if(result.Succeeded)
            {
                var appUser = await this._userManager.Users
                                            .Include(u => u.Photos)
                                            .FirstOrDefaultAsync(u => u.NormalizedUserName == userForLoginDto.Username.ToUpperInvariant());
                // prepare the dto to be returned
                var userToReturn = _mapper.Map<UserForListDto>(appUser);
                
                // gnerate token
                var newtoken = GenerateJwtToken(appUser).Result; 
                return Ok(new
                {
                    token = newtoken,
                    user
                });
            }

            // failed!
            return Unauthorized();
        }

        private async Task<string> GenerateJwtToken(User user)
        {
            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName)
            };

            // get roles
            var roles = await _userManager.GetRolesAsync(user);
            foreach(var role in roles)
            {
                claims.Add( new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.GetSection("AppSettings:Token").Value));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}