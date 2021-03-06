﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;
using JWT.Models;
using System.Security.Cryptography;
using System.IO;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Parameters;


namespace JWT.Controllers
{
    [Produces("application/json")]
    [Route("oauth")]
    #region AccountController

    public class AccountController : Controller
    {
        usuario user;
        private readonly IConfiguration _configuration;
        public string privateKey = System.IO.File.ReadAllText(@"privateKey.pem");
        //public string[] scope_inv = ["vehiculo.get", "foto.get", "estado.get", "vehiculo.put"];
        public string scope_inv = "[\"vehiculo.get\", \"foto.get\", \"estado.get\", \"vehiculo.put\"]";
        //public string[] scope_ofsub = ["afiliado.get", "pago.get", "afiliado.post", "pago.post", "afiliado.put"];
        public string scope_ofsub = "[\"afiliado.get\", \"pago.get\", \"afiliado.post\", \"pago.post\", \"afiliado.put\"]";
        public static string scope_subli = "[\"vehiculo.get\"]";
        public string inv_id = "inv01";
        public string ofsub_id = "ofsub01";
        public string subli_id = "subli01";
        public string inv_secret = "invpass";
        public string ofsub_secret = "ofsubpass";
        public string subli_secret = "sublipass";

        public AccountController(
            IConfiguration configuration)
        {
            this._configuration = configuration;
        }

        [HttpGet]
        public int valida([FromBody] valida vali)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var payload = DecodeToken(vali.token, vali.publickey);
                    return 1;
                }
                catch (Exception e)
                {
                    return -1;
                }
            }
            else
            {
                return -2;
            }
        }


        [HttpPost]
        [Route("token")]
        public IActionResult CreateUser(UserInfo model)
        {
            if (ModelState.IsValid)
            {

                var expiration = DateTime.UtcNow.AddHours(1);
                var expiration2 = DateTime.UtcNow;

                DateTime unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                long unixTimeStampInTicks = (expiration.ToUniversalTime() - unixStart).Ticks;
                var expiration3 = unixTimeStampInTicks / TimeSpan.TicksPerSecond;

                long unixTimeStampInTicks2 = (expiration2.ToUniversalTime() - unixStart).Ticks;
                var expiration4 = unixTimeStampInTicks2 / TimeSpan.TicksPerSecond;

                string id = model.client_id;
                string secret = model.client_secret;
                string type = model.grant_type;


                if (!string.Equals(type, "client_credentials") || (!string.Equals(id,inv_id) && !string.Equals(id, subli_id) && !string.Equals(id, ofsub_id))){
                    return NotFound();
                }
                else
                {
                    if (string.Equals(id, inv_id) && string.Equals(secret, inv_secret))
                    {

                        var pay = new JwtPayload
                        {
                            { "client_id", id},
                            { "scope", scope_inv},
                            { "exp", expiration3},
                            { "iat", expiration4},
                        };

                        var token = CreateToken(pay, privateKey);

                        return BuildToken(model, token);
                    }
                    else if (string.Equals(id, ofsub_id) && string.Equals(secret, ofsub_secret))
                    {

                        var pay = new JwtPayload
                        {
                            { "client_id", id},
                            { "scope", scope_ofsub},
                            { "exp", expiration3},
                            { "iat", expiration4},
                        };

                        var token = CreateToken(pay, privateKey);

                        return BuildToken(model, token);
                    }
                    else if (string.Equals(id, subli_id) && string.Equals(secret, subli_secret))
                    {

                        var pay = new JwtPayload
                        {
                            { "client_id", id},
                            { "scope", scope_subli},
                            { "exp", expiration3},
                            { "iat", expiration4},
                        };

                        var token = CreateToken(pay, privateKey);

                        return BuildToken(model, token);
                    }
                    else
                    {
                        return Unauthorized();
                    }
                }
                

                //var payload = DecodeToken(token, publicKey);  --******SIRVE PARA VALIDAR EL TOKEN POR MEDIO DE LA LLAVE PÚBLICA


            }
            else
            {
                return BadRequest(ModelState);
            }


            //return BuildToken(model);

        }


        private IActionResult BuildToken(UserInfo userInfo, object token)
        {
            user = new usuario();
            //user.ejecuta(token, userInfo);

            return Ok(new
            {
                token = token
            });

        }



        public static string CreateToken(JwtPayload payload, string privateRsaKey)
        {
            RSAParameters rsaParams;
            using (var tr = new StringReader(privateRsaKey))
            {
                var pemReader = new PemReader(tr);
                var keyPair = pemReader.ReadObject() as AsymmetricCipherKeyPair;
                if (keyPair == null)
                {
                    throw new Exception("Could not read RSA private key");
                }
                var privateRsaParams = keyPair.Private as RsaPrivateCrtKeyParameters;
                rsaParams = DotNetUtilities.ToRSAParameters(privateRsaParams);
            }
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(rsaParams);
                //Dictionary<string, object> payload = claims.ToDictionary(k => k.Type, v => (object)v.Value);

                DateTime centuryBegin = new DateTime(1970, 1, 1);
                //var exp = new TimeSpan(DateTime.Now.AddHours(1).Ticks - centuryBegin.Ticks).TotalSeconds;
                //var exp2 = new TimeSpan(DateTime.Now.Ticks - centuryBegin.Ticks).TotalSeconds;
                return Jose.JWT.Encode(payload, rsa, Jose.JwsAlgorithm.RS256);
            }
        }

        public static string DecodeToken(string token, string publicRsaKey)
        {
            RSAParameters rsaParams;

            using (var tr = new StringReader(publicRsaKey))
            {
                var pemReader = new PemReader(tr);
                var publicKeyParams = pemReader.ReadObject() as RsaKeyParameters;
                if (publicKeyParams == null)
                {
                    throw new Exception("Could not read RSA public key");
                }
                rsaParams = DotNetUtilities.ToRSAParameters(publicKeyParams);
            }
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(rsaParams);
                // This will throw if the signature is invalid
                return Jose.JWT.Decode(token, rsa, Jose.JwsAlgorithm.RS256);
            }
        }




    }

    #endregion
}
